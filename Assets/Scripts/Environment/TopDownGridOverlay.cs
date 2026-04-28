using UnityEngine;

/// <summary>
/// Draws a world-space grid overlay on the top-down camera using GL lines.
/// Attach to the MainCamera for a clean grid view of the maze.
/// </summary>
[RequireComponent(typeof(Camera))]
public class TopDownGridOverlay : MonoBehaviour
{
    [Header("Grid Settings")]
    public float cellSize = 4f;
    public int gridCountX = 32;
    public int gridCountZ = 32;
    public Color gridColor = new Color(0.3f, 0.35f, 0.5f, 0.4f);
    public Color majorGridColor = new Color(0.2f, 0.25f, 0.45f, 0.7f);
    public int majorGridEvery = 5;
    public float gridY = 4.1f; // Just above walls so grid is visible on top

    private Material lineMaterial;

    void Awake()
    {
        // Unlit line material for GL drawing
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
        lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
    }

    void OnPostRender()
    {
        if (lineMaterial == null) return;

        lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);
        GL.Begin(GL.LINES);

        float totalW = gridCountX * cellSize;
        float totalH = gridCountZ * cellSize;

        // Draw vertical lines (along Z axis)
        for (int x = 0; x <= gridCountX; x++)
        {
            bool isMajor = (x % majorGridEvery == 0);
            GL.Color(isMajor ? majorGridColor : gridColor);
            float xPos = x * cellSize;
            GL.Vertex3(xPos, gridY, 0);
            GL.Vertex3(xPos, gridY, totalH);
        }

        // Draw horizontal lines (along X axis)
        for (int z = 0; z <= gridCountZ; z++)
        {
            bool isMajor = (z % majorGridEvery == 0);
            GL.Color(isMajor ? majorGridColor : gridColor);
            float zPos = z * cellSize;
            GL.Vertex3(0, gridY, zPos);
            GL.Vertex3(totalW, gridY, zPos);
        }

        GL.End();
        GL.PopMatrix();
    }

    void OnDestroy()
    {
        if (lineMaterial != null)
            DestroyImmediate(lineMaterial);
    }
}
