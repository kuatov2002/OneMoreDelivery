using System.Collections.Generic;
using FIMSpace.FProceduralAnimation;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Executes a cone-shaped melee attack in the locked direction.
    /// Hitbox checks every frame during the active window, tracking already-hit
    /// targets so each is only damaged once. Synced with the lunge movement.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Melee Attack Cone")]
    public class AIActionMeleeAttackCone : AIAction
    {
        [Header("Cone Settings")]
        [Tooltip("Arc angle in degrees")]
        public float ConeAngle = 70f;

        [Tooltip("Attack reach")]
        public float AttackRange = 2.5f;

        [Header("Damage")]
        public int Damage = 25;
        public float KnockbackForce = 10f;
        public float InvincibilityDuration = 0.25f;
        public LayerMask TargetLayers;

        [Header("Active Window (synced with lunge)")]
        [Tooltip("Seconds after state entry before hitbox activates. Should match early lunge phase.")]
        public float ActiveStartTime = 0.04f;

        [Tooltip("How long the hitbox stays active. Covers the full lunge so moving into targets works.")]
        public float ActiveDuration = 0.26f;

        [Header("Lunge")]
        [Tooltip("Total distance the enemy lunges forward during attack")]
        public float LungeDistance = 3f;

        [Tooltip("Duration of the lunge in seconds")]
        public float LungeDuration = 0.28f;

        [Tooltip("Speed curve over the lunge (X: 0-1 time, Y: speed multiplier). Explosive burst → heavy deceleration.")]
        public AnimationCurve LungeCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 6f),
            new Keyframe(0.2f, 1f, 0f, 0f),
            new Keyframe(0.6f, 0.3f, -1f, -0.5f),
            new Keyframe(1f, 0f, -0.3f, 0f)
        );

        [Header("Legs Animator")]
        [Tooltip("Forward impulse power when swinging")]
        [SerializeField] private float _attackImpulsePower = 0.6f;
        [Tooltip("Fade out duration for legs procedural animation during attack")]
        [SerializeField] private float _legsFadeOutDuration = 0.08f;

        protected CharacterMovement _characterMovement;
        protected TopDownController _controller;
        protected Transform _characterRoot;
        protected Animator _animator;
        protected LegsAnimator _legsAnimator;
        protected MeleeEnemyProceduralBody _proceduralBody;
        protected Health _health;
        protected Vector3 _attackDirection;
        protected float _enterTime;
        protected Collider[] _hits = new Collider[16];

        // Track which targets were already hit this swing so each is only damaged once
        protected HashSet<GameObject> _hitTargets = new HashSet<GameObject>();

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            var character = gameObject.GetComponentInParent<Character>();
            _characterMovement = character?.FindAbility<CharacterMovement>();
            _controller = gameObject.GetComponentInParent<TopDownController>();
            _animator = character?.CharacterAnimator;
            _characterRoot = character != null ? character.transform : transform;
            _legsAnimator = gameObject.GetComponentInParent<LegsAnimator>();
            _proceduralBody = gameObject.GetComponentInParent<MeleeEnemyProceduralBody>();
            _health = gameObject.GetComponentInParent<Health>();
        }

        public override void OnEnterState()
        {
            base.OnEnterState();

            _characterMovement?.SetMovement(Vector2.zero);
            _proceduralBody?.SetState(MeleeEnemyProceduralBody.BodyState.Attack);

            // Use character's actual forward — matches the visual rotation set by Telegraph
            _attackDirection = _characterRoot.forward;
            _attackDirection.y = 0f;
            _attackDirection.Normalize();

            _enterTime = Time.time;
            _hitTargets.Clear();

            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }

            if (_legsAnimator != null)
            {
                _legsAnimator.User_FadeToDisabled(_legsFadeOutDuration);

                var impulse = new LegsAnimator.ImpulseExecutor(
                    new Vector3(0f, -0.5f, 1f),
                    0.3f,
                    0.5f
                );
                impulse.PowerMultiplier = _attackImpulsePower;
                _legsAnimator.User_AddImpulse(impulse);
            }
        }

        public override void PerformAction()
        {
            _characterMovement?.SetMovement(Vector2.zero);

            float elapsed = Time.time - _enterTime;

            // Lunge forward using AnimationCurve
            if (_controller != null && LungeDuration > 0f && elapsed <= LungeDuration)
            {
                float t = elapsed / LungeDuration;
                float speed = LungeCurve.Evaluate(t) * (LungeDistance / LungeDuration);
                _controller.AddForce(_attackDirection * speed);
            }

            // Check hitbox every frame during the active window.
            // Already-hit targets are tracked and skipped.
            if (elapsed >= ActiveStartTime && elapsed <= ActiveStartTime + ActiveDuration)
            {
                PerformConeHit();
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

                // Skip already-hit targets this swing
                if (_hitTargets.Contains(_hits[i].gameObject)) continue;

                Vector3 dirToTarget = _hits[i].transform.position - _characterRoot.position;
                dirToTarget.y = 0f;

                if (Vector3.Angle(_attackDirection, dirToTarget) > halfAngle) continue;

                Health health = _hits[i].GetComponent<Health>();
                if (health == null) continue;

                Vector3 knockbackDir = dirToTarget.normalized;
                health.Damage(Damage, _brain.Owner, 0.1f, InvincibilityDuration, knockbackDir * KnockbackForce);
                _hitTargets.Add(_hits[i].gameObject);
            }
        }

        public override void OnExitState()
        {
            base.OnExitState();
            _legsAnimator?.User_FadeEnabled(0.2f);

            // Remove super armor — enemy can be knocked back again after attack finishes
            if (_health != null)
                _health.ImmuneToKnockback = false;
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
