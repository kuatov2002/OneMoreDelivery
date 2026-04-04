using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Returns true when the target is within attack range AND the attack
    /// cooldown has elapsed for the required duration.
    /// No cone check — the Telegraph state handles tracking/rotation.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Attack Cooldown Ready")]
    public class AIDecisionAttackCooldownReady : AIDecision
    {
        [Tooltip("Distance at which the enemy can begin telegraphing")]
        public float AttackRange = 2.5f;

        [Tooltip("How long the target must stay in range before the attack triggers")]
        public float RequiredTimeInRange = 0.3f;

        protected MeleeAttackCooldown _cooldown;
        protected Transform _characterRoot;
        protected float _inRangeSince;
        protected bool _wasInRange;

        public override void Initialization()
        {
            _cooldown = gameObject.GetComponentInParent<MeleeAttackCooldown>();
            var character = gameObject.GetComponentInParent<Character>();
            _characterRoot = character != null ? character.transform : transform;
        }

        public override bool Decide()
        {
            if (_brain.Target == null) { _wasInRange = false; return false; }

            Vector3 dirToTarget = _brain.Target.position - _characterRoot.position;
            dirToTarget.y = 0f;

            bool inRange = dirToTarget.sqrMagnitude <= AttackRange * AttackRange;

            if (!inRange || _cooldown == null || !_cooldown.IsReady)
            {
                _wasInRange = false;
                return false;
            }

            if (!_wasInRange)
            {
                _wasInRange = true;
                _inRangeSince = Time.time;
            }

            return Time.time - _inRangeSince >= RequiredTimeInRange;
        }
    }
}
