using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// CotDG-style telegraph: a billboard star glow on the enemy's hand/weapon
    /// that grows in size and intensity during the telegraph phase.
    /// Combines: billboard quad with star shader + point light + spark particles.
    /// Attach to a hand bone or weapon attachment point.
    /// </summary>
    public class AttackTelegraphGlow : MonoBehaviour
    {
        [Header("Star Effect")]
        [Tooltip("Material using Custom/AttackTelegraph shader (will be instanced)")]
        [SerializeField] private Material _starMaterial;

        [Tooltip("Min size of the star quad at progress 0")]
        [SerializeField] private float _minSize = 0.4f;

        [Tooltip("Max size of the star quad at full charge")]
        [SerializeField] private float _maxSize = 2.0f;

        [Header("Light")]
        [SerializeField] private Color _lightColor = new Color(1f, 0.45f, 0.05f, 1f);
        [SerializeField] private float _maxLightIntensity = 3.5f;
        [SerializeField] private float _maxLightRange = 5f;

        [Header("Sparks")]
        [SerializeField] private Color _sparkColor = new Color(1f, 0.5f, 0.1f, 1f);
        [SerializeField] private int _maxParticles = 25;

        [Header("Timing")]
        [SerializeField] private AnimationCurve _intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private GameObject _quadObj;
        private MeshRenderer _quadRenderer;
        private Material _instanceMat;
        private Light _pointLight;
        private ParticleSystem _sparkSystem;
        private float _progress;

        private static readonly int PropFillProgress = Shader.PropertyToID("_FillProgress");
        private static readonly int PropIntensity = Shader.PropertyToID("_Intensity");

        public float Progress
        {
            get => _progress;
            set
            {
                _progress = Mathf.Clamp01(value);
                ApplyProgress();
            }
        }

        private void Awake()
        {
            BuildEffect();
            SetActive(false);
        }

        public void Show()
        {
            SetActive(true);
            Progress = 0f;

            if (_sparkSystem != null)
            {
                _sparkSystem.Clear();
                _sparkSystem.Play();
            }
        }

        public void Hide()
        {
            if (_sparkSystem != null)
                _sparkSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            SetActive(false);
            _progress = 0f;
        }

        private void SetActive(bool on)
        {
            if (_quadObj != null) _quadObj.SetActive(on);
            if (_pointLight != null) _pointLight.enabled = on;
        }

        private void ApplyProgress()
        {
            float t = _intensityCurve.Evaluate(_progress);

            // Scale the billboard quad
            if (_quadObj != null)
            {
                float size = Mathf.Lerp(_minSize, _maxSize, t);
                _quadObj.transform.localScale = Vector3.one * size;
            }

            // Drive shader progress and intensity
            if (_instanceMat != null)
            {
                _instanceMat.SetFloat(PropFillProgress, t);
                _instanceMat.SetFloat(PropIntensity, Mathf.Lerp(0.8f, 3.0f, t));
            }

            // Light
            if (_pointLight != null)
            {
                _pointLight.intensity = _maxLightIntensity * t;
                _pointLight.range = Mathf.Lerp(0.5f, _maxLightRange, t);
            }

            // Particles
            if (_sparkSystem != null)
            {
                var emission = _sparkSystem.emission;
                emission.rateOverTime = _maxParticles * t;
            }
        }

        private void BuildEffect()
        {
            BuildQuad();
            BuildLight();
            BuildSparks();
        }

        private void BuildQuad()
        {
            _quadObj = new GameObject("TelegraphStarQuad");
            _quadObj.transform.SetParent(transform, false);
            _quadObj.transform.localPosition = Vector3.zero;
            _quadObj.transform.localScale = Vector3.one * _minSize;

            var meshFilter = _quadObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateQuadMesh();

            _quadRenderer = _quadObj.AddComponent<MeshRenderer>();
            _quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _quadRenderer.receiveShadows = false;

            if (_starMaterial != null)
            {
                _instanceMat = new Material(_starMaterial);
                _quadRenderer.material = _instanceMat;
            }
        }

        private void BuildLight()
        {
            var lightObj = new GameObject("TelegraphPointLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = Vector3.zero;

            _pointLight = lightObj.AddComponent<Light>();
            _pointLight.type = LightType.Point;
            _pointLight.color = _lightColor;
            _pointLight.intensity = 0f;
            _pointLight.range = 0.5f;
            _pointLight.renderMode = LightRenderMode.Auto;
        }

        private void BuildSparks()
        {
            var sparkObj = new GameObject("TelegraphSparks");
            sparkObj.transform.SetParent(transform, false);
            sparkObj.transform.localPosition = Vector3.zero;

            _sparkSystem = sparkObj.AddComponent<ParticleSystem>();
            _sparkSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _sparkSystem.main;
            main.startLifetime = 0.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startColor = _sparkColor;
            main.maxParticles = _maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.4f;
            main.loop = true;
            main.playOnAwake = false;

            var emission = _sparkSystem.emission;
            emission.rateOverTime = 0f;
            emission.enabled = true;

            var shape = _sparkSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var colorOverLifetime = _sparkSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(_sparkColor, 0f),
                    new GradientColorKey(_sparkColor, 0.4f),
                    new GradientColorKey(_sparkColor * 0.3f, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.7f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = grad;

            var sizeOverLifetime = _sparkSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var renderer = sparkObj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.SetColor("_Color", _sparkColor);
            renderer.material.SetFloat("_Mode", 1);
        }

        private static Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh { name = "TelegraphStarQuad" };

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
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward
            };

            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (_instanceMat != null) Destroy(_instanceMat);
        }
    }
}
