using UnityEngine;

/// <summary>
/// Places dim atmospheric lights throughout the maze corridors.
/// Creates pools of light that flicker eerily.
/// </summary>
public class MazeLighting : MonoBehaviour
{
    public float flickerSpeed = 3f;
    public float flickerAmount = 0.1f;
    public float baseIntensity = 1.2f;
    public Color lightColor = new Color(1.0f, 0.98f, 0.95f);

    private Light pointLight;
    private float flickerOffset;

    void Start()
    {
        pointLight = GetComponent<Light>();
        if (pointLight == null)
        {
            pointLight = gameObject.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = lightColor;
            pointLight.intensity = baseIntensity;
            pointLight.range = 8f;
            pointLight.shadows = LightShadows.None;
        }

        flickerOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // No flickering for top-down view - stable lighting only
    }

    /// <summary>
    /// Static helper to place lights throughout the maze
    /// </summary>
    public static void PlaceLightsInMaze(Transform mazeParent, int mazeWidth, int mazeHeight, float cellSize)
    {
        // Place a light every few cells
        int spacing = 3;
        for (int x = 0; x < mazeWidth; x += spacing)
        {
            for (int y = 0; y < mazeHeight; y += spacing)
            {
                // Add some randomness to placement
                float offsetX = Random.Range(-cellSize * 0.3f, cellSize * 0.3f);
                float offsetZ = Random.Range(-cellSize * 0.3f, cellSize * 0.3f);

                Vector3 pos = new Vector3(
                    x * cellSize + cellSize / 2f + offsetX,
                    3f,
                    y * cellSize + cellSize / 2f + offsetZ
                );

                GameObject lightObj = new GameObject($"MazeLight_{x}_{y}");
                lightObj.transform.SetParent(mazeParent);
                lightObj.transform.position = pos;

                MazeLighting lighting = lightObj.AddComponent<MazeLighting>();

                // Vary the color slightly
                float hueShift = Random.Range(-0.05f, 0.05f);
                lighting.lightColor = new Color(
                    1.0f + hueShift * 0.1f,
                    0.98f - hueShift * 0.05f,
                    0.95f + hueShift * 0.05f
                );
                lighting.baseIntensity = Random.Range(0.8f, 1.5f);
            }
        }
    }
}
