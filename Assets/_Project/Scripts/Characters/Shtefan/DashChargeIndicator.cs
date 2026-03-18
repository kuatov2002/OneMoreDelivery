using UnityEngine;
using UnityEngine.UI;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Displays dash charges as shader-driven circles in world space above the character.
    /// Each circle has a thick outline that makes charge state immediately obvious.
    /// Glow and outline appear 0.05s before the charge is actually ready.
    /// </summary>
    public class DashChargeIndicator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Dash ability to read charges from. Auto-detected if left empty.")]
        public ShtefanChargeDash DashAbility;

        [Header("Layout")]
        [Tooltip("Offset above the character pivot")]
        public Vector3 Offset = new Vector3(0f, 2.2f, 0f);

        [Tooltip("Size of each circle in world units")]
        public float CircleSize = 0.18f;

        [Tooltip("Spacing between circle centers in world units")]
        public float Spacing = 0.24f;

        [Header("Shader Settings")]
        public Color OutlineColor = new Color(0.85f, 0.92f, 1f, 1f);
        public Color FillColor = new Color(0.7f, 0.85f, 1f, 0.85f);
        public Color EmptyFillColor = new Color(0.1f, 0.1f, 0.12f, 0.45f);

        [Range(0.01f, 0.2f)]
        public float OutlineWidth = 0.07f;

        [Range(0f, 0.2f)]
        public float GlowWidth = 0.1f;

        [Range(0f, 5f)]
        public float GlowIntensity = 2.5f;

        [Range(0f, 10f)]
        public float PulseSpeed = 3f;

        [Range(0f, 0.5f)]
        public float PulseAmount = 0.2f;

        [Header("Timing")]
        [Tooltip("Glow/outline appears this many seconds before charge is ready")]
        public float GlowAnticipation = 0.05f;

        private Canvas _canvas;
        private Material[] _materials;
        private Shader _shader;

        private static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");
        private static readonly int ChargedID = Shader.PropertyToID("_Charged");
        private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
        private static readonly int FillColorID = Shader.PropertyToID("_FillColor");
        private static readonly int EmptyFillColorID = Shader.PropertyToID("_EmptyFillColor");
        private static readonly int OutlineWidthID = Shader.PropertyToID("_OutlineWidth");
        private static readonly int GlowWidthID = Shader.PropertyToID("_GlowWidth");
        private static readonly int GlowIntensityID = Shader.PropertyToID("_GlowIntensity");
        private static readonly int PulseSpeedID = Shader.PropertyToID("_PulseSpeed");
        private static readonly int PulseAmountID = Shader.PropertyToID("_PulseAmount");

        void Start()
        {
            if (DashAbility == null)
                DashAbility = GetComponent<ShtefanChargeDash>();

            if (DashAbility == null)
            {
                Debug.LogWarning("DashChargeIndicator: no ShtefanChargeDash found.", this);
                enabled = false;
                return;
            }

            _shader = Shader.Find("UI/DashCharge");
            if (_shader == null)
            {
                Debug.LogError("DashChargeIndicator: shader 'UI/DashCharge' not found!", this);
                enabled = false;
                return;
            }

            CreateUI(DashAbility.MaxCharges);
        }

        void LateUpdate()
        {
            if (DashAbility == null || _materials == null) return;

            Camera cam = Camera.main;
            if (cam != null)
            {
                _canvas.transform.rotation = cam.transform.rotation;
            }

            int available = DashAbility.CurrentCharges;
            float recoveryTimer = DashAbility.CurrentRecoveryTimer;
            float recoveryTime = DashAbility.ChargeRecoveryTime;

            for (int i = 0; i < _materials.Length; i++)
            {
                Material mat = _materials[i];

                if (i < available)
                {
                    mat.SetFloat(FillAmountID, 1f);
                    mat.SetFloat(ChargedID, 1f);
                }
                else if (i == available && recoveryTimer > 0f)
                {
                    float progress = 1f - recoveryTimer / recoveryTime;
                    mat.SetFloat(FillAmountID, progress);
                    // Glow/outline snap on 0.05s before ready
                    mat.SetFloat(ChargedID, recoveryTimer <= GlowAnticipation ? 1f : 0f);
                }
                else
                {
                    mat.SetFloat(FillAmountID, 0f);
                    mat.SetFloat(ChargedID, 0f);
                }
            }
        }

        void OnDestroy()
        {
            if (_materials != null)
            {
                foreach (var mat in _materials)
                {
                    if (mat != null) Destroy(mat);
                }
            }
        }

        private void CreateUI(int chargeCount)
        {
            var canvasObj = new GameObject("DashChargeCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.localPosition = Offset;

            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = 100;

            canvasObj.AddComponent<CanvasScaler>();

            var rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 50f);
            rt.localScale = Vector3.one * 0.005f;

            _materials = new Material[chargeCount];

            float canvasScale = 0.005f;
            float totalWidth = (chargeCount - 1) * Spacing / canvasScale;
            float startX = -totalWidth / 2f;
            float sizeInCanvas = CircleSize / canvasScale;

            float glowPadding = GlowWidth / canvasScale * 2f;
            float quadSize = sizeInCanvas + glowPadding;

            for (int i = 0; i < chargeCount; i++)
            {
                var circleObj = new GameObject($"Charge_{i}");
                circleObj.transform.SetParent(canvasObj.transform, false);

                var mat = new Material(_shader);
                mat.SetColor(OutlineColorID, OutlineColor);
                mat.SetColor(FillColorID, FillColor);
                mat.SetColor(EmptyFillColorID, EmptyFillColor);
                mat.SetFloat(OutlineWidthID, OutlineWidth);
                mat.SetFloat(GlowWidthID, GlowWidth);
                mat.SetFloat(GlowIntensityID, GlowIntensity);
                mat.SetFloat(PulseSpeedID, PulseSpeed);
                mat.SetFloat(PulseAmountID, PulseAmount);
                mat.SetFloat(FillAmountID, 1f);
                mat.SetFloat(ChargedID, 1f);

                _materials[i] = mat;

                var rawImage = circleObj.AddComponent<RawImage>();
                rawImage.material = mat;
                rawImage.color = Color.white;

                var circleRt = circleObj.GetComponent<RectTransform>();
                circleRt.anchoredPosition = new Vector2(startX + i * (Spacing / canvasScale), 0f);
                circleRt.sizeDelta = Vector2.one * quadSize;
            }
        }
    }
}
