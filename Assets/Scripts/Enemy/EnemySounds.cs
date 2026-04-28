using UnityEngine;

/// <summary>
/// Makes the enemy produce ambient horror sounds - breathing, growling, footsteps.
/// Sound intensity scales with enemy state.
/// </summary>
public class EnemySounds : MonoBehaviour
{
    private AudioSource audioSource;
    private EnemyAI enemyAI;

    [Header("Sound Settings")]
    public float idleVolume = 0.1f;
    public float chaseVolume = 0.5f;
    public float breathingInterval = 3f;

    private float breathTimer;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 25f;
        audioSource.minDistance = 2f;
        audioSource.loop = false;

        enemyAI = GetComponent<EnemyAI>();
    }

    void Update()
    {
        if (enemyAI == null) return;

        breathTimer += Time.deltaTime;

        float targetVolume = enemyAI.CurrentState == EnemyAI.EnemyState.Chase ? chaseVolume : idleVolume;
        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.deltaTime * 2f);

        if (breathTimer >= breathingInterval)
        {
            breathTimer = 0f;
            // Generates procedural breathing sound
            PlayProceduralBreath();
        }
    }

    void PlayProceduralBreath()
    {
        // Create a short procedural breathing clip
        int sampleRate = 22050;
        float duration = 0.8f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        AudioClip clip = AudioClip.Create("breath", sampleCount, 1, sampleRate, false);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float envelope = Mathf.Sin(t * Mathf.PI);
            float noise = (Random.value * 2f - 1f) * 0.3f;
            float tone = Mathf.Sin(2f * Mathf.PI * 80f * t) * 0.1f;
            samples[i] = (noise + tone) * envelope;
        }

        clip.SetData(samples, 0);
        audioSource.PlayOneShot(clip);
    }
}
