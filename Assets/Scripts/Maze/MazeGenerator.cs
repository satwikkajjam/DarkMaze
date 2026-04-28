using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Procedural maze generator that creates an infinite-feeling maze using
/// recursive backtracking algorithm. Generates walls, floors, ceilings, and
/// places trinkets and enemies throughout the maze.
/// </summary>
public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Dimensions")]
    public int mazeWidth = 30;
    public int mazeHeight = 30;
    public float cellSize = 4f;
    public float wallHeight = 4f;
    public float wallThickness = 0.3f;

    [Header("Content")]
    public int enemyCount = 20;
    public int trinketCount = 45;
    public int requiredTrinketsToWin = 14;

    [Header("Hazards")]
    public int potholeCount = 28;

    [Header("Materials")]
    public Material wallMaterial;
    public Material floorMaterial;
    public Material ceilingMaterial;
    public Material trinketMaterial;
    public Material enemyMaterial;
    public Material exitMaterial;
    public Material potholeMaterial;

    private bool[,] visited;
    private bool[,,] walls; // [x, y, direction] 0=North, 1=East, 2=South, 3=West
    private List<Vector2Int> deadEnds = new List<Vector2Int>();
    private Transform mazeParent;

    public Vector3 PlayerSpawnPoint { get; private set; }
    public Vector3 ExitPosition { get; private set; }

    void Awake()
    {
        CreateMaterials();
    }

    void CreateMaterials()
    {
        Shader standardShader = Shader.Find("Standard");
        if (standardShader == null)
            standardShader = Shader.Find("Universal Render Pipeline/Lit");
        if (standardShader == null)
            standardShader = Shader.Find("Sprites/Default");

        if (wallMaterial == null)
        {
            wallMaterial = new Material(standardShader);
            wallMaterial.color = new Color(0.17f, 0.2f, 0.24f);
        }

        if (floorMaterial == null)
        {
            floorMaterial = new Material(standardShader);
            floorMaterial.color = new Color(0.09f, 0.11f, 0.13f);
        }

        if (ceilingMaterial == null)
        {
            ceilingMaterial = new Material(standardShader);
            ceilingMaterial.color = new Color(0.11f, 0.13f, 0.16f);
        }

        if (trinketMaterial == null)
        {
            trinketMaterial = new Material(standardShader);
            trinketMaterial.color = new Color(0.2f, 0.8f, 1f);
            trinketMaterial.EnableKeyword("_EMISSION");
            trinketMaterial.SetColor("_EmissionColor", new Color(0.2f, 0.8f, 1f) * 2f);
        }

        if (enemyMaterial == null)
        {
            enemyMaterial = new Material(standardShader);
            enemyMaterial.color = new Color(0.1f, 0.02f, 0.02f);
            enemyMaterial.EnableKeyword("_EMISSION");
            enemyMaterial.SetColor("_EmissionColor", new Color(0.5f, 0.05f, 0.05f));
        }

        if (exitMaterial == null)
        {
            exitMaterial = new Material(standardShader);
            exitMaterial.color = new Color(0.1f, 1f, 0.3f);
            exitMaterial.EnableKeyword("_EMISSION");
            exitMaterial.SetColor("_EmissionColor", new Color(0.1f, 1f, 0.3f) * 3f);
        }

        if (potholeMaterial == null)
        {
            potholeMaterial = new Material(standardShader);
            potholeMaterial.color = new Color(0.08f, 0.09f, 0.12f);
            potholeMaterial.EnableKeyword("_EMISSION");
            potholeMaterial.SetColor("_EmissionColor", new Color(0.08f, 0.12f, 0.2f));
        }
    }

    public void GenerateMaze()
    {
        // Clean up old maze
        if (mazeParent != null)
            Destroy(mazeParent.gameObject);

        mazeParent = new GameObject("MazeGeometry").transform;
        mazeParent.SetParent(transform);

        // Initialize arrays
        visited = new bool[mazeWidth, mazeHeight];
        walls = new bool[mazeWidth, mazeHeight, 4];

        // All walls start as existing
        for (int x = 0; x < mazeWidth; x++)
            for (int y = 0; y < mazeHeight; y++)
                for (int d = 0; d < 4; d++)
                    walls[x, y, d] = true;

        // Generate maze using recursive backtracking
        GenerateMazeRecursive(0, 0);

        // Find dead ends for trinket placement
        FindDeadEnds();

        // Build geometry
        BuildFloorAndCeiling();
        BuildWalls();

        // Set spawn and exit
        PlayerSpawnPoint = CellToWorld(0, 0) + Vector3.up * 1f;
        Vector2Int exitCell = new Vector2Int(mazeWidth - 1, mazeHeight - 1);
        ExitPosition = CellToWorld(exitCell.x, exitCell.y);

        // Place content
        PlaceExit(exitCell);
        PlaceTrinkets();
        PlaceEnemies();
        PlacePotholes();

        // Place atmospheric lighting
        MazeLighting.PlaceLightsInMaze(mazeParent, mazeWidth, mazeHeight, cellSize);

        // Build NavMesh
        BuildNavMesh();
    }

    void GenerateMazeRecursive(int x, int y)
    {
        visited[x, y] = true;

        // Randomize direction order
        int[] directions = { 0, 1, 2, 3 };
        ShuffleArray(directions);

        foreach (int dir in directions)
        {
            int nx = x + DirX(dir);
            int ny = y + DirY(dir);

            if (nx >= 0 && nx < mazeWidth && ny >= 0 && ny < mazeHeight && !visited[nx, ny])
            {
                // Remove wall between current cell and next
                walls[x, y, dir] = false;
                walls[nx, ny, OppositeDir(dir)] = false;

                GenerateMazeRecursive(nx, ny);
            }
        }
    }

    void FindDeadEnds()
    {
        deadEnds.Clear();
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                int openWalls = 0;
                for (int d = 0; d < 4; d++)
                {
                    if (!walls[x, y, d]) openWalls++;
                }
                if (openWalls == 1) deadEnds.Add(new Vector2Int(x, y));
            }
        }
    }

    void BuildFloorAndCeiling()
    {
        float totalWidth = mazeWidth * cellSize;
        float totalHeight = mazeHeight * cellSize;

        // Floor - bright color for top-down visibility
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(mazeParent);
        floor.transform.position = new Vector3(totalWidth / 2f, -0.05f, totalHeight / 2f);
        floor.transform.localScale = new Vector3(totalWidth + 2f, 0.1f, totalHeight + 2f);
        Renderer floorRenderer = floor.GetComponent<Renderer>();
        floorRenderer.material = floorMaterial;
        floorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        floorRenderer.receiveShadows = false;
        floor.layer = LayerMask.NameToLayer("Default");
        floor.isStatic = true;

        // No ceiling - top-down camera needs to see inside the maze
    }

    void BuildWalls()
    {
        // Outer walls
        float totalWidth = mazeWidth * cellSize;
        float totalHeight = mazeHeight * cellSize;

        // North outer wall
        CreateWall(new Vector3(totalWidth / 2f, wallHeight / 2f, totalHeight),
                   new Vector3(totalWidth + 2f, wallHeight, wallThickness), "OuterWallN");
        // South outer wall
        CreateWall(new Vector3(totalWidth / 2f, wallHeight / 2f, 0),
                   new Vector3(totalWidth + 2f, wallHeight, wallThickness), "OuterWallS");
        // East outer wall
        CreateWall(new Vector3(totalWidth, wallHeight / 2f, totalHeight / 2f),
                   new Vector3(wallThickness, wallHeight, totalHeight + 2f), "OuterWallE");
        // West outer wall
        CreateWall(new Vector3(0, wallHeight / 2f, totalHeight / 2f),
                   new Vector3(wallThickness, wallHeight, totalHeight + 2f), "OuterWallW");

        // Internal walls
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                Vector3 cellCenter = CellToWorld(x, y);

                // North wall
                if (walls[x, y, 0] && y < mazeHeight - 1)
                {
                    Vector3 pos = cellCenter + new Vector3(0, wallHeight / 2f, cellSize / 2f);
                    CreateWall(pos, new Vector3(cellSize, wallHeight, wallThickness),
                              $"Wall_{x}_{y}_N");
                }

                // East wall
                if (walls[x, y, 1] && x < mazeWidth - 1)
                {
                    Vector3 pos = cellCenter + new Vector3(cellSize / 2f, wallHeight / 2f, 0);
                    CreateWall(pos, new Vector3(wallThickness, wallHeight, cellSize),
                              $"Wall_{x}_{y}_E");
                }
            }
        }
    }

    GameObject CreateWall(Vector3 position, Vector3 scale, string name)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(mazeParent);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        Renderer wallRenderer = wall.GetComponent<Renderer>();
        wallRenderer.material = wallMaterial;
        // Disable shadows so top-down view stays clean with no black patches
        wallRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        wallRenderer.receiveShadows = false;
        wall.layer = LayerMask.NameToLayer("Default");
        wall.isStatic = true;
        return wall;
    }

    void PlaceExit(Vector2Int cell)
    {
        Vector3 pos = CellToWorld(cell.x, cell.y);

        GameObject exit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        exit.name = "Exit";
        exit.transform.SetParent(mazeParent);
        exit.transform.position = pos + Vector3.up * 0.05f;
        exit.transform.localScale = new Vector3(2f, 0.1f, 2f);
        exit.GetComponent<Renderer>().material = exitMaterial;

        // Exit trigger
        BoxCollider trigger = exit.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(1.5f, 20f, 1.5f);

        exit.AddComponent<ExitPortal>();

        // Exit light
        Light exitLight = exit.AddComponent<Light>();
        exitLight.type = LightType.Point;
        exitLight.color = new Color(0.1f, 1f, 0.3f);
        exitLight.intensity = 3f;
        exitLight.range = 10f;

        ExitPosition = pos;
    }

    void PlaceTrinkets()
    {
        TrinketManager trinketManager = FindFirstObjectByType<TrinketManager>();
        if (trinketManager == null)
        {
            trinketManager = new GameObject("TrinketManager").AddComponent<TrinketManager>();
        }

        trinketManager.totalTrinkets = trinketCount;
        trinketManager.requiredTrinketsToWin = requiredTrinketsToWin;

        // Shuffle dead ends for random placement
        List<Vector2Int> placements = new List<Vector2Int>(deadEnds);
        ShuffleList(placements);

        // Prioritize maze-edge cells so more trinkets appear along the perimeter.
        List<Vector2Int> edgeCells = new List<Vector2Int>();
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                bool isEdge = x == 0 || y == 0 || x == mazeWidth - 1 || y == mazeHeight - 1;
                if (!isEdge) continue;
                if ((x == 0 && y == 0) || (x == mazeWidth - 1 && y == mazeHeight - 1)) continue;
                edgeCells.Add(new Vector2Int(x, y));
            }
        }
        ShuffleList(edgeCells);

        // Also add some non-dead-end cells for variety
        List<Vector2Int> otherCells = new List<Vector2Int>();
        for (int x = 0; x < mazeWidth; x++)
            for (int y = 0; y < mazeHeight; y++)
                if (!deadEnds.Contains(new Vector2Int(x, y)) && !(x == 0 && y == 0) &&
                    !(x == mazeWidth - 1 && y == mazeHeight - 1))
                    otherCells.Add(new Vector2Int(x, y));
        ShuffleList(otherCells);

        List<Vector2Int> trinketPositions = new List<Vector2Int>();

        // Use edge cells first so each maze border gets more collectible density.
        int fromEdges = Mathf.Min(edgeCells.Count, trinketCount - 1);
        for (int i = 0; i < fromEdges; i++)
            trinketPositions.Add(edgeCells[i]);

        // Use dead ends first, then other cells
        int fromDeadEnds = Mathf.Min(placements.Count, trinketCount - 1 - trinketPositions.Count);
        for (int i = 0; i < fromDeadEnds; i++)
        {
            if (!trinketPositions.Contains(placements[i]))
                trinketPositions.Add(placements[i]);
        }

        int remaining = trinketCount - trinketPositions.Count;
        for (int i = 0; i < remaining && i < otherCells.Count; i++)
        {
            if (!trinketPositions.Contains(otherCells[i]))
                trinketPositions.Add(otherCells[i]);
        }

        // Place trinkets 1-13
        for (int i = 0; i < trinketPositions.Count - 1 && i < trinketCount - 1; i++)
        {
            CreateTrinket(trinketPositions[i], i, false, trinketManager);
        }

        // Place final trinket (14th) - hidden with no guide, sky changes color near it
        if (trinketPositions.Count > 0)
        {
            // Place final trinket in a hard-to-find location (center-ish of maze)
            Vector2Int finalPos = new Vector2Int(mazeWidth / 2 + Random.Range(-3, 4),
                                                  mazeHeight / 2 + Random.Range(-3, 4));
            finalPos.x = Mathf.Clamp(finalPos.x, 1, mazeWidth - 2);
            finalPos.y = Mathf.Clamp(finalPos.y, 1, mazeHeight - 2);

            Trinket finalTrinket = CreateTrinket(finalPos, trinketCount - 1, true, trinketManager);
            trinketManager.SetFinalTrinket(finalTrinket);
        }
    }

    Trinket CreateTrinket(Vector2Int cell, int index, bool isFinal, TrinketManager manager)
    {
        // Create trinket as a clearly visible box
        GameObject trinketObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trinketObj.name = isFinal ? "FinalTrinket" : $"Trinket_{index}";
        trinketObj.transform.SetParent(mazeParent);
        trinketObj.transform.position = CellToWorld(cell.x, cell.y) + Vector3.up * 1.2f;
        trinketObj.transform.localScale = Vector3.one * 0.7f;

        Renderer rend = trinketObj.GetComponent<Renderer>();
        Material mat = new Material(trinketMaterial);
        if (isFinal)
        {
            mat.color = new Color(1f, 0.2f, 0.8f);
            mat.SetColor("_EmissionColor", new Color(1f, 0.2f, 0.8f) * 3f);
        }
        rend.material = mat;

        // Remove default collider and add trigger
        Destroy(trinketObj.GetComponent<BoxCollider>());
        BoxCollider col = trinketObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = Vector3.one * 4f;

        Trinket trinket = trinketObj.AddComponent<Trinket>();
        trinket.trinketIndex = index;
        trinket.isFinalTrinket = isFinal;
        trinket.glowColor = isFinal ? new Color(1f, 0.2f, 0.8f) : new Color(0.3f, 0.8f, 1f);

        return trinket;
    }

    void PlaceEnemies()
    {
        List<Vector2Int> availableCells = new List<Vector2Int>();
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                // Don't place enemies near spawn or exit
                if (x <= 2 && y <= 2) continue;
                if (x >= mazeWidth - 3 && y >= mazeHeight - 3) continue;
                availableCells.Add(new Vector2Int(x, y));
            }
        }

        ShuffleList(availableCells);

        for (int i = 0; i < Mathf.Min(enemyCount, availableCells.Count); i++)
        {
            CreateEnemy(availableCells[i], i);
        }
    }

    void CreateEnemy(Vector2Int cell, int index)
    {
        // Create demonic entity - tall dark figure
        GameObject enemy = new GameObject($"DemonicEntity_{index}");
        enemy.transform.SetParent(mazeParent);
        enemy.transform.position = CellToWorld(cell.x, cell.y) + Vector3.up * 0.05f;

        // Body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(enemy.transform);
        body.transform.localPosition = Vector3.up * 1.2f;
        body.transform.localScale = new Vector3(0.6f, 1.2f, 0.6f);
        body.GetComponent<Renderer>().material = enemyMaterial;

        // Glowing eyes
        GameObject leftEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftEye.name = "LeftEye";
        leftEye.transform.SetParent(enemy.transform);
        leftEye.transform.localPosition = new Vector3(-0.12f, 2.2f, 0.25f);
        leftEye.transform.localScale = Vector3.one * 0.12f;
        Material eyeMat = new Material(enemyMaterial);
        eyeMat.color = Color.red;
        eyeMat.SetColor("_EmissionColor", Color.red * 5f);
        leftEye.GetComponent<Renderer>().material = eyeMat;
        Destroy(leftEye.GetComponent<SphereCollider>());

        GameObject rightEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightEye.name = "RightEye";
        rightEye.transform.SetParent(enemy.transform);
        rightEye.transform.localPosition = new Vector3(0.12f, 2.2f, 0.25f);
        rightEye.transform.localScale = Vector3.one * 0.12f;
        rightEye.GetComponent<Renderer>().material = eyeMat;
        Destroy(rightEye.GetComponent<SphereCollider>());

        // Add NavMeshAgent
        NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
        agent.speed = 2.5f;
        agent.angularSpeed = 180f;
        agent.acceleration = 8f;
        agent.height = 2.4f;
        agent.radius = 0.4f;

        // Add AI
        EnemyAI ai = enemy.AddComponent<EnemyAI>();
        ai.patrolRadius = 25f;

        // Add sound
        enemy.AddComponent<EnemySounds>();

        // Enemy light (faint red glow)
        Light enemyLight = enemy.AddComponent<Light>();
        enemyLight.type = LightType.Point;
        enemyLight.color = new Color(0.8f, 0.1f, 0.1f);
        enemyLight.intensity = 0.5f;
        enemyLight.range = 5f;
    }

    void PlacePotholes()
    {
        if (potholeCount <= 0) return;

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 1; x < mazeWidth - 1; x++)
        {
            for (int y = 1; y < mazeHeight - 1; y++)
            {
                // Keep potholes in traversed paths, not dead ends.
                int openWalls = 0;
                for (int d = 0; d < 4; d++)
                {
                    if (!walls[x, y, d]) openWalls++;
                }

                if (openWalls < 2) continue;
                if (x <= 2 && y <= 2) continue;
                if (x >= mazeWidth - 3 && y >= mazeHeight - 3) continue;

                candidates.Add(new Vector2Int(x, y));
            }
        }

        ShuffleList(candidates);
        int count = Mathf.Min(potholeCount, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            CreatePothole(candidates[i], i);
        }
    }

    void CreatePothole(Vector2Int cell, int index)
    {
        Vector3 center = CellToWorld(cell.x, cell.y);

        GameObject pothole = new GameObject($"Pothole_{index}");
        pothole.transform.SetParent(mazeParent);
        pothole.transform.position = center + new Vector3(0f, 0.02f, 0f);

        GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "Rim";
        rim.transform.SetParent(pothole.transform);
        rim.transform.localPosition = new Vector3(0f, -0.02f, 0f);
        rim.transform.localScale = new Vector3(1.6f, 0.08f, 1.6f);
        Renderer rimRenderer = rim.GetComponent<Renderer>();
        rimRenderer.material = potholeMaterial;
        rimRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rimRenderer.receiveShadows = false;
        Destroy(rim.GetComponent<CapsuleCollider>());

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        core.name = "Core";
        core.transform.SetParent(pothole.transform);
        core.transform.localPosition = new Vector3(0f, -0.12f, 0f);
        core.transform.localScale = new Vector3(1.05f, 0.06f, 1.05f);
        Renderer coreRenderer = core.GetComponent<Renderer>();
        coreRenderer.material = potholeMaterial;
        coreRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        coreRenderer.receiveShadows = false;
        coreRenderer.material.color = new Color(0.02f, 0.02f, 0.03f);
        coreRenderer.material.SetColor("_EmissionColor", new Color(0.02f, 0.03f, 0.05f));
        Destroy(core.GetComponent<CapsuleCollider>());

        SphereCollider trigger = pothole.AddComponent<SphereCollider>();
        trigger.radius = 1.35f;
        trigger.center = new Vector3(0f, 0.45f, 0f);
        trigger.isTrigger = true;

        Light potholeLight = pothole.AddComponent<Light>();
        potholeLight.type = LightType.Point;
        potholeLight.color = new Color(0.3f, 0.45f, 0.7f);
        potholeLight.intensity = 0.25f;
        potholeLight.range = 4.5f;

        pothole.AddComponent<PotholeHazard>();
        pothole.layer = LayerMask.NameToLayer("Default");
    }

    void BuildNavMesh()
    {
        // Build NavMesh at runtime using NavMeshBuilder
        var sources = new List<NavMeshBuildSource>();
        var markups = new List<NavMeshBuildMarkup>();

        NavMeshBuilder.CollectSources(
            new Bounds(
                new Vector3(mazeWidth * cellSize / 2f, wallHeight / 2f, mazeHeight * cellSize / 2f),
                new Vector3(mazeWidth * cellSize + 10f, wallHeight + 5f, mazeHeight * cellSize + 10f)
            ),
            ~0,
            NavMeshCollectGeometry.PhysicsColliders,
            0,
            markups,
            sources
        );

        NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
        settings.agentRadius = 0.4f;
        settings.agentHeight = 2f;
        settings.agentSlope = 45f;
        settings.agentClimb = 0.4f;

        Bounds bounds = new Bounds(
            new Vector3(mazeWidth * cellSize / 2f, wallHeight / 2f, mazeHeight * cellSize / 2f),
            new Vector3(mazeWidth * cellSize + 10f, wallHeight + 5f, mazeHeight * cellSize + 10f)
        );

        NavMeshData navMeshData = NavMeshBuilder.BuildNavMeshData(
            settings, sources, bounds, Vector3.zero, Quaternion.identity
        );

        if (navMeshData != null)
        {
            NavMesh.AddNavMeshData(navMeshData);
        }
    }

    Vector3 CellToWorld(int x, int y)
    {
        return new Vector3(x * cellSize + cellSize / 2f, 0, y * cellSize + cellSize / 2f);
    }

    int DirX(int dir) => dir switch { 1 => 1, 3 => -1, _ => 0 };
    int DirY(int dir) => dir switch { 0 => 1, 2 => -1, _ => 0 };
    int OppositeDir(int dir) => (dir + 2) % 4;

    void ShuffleArray(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
