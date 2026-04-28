using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Trinket collectible that the player must find to escape the maze.
/// Glows with an eerie light and has a pickup interaction.
/// </summary>
public class Trinket : MonoBehaviour
{
    public int trinketIndex;
    public bool isFinalTrinket;
    public Color glowColor = new Color(0.3f, 0.8f, 1f);
    public float glowIntensity = 2f;
    public float bobSpeed = 1.5f;
    public float bobHeight = 0.3f;
    public float rotateSpeed = 45f;
    public float pickupRange = 3.5f;

    private Vector3 startPos;
    private Light pointLight;
    private bool collected;
    private Transform player;

    public bool IsCollected => collected;

    void Start()
    {
        startPos = transform.position;

        // Add glow light
        pointLight = gameObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = isFinalTrinket ? new Color(1f, 0.2f, 0.8f) : glowColor;
        pointLight.intensity = glowIntensity;
        pointLight.range = 8f;

        // Tag setup
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void Update()
    {
        if (collected) return;

        // Bob and rotate animation
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

        // Pulsing glow
        float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.3f;
        pointLight.intensity = glowIntensity * pulse;

        // Check for pickup
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= pickupRange && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Collect();
        }
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;

        TrinketManager manager = FindFirstObjectByType<TrinketManager>();
        if (manager != null)
        {
            manager.OnTrinketCollected(this);
        }

        // Play collection effect then destroy
        StartCoroutine(CollectEffect());
    }

    System.Collections.IEnumerator CollectEffect()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = originalScale * (1f - t);
            pointLight.intensity = glowIntensity * (1f - t) * 3f;
            pointLight.range = 8f + t * 15f;
            yield return null;
        }

        Destroy(gameObject);
    }
}
