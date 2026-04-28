using UnityEngine;

/// <summary>
/// Procedural ambient audio system that generates eerie atmospheric sounds.
/// No audio files needed - everything is synthesized at runtime.
/// </summary>
public class AmbientAudioManager : MonoBehaviour
{
    [Header("Ambient Settings")]
    public float masterVolume = 0.3f;

    private AudioSource ambientSource;
    private AudioSource windSource;
    private AudioSource heartbeatSource;

    private PlayerHealth playerHealth;

    void Start()
    {
        // Main ambient drone
        ambientSource = CreateAudioSource("AmbientDrone", true, 0f);
        ambientSource.clip = GenerateAmbientDrone();
        ambientSource.volume = masterVolume * 0.5f;
        ambientSource.Play();

        // Wind
        windSource = CreateAudioSource("Wind", true, 0f);
        windSource.clip = GenerateWindSound();
        windSource.volume = masterVolume * 0.3f;
        windSource.Play();

        // Heartbeat
        heartbeatSource = CreateAudioSource("Heartbeat", true, 0f);
        heartbeatSource.clip = GenerateHeartbeat();
        heartbeatSource.volume = 0f;
        heartbeatSource.Play();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerHealth = playerObj.GetComponent<PlayerHealth>();
    }

    void Update()
    {
        // Dynamic heartbeat based on enemy proximity  
        if (playerHealth != null)
        {
            float targetVol = playerHealth.heartbeatIntensity * masterVolume;
            heartbeatSource.volume = Mathf.Lerp(heartbeatSource.volume, targetVol, Time.deltaTime * 3f);
            heartbeatSource.pitch = 0.8f + playerHealth.heartbeatIntensity * 0.6f;
        }
    }

    AudioSource CreateAudioSource(string name, bool loop, float spatialBlend)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        AudioSource source = obj.AddComponent<AudioSource>();
        source.loop = loop;
        source.spatialBlend = spatialBlend;
        source.playOnAwake = false;
        return source;
    }

    AudioClip GenerateAmbientDrone()
    {
        int sampleRate = 22050;
        float duration = 10f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;

            // Low drone
            float drone = Mathf.Sin(2f * Mathf.PI * 40f * t) * 0.15f;
            drone += Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.08f;
            drone += Mathf.Sin(2f * Mathf.PI * 53f * t) * 0.06f;

            // Subtle modulation
            float mod = Mathf.Sin(2f * Mathf.PI * 0.1f * t) * 0.5f + 0.5f;
            drone *= mod;

            // Very quiet high-frequency creepiness 
            float creep = Mathf.Sin(2f * Mathf.PI * 440f * t + Mathf.Sin(t * 0.5f) * 3f) * 0.02f;

            samples[i] = Mathf.Clamp(drone + creep, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("AmbientDrone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    AudioClip GenerateWindSound()
    {
        int sampleRate = 22050;
        float duration = 8f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float phase = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;

            // Filtered noise for wind
            float noise = (Random.value * 2f - 1f);

            // Very simple low-pass filter
            float envelope = Mathf.Sin(2f * Mathf.PI * 0.15f * t) * 0.5f + 0.5f;
            envelope *= Mathf.Sin(2f * Mathf.PI * 0.07f * t) * 0.3f + 0.7f;

            phase += noise * 0.1f;
            samples[i] = Mathf.Sin(phase) * envelope * 0.15f;
        }

        // Smooth the output
        for (int pass = 0; pass < 3; pass++)
        {
            for (int i = 1; i < sampleCount - 1; i++)
            {
                samples[i] = (samples[i - 1] + samples[i] + samples[i + 1]) / 3f;
            }
        }

        AudioClip clip = AudioClip.Create("Wind", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    AudioClip GenerateHeartbeat()
    {
        int sampleRate = 22050;
        float duration = 1.2f; // One heartbeat cycle
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;

            // Double thump heartbeat
            float beat1 = 0f;
            float beat2 = 0f;

            // First beat
            if (t < 0.12f)
            {
                float env = Mathf.Sin(t / 0.12f * Mathf.PI);
                beat1 = Mathf.Sin(2f * Mathf.PI * 50f * t) * env * 0.8f;
            }

            // Second beat (slightly delayed and quieter)
            float t2 = t - 0.2f;
            if (t2 > 0 && t2 < 0.1f)
            {
                float env = Mathf.Sin(t2 / 0.1f * Mathf.PI);
                beat2 = Mathf.Sin(2f * Mathf.PI * 45f * t2) * env * 0.5f;
            }

            samples[i] = Mathf.Clamp(beat1 + beat2, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Heartbeat", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
