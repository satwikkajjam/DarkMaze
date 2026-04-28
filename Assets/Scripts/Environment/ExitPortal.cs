using UnityEngine;

/// <summary>
/// Exit portal that the player must reach after collecting all 14 trinkets.
/// Shows visual feedback about being locked/unlocked.
/// </summary>
public class ExitPortal : MonoBehaviour
{
    private Light portalLight;
    private Renderer portalRenderer;
    private bool isUnlocked;
    private TrinketManager trinketManager;

    public Color lockedColor = new Color(0.5f, 0.1f, 0.1f);
    public Color unlockedColor = new Color(0.1f, 1f, 0.3f);

    void Start()
    {
        portalLight = GetComponent<Light>();
        portalRenderer = GetComponent<Renderer>();
        trinketManager = FindFirstObjectByType<TrinketManager>();
        UpdateVisuals();
    }

    void Update()
    {
        if (trinketManager != null)
        {
            isUnlocked = trinketManager.AllCollected;
        }

        UpdateVisuals();

        // Pulsing effect
        if (portalLight != null)
        {
            float pulse = 2f + Mathf.Sin(Time.time * (isUnlocked ? 3f : 1f)) * 1f;
            portalLight.intensity = pulse;
        }
    }

    void UpdateVisuals()
    {
        Color targetColor = isUnlocked ? unlockedColor : lockedColor;

        if (portalRenderer != null)
        {
            portalRenderer.material.color = targetColor;
            portalRenderer.material.SetColor("_EmissionColor", targetColor * 2f);
        }

        if (portalLight != null)
        {
            portalLight.color = targetColor;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (isUnlocked)
        {
            GameManager.Instance?.OnPlayerEscaped();
        }
        else
        {
            GameManager.Instance?.ShowMessage("The exit is sealed. Collect 14 trinkets to win!");
        }
    }
}
