using UnityEngine;

/// <summary>
/// Head bob effect for the camera while walking/running.
/// Adds immersion and sense of movement.
/// </summary>
public class HeadBob : MonoBehaviour
{
    public float walkBobSpeed = 8f;
    public float walkBobAmount = 0.03f;
    public float sprintBobSpeed = 12f;
    public float sprintBobAmount = 0.06f;
    public float crouchBobSpeed = 5f;
    public float crouchBobAmount = 0.015f;
    public float smoothing = 10f;

    private float defaultYPos;
    private float timer;
    private PlayerController playerController;

    void Start()
    {
        defaultYPos = transform.localPosition.y;

        Transform parent = transform.parent;
        while (parent != null)
        {
            playerController = parent.GetComponent<PlayerController>();
            if (playerController != null) break;
            parent = parent.parent;
        }
    }

    void Update()
    {
        if (playerController == null) return;

        float speed = new Vector3(playerController.Velocity.x, 0, playerController.Velocity.z).magnitude;

        if (speed > 0.1f)
        {
            float bobSpeed, bobAmount;

            if (playerController.IsCrouching)
            {
                bobSpeed = crouchBobSpeed;
                bobAmount = crouchBobAmount;
            }
            else if (playerController.IsSprinting)
            {
                bobSpeed = sprintBobSpeed;
                bobAmount = sprintBobAmount;
            }
            else
            {
                bobSpeed = walkBobSpeed;
                bobAmount = walkBobAmount;
            }

            timer += Time.deltaTime * bobSpeed;
            float newY = defaultYPos + Mathf.Sin(timer) * bobAmount;
            float newX = Mathf.Cos(timer * 0.5f) * bobAmount * 0.5f;

            Vector3 pos = transform.localPosition;
            pos.y = Mathf.Lerp(pos.y, newY, smoothing * Time.deltaTime);
            pos.x = Mathf.Lerp(pos.x, newX, smoothing * Time.deltaTime);
            transform.localPosition = pos;
        }
        else
        {
            timer = 0f;
            Vector3 pos = transform.localPosition;
            pos.y = Mathf.Lerp(pos.y, defaultYPos, smoothing * Time.deltaTime);
            pos.x = Mathf.Lerp(pos.x, 0f, smoothing * Time.deltaTime);
            transform.localPosition = pos;
        }
    }
}
