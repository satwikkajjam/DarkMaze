using UnityEngine;

/// <summary>
/// Keeps the camera at a fixed angled offset from the target for an isometric-like gameplay view.
/// </summary>
public class AngledFollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 16f, -4f);
    public Vector3 lookOffset = new Vector3(0f, 0.8f, 4.5f);
    public float followSmoothing = 10f;

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmoothing * Time.deltaTime);
        transform.LookAt(target.position + lookOffset);
    }
}