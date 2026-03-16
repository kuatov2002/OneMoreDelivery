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

        protected CharacterMovement _characterMovement;
        protected TopDownController _controller;
        protected Health _health;
        protected Animator _animator;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            var character = gameObject.GetComponentInParent<Character>();
            _characterMovement = character?.FindAbility<CharacterMovement>();
            _controller = gameObject.GetComponentInParent<TopDownController>();
            _health = character?.CharacterHealth;
            _animator = character?.CharacterAnimator;
        }

        public override void OnEnterState()
        {
            base.OnEnterState();

            _characterMovement?.SetMovement(Vector2.zero);

            if (_controller != null && _health != null)
            {
                Vector3 knockbackDir = _health.LastDamageDirection.normalized;
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
        }

        public override void PerformAction()
        {
            _characterMovement?.SetMovement(Vector2.zero);
        }
    }
}
