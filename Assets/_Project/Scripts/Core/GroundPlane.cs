using UnityEngine;
using Unity.AI.Navigation;

namespace TowerDefense.Core
{
    /// <summary>
    /// Creates and manages the ground plane to match the grid.
    /// Attach this to a GameObject in your scene - it will auto-generate the ground mesh.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class GroundPlane : MonoBehaviour
    {
        [Header("Size Settings")]
        [SerializeField] private bool useGridManagerSize = true;
        [SerializeField] private float width = 10f;
        [SerializeField] private float height = 15f;
        [SerializeField] private float padding = 2f;  // Extra space around the grid

        [Header("Visual Settings")]
        [SerializeField] private bool hideVisual = true; // Hide since tile sprites handle visuals
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Color groundColor = Color.white;

        [Header("Navigation")]
        [SerializeField] private bool bakeNavMesh = true;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private NavMeshSurface navMeshSurface;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            // Force hide - tile sprites handle all visuals now
            hideVisual = true;
            CreateGroundPlane();
        }

        private void CreateGroundPlane()
        {
            float w = width;
            float h = height;

            // Get size from GridManager if available
            if (useGridManagerSize && GridManager.Instance != null)
            {
                w = GridManager.Instance.GridWidth * GridManager.Instance.CellSize;
                h = GridManager.Instance.GridHeight * GridManager.Instance.CellSize;
            }

            // Add padding
            float totalWidth = w + padding * 2;
            float totalHeight = h + padding * 2;

            // Create mesh
            Mesh mesh = new Mesh();
            mesh.name = "GroundPlaneMesh";

            // Vertices - centered on grid with padding
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(-padding, 0, -padding);
            vertices[1] = new Vector3(w + padding, 0, -padding);
            vertices[2] = new Vector3(-padding, 0, h + padding);
            vertices[3] = new Vector3(w + padding, 0, h + padding);

            // Triangles
            int[] triangles = new int[6]
            {
                0, 2, 1,
                2, 3, 1
            };

            // UVs
            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(1, 0);
            uvs[2] = new Vector2(0, 1);
            uvs[3] = new Vector2(1, 1);

            // Normals (pointing up)
            Vector3[] normals = new Vector3[4];
            normals[0] = Vector3.up;
            normals[1] = Vector3.up;
            normals[2] = Vector3.up;
            normals[3] = Vector3.up;

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.normals = normals;

            meshFilter.mesh = mesh;

            // Setup material
            // Hide renderer if using tile sprites for visuals
            if (hideVisual)
            {
                meshRenderer.enabled = false;
            }
            else if (groundMaterial != null)
            {
                meshRenderer.material = groundMaterial;
            }
            else
            {
                // Create a simple unlit material
                Material mat = CreateSafeMaterial(groundColor);
                meshRenderer.material = mat;
            }

            // Add collider for raycasting (tower placement, etc.)
            var existingCollider = GetComponent<MeshCollider>();
            if (existingCollider == null)
            {
                var collider = gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }
            else
            {
                existingCollider.sharedMesh = mesh;
            }

            // Set layer for ground detection
            int groundLayer = LayerMask.NameToLayer("Ground");
            gameObject.layer = groundLayer >= 0 ? groundLayer : 0; // Default layer if Ground doesn't exist

            // Setup NavMesh for pathfinding
            if (bakeNavMesh)
            {
                SetupNavMesh();
            }
        }

        private void SetupNavMesh()
        {
            navMeshSurface = GetComponent<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
            }

            // Use default settings and bake the NavMesh
            navMeshSurface.BuildNavMesh();
            Debug.Log("[GroundPlane] NavMesh baked successfully");
        }

        // Rebake NavMesh (call after placing/removing towers)
        public void RebakeNavMesh()
        {
            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
            }
        }

        // Call this to regenerate the ground after grid changes
        public void Regenerate()
        {
            CreateGroundPlane();
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

        private void OnDrawGizmos()
        {
            float w = width;
            float h = height;

            if (Application.isPlaying && useGridManagerSize && GridManager.Instance != null)
            {
                w = GridManager.Instance.GridWidth * GridManager.Instance.CellSize;
                h = GridManager.Instance.GridHeight * GridManager.Instance.CellSize;
            }

            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            Vector3 center = transform.position + new Vector3(w / 2, 0, h / 2);
            Vector3 size = new Vector3(w + padding * 2, 0.01f, h + padding * 2);
            Gizmos.DrawCube(center, size);
        }
    }
}
