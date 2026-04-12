using System;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Van Helsing's passive: every HitsPerBurst consecutive hits on an enemy
    /// trigger a burst that applies BurstMultiplier to the outgoing damage
    /// for that one hit (default: 4th hit deals +100% = ×2 damage).
    ///
    /// USAGE
    /// ─────
    /// Call  RegisterHit()  from your weapon or projectile each time a hit lands.
    /// RegisterHit() returns the damage multiplier to apply:
    ///   • 1f    — normal hit
    ///   • BurstMultiplier (2f) — burst hit (combo counter resets afterward)
    ///
    /// If you prefer to check separately, query BurstReady and then call
    /// ConsumeAndGetMultiplier() once before dealing damage.
    ///
    /// The combo resets automatically after ComboResetTime seconds without a hit.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/Abilities/Van Helsing Passive")]
    public class VanHelsingPassive : CharacterAbility
    {
        // ── Settings ─────────────────────────────────────────────────────────

        [Header("Combo")]
        [Tooltip("Hits required to trigger the burst (GDD: 4)")]
        public int HitsPerBurst = 4;

        [Tooltip("Damage multiplier on the burst hit (2 = +100%)")]
        public float BurstMultiplier = 2f;

        [Tooltip("Seconds without a hit before the combo resets")]
        public float ComboResetTime = 3f;

        // ── State ─────────────────────────────────────────────────────────────

        /// <summary>Current consecutive hit count (0 … HitsPerBurst - 1).</summary>
        public int CurrentHitCount { get; private set; }

        /// <summary>True when the next RegisterHit() call will trigger a burst.</summary>
        public bool BurstReady => CurrentHitCount >= HitsPerBurst - 1;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Fired each time the hit count changes. Carries the new count.</summary>
        public event Action<int> OnHitCountChanged;

        /// <summary>Fired when a burst triggers (before the counter resets).</summary>
        public event Action OnBurstTriggered;

        // ── Private ───────────────────────────────────────────────────────────

        private float _lastHitTime;

        // ── Initialization ────────────────────────────────────────────────────

        protected override void Initialization()
        {
            base.Initialization();
            CurrentHitCount = 0;
            _lastHitTime = float.NegativeInfinity;
        }

        // ── Update ────────────────────────────────────────────────────────────

        public override void ProcessAbility()
        {
            base.ProcessAbility();

            if (CurrentHitCount > 0
                && Time.time - _lastHitTime > ComboResetTime)
            {
                SetHitCount(0);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Call each time a hit lands on an enemy.
        /// Returns the damage multiplier to apply to that hit's damage value.
        ///   1f              — regular hit
        ///   BurstMultiplier — burst hit (counter resets after this call)
        /// </summary>
        public float RegisterHit()
        {
            _lastHitTime = Time.time;
            int next = CurrentHitCount + 1;

            if (next >= HitsPerBurst)
            {
                // Burst!
                SetHitCount(0, burst: true);
                return BurstMultiplier;
            }

            SetHitCount(next);
            return 1f;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetHitCount(int value, bool burst = false)
        {
            CurrentHitCount = value;

            if (burst)
                OnBurstTriggered?.Invoke();

            OnHitCountChanged?.Invoke(CurrentHitCount);
        }
    }
}
