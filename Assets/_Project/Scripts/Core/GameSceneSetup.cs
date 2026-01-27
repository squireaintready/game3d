using UnityEngine;
using TowerDefense.UI;

namespace TowerDefense.Core
{
    /// <summary>
    /// Auto-sets up the game scene with proper ground plane and camera settings.
    /// Add this to any GameObject in your scene - it will run before other scripts.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before other scripts
    public class GameSceneSetup : MonoBehaviour
    {
        [Header("Ground Settings (Disabled - GridManager tiles handle ground)")]
        // Ground creation disabled - keeping field for inspector reference
        #pragma warning disable 0414
        [SerializeField] private bool autoCreateGround = false;
        #pragma warning restore 0414
        [SerializeField] private float groundPadding = 5f;
        [SerializeField] private Color groundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        [SerializeField] private Material customGroundMaterial;

        [Header("Camera Settings")]
        [SerializeField] private bool fixCameraClipping = true;
        [SerializeField] private float cameraNearClip = 0.1f;
        [SerializeField] private float cameraFarClip = 200f;
        [SerializeField] private bool setupTopDownCamera = true;
        [Tooltip("Camera angle from straight down (0 = top-down, 45 = isometric, 90 = side view)")]
        [Range(0f, 90f)]
        [SerializeField] private float cameraAngle = 30f; // Slight isometric tilt
        [SerializeField] private float cameraHeight = 12f;
        [SerializeField] private bool useOrthographic = false;
        [SerializeField] private float orthographicSize = 6f;
        [Tooltip("Shift grid in window: positive = grid moves up, negative = grid moves down")]
        [SerializeField] private float verticalViewOffset = 1.28f;

        [Header("UI")]
        [SerializeField] private bool autoCreateUI = true;

        [Header("Auto Start")]
        [SerializeField] private bool autoStartGame = true;
        [SerializeField] private float startDelay = 1f;
        [SerializeField] private bool skipBuildPhase = false;

        [Header("Debug")]
        [SerializeField] private bool logSetupActions = true;
        [SerializeField] private bool updateCameraInRealtime = true;

        private void Awake()
        {
            if (fixCameraClipping)
            {
                FixCamera();
            }

            // Ground creation disabled - GridManager tiles handle visuals
            // if (autoCreateGround)
            // {
            //     CreateOrFixGround();
            // }

            if (autoCreateUI)
            {
                SetupUI();
            }

            if (autoStartGame)
            {
                Invoke(nameof(AutoStartGame), startDelay);
            }
        }

        private void Update()
        {
            // Allow real-time camera adjustment during play mode
            if (updateCameraInRealtime && setupTopDownCamera)
            {
                var cam = UnityEngine.Camera.main;
                if (cam != null)
                {
                    SetupTopDownCamera(cam);
                }
            }
        }

        private void AutoStartGame()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame(Difficulty.Normal);
                if (logSetupActions)
                {
                    Debug.Log("[GameSceneSetup] Auto-started game");
                }

