using UnityEngine;
using UnityEngine.UI;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Displays dash charges as circles in world space above the character's head.
    /// Each circle shows a radial fill when the charge is recovering.
    /// Attach to the same GameObject that has ShtefanChargeDash.
    /// </summary>
    public class DashChargeIndicator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Dash ability to read charges from. Auto-detected if left empty.")]
        public ShtefanChargeDash DashAbility;

        [Header("Layout")]
        [Tooltip("Offset above the character pivot")]
        public Vector3 Offset = new Vector3(0f, 2.2f, 0f);

        [Tooltip("Diameter of each circle in world units")]
        public float CircleSize = 0.12f;

        [Tooltip("Spacing between circle centers in world units")]
        public float Spacing = 0.18f;

        [Header("Colors")]
        public Color AvailableColor = Color.white;
        public Color BackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);

        private Canvas _canvas;
        private Image[] _bgImages;
        private Image[] _fillImages;
        private Sprite _circleSprite;

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

            _circleSprite = CreateCircleSprite(64);
            CreateUI(DashAbility.MaxCharges);
        }

        void LateUpdate()
        {
            if (DashAbility == null) return;

            // Billboard toward camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                _canvas.transform.rotation = cam.transform.rotation;
            }

            // Update circles based on sequential recovery
            int available = DashAbility.CurrentCharges;
            int maxCharges = DashAbility.MaxCharges;
            float recoveryTimer = DashAbility.CurrentRecoveryTimer;
            float recoveryTime = DashAbility.ChargeRecoveryTime;

            for (int i = 0; i < _fillImages.Length; i++)
            {
                if (i < available)
                {
                    // Charge is available — full circle
                    _fillImages[i].fillAmount = 1f;
                }
                else if (i == available && recoveryTimer > 0f)
                {
                    // This is the charge currently recovering — show progress
                    _fillImages[i].fillAmount = 1f - recoveryTimer / recoveryTime;
                }
                else
                {
                    // Queued for recovery — empty
                    _fillImages[i].fillAmount = 0f;
                }
            }
        }

        // ── UI Construction ─────────────────────────────────────────────────

        private void CreateUI(int chargeCount)
        {
            // World-space canvas
            var canvasObj = new GameObject("DashChargeCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.localPosition = Offset;

            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = 100;

            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.dynamicPixelsPerUnit = 100f;

            var rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 50f);
            rt.localScale = Vector3.one * 0.005f;

            _bgImages = new Image[chargeCount];
            _fillImages = new Image[chargeCount];

            float totalWidth = (chargeCount - 1) * Spacing / 0.005f; // in canvas units
            float startX = -totalWidth / 2f;
            float sizeInCanvas = CircleSize / 0.005f; // circle size in canvas units

            for (int i = 0; i < chargeCount; i++)
            {
                // Background circle
                var bgObj = new GameObject($"Charge_BG_{i}");
                bgObj.transform.SetParent(canvasObj.transform, false);

                var bgImage = bgObj.AddComponent<Image>();
                bgImage.sprite = _circleSprite;
                bgImage.color = BackgroundColor;

                var bgRt = bgObj.GetComponent<RectTransform>();
                bgRt.anchoredPosition = new Vector2(startX + i * (Spacing / 0.005f), 0f);
                bgRt.sizeDelta = Vector2.one * sizeInCanvas;

                _bgImages[i] = bgImage;

                // Fill circle (child, radial fill)
                var fillObj = new GameObject($"Charge_Fill_{i}");
                fillObj.transform.SetParent(bgObj.transform, false);

                var fillImage = fillObj.AddComponent<Image>();
                fillImage.sprite = _circleSprite;
                fillImage.color = AvailableColor;
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Radial360;
                fillImage.fillOrigin = (int)Image.Origin360.Top;
                fillImage.fillClockwise = true;
                fillImage.fillAmount = 1f;

                var fillRt = fillObj.GetComponent<RectTransform>();
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;

                _fillImages[i] = fillImage;
            }
        }

        // ── Circle sprite generation ────────────────────────────────────────

        private static Sprite CreateCircleSprite(int resolution)
        {
            var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float center = resolution * 0.5f;
            float radius = center - 1f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(
                        new Vector2(x + 0.5f, y + 0.5f),
                        new Vector2(center, center));

                    if (dist <= radius)
                        tex.SetPixel(x, y, Color.white);
                    else if (dist <= radius + 1f)
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius + 1f - dist)));
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }

            tex.Apply();

            return Sprite.Create(
                tex,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                resolution);
        }
    }
}
