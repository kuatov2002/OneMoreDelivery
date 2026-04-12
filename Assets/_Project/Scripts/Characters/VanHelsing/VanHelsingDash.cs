using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Van Helsing's dash ability.
    ///
    /// Extends ShtefanChargeDash with two extra effects:
    ///   • Afterimage trail  — during the dash, ghost copies of the character
    ///     are spawned at sampled positions and fade out.
    ///   • Landing AoE       — on dash end, a Physics.OverlapSphere damages all
    ///     enemies within LandingAoERadius for LandingAoEDamage.
    ///
    /// Van Helsing always uses a single dash charge (MaxCharges is forced to 1
    /// in Initialization so the inspector value is ignored at runtime).
    /// ChargeRecoveryTime defaults to 1.2 s (per GDD).
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/Abilities/Van Helsing Dash")]
    public class VanHelsingDash : ShtefanChargeDash
    {
        // ── Landing AoE ─────────────────────────────────────────────────────

        [Header("Landing AoE")]
        [Tooltip("Radius of the damage zone spawned on dash landing")]
        public float LandingAoERadius = 2.5f;

        [Tooltip("Damage dealt to enemies caught in the landing zone")]
        public float LandingAoEDamage = 40f;

        [Tooltip("Hit-flash duration passed to enemy Health.Damage()")]
        public float LandingFlickerDuration = 0.1f;

        [Tooltip("Invincibility window applied to enemies after the AoE hit")]
        public float LandingInvincibilityDuration = 0.05f;

        [Tooltip("Layers to query for AoE targets")]
        public LayerMask LandingDamageLayerMask = 1 << 13; // Enemies

        // ── Afterimage ──────────────────────────────────────────────────────

        [Header("Afterimage")]
        [Tooltip("Optional ghost prefab to spawn as afterimage. "
               + "Leave empty to skip visual (mechanic still works).")]
        public GameObject AfterimageGhostPrefab;

        [Tooltip("Number of ghost samples to record during the dash")]
        public int AfterimageCount = 5;

        [Tooltip("How long each ghost lives before being destroyed")]
        public float AfterimageLifetime = 0.35f;

        [Tooltip("Tint colour applied to each ghost's materials")]
        public Color AfterimageColor = new Color(0.35f, 0.65f, 1f, 0.55f);

        // ── Private state ────────────────────────────────────────────────────

        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");

        private readonly Queue<(Vector3 pos, Quaternion rot)> _sampleRing = new();
        private float _sampleInterval;
        private float _sampleTimer;

        // ── Initialization ───────────────────────────────────────────────────

        protected override void Initialization()
        {
            // GDD: single dash with 1.2 s cooldown — override inspector values
            MaxCharges = 1;
            ChargeRecoveryTime = 1.2f;

            base.Initialization();

            _sampleInterval = DashDuration / Mathf.Max(1, AfterimageCount);
        }

        // ── Dash lifecycle ───────────────────────────────────────────────────

        protected override void StartDash()
        {
            _sampleRing.Clear();
            _sampleTimer = 0f;
            base.StartDash();
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();

            if (_dashing)
                SamplePosition();
        }

        protected override void StopDash()
        {
            base.StopDash();

            SpawnAfterimages();
            DealLandingAoE();
        }

        // ── Afterimage helpers ────────────────────────────────────────────────

        private void SamplePosition()
        {
            _sampleTimer -= Time.deltaTime;
            if (_sampleTimer > 0f) return;

            _sampleRing.Enqueue((transform.position, transform.rotation));

            if (_sampleRing.Count > AfterimageCount)
                _sampleRing.Dequeue();

            _sampleTimer = _sampleInterval;
        }

        private void SpawnAfterimages()
        {
            if (AfterimageGhostPrefab == null || _sampleRing.Count == 0) return;

            foreach (var (pos, rot) in _sampleRing)
            {
                var ghost = Instantiate(AfterimageGhostPrefab, pos, rot);
                TintGhost(ghost);
                Destroy(ghost, AfterimageLifetime);
            }
        }

        private void TintGhost(GameObject ghost)
        {
            foreach (var rend in ghost.GetComponentsInChildren<Renderer>())
            {
                // Instance materials — never mutate the shared asset
                var mats = rend.materials;
                foreach (var mat in mats)
                    mat.SetColor(_baseColorId, AfterimageColor);
                rend.materials = mats;
            }
        }

        // ── Landing AoE ───────────────────────────────────────────────────────

        private void DealLandingAoE()
        {
            if (LandingAoEDamage <= 0f) return;

            Collider[] hits = Physics.OverlapSphere(
                transform.position, LandingAoERadius,
                LandingDamageLayerMask,
                QueryTriggerInteraction.Collide);

            foreach (Collider hit in hits)
            {
                if (!hit.TryGetComponent<Health>(out var health)) continue;
                if (health.gameObject == gameObject) continue; // no self-damage

                Vector3 dir = (hit.transform.position - transform.position).normalized;

                health.Damage(
                    LandingAoEDamage,
                    gameObject,
                    LandingFlickerDuration,
                    LandingInvincibilityDuration,
                    dir);
            }
        }
    }
}
