using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Toggles the world between dark and light visibility modes.
/// </summary>
public class WorldLightingModeController : MonoBehaviour
{
    public enum LightingMode
    {
        Dark,
        Light
    }

    public static LightingMode CurrentMode { get; private set; } = LightingMode.Dark;
    public static bool IsLightMode => CurrentMode == LightingMode.Light;

    [Header("References")]
    public Light playerHeadLamp;
    public Camera targetCamera;

    [Header("Dark Mode")]
    public Color darkFogColor = new Color(0.05f, 0.07f, 0.1f);
    public Color darkAmbientColor = new Color(0.1f, 0.12f, 0.15f);
    public Color darkBackgroundColor = new Color(0.03f, 0.04f, 0.07f);
    public float darkFogDensity = 0.032f;

    [Header("Light Mode")]
    public Color lightFogColor = new Color(0.24f, 0.28f, 0.34f);
    public Color lightAmbientColor = new Color(0.62f, 0.66f, 0.72f);
    public Color lightBackgroundColor = new Color(0.84f, 0.87f, 0.92f);
    public float lightFogDensity = 0.008f;

    void Start()
    {
        ApplyMode(CurrentMode);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            ToggleMode();
        }
    }

    void ToggleMode()
    {
        CurrentMode = IsLightMode ? LightingMode.Dark : LightingMode.Light;
        ApplyMode(CurrentMode);
    }

    void ApplyMode(LightingMode mode)
    {
        bool lightMode = mode == LightingMode.Light;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = lightMode ? lightFogDensity : darkFogDensity;
        RenderSettings.fogColor = lightMode ? lightFogColor : darkFogColor;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = lightMode ? lightAmbientColor : darkAmbientColor;
        RenderSettings.reflectionIntensity = lightMode ? 0.22f : 0.08f;
        RenderSettings.skybox = null;
        RenderSettings.subtractiveShadowColor = lightMode ? new Color(0.18f, 0.2f, 0.24f) : new Color(0.04f, 0.05f, 0.07f);

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            targetCamera.backgroundColor = lightMode ? lightBackgroundColor : darkBackgroundColor;
        }

        if (playerHeadLamp != null)
        {
            playerHeadLamp.enabled = true;
            playerHeadLamp.intensity = lightMode ? 0.9f : 1.25f;
            playerHeadLamp.range = lightMode ? 14f : 16f;
            playerHeadLamp.spotAngle = lightMode ? 60f : 70f;
        }
    }
}
