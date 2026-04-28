using UnityEngine;

/// <summary>
/// Stores a shared reference to the minimap render texture for UI drawing.
/// </summary>
public class MiniMapCameraController : MonoBehaviour
{
    public static RenderTexture ActiveTexture { get; private set; }

    void Awake()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
            ActiveTexture = cam.targetTexture;
    }

    void OnDestroy()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null && ActiveTexture == cam.targetTexture)
            ActiveTexture = null;
    }
}
