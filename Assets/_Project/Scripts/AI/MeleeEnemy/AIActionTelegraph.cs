using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Telegraph action: stops movement, locks facing direction toward target,
    /// plays windup animation. Direction stays locked for the subsequent Attack state.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Telegraph")]
    public class AIActionTelegraph : AIAction
    {
        protected CharacterMovement _characterMovement;
        protected CharacterOrientation3D _orientation;
        protected Animator _animator;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            var character = gameObject.GetComponentInParent<Character>();
            _characterMovement = character?.FindAbility<CharacterMovement>();
            _orientation = character?.FindAbility<CharacterOrientation3D>();
            _animator = character?.CharacterAnimator;
        }

        public override void OnEnterState()
        {
            base.OnEnterState();

            _characterMovement?.SetMovement(Vector2.zero);

            if (_brain.Target != null && _orientation != null)
            {
                Vector3 dirToTarget = _brain.Target.position - transform.position;
                dirToTarget.y = 0f;
                if (dirToTarget.sqrMagnitude > 0.001f)
                {
                    _orientation.ForcedRotation = true;
                    _orientation.ForcedRotationDirection = dirToTarget.normalized;
                }
            }

            if (_animator != null)
            {
                _animator.SetTrigger("Telegraph");
            }
        }

        public override void PerformAction()
        {
            _characterMovement?.SetMovement(Vector2.zero);
        }
    }
}
