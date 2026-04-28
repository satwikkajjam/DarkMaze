using UnityEngine;

/// <summary>
/// Applies gradual damage while the player stays inside a pothole trigger.
/// </summary>
public class PotholeHazard : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.LoseHeart();
    }
}
