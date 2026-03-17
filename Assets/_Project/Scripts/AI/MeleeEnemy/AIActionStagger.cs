using FIMSpace.FProceduralAnimation;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Stagger action: applies knockback, freezes movement for the duration.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Stagger")]
    public class AIActionStagger : AIAction
    {
        [Tooltip("Knockback force applied on stagger")]
        public float KnockbackForce = 5f;

        [Header("Legs Animator")]
        [Tooltip("Hips impulse power on stagger hit")]
        [SerializeField] private float _legsImpulsePower = 0.5f;
        [Tooltip("Hips impulse duration on stagger hit")]
        [SerializeField] private float _legsImpulseDuration = 0.4f;

        protected CharacterMovement _characterMovement;
        protected TopDownController _controller;
        protected Health _health;
        protected Animator _animator;
        protected LegsAnimator _legsAnimator;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            var character = gameObject.GetComponentInParent<Character>();
            _characterMovement = character?.FindAbility<CharacterMovement>();
            _controller = gameObject.GetComponentInParent<TopDownController>();
            _health = character?.CharacterHealth;
            _animator = character?.CharacterAnimator;
            _legsAnimator = gameObject.GetComponentInParent<LegsAnimator>();
        }

        public override void OnEnterState()
        {
            base.OnEnterState();

            _characterMovement?.SetMovement(Vector2.zero);

            Vector3 knockbackDir = Vector3.zero;
            if (_controller != null && _health != null)
            {
                knockbackDir = _health.LastDamageDirection.normalized;
                if (knockbackDir.sqrMagnitude < 0.001f && _brain.Target != null)
                {
                    knockbackDir = (transform.position - _brain.Target.position).normalized;
                }
                _controller.Impact(knockbackDir, KnockbackForce);
            }

            if (_animator != null)
            {
                _animator.SetTrigger("Stagger");
            }

            if (_legsAnimator != null)
            {
                var impulse = new LegsAnimator.ImpulseExecutor(
                    new Vector3(0f, -1f, 0f),
                    _legsImpulseDuration,
                    0.7f
                );
                impulse.PowerMultiplier = _legsImpulsePower;
                impulse.WorldTranslation = knockbackDir * 0.3f;
                _legsAnimator.User_AddImpulse(impulse);
            }
        }

        public override void PerformAction()
        {
            _characterMovement?.SetMovement(Vector2.zero);
        }
    }
}
