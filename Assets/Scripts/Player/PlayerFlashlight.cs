using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Creates a flashlight that the player can toggle on/off.
/// Enemies can detect the flashlight beam.
/// </summary>
public class PlayerFlashlight : MonoBehaviour
{
    public Light flashlight;
    public float batteryLife = 100f;
    public float drainRate = 5f;
    public float rechargeRate = 2f;
    public bool isOn;

    public float BatteryPercent => batteryLife / 100f;

    void Start()
    {
        if (flashlight == null)
        {
            GameObject lightObj = new GameObject("Flashlight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.forward * 0.3f;
            lightObj.transform.localRotation = Quaternion.identity;
            flashlight = lightObj.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.spotAngle = 45f;
            flashlight.range = 20f;
            flashlight.intensity = 2f;
            flashlight.color = new Color(1f, 0.95f, 0.8f);
            flashlight.shadows = LightShadows.Soft;
        }
        flashlight.enabled = false;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            ToggleFlashlight();
        }

        if (isOn)
        {
            batteryLife -= drainRate * Time.deltaTime;
            if (batteryLife <= 0f)
            {
                batteryLife = 0f;
                ToggleFlashlight();
            }
        }
        else
        {
            batteryLife = Mathf.Min(100f, batteryLife + rechargeRate * Time.deltaTime);
        }
    }

    void ToggleFlashlight()
    {
        if (batteryLife <= 0f && !isOn) return;
        isOn = !isOn;
        flashlight.enabled = isOn;
    }
}
