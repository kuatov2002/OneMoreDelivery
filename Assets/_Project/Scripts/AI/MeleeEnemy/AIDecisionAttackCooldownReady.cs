using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Returns true when the target is within attack range AND the attack
    /// cooldown has elapsed. Used on the Chase state to trigger Telegraph.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Attack Cooldown Ready")]
    public class AIDecisionAttackCooldownReady : AIDecision
    {
        [Tooltip("Distance at which the enemy can begin telegraphing")]
        public float AttackRange = 2.5f;

        protected MeleeAttackCooldown _cooldown;

        public override void Initialization()
        {
            _cooldown = gameObject.GetComponentInParent<MeleeAttackCooldown>();
        }

        public override bool Decide()
        {
            if (_brain.Target == null) return false;

            float distance = Vector3.Distance(transform.position, _brain.Target.position);
            if (distance > AttackRange) return false;

            return _cooldown != null && _cooldown.IsReady;
        }
    }
}
