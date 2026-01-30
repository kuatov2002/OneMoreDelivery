// ═══════════════════════════════════════════════════════════════════
// DECISIONS
// ═══════════════════════════════════════════════════════════════════

using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Decision that checks if current health is below threshold (for phase transitions)
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Health Threshold")]
    public class AIDecisionHealthThreshold : AIDecision
    {
        [Tooltip("Health percentage threshold (0-1)")]
        public float HealthThreshold = 0.6f;
        
        [Tooltip("Comparison mode")]
        public enum ComparisonMode { Below, Above }
        public ComparisonMode Mode = ComparisonMode.Below;

        protected Health _health;

        public override void Initialization()
        {
            _health = _brain.Owner.GetComponent<Health>();
        }

        public override bool Decide()
        {
            if (_health == null) return false;

            float healthPercentage = _health.CurrentHealth / _health.MaximumHealth;

            if (Mode == ComparisonMode.Below)
                return healthPercentage <= HealthThreshold;
            else
                return healthPercentage >= HealthThreshold;
        }
    }
}