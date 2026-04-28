using UnityEngine;

/// <summary>
/// Main scene bootstrapper that sets up EVERYTHING at runtime.
/// This is the single entry point - it creates the player, maze, enemies,
/// trinkets, UI, atmosphere, and all game systems.
/// Attach this to an empty GameObject in the scene.
/// </summary>
[DefaultExecutionOrder(-100)]
public class EchoMazeBootstrap : MonoBehaviour
{
    Material CreateLitMaterial(Color color, Color emissionColor)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");

        Material material = new Material(shader);
        material.color = color;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emissionColor);
        return material;
    }

    void ApplyRendererStyle(Renderer renderer, Material material)
    {
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    void CreateCharacterPart(Transform parent, PrimitiveType primitiveType, string partName,
        Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = partName;
        part.transform.SetParent(parent);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        ApplyRendererStyle(part.GetComponent<Renderer>(), material);
    }

    void CreateStylizedPlayerBody(GameObject player)
    {
        Material coatMaterial = CreateLitMaterial(new Color(0.18f, 0.22f, 0.45f), new Color(0.02f, 0.03f, 0.08f));
        Material skinMaterial = CreateLitMaterial(new Color(0.95f, 0.82f, 0.72f), new Color(0.03f, 0.02f, 0.02f));
        Material hairMaterial = CreateLitMaterial(new Color(0.82f, 0.84f, 0.9f), new Color(0.04f, 0.04f, 0.05f));
        Material accentMaterial = CreateLitMaterial(new Color(0.9f, 0.95f, 1f), new Color(0.05f, 0.06f, 0.08f));
        Material bladeMaterial = CreateLitMaterial(new Color(0.82f, 0.85f, 0.9f), new Color(0.03f, 0.03f, 0.04f));

        GameObject visualRoot = new GameObject("PlayerVisual");
        visualRoot.transform.SetParent(player.transform);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;

        CreateCharacterPart(visualRoot.transform, PrimitiveType.Capsule, "Torso",
            new Vector3(0f, 0.95f, 0f), new Vector3(0.55f, 0.65f, 0.42f), coatMaterial);
        CreateCharacterPart(visualRoot.transform, PrimitiveType.Sphere, "Head",
            new Vector3(0f, 1.95f, 0.04f), new Vector3(0.42f, 0.42f, 0.42f), skinMaterial);
        CreateCharacterPart(visualRoot.transform, PrimitiveType.Sphere, "Hair",
            new Vector3(0f, 2.13f, -0.02f), new Vector3(0.48f, 0.28f, 0.48f), hairMaterial);
        CreateCharacterPart(visualRoot.transform, PrimitiveType.Cube, "HairBack",
            new Vector3(0f, 1.92f, -0.18f), new Vector3(0.44f, 0.42f, 0.2f), hairMaterial);
        CreateCharacterPart(visualRoot.transform, PrimitiveType.Cube, "ArmLeft",
            new Vector3(-0.42f, 1.02f, 0f), new Vector3(0.16f, 0.55f, 0.16f), accentMaterial);
        CreateCharacterPart(visualRoot.transform, PrimitiveType.Cube, "ArmRight",
            new Vector3(0.44f, 0.96f, 0.04f), new Vector3(0.16f, 0.65f, 0.16f), accentMaterial);
        CreateCharacterPart(visualRoot.transform, PrimitiveType.Cube, "LegLeft",
            new Vector3(-0.14f, 0.35f, 0f), new Vector3(0.16f, 0.65f, 0.18f), coatMaterial);
        CreateCharacterPart(visualRoot.transform, PrimitiveType.Cube, "LegRight",
            new Vector3(0.14f, 0.35f, 0f), new Vector3(0.16f, 0.65f, 0.18f), coatMaterial);
        CreateCharacterPart(visualRoot.transform, PrimitiveType.Cube, "Scarf",
            new Vector3(0f, 1.48f, 0.18f), new Vector3(0.48f, 0.12f, 0.2f), accentMaterial);
        CreateCharacterPart(visualRoot.transform, PrimitiveType.Cube, "Sword",
            new Vector3(0.62f, 0.88f, 0.18f), new Vector3(0.1f, 0.12f, 0.9f), bladeMaterial);
    }

    void Awake()
    {
        SetupLayers();
        SetupRenderSettings();
        CreateGameManager();
        CreatePlayer();
        CreateMaze();
        CreateMiniMap();
        CreateGuidingEye();
        CreateEnvironmentControllers();
        CreateUI();
        CreateAmbientAudio();
    }

    void SetupLayers()
    {
        // Layers are set up in project settings, but we work with defaults here
    }

    void SetupRenderSettings()
    {
        // Remove default directional light
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in allLights)
        {
            if (l.type == LightType.Directional)
                DestroyImmediate(l.gameObject);
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.032f;
        RenderSettings.fogColor = new Color(0.05f, 0.07f, 0.1f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.1f, 0.12f, 0.15f);
        RenderSettings.reflectionIntensity = 0.08f;
        RenderSettings.skybox = null;
        RenderSettings.subtractiveShadowColor = new Color(0.04f, 0.05f, 0.07f);
    }

    void CreateGameManager()
    {
        GameObject gmObj = new GameObject("GameManager");
        gmObj.AddComponent<GameManager>();
    }

    void CreatePlayer()
    {
        // Player root
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Default");
        player.transform.position = new Vector3(2f, 1f, 2f);

        // Character Controller
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = Vector3.up;

        CreateStylizedPlayerBody(player);

        // Remove ALL existing cameras and audio listeners first
        Camera[] allCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera c in allCams)
            DestroyImmediate(c.gameObject);
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        foreach (AudioListener l in listeners)
            DestroyImmediate(l.gameObject);

        // Normal chase camera
        GameObject camObj = new GameObject("MainCamera");
        camObj.tag = "MainCamera";
        camObj.transform.position = player.transform.position + new Vector3(0f, 6.4f, -6.5f);
        camObj.transform.rotation = Quaternion.Euler(36f, 0f, 0f);

        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = false;
        cam.fieldOfView = 72f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 120f;
        cam.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.depth = 0;
        cam.allowMSAA = true;

        AngledFollowCamera followCamera = camObj.AddComponent<AngledFollowCamera>();
        followCamera.target = player.transform;
        followCamera.offset = new Vector3(0f, 6.4f, -6.5f);
        followCamera.lookOffset = new Vector3(0f, 1.1f, 4.2f);
        followCamera.followSmoothing = 8f;

        camObj.AddComponent<AudioListener>();

        GameObject headLampObj = new GameObject("HeadLamp");
        headLampObj.transform.SetParent(player.transform);
        headLampObj.transform.localPosition = new Vector3(0f, 1.35f, 0.2f);
        headLampObj.transform.localRotation = Quaternion.identity;
        Light headLamp = headLampObj.AddComponent<Light>();
        headLamp.type = LightType.Spot;
        headLamp.spotAngle = 85f;
        headLamp.range = 14f;
        headLamp.intensity = 1.2f;
        headLamp.color = new Color(1f, 0.97f, 0.9f);
        headLamp.shadows = LightShadows.Soft;

        PlayerFlashlight flashlight = player.AddComponent<PlayerFlashlight>();
        flashlight.batteryLife = 100f;
        flashlight.drainRate = 3f;
        flashlight.rechargeRate = 1.5f;

        // Player scripts
        PlayerController pc = player.AddComponent<PlayerController>();
        pc.useMouseLook = false;
        pc.cameraHolder = null;
        pc.movementReference = camObj.transform;

        Transform visualRoot = player.transform.Find("PlayerVisual");
        if (visualRoot != null)
        {
            visualRoot.gameObject.AddComponent<PlayerRunAnimation>();
        }

        player.AddComponent<PlayerHealth>();

        GameObject lightingObj = new GameObject("WorldLightingModeController");
        WorldLightingModeController lightingController = lightingObj.AddComponent<WorldLightingModeController>();
        lightingController.playerHeadLamp = headLamp;
        lightingController.targetCamera = cam;
    }

    void CreateMaze()
    {
        GameObject mazeObj = new GameObject("MazeSystem");
        MazeGenerator maze = mazeObj.AddComponent<MazeGenerator>();
        maze.GenerateMaze();

        // Move player to spawn point
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = maze.PlayerSpawnPoint;
        }
    }

    void CreateMiniMap()
    {
        MazeGenerator maze = FindFirstObjectByType<MazeGenerator>();
        if (maze == null) return;

        float mazeWidthWorld = maze.mazeWidth * maze.cellSize;
        float mazeHeightWorld = maze.mazeHeight * maze.cellSize;
        float mazeCenterX = mazeWidthWorld * 0.5f;
        float mazeCenterZ = mazeHeightWorld * 0.5f;

        GameObject mapCamObj = new GameObject("MiniMapCamera");
        mapCamObj.transform.position = new Vector3(mazeCenterX, 70f, mazeCenterZ);
        mapCamObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Camera mapCam = mapCamObj.AddComponent<Camera>();
        mapCam.orthographic = true;
        mapCam.orthographicSize = Mathf.Max(mazeWidthWorld, mazeHeightWorld) * 0.55f;
        mapCam.nearClipPlane = 0.1f;
        mapCam.farClipPlane = 200f;
        mapCam.clearFlags = CameraClearFlags.SolidColor;
        mapCam.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);
        mapCam.depth = -5f;
        mapCam.allowMSAA = false;
        mapCam.allowHDR = false;

        RenderTexture miniMapTexture = new RenderTexture(512, 512, 16);
        miniMapTexture.name = "MiniMapRT";
        mapCam.targetTexture = miniMapTexture;

        mapCamObj.AddComponent<MiniMapCameraController>();
    }

    void CreateGuidingEye()
    {
        GameObject eyeObj = new GameObject("GuidingEye");
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            eyeObj.transform.position = player.transform.position + Vector3.up * 2.5f + player.transform.forward * 3f;
        }
        eyeObj.AddComponent<GuidingEye>();
    }

    void CreateEnvironmentControllers()
    {
        // Sky Color Controller
        GameObject skyController = new GameObject("SkyColorController");
        skyController.AddComponent<SkyColorController>();

        // Fog Particles
        GameObject fogObj = new GameObject("FogParticles");
        fogObj.AddComponent<MazeFogEffect>();
    }

    void CreateUI()
    {
        GameObject uiObj = new GameObject("GameUI");
        uiObj.AddComponent<GameUI>();
    }

    void CreateAmbientAudio()
    {
        GameObject audioObj = new GameObject("AmbientAudio");
        audioObj.AddComponent<AmbientAudioManager>();
    }
}
