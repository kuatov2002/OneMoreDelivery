using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Executes a cone-shaped melee attack in the locked direction.
    /// Hitbox activates only during a specific time window (active frames).
    /// Gets attack direction from AIActionTelegraph.LockedDirection or character forward.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Melee Attack Cone")]
    public class AIActionMeleeAttackCone : AIAction
    {
        [Header("Cone Settings")]
        [Tooltip("Arc angle in degrees")]
        public float ConeAngle = 60f;

        [Tooltip("Attack reach")]
        public float AttackRange = 2.5f;

        [Header("Damage")]
        public int Damage = 20;
        public float KnockbackForce = 8f;
        public float InvincibilityDuration = 0.2f;
        public LayerMask TargetLayers;

        [Header("Active Window")]
        [Tooltip("Seconds after state entry before hitbox activates")]
        public float ActiveStartTime = 0.1f;

        [Tooltip("How long the hitbox stays active")]
        public float ActiveDuration = 0.1f;

        protected CharacterMovement _characterMovement;
        protected Transform _characterRoot;
        protected Animator _animator;
        protected Vector3 _attackDirection;
        protected float _enterTime;
        protected bool _hasHit;
        protected Collider[] _hits = new Collider[16];

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            var character = gameObject.GetComponentInParent<Character>();
            _characterMovement = character?.FindAbility<CharacterMovement>();
            _animator = character?.CharacterAnimator;
            _characterRoot = character != null ? character.transform : transform;
        }

        public override void OnEnterState()
        {
            base.OnEnterState();

            _characterMovement?.SetMovement(Vector2.zero);

            // Use character's actual forward — matches the visual rotation set by Telegraph
            _attackDirection = _characterRoot.forward;
            _attackDirection.y = 0f;
            _attackDirection.Normalize();

            _enterTime = Time.time;
            _hasHit = false;

            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }
        }

        public override void PerformAction()
        {
            _characterMovement?.SetMovement(Vector2.zero);

            if (_hasHit) return;

            float elapsed = Time.time - _enterTime;
            if (elapsed >= ActiveStartTime && elapsed <= ActiveStartTime + ActiveDuration)
            {
                PerformConeHit();
                _hasHit = true;
            }
        }

        protected virtual void PerformConeHit()
        {
            int count = Physics.OverlapSphereNonAlloc(_characterRoot.position, AttackRange, _hits, TargetLayers);
            float halfAngle = ConeAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                if (_hits[i] == null) continue;
                if (_hits[i].gameObject == _brain.Owner) continue;

                Vector3 dirToTarget = _hits[i].transform.position - _characterRoot.position;
                dirToTarget.y = 0f;

                if (Vector3.Angle(_attackDirection, dirToTarget) > halfAngle) continue;

                Health health = _hits[i].GetComponent<Health>();
                if (health == null) continue;

                Vector3 knockbackDir = dirToTarget.normalized;
                health.Damage(Damage, _brain.Owner, 0.1f, InvincibilityDuration, knockbackDir * KnockbackForce);
            }
        }

        public override void OnExitState()
        {
            base.OnExitState();
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Vector3 forward = Application.isPlaying && _characterRoot != null
                ? _attackDirection
                : transform.forward;

            float halfAngle = ConeAngle / 2f;

            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * forward;
            Gizmos.DrawLine(transform.position, transform.position + leftDir * AttackRange);
            Gizmos.DrawLine(transform.position, transform.position + rightDir * AttackRange);
            Gizmos.DrawWireSphere(transform.position, AttackRange);
        }
    }
}