                if (skipBuildPhase)
                {
                    Invoke(nameof(SkipBuildPhase), 0.1f);
                }
            }
            else
            {
                Debug.LogWarning("[GameSceneSetup] GameManager.Instance is null, cannot auto-start");
            }
        }

        private void SkipBuildPhase()
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.StartWaveEarly();
                if (logSetupActions)
                {
                    Debug.Log("[GameSceneSetup] Skipped build phase, starting wave");
                }
            }
        }

        private void SetupUI()
        {
            // Find or create Canvas
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                if (logSetupActions)
                {
                    Debug.Log("[GameSceneSetup] Created new Canvas");
                }
            }

            // Add EventSystem if not present (needed for UI interaction)
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (logSetupActions)
                {
                    Debug.Log("[GameSceneSetup] Created EventSystem");
                }
            }

            // Add GameUIAutoSetup if not already present
            if (canvas.GetComponent<GameUIAutoSetup>() == null)
            {
                canvas.gameObject.AddComponent<GameUIAutoSetup>();
                if (logSetupActions)
                {
                    Debug.Log("[GameSceneSetup] Added GameUIAutoSetup to Canvas");
                }
            }

            // Add TowerSelectionManager if not present (needed for tower click selection)
            if (TowerSelectionManager.Instance == null)
            {
                var selectionManagerObj = new GameObject("TowerSelectionManager");
                selectionManagerObj.AddComponent<TowerSelectionManager>();
                if (logSetupActions)
                {
                    Debug.Log("[GameSceneSetup] Created TowerSelectionManager");
                }
            }

            // Add TowerInfoWorldUI if not present (shows sell/upgrade buttons on selected towers)
            if (TowerInfoWorldUI.Instance == null)
            {
                var towerInfoUIObj = new GameObject("TowerInfoWorldUI");
                towerInfoUIObj.AddComponent<TowerInfoWorldUI>();
                if (logSetupActions)
                {
                    Debug.Log("[GameSceneSetup] Created TowerInfoWorldUI");
                }
            }
        }

        private void FixCamera()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                if (logSetupActions) Debug.Log("[GameSceneSetup] No main camera found");
                return;
            }

            cam.nearClipPlane = cameraNearClip;
            cam.farClipPlane = cameraFarClip;

            if (setupTopDownCamera)
            {
                SetupTopDownCamera(cam);
            }

            // Add PhysicsRaycaster for world-space UI click detection
            if (cam.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>() == null)
            {
                cam.gameObject.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
                if (logSetupActions)
                {
                    Debug.Log("[GameSceneSetup] Added PhysicsRaycaster to camera");
                }
            }

            if (logSetupActions)
            {
                Debug.Log($"[GameSceneSetup] Camera: pos={cam.transform.position}, rot={cam.transform.rotation.eulerAngles}, ortho={cam.orthographic}, near={cameraNearClip}, far={cameraFarClip}");
            }
        }

        private void SetupTopDownCamera(UnityEngine.Camera cam)
        {
            // Get grid center
            float gridCenterX = 3.5f; // Default for 7-wide grid
            float gridCenterZ = 5f;   // Default for 10-tall grid

            var gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager != null)
            {
                gridCenterX = gridManager.GridWidth * gridManager.CellSize / 2f;
                gridCenterZ = gridManager.GridHeight * gridManager.CellSize / 2f;
            }

            // Set orthographic or perspective
            cam.orthographic = useOrthographic;
            if (useOrthographic)
            {
                cam.orthographicSize = orthographicSize;
            }

            // Calculate camera position
            // For top-down: camera is above grid center, looking down at an angle
            float angleRad = cameraAngle * Mathf.Deg2Rad;

            // Camera offset from center based on angle
            // At 0 degrees (straight down), offset is 0
            // At higher angles, camera moves back (negative Z)
            float zOffset = -Mathf.Tan(angleRad) * cameraHeight;

            // Apply vertical view offset to shift grid position in view
            // Positive offset moves camera toward spawn (lower Z), pushing grid UP in view
            float viewOffsetZ = -verticalViewOffset;

            Vector3 cameraPos = new Vector3(
                gridCenterX,
                cameraHeight,
                gridCenterZ + zOffset + viewOffsetZ
            );

            // Camera rotation: look at grid center
            // X rotation = 90 - angle (90 is straight down, lower values tilt forward)
            Vector3 cameraRot = new Vector3(
                90f - cameraAngle,
                0f,
                0f
            );

            cam.transform.position = cameraPos;
            cam.transform.rotation = Quaternion.Euler(cameraRot);

            if (logSetupActions)
            {
                Debug.Log($"[GameSceneSetup] Top-down camera: angle={cameraAngle}, height={cameraHeight}, ortho={useOrthographic}");
            }
        }

        private void CreateOrFixGround()
        {
            // Find existing ground plane
            GameObject existingGround = GameObject.Find("Ground") ?? GameObject.Find("GroundPlane") ?? GameObject.Find("Plane");

            // Also check for objects tagged as ground
            if (existingGround == null)
            {
                var groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer >= 0)
                {
                    var allObjects = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
                    foreach (var obj in allObjects)
                    {
                        if (obj.gameObject.layer == groundLayer)
                        {
                            existingGround = obj.gameObject;
                            break;
                        }
                    }
                }
            }

            float width = 20f;
            float height = 25f;

            // Try to get size from GridManager
            var gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager != null)
            {
                width = gridManager.GridWidth * gridManager.CellSize + groundPadding * 2;
                height = gridManager.GridHeight * gridManager.CellSize + groundPadding * 2;
            }

            if (existingGround != null)
            {
                // Fix existing ground - scale it properly
                FixExistingGround(existingGround, width, height);
            }
            else
            {
                // Create new ground
                CreateNewGround(width, height);
            }
        }

        private void FixExistingGround(GameObject ground, float width, float height)
        {
            // Make sure it's flat on the XZ plane
            ground.transform.rotation = Quaternion.identity;
            ground.transform.position = new Vector3(width / 2 - groundPadding, 0, height / 2 - groundPadding);

            // Scale to match grid - Unity Plane is 10x10 units by default
            ground.transform.localScale = new Vector3(width / 10f, 1f, height / 10f);

            // Ensure it has a collider
            var collider = ground.GetComponent<Collider>();
            if (collider == null)
            {
                ground.AddComponent<MeshCollider>();
            }

            // Set layer
            var groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                ground.layer = groundLayer;
            }

            if (logSetupActions)
            {
                Debug.Log($"[GameSceneSetup] Fixed existing ground: {ground.name}, size={width}x{height}");
            }
        }

        private void CreateNewGround(float width, float height)
        {
            // Create a simple quad ground
            GameObject ground = new GameObject("Ground");
            ground.transform.position = Vector3.zero;

            var meshFilter = ground.AddComponent<MeshFilter>();
            var meshRenderer = ground.AddComponent<MeshRenderer>();
            var meshCollider = ground.AddComponent<MeshCollider>();

            // Create mesh
            Mesh mesh = new Mesh();
            mesh.name = "GroundMesh";

            float startX = -groundPadding;
            float startZ = -groundPadding;
            float endX = width - groundPadding;
            float endZ = height - groundPadding;

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(startX, 0, startZ),
                new Vector3(endX, 0, startZ),
                new Vector3(startX, 0, endZ),
                new Vector3(endX, 0, endZ)
            };

            int[] triangles = new int[6]
            {
                0, 2, 1,
                2, 3, 1
            };

            Vector2[] uvs = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            Vector3[] normals = new Vector3[4]
            {
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.normals = normals;

            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;

            // Setup material
            if (customGroundMaterial != null)
            {
                meshRenderer.material = customGroundMaterial;
            }
            else
            {
                var mat = CreateSafeMaterial(groundColor);
                meshRenderer.material = mat;
            }

            // Set layer
            var groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                ground.layer = groundLayer;
            }

            if (logSetupActions)
            {
                Debug.Log($"[GameSceneSetup] Created new ground plane: size={width}x{height}");
            }
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

            Debug.LogWarning("Could not find any valid shader for material creation");
            return null;
        }
    }
}
