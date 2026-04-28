using UnityEngine;

/// <summary>
/// Controls the sky/fog color change as the player approaches the final (14th) trinket.
/// The environment itself signals the location of the hidden trinket.
/// Colors shift from dark ominous tones to a colder glow near the trinket.
/// </summary>
public class SkyColorController : MonoBehaviour
{
    [Header("Sky Color Settings")]
    public Color darkDefaultSkyColor = new Color(0.03f, 0.04f, 0.07f);
    public Color darkNearFinalTrinketColor = new Color(0.07f, 0.13f, 0.2f);
    public Color darkVeryCloseTrinketColor = new Color(0.12f, 0.22f, 0.34f);
    public Color lightDefaultSkyColor = new Color(0.84f, 0.87f, 0.92f);
    public Color lightNearFinalTrinketColor = new Color(0.64f, 0.74f, 0.88f);
    public Color lightVeryCloseTrinketColor = new Color(0.52f, 0.66f, 0.84f);

    [Header("Fog Settings")]
    public Color darkDefaultFogColor = new Color(0.05f, 0.07f, 0.1f);
    public Color darkNearFinalFogColor = new Color(0.1f, 0.16f, 0.23f);
    public float darkDefaultFogDensity = 0.032f;
    public float darkNearFinalFogDensity = 0.022f;
    public Color lightDefaultFogColor = new Color(0.82f, 0.85f, 0.9f);
    public Color lightNearFinalFogColor = new Color(0.72f, 0.79f, 0.88f);
    public float lightDefaultFogDensity = 0.008f;
    public float lightNearFinalFogDensity = 0.006f;

    [Header("Ambient Light")]
    public Color darkDefaultAmbientColor = new Color(0.1f, 0.12f, 0.15f);
    public Color darkNearFinalAmbientColor = new Color(0.22f, 0.28f, 0.34f);
    public Color lightDefaultAmbientColor = new Color(0.6f, 0.62f, 0.67f);
    public Color lightNearFinalAmbientColor = new Color(0.72f, 0.75f, 0.8f);

    [Header("Detection Range")]
    public float maxDetectionRange = 40f;
    public float closeRange = 15f;
    public float transitionSpeed = 1.5f;

    private TrinketManager trinketManager;
    private Transform player;
    private Camera mainCam;
    private float currentProximity;

    void Start()
    {
        trinketManager = FindFirstObjectByType<TrinketManager>();
        mainCam = Camera.main;

        // Set initial sky/fog
        RenderSettings.fogMode = FogMode.Exponential;
        ApplyBaseAtmosphere(0f);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void Update()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            return;
        }

        if (trinketManager == null) return;

        // Only activate after collecting 13 trinkets (all but the final one)
        bool showGuidance = trinketManager.CollectedCount >= trinketManager.TotalTrinkets - 1;

        float targetProximity = 0f;

        if (showGuidance && !trinketManager.AllCollected)
        {
            float distToFinal = trinketManager.GetDistanceToFinalTrinket(player.position);

            if (distToFinal <= maxDetectionRange)
            {
                targetProximity = 1f - Mathf.Clamp01(distToFinal / maxDetectionRange);

                // Extra intensity when very close
                if (distToFinal <= closeRange)
                {
                    float closeProximity = 1f - Mathf.Clamp01(distToFinal / closeRange);
                    targetProximity = Mathf.Lerp(targetProximity, 1f, closeProximity);
                }
            }
        }

        // Smooth transition
        currentProximity = Mathf.Lerp(currentProximity, targetProximity, transitionSpeed * Time.deltaTime);

        Color skyColor = Color.Lerp(GetDefaultSkyColor(),
            currentProximity > 0.7f ? GetVeryCloseSkyColor() : GetNearFinalSkyColor(),
            currentProximity);
        if (mainCam != null)
            mainCam.backgroundColor = skyColor;

        // Apply fog
        RenderSettings.fogColor = Color.Lerp(GetDefaultFogColor(), GetNearFinalFogColor(), currentProximity);
        RenderSettings.fogDensity = Mathf.Lerp(GetDefaultFogDensity(), GetNearFinalFogDensity(), currentProximity);
        RenderSettings.fog = true;

        // Apply ambient
        RenderSettings.ambientLight = Color.Lerp(GetDefaultAmbientColor(), GetNearFinalAmbientColor(), currentProximity);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
    }

    void ApplyBaseAtmosphere(float proximity)
    {
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = Mathf.Lerp(GetDefaultFogDensity(), GetNearFinalFogDensity(), proximity);
        RenderSettings.fogColor = Color.Lerp(GetDefaultFogColor(), GetNearFinalFogColor(), proximity);
        RenderSettings.ambientLight = Color.Lerp(GetDefaultAmbientColor(), GetNearFinalAmbientColor(), proximity);

        if (mainCam != null)
            mainCam.backgroundColor = Color.Lerp(GetDefaultSkyColor(), GetNearFinalSkyColor(), proximity);
    }

    bool IsLightMode => WorldLightingModeController.IsLightMode;

    Color GetDefaultSkyColor() => IsLightMode ? lightDefaultSkyColor : darkDefaultSkyColor;
    Color GetNearFinalSkyColor() => IsLightMode ? lightNearFinalTrinketColor : darkNearFinalTrinketColor;
    Color GetVeryCloseSkyColor() => IsLightMode ? lightVeryCloseTrinketColor : darkVeryCloseTrinketColor;
    Color GetDefaultFogColor() => IsLightMode ? lightDefaultFogColor : darkDefaultFogColor;
    Color GetNearFinalFogColor() => IsLightMode ? lightNearFinalFogColor : darkNearFinalFogColor;
    float GetDefaultFogDensity() => IsLightMode ? lightDefaultFogDensity : darkDefaultFogDensity;
    float GetNearFinalFogDensity() => IsLightMode ? lightNearFinalFogDensity : darkNearFinalFogDensity;
    Color GetDefaultAmbientColor() => IsLightMode ? lightDefaultAmbientColor : darkDefaultAmbientColor;
    Color GetNearFinalAmbientColor() => IsLightMode ? lightNearFinalAmbientColor : darkNearFinalAmbientColor;
}
