using UnityEngine;

/// <summary>
/// Handles player health, death, and heartbeat effect when enemies are near.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public float damageOnCatch = 100f;

    [Header("Hearts")]
    public int maxHearts = 3;
    public int currentHearts;

    [Header("Heartbeat Effect")]
    public float heartbeatDistance = 15f;
    public float heartbeatIntensity;

    private bool isDead;

    public bool IsDead => isDead;
    public float HealthPercent => currentHealth / maxHealth;

    public System.Action OnPlayerDeath;
    public System.Action<float> OnDamageTaken;

    void Start()
    {
        currentHealth = maxHealth;
        currentHearts = maxHearts;
    }

    void Update()
    {
        if (isDead) return;
        UpdateHeartbeat();
    }

    void UpdateHeartbeat()
    {
        float closestEnemy = float.MaxValue;
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < closestEnemy) closestEnemy = dist;
        }

        heartbeatIntensity = closestEnemy < heartbeatDistance
            ? 1f - (closestEnemy / heartbeatDistance)
            : 0f;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        OnDamageTaken?.Invoke(amount);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        OnPlayerDeath?.Invoke();
        GameManager.Instance?.OnPlayerDied();
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        currentHearts = maxHearts;
        isDead = false;
    }

    public bool LoseHeart()
    {
        if (isDead || currentHearts <= 0) return false;

        currentHearts = Mathf.Max(0, currentHearts - 1);
        OnDamageTaken?.Invoke(maxHealth / Mathf.Max(1, maxHearts));

        if (currentHearts <= 0)
        {
            Die();
            return true;
        }

        return false;
    }
}
