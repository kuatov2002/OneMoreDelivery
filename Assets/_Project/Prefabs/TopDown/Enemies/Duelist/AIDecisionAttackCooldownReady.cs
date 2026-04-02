using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Returns true when the target is within attack range, inside the facing
    /// cone, AND the attack cooldown has elapsed.
    /// Uses character root forward for cone check — no CharacterOrientation3D needed.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Attack Cooldown Ready")]
    public class AIDecisionAttackCooldownReady : AIDecision
    {
        [Tooltip("Distance at which the enemy can begin telegraphing")]
        public float AttackRange = 2.5f;

        [Tooltip("Detection cone angle in degrees (should match AIActionMeleeAttackCone.ConeAngle)")]
        public float ConeAngle = 60f;

        protected MeleeAttackCooldown _cooldown;
        protected Transform _characterRoot;

        public override void Initialization()
        {
            _cooldown = gameObject.GetComponentInParent<MeleeAttackCooldown>();
            var character = gameObject.GetComponentInParent<Character>();
            _characterRoot = character != null ? character.transform : transform;
        }

        public override bool Decide()
        {
            if (_brain.Target == null) return false;

            Vector3 dirToTarget = _brain.Target.position - _characterRoot.position;
            dirToTarget.y = 0f;

            if (dirToTarget.sqrMagnitude > AttackRange * AttackRange) return false;

            // Cone check: target must be within the facing cone
            Vector3 forward = _characterRoot.forward;
            forward.y = 0f;

            if (Vector3.Angle(forward, dirToTarget) > ConeAngle / 2f) return false;

            return _cooldown != null && _cooldown.IsReady;
        }
    }
}
