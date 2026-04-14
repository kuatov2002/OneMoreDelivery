using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Watches the enemy's Health and enters "Desperation" mode when HP drops
    /// below a threshold. In this mode:
    ///   • attack cooldown shortens (more aggressive pacing)
    ///   • telegraph duration shortens (tighter dodge window, still readable)
    ///   • stalk speed increases (more intimidating circling)
    ///   • optional emissive pulse on the body renderer (visual tell)
    ///
    /// The goal is NOT to raise overall difficulty — a full-HP duelist is already
    /// the balance point. Desperation makes the last ~30% of the fight feel like
    /// a climax instead of a formality.
    ///
    /// Original values are cached on Awake and restored on death/disable so that
    /// a pooled prefab behaves correctly on respawn.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Melee Enemy/Duelist Desperation")]
    public class DuelistDesperation : MonoBehaviour
    {
        [Header("Threshold")]
        [Tooltip("HP fraction below which desperation kicks in (0-1). Default = 0.30 (30%).")]
        [Range(0.05f, 0.9f)]
        public float HealthThreshold = 0.30f;

        [Header("Tunable Overrides")]
        [Tooltip("Multiplier applied to MeleeAttackCooldown.Cooldown. 0.6 = 40% faster attacks.")]
        [Range(0.3f, 1f)]
        public float CooldownMultiplier = 0.6f;

        [Tooltip("Multiplier applied to AIActionTelegraph telegraph duration. 0.75 = 25% faster windup.")]
        [Range(0.4f, 1f)]
        public float TelegraphMultiplier = 0.75f;

        [Tooltip("Multiplier applied to AIActionStalk BaseSpeedFactor. 1.15 = 15% faster circling.")]
        [Range(1f, 2f)]
        public float StalkSpeedMultiplier = 1.15f;

        [Header("Visual Pulse (optional)")]
        [Tooltip("Renderers to tint with emissive pulse. Leave empty to skip visual.")]
        [SerializeField] private Renderer[] _pulseRenderers;

        [Tooltip("Emissive color pulsed on when desperate")]
        [SerializeField] private Color _desperateEmission = new Color(0.9f, 0.05f, 0.02f, 1f);

        [Tooltip("Pulse frequency (Hz)")]
        [SerializeField] private float _pulseFrequency = 2f;

        [Tooltip("Peak emission intensity multiplier during pulse")]
        [SerializeField] private float _pulsePeak = 2.5f;

        // ── dependencies (resolved in Awake/OnEnable) ─────────────────────────
        private Health _health;
        private MeleeAttackCooldown _cooldown;
        private AIActionStalk _stalk;
        private AIAction[] _actions;

        // ── cached originals ──────────────────────────────────────────────────
        private float _origCooldown;
        private float _origStalkSpeed;
        // Telegraph duration is private — we mirror the adjustment into a public property we add below
        // via reflection, falling back to no-op if the field shape changes.
        private float[] _origTelegraphDurations;
        private AIActionTelegraph[] _telegraphs;

        private bool _isDesperate;

        private MaterialPropertyBlock _mpb;
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

        public bool IsDesperate => _isDesperate;

        protected virtual void Awake()
        {
            _health = GetComponentInParent<Health>();
            _cooldown = GetComponentInParent<MeleeAttackCooldown>();

            // Find every AIAction in children (AIBrain puts actions on child GameObject typically)
            _actions = GetComponentsInChildren<AIAction>(true);

            _stalk = GetComponentInChildren<AIActionStalk>(true);
            _telegraphs = GetComponentsInChildren<AIActionTelegraph>(true);

            CacheOriginals();

            if (_pulseRenderers != null && _pulseRenderers.Length > 0)
                _mpb = new MaterialPropertyBlock();
        }

        protected virtual void OnEnable()
        {
            _isDesperate = false;
            RestoreOriginals();
            ApplyEmission(Color.black);
        }

        protected virtual void OnDisable()
        {
            _isDesperate = false;
            RestoreOriginals();
            ApplyEmission(Color.black);
        }

        protected virtual void Update()
        {
            if (_health == null || _health.MaximumHealth <= 0f) return;

            float hpFraction = _health.CurrentHealth / _health.MaximumHealth;
            bool shouldBeDesperate = hpFraction <= HealthThreshold && _health.CurrentHealth > 0f;

            if (shouldBeDesperate && !_isDesperate)
                EnterDesperation();
            else if (!shouldBeDesperate && _isDesperate)
                ExitDesperation();

            if (_isDesperate)
                PulseEmission();
        }

        private void CacheOriginals()
        {
            if (_cooldown != null)
                _origCooldown = _cooldown.Cooldown;

            if (_stalk != null)
                _origStalkSpeed = _stalk.BaseSpeedFactor;

            if (_telegraphs != null)
            {
                _origTelegraphDurations = new float[_telegraphs.Length];
                for (int i = 0; i < _telegraphs.Length; i++)
                {
                    var telegraph = _telegraphs[i];
                    if (telegraph == null) continue;
                    _origTelegraphDurations[i] = GetTelegraphDuration(telegraph);
                }
            }
        }

        private void EnterDesperation()
        {
            _isDesperate = true;

            if (_cooldown != null)
                _cooldown.Cooldown = _origCooldown * CooldownMultiplier;

            if (_stalk != null)
                _stalk.BaseSpeedFactor = Mathf.Clamp01(_origStalkSpeed * StalkSpeedMultiplier);

            if (_telegraphs != null)
            {
                for (int i = 0; i < _telegraphs.Length; i++)
                {
                    var telegraph = _telegraphs[i];
                    if (telegraph == null) continue;
                    SetTelegraphDuration(telegraph, _origTelegraphDurations[i] * TelegraphMultiplier);
                }
            }
        }

        private void ExitDesperation()
        {
            _isDesperate = false;
            RestoreOriginals();
            ApplyEmission(Color.black);
        }

        private void RestoreOriginals()
        {
            if (_cooldown != null && _origCooldown > 0f)
                _cooldown.Cooldown = _origCooldown;

            if (_stalk != null && _origStalkSpeed > 0f)
                _stalk.BaseSpeedFactor = _origStalkSpeed;

            if (_telegraphs != null && _origTelegraphDurations != null)
            {
                int len = Mathf.Min(_telegraphs.Length, _origTelegraphDurations.Length);
                for (int i = 0; i < len; i++)
                {
                    var telegraph = _telegraphs[i];
                    if (telegraph == null) continue;
                    if (_origTelegraphDurations[i] > 0f)
                        SetTelegraphDuration(telegraph, _origTelegraphDurations[i]);
                }
            }
        }

        private void PulseEmission()
        {
            if (_pulseRenderers == null || _pulseRenderers.Length == 0 || _mpb == null) return;

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * _pulseFrequency * Mathf.PI * 2f);
            Color c = _desperateEmission * pulse * _pulsePeak;
            ApplyEmission(c);
        }

        private void ApplyEmission(Color emission)
        {
            if (_pulseRenderers == null || _pulseRenderers.Length == 0 || _mpb == null) return;

            foreach (var r in _pulseRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionColorID, emission);
                r.SetPropertyBlock(_mpb);
            }
        }

        // ── Telegraph duration access via reflection ─────────────────────────
        // AIActionTelegraph._telegraphDuration is private. Rather than patching that
        // file (which could conflict with future Duelist iterations), we access it
        // via reflection. If the field is renamed, desperation silently skips it.

        private static readonly System.Reflection.FieldInfo _telegraphDurationField =
            typeof(AIActionTelegraph).GetField(
                "_telegraphDuration",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        private static float GetTelegraphDuration(AIActionTelegraph telegraph)
        {
            if (_telegraphDurationField == null) return 0f;
            object value = _telegraphDurationField.GetValue(telegraph);
            return value is float f ? f : 0f;
        }

        private static void SetTelegraphDuration(AIActionTelegraph telegraph, float newDuration)
        {
            if (_telegraphDurationField == null) return;
            _telegraphDurationField.SetValue(telegraph, newDuration);
        }
    }
}
