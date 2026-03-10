using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Ground-projected attack telegraph indicator (sector/circle).
    /// Attach to a child GameObject with a quad mesh.
    /// Drives a Custom/AttackTelegraph shader to show a filling danger zone.
    /// </summary>
    public class AttackTelegraphCircle : MonoBehaviour
    {
        [Header("Visuals")]
        [Tooltip("Material using Custom/AttackTelegraph shader (will be instanced at runtime)")]
        [SerializeField] private Material _telegraphMaterial;

        [Header("Colors")]
        [SerializeField] private Color _edgeColor = new Color(1f, 0.2f, 0.1f, 0.35f);
        [SerializeField] private Color _fillColor = new Color(1f, 0.1f, 0.0f, 0.55f);

        [Header("Settings")]
        [SerializeField] private float _groundOffset = 0.05f;

        private Material _instanceMat;
        private MeshRenderer _renderer;
        private MeshFilter _meshFilter;

        private static readonly int PropFillProgress = Shader.PropertyToID("_FillProgress");
        private static readonly int PropArc = Shader.PropertyToID("_Arc");
        private static readonly int PropDirection = Shader.PropertyToID("_Direction");
        private static readonly int PropColor = Shader.PropertyToID("_Color");
        private static readonly int PropFillColor = Shader.PropertyToID("_FillColor");

        /// <summary>Current fill progress 0..1</summary>
        public float FillProgress
        {
            get => _fillProgress;
            set
            {
                _fillProgress = Mathf.Clamp01(value);
                if (_instanceMat != null)
                    _instanceMat.SetFloat(PropFillProgress, _fillProgress);
            }
        }
        private float _fillProgress;

        /// <summary>Arc angle in degrees</summary>
        public float Arc
        {
            get => _arc;
            set
            {
                _arc = value;
                if (_instanceMat != null)
                    _instanceMat.SetFloat(PropArc, _arc);
            }
        }
        private float _arc = 60f;

        /// <summary>Direction angle in degrees (world Y rotation)</summary>
        public float Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                if (_instanceMat != null)
                    _instanceMat.SetFloat(PropDirection, _direction);
            }
        }
        private float _direction;

        /// <summary>Attack range — sets quad scale</summary>
        public float Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                UpdateScale();
            }
        }
        private float _radius = 2.5f;

        private void Awake()
        {
            EnsureComponents();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Show the telegraph, reset fill to 0.
        /// Call this from AIActionTelegraph.OnEnterState.
        /// </summary>
        public void Show(float arcAngle, float radius, Vector3 worldDirection)
        {
            EnsureComponents();
            gameObject.SetActive(true);

            Arc = arcAngle;
            Radius = radius;
            FillProgress = 0f;

            // Convert world direction to angle
            Direction = Mathf.Atan2(worldDirection.x, worldDirection.z) * Mathf.Rad2Deg;

            // Position flat on the ground under the enemy
            Transform parent = transform.parent;
            if (parent != null)
            {
                transform.position = parent.position + Vector3.up * _groundOffset;
            }

            // Keep quad flat (rotated to face up)
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            _instanceMat.SetColor(PropColor, _edgeColor);
            _instanceMat.SetColor(PropFillColor, _fillColor);
        }

        /// <summary>
        /// Hide the telegraph.
        /// Call this from AIActionTelegraph.OnExitState.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            FillProgress = 0f;
        }

        private void EnsureComponents()
        {
            if (_renderer != null && _instanceMat != null) return;

            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null)
                _meshFilter = gameObject.AddComponent<MeshFilter>();

            if (_meshFilter.sharedMesh == null)
                _meshFilter.sharedMesh = CreateQuadMesh();

            _renderer = GetComponent<MeshRenderer>();
            if (_renderer == null)
                _renderer = gameObject.AddComponent<MeshRenderer>();

            if (_telegraphMaterial != null)
            {
                _instanceMat = new Material(_telegraphMaterial);
                _renderer.material = _instanceMat;
            }

            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;

            UpdateScale();
        }

        private void UpdateScale()
        {
            float diameter = _radius * 2f;
            transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        private static Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh { name = "TelegraphQuad" };

            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f)
            };

            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };

            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.normals = new Vector3[]
            {
                Vector3.back, Vector3.back, Vector3.back, Vector3.back
            };

            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (_instanceMat != null)
                Destroy(_instanceMat);
        }
    }
}
