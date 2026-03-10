using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Glowing sphere VFX on the enemy's hand that intensifies during the telegraph phase.
    /// Combines: emissive sphere mesh + point light + spark particles.
    /// Attach this to the hand bone (e.g. mmntnsrig:RightHand) or WeaponAttachment.
    /// </summary>
    public class AttackTelegraphGlow : MonoBehaviour
    {
        [Header("Glow Sphere")]
        [Tooltip("Material using Custom/TelegraphGlow shader (will be instanced)")]
        [SerializeField] private Material _glowMaterial;

        [Tooltip("Start scale of the glow sphere")]
        [SerializeField] private float _minScale = 0.05f;

        [Tooltip("Max scale of the glow sphere at full charge")]
        [SerializeField] private float _maxScale = 0.35f;

        [Header("Light")]
        [SerializeField] private Color _lightColor = new Color(1f, 0.25f, 0.05f, 1f);

        [Tooltip("Max light intensity at full charge")]
        [SerializeField] private float _maxLightIntensity = 3f;

        [Tooltip("Max light range at full charge")]
        [SerializeField] private float _maxLightRange = 4f;

        [Header("Sparks")]
        [Tooltip("Color of spark particles")]
        [SerializeField] private Color _sparkColor = new Color(1f, 0.4f, 0.1f, 1f);

        [SerializeField] private int _maxParticles = 20;

        [Header("Timing")]
        [Tooltip("Easing curve for the glow intensity (X = telegraph progress, Y = glow intensity)")]
        [SerializeField] private AnimationCurve _intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // --- Runtime ---
        private GameObject _sphereObj;
        private MeshRenderer _sphereRenderer;
        private Material _instanceMat;
        private Light _pointLight;
        private ParticleSystem _sparkSystem;
        private float _progress;

        private static readonly int PropIntensity = Shader.PropertyToID("_Intensity");

        /// <summary>
        /// Current charge progress 0..1. Drive this from AIActionTelegraph.
        /// </summary>
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

        /// <summary>
        /// Show and begin charging.
        /// </summary>
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

        /// <summary>
        /// Hide and reset.
        /// </summary>
        public void Hide()
        {
            if (_sparkSystem != null)
                _sparkSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            SetActive(false);
            _progress = 0f;
        }

        private void SetActive(bool on)
        {
            if (_sphereObj != null) _sphereObj.SetActive(on);
            if (_pointLight != null) _pointLight.enabled = on;
        }

        private void ApplyProgress()
        {
            float t = _intensityCurve.Evaluate(_progress);

            // Scale sphere
            if (_sphereObj != null)
            {
                float s = Mathf.Lerp(_minScale, _maxScale, t);
                _sphereObj.transform.localScale = Vector3.one * s;
            }

            // Shader intensity
            if (_instanceMat != null)
                _instanceMat.SetFloat(PropIntensity, t * 2f);

            // Light
            if (_pointLight != null)
            {
                _pointLight.intensity = _maxLightIntensity * t;
                _pointLight.range = Mathf.Lerp(0.5f, _maxLightRange, t);
            }

            // Particle emission rate scales with progress
            if (_sparkSystem != null)
            {
                var emission = _sparkSystem.emission;
                emission.rateOverTime = _maxParticles * t;
            }
        }

        // ─────────── Build everything via code ───────────

        private void BuildEffect()
        {
            BuildSphere();
            BuildLight();
            BuildSparks();
        }

        private void BuildSphere()
        {
            _sphereObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _sphereObj.name = "TelegraphGlowSphere";
            _sphereObj.transform.SetParent(transform, false);
            _sphereObj.transform.localPosition = Vector3.zero;
            _sphereObj.transform.localScale = Vector3.one * _minScale;

            // Remove collider — we only need the visual
            var col = _sphereObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            _sphereRenderer = _sphereObj.GetComponent<MeshRenderer>();
            _sphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _sphereRenderer.receiveShadows = false;

            if (_glowMaterial != null)
            {
                _instanceMat = new Material(_glowMaterial);
                _sphereRenderer.material = _instanceMat;
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

            // Stop auto-play so we control it
            _sparkSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _sparkSystem.main;
            main.startLifetime = 0.4f;
            main.startSpeed = 0.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = _sparkColor;
            main.maxParticles = _maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.3f; // float upward
            main.loop = true;
            main.playOnAwake = false;

            var emission = _sparkSystem.emission;
            emission.rateOverTime = 0f;
            emission.enabled = true;

            var shape = _sparkSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;

            // Color over lifetime: fade out
            var colorOverLifetime = _sparkSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(_sparkColor, 0f),
                    new GradientColorKey(_sparkColor, 0.5f),
                    new GradientColorKey(_sparkColor * 0.5f, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = grad;

            // Size over lifetime: shrink
            var sizeOverLifetime = _sparkSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            // Renderer — use default particle material
            var renderer = sparkObj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.SetColor("_Color", _sparkColor);
            renderer.material.SetFloat("_Mode", 1); // Additive
        }

        private void OnDestroy()
        {
            if (_instanceMat != null) Destroy(_instanceMat);
        }
    }
}
