using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility to automatically set up the Echo Maze scene.
/// Run from menu: Echo Maze > Setup Scene
/// Also auto-runs when the project is first opened.
/// </summary>
[InitializeOnLoad]
public class EchoMazeSceneSetup : Editor
{
    static EchoMazeSceneSetup()
    {
        EditorApplication.delayCall += CheckAndSetupScene;
    }

    static void CheckAndSetupScene()
    {
        // Check if bootstrap already exists in scene
        if (Object.FindFirstObjectByType<EchoMazeBootstrap>() != null)
            return;

        // Check if we're in a new/empty scene
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.rootCount <= 1) // Only default camera or empty
        {
            SetupScene();
        }
    }

    [MenuItem("Echo Maze/Setup Scene")]
    public static void SetupSceneMenu()
    {
        SetupScene();
        Debug.Log("Echo Maze scene setup complete! Press Play to start the game.");
    }

    [MenuItem("Echo Maze/Create New Maze Scene")]
    public static void CreateNewScene()
    {
        // Create new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetupScene();
        
        // Save scene
        string scenePath = "Assets/Scenes/EchoMaze.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        
        // Add to build settings
        EditorBuildSettingsScene[] scenes = { new EditorBuildSettingsScene(scenePath, true) };
        EditorBuildSettings.scenes = scenes;
        
        Debug.Log("Echo Maze scene created and saved! Press Play to start the game.");
    }

    static void SetupScene()
    {
        // Remove existing main camera if any
        Camera existingCam = Camera.main;
        if (existingCam != null)
        {
            DestroyImmediate(existingCam.gameObject);
        }

        // Remove any existing directional light
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
                DestroyImmediate(light.gameObject);
        }

        // Remove existing bootstrap if any
        EchoMazeBootstrap existingBootstrap = Object.FindFirstObjectByType<EchoMazeBootstrap>();
        if (existingBootstrap != null)
        {
            DestroyImmediate(existingBootstrap.gameObject);
        }

        // Create bootstrap object
        GameObject bootstrap = new GameObject("EchoMazeBootstrap");
        bootstrap.AddComponent<EchoMazeBootstrap>();

        // Setup render settings for light atmosphere
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.032f;
        RenderSettings.fogColor = new Color(0.05f, 0.07f, 0.1f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.1f, 0.12f, 0.15f);
        RenderSettings.skybox = null;
        RenderSettings.reflectionIntensity = 0.08f;

        // Ensure Player tag exists
        EnsureTagExists("Player");

        // Mark scene dirty
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        // Setup build settings
        string scenePath = SceneManager.GetActiveScene().path;
        if (!string.IsNullOrEmpty(scenePath))
        {
            EditorBuildSettingsScene[] scenes = { new EditorBuildSettingsScene(scenePath, true) };
            EditorBuildSettings.scenes = scenes;
        }
    }

    static void EnsureTagExists(string tag)
    {
        // Tags are managed in TagManager - Player tag exists by default in Unity
        // No action needed for "Player" as it's a built-in tag
    }

    [MenuItem("Echo Maze/Quick Play Info")]
    public static void ShowPlayInfo()
    {
        EditorUtility.DisplayDialog("Echo Maze - Controls",
            "CONTROLS:\n" +
            "WASD - Move\n" +
            "Mouse - Look Around\n" +
            "Shift - Sprint\n" +
            "C / Ctrl - Crouch (Stealth)\n" +
            "F - Toggle Flashlight\n" +
            "E - Collect Trinket\n" +
            "Space - Jump\n" +
            "ESC - Pause\n\n" +
            "OBJECTIVE:\n" +
            "Collect 14 trinkets to win immediately.\n" +
            "Extra trinkets can appear along maze edges.\n" +
            "The Guiding Eye helps locate most trinkets.\n" +
            "Avoid demonic entities. They can hear and see you.\n" +
            "Crouching reduces detection. Sprinting increases it.",
            "Got it!");
    }
}
