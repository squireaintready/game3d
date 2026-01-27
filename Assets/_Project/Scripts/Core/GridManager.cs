using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace TowerDefense.Core
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 7;
        [SerializeField] private int gridHeight = 10;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 gridOrigin = Vector3.zero;

        [Header("Special Cell Positions")]
        // Spawns at TOP of grid, enemies walk DOWN to goal at BOTTOM
        [SerializeField] private Vector2Int leftSpawnCell = new Vector2Int(2, 9);
        [SerializeField] private Vector2Int rightSpawnCell = new Vector2Int(4, 9);
        [SerializeField] private Vector2Int goalCell = new Vector2Int(3, 0);

        [Header("Tile Sprites")]
        [SerializeField] private Sprite tilePathSprite;
        [SerializeField] private Sprite tileSpawnSprite;
        [SerializeField] private Sprite tileGoalSprite;

        [Header("Visual Settings")]
        [SerializeField] private bool showGridOverlay = true;
        [SerializeField] private Material gridMaterial;
        [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.3f);
        [SerializeField] private Color occupiedColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        [Header("Blocked Cells (Water/Obstacles)")]
        [SerializeField] private List<Vector2Int> blockedCells = new List<Vector2Int>();

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public float CellSize => cellSize;
        public Vector2Int LeftSpawnCell => leftSpawnCell;
        public Vector2Int RightSpawnCell => rightSpawnCell;
        public Vector2Int GoalCell => goalCell;

        private CellState[,] grid;
        private Tower[,] towers;
        private GameObject gridOverlay;
        private MeshRenderer gridRenderer;
        private GameObject tilesContainer;
        private List<SpriteRenderer> tileRenderers = new List<SpriteRenderer>();

        // Sorting order constants
        private const int TILE_PATH_ORDER = 0;
        private const int TILE_SPAWN_ORDER = 5;
        private const int TILE_GOAL_ORDER = 8;
        private const int GRID_OVERLAY_ORDER = 10;

        // NavMesh for pathfinding
        private NavMeshSurface navMeshSurface;
        private GameObject navMeshGround;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            LoadTileSprites();
            InitializeGrid();
            CreateGridBackground(); // Lighter gray behind tiles
            CreateTileLayer();
            CreateGridOverlay();
            CreateBoundaryWalls(); // Invisible walls to keep enemies in bounds
            EnsureGroundPlane();
            SetupSpawnAndGoalPoints();
        }

        private void Start()
        {
            // Camera setup in Start() when screen dimensions are reliable
            FixCameraSettings();

            // Setup NavMesh for enemy pathfinding
            SetupNavMesh();

            // Delayed cleanup to catch Ground objects created by other scripts
            Invoke(nameof(CleanupGroundObjects), 0.1f);
        }

        private void CleanupGroundObjects()
        {
            // Find and hide/destroy any Ground objects - tiles handle visuals
            // But don't remove our NavMesh ground
            string[] groundNames = { "Ground", "GroundPlane", "Plane" };
            foreach (string name in groundNames)
            {
                var ground = GameObject.Find(name);
                if (ground != null && ground != navMeshGround)
                {
                    var renderer = ground.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                        Debug.Log($"[GridManager] Disabled renderer on '{name}'");
                    }
                }
            }
        }

        private void SetupNavMesh()
        {
            // Create an invisible ground plane for NavMesh baking
            navMeshGround = new GameObject("NavMeshGround");
            navMeshGround.transform.SetParent(transform);
            navMeshGround.transform.position = Vector3.zero;

            // Create mesh that covers the entire grid
            var meshFilter = navMeshGround.AddComponent<MeshFilter>();
            var meshRenderer = navMeshGround.AddComponent<MeshRenderer>();
            var meshCollider = navMeshGround.AddComponent<MeshCollider>();

            float w = gridWidth * cellSize;
            float h = gridHeight * cellSize;
            float padding = 1f; // Small padding around edges

            Mesh mesh = new Mesh();
            mesh.name = "NavMeshGroundMesh";

            mesh.vertices = new Vector3[]
            {
                new Vector3(-padding, 0, -padding),
                new Vector3(w + padding, 0, -padding),
                new Vector3(-padding, 0, h + padding),
                new Vector3(w + padding, 0, h + padding)
            };
            mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };

            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;

            // Hide the renderer - tiles handle visuals
            meshRenderer.enabled = false;

            // Add NavMeshSurface and bake
            navMeshSurface = navMeshGround.AddComponent<NavMeshSurface>();
            navMeshSurface.collectObjects = CollectObjects.All;
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            // Bake the NavMesh
            navMeshSurface.BuildNavMesh();
            Debug.Log($"[GridManager] NavMesh baked successfully. Grid size: {w}x{h}");
        }

        /// <summary>
        /// Rebake NavMesh after placing/removing towers
        /// </summary>
        public void RebakeNavMesh()
        {
            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
                Debug.Log("[GridManager] NavMesh rebaked");
            }
        }

        private void LoadTileSprites()
        {
            // 3D version: We use colored quads instead of sprites
            // Clear any broken references from 2D version
            tilePathSprite = null;
            tileSpawnSprite = null;
            tileGoalSprite = null;
            Debug.Log("[GridManager] 3D mode: Using colored quads for ground tiles");
        }

        private void CreateGridBackground()
        {
            // Load sand textures and create materials
            Material[] sandMaterials = LoadSandMaterials();

            if (sandMaterials == null || sandMaterials.Length == 0)
            {
                Debug.LogWarning("[GridManager] No sand textures found, using fallback color");
                CreateSimpleGroundPlane();
                return;
            }

            // Create container
            GameObject groundContainer = new GameObject("GroundTiles");
            groundContainer.transform.SetParent(transform);

            // Create one shared mesh for all tiles (more efficient)
            Mesh sharedMesh = CreateTileMesh();

            // Seeded random for consistent pattern
            System.Random rng = new System.Random(42);

            // Create tiles
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    GameObject tile = new GameObject($"Tile_{x}_{y}");
                    tile.transform.SetParent(groundContainer.transform);
                    tile.transform.position = new Vector3((x + 0.5f) * cellSize, -0.01f, (y + 0.5f) * cellSize);
                    tile.transform.rotation = Quaternion.Euler(0f, rng.Next(4) * 90f, 0f);

                    var mf = tile.AddComponent<MeshFilter>();
                    var mr = tile.AddComponent<MeshRenderer>();
                    mf.sharedMesh = sharedMesh;
                    mr.sharedMaterial = sandMaterials[rng.Next(sandMaterials.Length)];
                }
            }

            Debug.Log($"[GridManager] Created ground with {sandMaterials.Length} sand texture variations");
        }

        private Material[] LoadSandMaterials()
        {
            // Try loading from Resources
            Texture2D tex1 = Resources.Load<Texture2D>("Textures/PFK_Texture_Ground_Sand_01");
            Texture2D tex2 = Resources.Load<Texture2D>("Textures/PFK_Texture_Ground_Sand_02");
            Texture2D tex3 = Resources.Load<Texture2D>("Textures/PFK_Texture_Ground_Sand_03");

            List<Texture2D> textures = new List<Texture2D>();
            if (tex1 != null) textures.Add(tex1);
            if (tex2 != null) textures.Add(tex2);
            if (tex3 != null) textures.Add(tex3);

            if (textures.Count == 0) return null;

            // Find shader
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Unlit/Texture");

            if (shader == null) return null;

            // Create materials
            Material[] materials = new Material[textures.Count];
            for (int i = 0; i < textures.Count; i++)
            {
                materials[i] = new Material(shader);
                materials[i].mainTexture = textures[i];
                if (materials[i].HasProperty("_BaseMap"))
                    materials[i].SetTexture("_BaseMap", textures[i]);
            }

            return materials;
        }

        private Mesh CreateTileMesh()
        {
            Mesh mesh = new Mesh();
            float half = cellSize * 0.5f;

            mesh.vertices = new Vector3[]
            {
                new Vector3(-half, 0, -half),
                new Vector3(half, 0, -half),
                new Vector3(-half, 0, half),
                new Vector3(half, 0, half)
            };
            mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };

            return mesh;
        }

        private void CreateSimpleGroundPlane()
        {
            GameObject bg = new GameObject("GroundPlane");
            bg.transform.SetParent(transform);
            bg.transform.position = new Vector3(gridWidth * cellSize / 2f, -0.01f, gridHeight * cellSize / 2f);

            var mf = bg.AddComponent<MeshFilter>();
            var mr = bg.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            float hw = gridWidth * cellSize / 2f;
            float hh = gridHeight * cellSize / 2f;

            mesh.vertices = new Vector3[]
            {
                new Vector3(-hw, 0, -hh), new Vector3(hw, 0, -hh),
                new Vector3(-hw, 0, hh), new Vector3(hw, 0, hh)
            };
            mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };

            mf.mesh = mesh;
            mr.material = CreateSafeMaterial(new Color(0.76f, 0.70f, 0.50f));
        }

        private void CreateTileLayer()
        {
            tilesContainer = new GameObject("TilesContainer");
            tilesContainer.transform.SetParent(transform);
            tilesContainer.transform.position = Vector3.zero;

            // With sand texture as background, we only create markers for spawn and goal
            // No need for individual path tiles
            Color spawnColor = new Color(0.6f, 0.3f, 0.3f, 0.5f);       // Semi-transparent reddish spawn
            Color goalColor = new Color(0.3f, 0.5f, 0.7f, 0.5f);        // Semi-transparent bluish goal

            // Create spawn tiles on top of sand (spawns at top, facing downward)
            CreateTileAt(leftSpawnCell, tileSpawnSprite, TILE_SPAWN_ORDER, "SpawnTile_Left", spawnColor, flipX: true, flipY: false);
            CreateTileAt(rightSpawnCell, tileSpawnSprite, TILE_SPAWN_ORDER, "SpawnTile_Right", spawnColor, flipX: false, flipY: false);

            // Create goal tile on top of sand
            Debug.Log($"[GridManager] Creating goal tile at {goalCell} with sprite: {tileGoalSprite?.name ?? "NULL"}");
            CreateTileAt(goalCell, tileGoalSprite, TILE_GOAL_ORDER, "GoalTile", goalColor, flipX: false, flipY: false);

            Debug.Log($"[GridManager] Created spawn/goal markers on sand background");
        }

        private void CreateTileAt(Vector2Int cellPos, Sprite sprite, int sortingOrder, string tileName, Color fallbackColor, bool flipX = false, bool flipY = false)
        {
            GameObject tileObj = new GameObject($"{tileName}_{cellPos.x}_{cellPos.y}");
            tileObj.transform.SetParent(tilesContainer.transform);

            // Position at cell center, flat on ground
            Vector3 worldPos = GridToWorld(cellPos);
            tileObj.transform.position = new Vector3(worldPos.x, 0.01f + sortingOrder * 0.001f, worldPos.z);

            if (sprite != null)
            {
                // Use sprite
                tileObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                // Calculate scale based on sprite type
                float spriteSizeX = sprite.bounds.size.x;
                float spriteSizeY = sprite.bounds.size.y;
                float scale;

                // Goal tile has significant transparent padding - use larger scale
                if (tileName.Contains("Goal"))
                {
                    // Scale based on width, then boost to compensate for padding
                    float baseSize = Mathf.Max(spriteSizeX, spriteSizeY);
                    if (baseSize <= 0) baseSize = 1f;
                    scale = (cellSize * 2.5f) / baseSize; // Larger scale for visibility
                }
                else
                {
                    // Standard tiles: use smaller dimension
                    float spriteSize = Mathf.Min(spriteSizeX, spriteSizeY);
                    if (spriteSize <= 0) spriteSize = 1f;
                    scale = cellSize / spriteSize;
                }

                tileObj.transform.localScale = new Vector3(scale, scale, scale);

                Debug.Log($"[GridManager] {tileName} at {cellPos}: sprite bounds=({spriteSizeX:F2}, {spriteSizeY:F2}), scale={scale:F2}, worldPos={worldPos}");

                SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = sortingOrder;
                sr.color = Color.white; // Ensure full visibility
                sr.flipX = flipX; // Mirror sprite horizontally (across Y axis)
                sr.flipY = flipY; // Mirror sprite vertically (across X axis)
                SetupSpriteMaterial(sr);
                tileRenderers.Add(sr);

                // Extra visibility check for goal tile
                if (tileName.Contains("Goal"))
                {
                    Debug.Log($"[GridManager] Goal tile created! Position={tileObj.transform.position}, Rotation={tileObj.transform.rotation.eulerAngles}, Scale={tileObj.transform.localScale}, SpriteRenderer enabled={sr.enabled}");
                }
            }
            else
            {
                // Fallback: create a colored quad
                CreateColoredQuad(tileObj, fallbackColor);
            }
        }

        private void CreateColoredQuad(GameObject parent, Color color)
        {
            var meshFilter = parent.AddComponent<MeshFilter>();
            var meshRenderer = parent.AddComponent<MeshRenderer>();

            // Create a simple quad mesh
            Mesh mesh = new Mesh();
            float halfSize = cellSize * 0.48f; // Slightly smaller than cell

            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-halfSize, 0, -halfSize),
                new Vector3(halfSize, 0, -halfSize),
                new Vector3(-halfSize, 0, halfSize),
                new Vector3(halfSize, 0, halfSize)
            };

            int[] triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            Vector2[] uvs = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            Vector3[] normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.normals = normals;

            meshFilter.mesh = mesh;
            meshRenderer.material = CreateSafeMaterial(color);
        }

        private void SetupSpriteMaterial(SpriteRenderer sr)
        {
            // Don't override the material - Unity's default SpriteRenderer material works in URP
            // Creating a new material can cause magenta rendering issues
            // Just leave sr.material as default
        }

        private void CreateBoundaryWalls()
        {
            // Create invisible walls around the grid perimeter to keep enemies in bounds
            // These use NavMeshObstacle to block enemy pathfinding but don't prevent tower placement

            GameObject boundaryContainer = new GameObject("BoundaryWalls");
            boundaryContainer.transform.SetParent(transform);

            float wallThickness = 0.5f;
            float wallHeight = 2f;
            float gridWorldWidth = gridWidth * cellSize;
            float gridWorldHeight = gridHeight * cellSize;

            // Left wall
            CreateBoundaryWall(boundaryContainer.transform, "LeftWall",
                new Vector3(-wallThickness / 2f, wallHeight / 2f, gridWorldHeight / 2f),
                new Vector3(wallThickness, wallHeight, gridWorldHeight + wallThickness * 2));

            // Right wall
            CreateBoundaryWall(boundaryContainer.transform, "RightWall",
                new Vector3(gridWorldWidth + wallThickness / 2f, wallHeight / 2f, gridWorldHeight / 2f),
                new Vector3(wallThickness, wallHeight, gridWorldHeight + wallThickness * 2));

            // Bottom wall (leave gap for goal - goal is now at bottom)
            float goalX = (goalCell.x + 0.5f) * cellSize;

            // Bottom-left segment (from left edge to just before goal)
            if (goalCell.x > 0)
            {
                float segmentWidth = goalX - cellSize / 2f;
                CreateBoundaryWall(boundaryContainer.transform, "BottomWall_Left",
                    new Vector3(segmentWidth / 2f, wallHeight / 2f, -wallThickness / 2f),
                    new Vector3(segmentWidth, wallHeight, wallThickness));
            }

            // Bottom-right segment (from just after goal to right edge)
            if (goalCell.x < gridWidth - 1)
            {
                float segmentStart = goalX + cellSize / 2f;
                float segmentWidth = gridWorldWidth - segmentStart;
                CreateBoundaryWall(boundaryContainer.transform, "BottomWall_Right",
                    new Vector3(segmentStart + segmentWidth / 2f, wallHeight / 2f, -wallThickness / 2f),
                    new Vector3(segmentWidth, wallHeight, wallThickness));
            }

            // Top wall (leave gaps for spawn points - spawns are now at top)
            float leftSpawnX = (leftSpawnCell.x + 0.5f) * cellSize;
            float rightSpawnX = (rightSpawnCell.x + 0.5f) * cellSize;

            // Top-left segment (from left edge to just before left spawn)
            if (leftSpawnCell.x > 0)
            {
                float segmentWidth = leftSpawnX - cellSize / 2f;
                CreateBoundaryWall(boundaryContainer.transform, "TopWall_Left",
                    new Vector3(segmentWidth / 2f, wallHeight / 2f, gridWorldHeight + wallThickness / 2f),
                    new Vector3(segmentWidth, wallHeight, wallThickness));
            }

            // Top-middle segment (between spawns)
            float middleStart = leftSpawnX + cellSize / 2f;
            float middleEnd = rightSpawnX - cellSize / 2f;
            float middleWidth = middleEnd - middleStart;
            if (middleWidth > 0)
            {
                CreateBoundaryWall(boundaryContainer.transform, "TopWall_Middle",
                    new Vector3(middleStart + middleWidth / 2f, wallHeight / 2f, gridWorldHeight + wallThickness / 2f),
                    new Vector3(middleWidth, wallHeight, wallThickness));
            }

            // Top-right segment (from just after right spawn to right edge)
            if (rightSpawnCell.x < gridWidth - 1)
            {
                float segmentStart = rightSpawnX + cellSize / 2f;
                float segmentWidth = gridWorldWidth - segmentStart;
                CreateBoundaryWall(boundaryContainer.transform, "TopWall_Right",
                    new Vector3(segmentStart + segmentWidth / 2f, wallHeight / 2f, gridWorldHeight + wallThickness / 2f),
                    new Vector3(segmentWidth, wallHeight, wallThickness));
            }

            Debug.Log($"[GridManager] Created boundary walls for {gridWidth}x{gridHeight} grid");
        }

        private void CreateBoundaryWall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent);
            wall.transform.position = position;

            // Add box collider for physics
            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.size = size;

            // Add NavMeshObstacle to block enemy pathfinding
            UnityEngine.AI.NavMeshObstacle obstacle = wall.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            obstacle.size = size;
        }

        private void SetupSpawnAndGoalPoints()
        {
            // Find or create SpawnPoint objects
            var existingSpawn = GameObject.Find("SpawnPoint");
            var existingSpawn2 = GameObject.Find("SpawnPoint2");
            var existingGoal = GameObject.Find("GoalPoint");

            // Left spawn point
            if (existingSpawn != null)
            {
                existingSpawn.transform.position = GridToWorld(leftSpawnCell);
                existingSpawn.name = "SpawnPoint_Left";
            }
            else
            {
                GameObject spawn1 = new GameObject("SpawnPoint_Left");
                spawn1.transform.position = GridToWorld(leftSpawnCell);
                spawn1.tag = "SpawnPoint";
            }

            // Right spawn point
            if (existingSpawn2 != null)
            {
                existingSpawn2.transform.position = GridToWorld(rightSpawnCell);
                existingSpawn2.name = "SpawnPoint_Right";
            }
            else
            {
                GameObject spawn2 = new GameObject("SpawnPoint_Right");
                spawn2.transform.position = GridToWorld(rightSpawnCell);
                spawn2.tag = "SpawnPoint";
            }

            // Goal point
            if (existingGoal != null)
            {
                existingGoal.transform.position = GridToWorld(goalCell);
            }
            else
            {
                GameObject goal = new GameObject("GoalPoint");
                goal.transform.position = GridToWorld(goalCell);
                goal.tag = "GoalPoint";
            }

            Debug.Log($"[GridManager] Spawn points: Left={GridToWorld(leftSpawnCell)}, Right={GridToWorld(rightSpawnCell)}, Goal={GridToWorld(goalCell)}");
        }

        private void EnsureGroundPlane()
        {
            // Delete any existing ground objects - we don't need separate ground planes
            // The tile sprites cover the entire grid
            string[] groundNames = { "Ground", "GroundPlane", "Plane" };
            foreach (string name in groundNames)
            {
                var ground = GameObject.Find(name);
                if (ground != null)
                {
                    // Use DestroyImmediate in editor, Destroy at runtime
                    if (Application.isPlaying)
                        Destroy(ground);
                    else
                        DestroyImmediate(ground);
                    Debug.Log($"[GridManager] Removed '{name}' object");
                }
            }

            // Also disable any MeshRenderer on objects that might cause magenta
            var allRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var renderer in allRenderers)
            {
                if (renderer.gameObject.name.ToLower().Contains("ground") ||
                    renderer.gameObject.name.ToLower().Contains("plane"))
                {
                    renderer.enabled = false;
                    Debug.Log($"[GridManager] Disabled renderer on '{renderer.gameObject.name}'");
                }
            }

            // The grid tiles themselves serve as the ground
            // Camera background color handles the rest
        }

        private void FixCameraSettings()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;

            // Set background color - darker for 3D depth feel
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.18f, 0.22f, 1f);

            // TOP-DOWN WITH SLIGHT ANGLE: Perspective camera looking down
            cam.orthographic = false;
            cam.fieldOfView = 36f;

            // Grid dimensions in world units
            float gridWorldWidth = gridWidth * cellSize;
            float gridWorldHeight = gridHeight * cellSize;

            // Camera pitch angle (overhead view for full grid visibility)
            float cameraPitch = 70f; // 70 degrees = mostly overhead, slight forward tilt
            float cameraYaw = 0f; // No rotation around Y axis

            // Calculate camera position to frame the grid
            // Camera looks at grid center, positioned back and up
            float gridCenterX = gridWorldWidth / 2f;
            float gridCenterZ = gridWorldHeight / 2f;

            // Distance from grid center to fit entire grid in view
            // We want the grid height to fill the screen vertically (edge-to-edge)
            float verticalFOV = cam.fieldOfView * Mathf.Deg2Rad;
            float horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(verticalFOV / 2f) * cam.aspect);

            // For angled camera looking at ground plane:
            // The visible height in world space depends on the pitch angle
            // At steep angles, we need to see the full grid depth projected onto the view
            float pitchRad = cameraPitch * Mathf.Deg2Rad;

            // Calculate the distance needed to fit the grid in view
            // The grid appears foreshortened when viewed at an angle
            // We need to account for both the grid's actual height AND the perspective distortion

            // The key insight: at a steep angle, the near and far edges of the grid
            // are at different distances from the camera, so we need extra margin
            float cosPitch = Mathf.Cos(pitchRad);
            float sinPitch = Mathf.Sin(pitchRad);

            // For a nearly top-down view, we need to fit the full grid in viewport
            // Add padding (35%) to ensure full grid visibility tip-to-tip with margin
            float paddingMultiplier = 1.35f;
            float effectiveGridHeight = gridWorldHeight * paddingMultiplier;
            float effectiveGridWidth = gridWorldWidth * paddingMultiplier;

            // Distance calculation: grid should fill the vertical FOV
            // Account for foreshortening at the viewing angle
            float apparentHeight = effectiveGridHeight * cosPitch + 2f * sinPitch; // Add vertical component
            float distanceForHeight = (apparentHeight / 2f) / Mathf.Tan(verticalFOV / 2f);
            float distanceForWidth = (effectiveGridWidth / 2f) / Mathf.Tan(horizontalFOV / 2f);

            // Use the larger distance to ensure full grid visibility
            float cameraDistance = Mathf.Max(distanceForHeight, distanceForWidth);

            // Add extra distance for full grid visibility at steep angles
            cameraDistance *= 1.25f;

            // Position camera: offset back (negative Z) and up (positive Y) from grid center
            float cameraY = sinPitch * cameraDistance;
            float cameraZOffset = -cosPitch * cameraDistance;

            cam.transform.position = new Vector3(gridCenterX, cameraY, gridCenterZ + cameraZOffset);
            cam.transform.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);

            Debug.Log($"[GridManager] Camera: perspective FOV={cam.fieldOfView}, pitch={cameraPitch}°, distance={cameraDistance:F2}, pos={cam.transform.position}");

            // Setup lighting for 3D low-poly look
            SetupLighting();
        }

        private void SetupLighting()
        {
            // Find or create directional light
            Light directionalLight = FindFirstObjectByType<Light>();
            if (directionalLight == null || directionalLight.type != LightType.Directional)
            {
                GameObject lightObj = new GameObject("DirectionalLight");
                directionalLight = lightObj.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
            }

            // Warm sunlight from upper-front-right (good for low-poly)
            directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            directionalLight.color = new Color(1f, 0.95f, 0.85f); // Warm white
            directionalLight.intensity = 1.2f;
            directionalLight.shadows = LightShadows.Soft;

            // Set ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.45f, 0.5f); // Cool ambient

            Debug.Log("[GridManager] Lighting configured for 3D low-poly style");
        }

        private Material CreateSafeMaterial(Color color)
        {
            string[] shaderNames = new string[]
            {
                "Universal Render Pipeline/Unlit",
                "Universal Render Pipeline/Lit",
                "Sprites/Default",
                "UI/Default",
                "Unlit/Color"
            };

            foreach (string shaderName in shaderNames)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    Material mat = new Material(shader);
                    mat.color = color;

                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", color);
                    }

                    return mat;
                }
            }

            var defaultSpriteMat = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline?.defaultMaterial;
            if (defaultSpriteMat != null)
            {
                Material mat = new Material(defaultSpriteMat);
                mat.color = color;
                return mat;
            }

            Debug.LogWarning("Could not find any valid shader for material creation");
            return new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameState state)
        {
            SetGridOverlayVisible(state == GameState.Building || state == GameState.WaveActive);
        }

        private void InitializeGrid()
        {
            // Force correct grid dimensions (scene may have old serialized values)
            gridWidth = 7;
            gridHeight = 10;
            // Spawns at TOP (y=9), goal at BOTTOM (y=0) - enemies walk downward
            leftSpawnCell = new Vector2Int(2, 9);
            rightSpawnCell = new Vector2Int(4, 9);
            goalCell = new Vector2Int(3, 0);

            grid = new CellState[gridWidth, gridHeight];
            towers = new Tower[gridWidth, gridHeight];

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y] = CellState.Empty;
                }
            }

            // Block spawn and goal cells (with bounds check)
            if (IsValidCell(leftSpawnCell))
                grid[leftSpawnCell.x, leftSpawnCell.y] = CellState.Blocked;
            if (IsValidCell(rightSpawnCell))
                grid[rightSpawnCell.x, rightSpawnCell.y] = CellState.Blocked;
            if (IsValidCell(goalCell))
                grid[goalCell.x, goalCell.y] = CellState.Blocked;

            // Block any additional cells from the list
            foreach (var cell in blockedCells)
            {
                if (IsValidCell(cell))
                {
                    grid[cell.x, cell.y] = CellState.Blocked;
                }
            }

            Debug.Log($"[GridManager] Grid initialized {gridWidth}x{gridHeight}, leftSpawn={leftSpawnCell}, rightSpawn={rightSpawnCell}, goal={goalCell}");
        }

        private void CreateGridOverlay()
        {
            gridOverlay = new GameObject("GridOverlay");
            gridOverlay.transform.SetParent(transform);
            gridOverlay.transform.position = gridOrigin + new Vector3(0, 0.03f, 0);

            var meshFilter = gridOverlay.AddComponent<MeshFilter>();
            gridRenderer = gridOverlay.AddComponent<MeshRenderer>();

            meshFilter.mesh = CreateGridMesh();

            if (gridMaterial != null)
            {
                gridRenderer.material = gridMaterial;
            }
            else
            {
                // Subtle white lines for clean look
                gridRenderer.material = CreateSafeMaterial(new Color(1f, 1f, 1f, 0.2f));
            }

            SetGridOverlayVisible(showGridOverlay);
        }

        private Mesh CreateGridMesh()
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var indices = new List<int>();

            float lineWidth = 0.02f;

            for (int x = 0; x <= gridWidth; x++)
            {
                float xPos = x * cellSize;
                int baseIndex = vertices.Count;

                vertices.Add(new Vector3(xPos - lineWidth, 0, 0));
                vertices.Add(new Vector3(xPos + lineWidth, 0, 0));
                vertices.Add(new Vector3(xPos + lineWidth, 0, gridHeight * cellSize));
                vertices.Add(new Vector3(xPos - lineWidth, 0, gridHeight * cellSize));

                indices.AddRange(new int[] { baseIndex, baseIndex + 2, baseIndex + 1 });
                indices.AddRange(new int[] { baseIndex, baseIndex + 3, baseIndex + 2 });
            }

            for (int y = 0; y <= gridHeight; y++)
            {
                float yPos = y * cellSize;
                int baseIndex = vertices.Count;

                vertices.Add(new Vector3(0, 0, yPos - lineWidth));
                vertices.Add(new Vector3(0, 0, yPos + lineWidth));
                vertices.Add(new Vector3(gridWidth * cellSize, 0, yPos + lineWidth));
                vertices.Add(new Vector3(gridWidth * cellSize, 0, yPos - lineWidth));

                indices.AddRange(new int[] { baseIndex, baseIndex + 2, baseIndex + 1 });
                indices.AddRange(new int[] { baseIndex, baseIndex + 3, baseIndex + 2 });
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();

            return mesh;
        }

        public void SetGridOverlayVisible(bool visible)
        {
            if (gridRenderer != null)
            {
                gridRenderer.enabled = visible;
            }
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return gridOrigin + new Vector3(
                (gridPos.x + 0.5f) * cellSize,
                0,
                (gridPos.y + 0.5f) * cellSize
            );
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - gridOrigin;
            return new Vector2Int(
                Mathf.FloorToInt(localPos.x / cellSize),
                Mathf.FloorToInt(localPos.z / cellSize)
            );
        }

        public bool IsValidCell(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < gridWidth &&
                   cell.y >= 0 && cell.y < gridHeight;
        }

        public bool IsCellEmpty(Vector2Int cell)
        {
            if (!IsValidCell(cell)) return false;
            return grid[cell.x, cell.y] == CellState.Empty;
        }

        public bool IsCellBlocked(Vector2Int cell)
        {
            if (!IsValidCell(cell)) return true;
            return grid[cell.x, cell.y] == CellState.Blocked;
        }

        public bool IsCellOccupied(Vector2Int cell)
        {
            if (!IsValidCell(cell)) return true;
            return grid[cell.x, cell.y] == CellState.Occupied;
        }

        public CellState GetCellState(Vector2Int cell)
        {
            if (!IsValidCell(cell)) return CellState.Blocked;
            return grid[cell.x, cell.y];
        }

        public Tower GetTowerAt(Vector2Int cell)
        {
            if (!IsValidCell(cell)) return null;
            return towers[cell.x, cell.y];
        }

        public bool PlaceTower(Vector2Int cell, Tower tower)
        {
            if (!IsCellEmpty(cell)) return false;

            grid[cell.x, cell.y] = CellState.Occupied;
            towers[cell.x, cell.y] = tower;

            // Rebake NavMesh so enemies path around the new tower
            RebakeNavMesh();
            return true;
        }

        public bool RemoveTower(Vector2Int cell)
        {
            if (!IsValidCell(cell)) return false;
            if (grid[cell.x, cell.y] != CellState.Occupied) return false;

            grid[cell.x, cell.y] = CellState.Empty;
            towers[cell.x, cell.y] = null;

            // Rebake NavMesh so enemies can use the freed space
            RebakeNavMesh();
            return true;
        }

        public List<Vector2Int> GetEmptyCells()
        {
            var emptyCells = new List<Vector2Int>();
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (grid[x, y] == CellState.Empty)
                    {
                        emptyCells.Add(new Vector2Int(x, y));
                    }
                }
            }
            return emptyCells;
        }

        public List<Tower> GetAllTowers()
        {
            var allTowers = new List<Tower>();
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (towers[x, y] != null)
                    {
                        allTowers.Add(towers[x, y]);
                    }
                }
            }
            return allTowers;
        }

        public void SetBlockedCells(List<Vector2Int> cells)
        {
            blockedCells = cells;
            foreach (var cell in blockedCells)
            {
                if (IsValidCell(cell))
                {
                    grid[cell.x, cell.y] = CellState.Blocked;
                }
            }
        }

        // Get spawn points for WaveManager
        public Vector3[] GetSpawnPositions()
        {
            return new Vector3[]
            {
                GridToWorld(leftSpawnCell),
                GridToWorld(rightSpawnCell)
            };
        }

        public Vector3 GetGoalPosition()
        {
            return GridToWorld(goalCell);
        }

        /// <summary>
        /// Returns the viewport X coordinate (0-1) of the grid's right edge.
        /// </summary>
        public float GetGridRightEdgeViewportX()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return 1f;

            Vector3 gridRightEdge = new Vector3(gridWidth * cellSize, 0f, gridHeight * cellSize / 2f);
            Vector3 viewportPos = cam.WorldToViewportPoint(gridRightEdge);
            return viewportPos.x;
        }

        /// <summary>
        /// Converts inches to pixels using screen DPI.
        /// </summary>
        public float InchesToPixels(float inches)
        {
            float dpi = Screen.dpi > 0 ? Screen.dpi : 96f;
            return inches * dpi;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 center = GridToWorld(new Vector2Int(x, y));
                    Color color = grid[x, y] switch
                    {
                        CellState.Empty => validColor,
                        CellState.Occupied => occupiedColor,
                        CellState.Blocked => invalidColor,
                        _ => Color.white
                    };

                    Gizmos.color = color;
                    Gizmos.DrawWireCube(center, new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
                }
            }
        }
    }

    public enum CellState
    {
        Empty,
        Occupied,
        Blocked
    }
}
