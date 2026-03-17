using FIMSpace.FProceduralAnimation;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Recovery action: no movement, no attacks. On exit, stamps the attack
    /// cooldown so the 2.0s wait begins.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Melee Recovery")]
    public class AIActionMeleeRecovery : AIAction
    {
        [Header("Legs Animator")]
        [Tooltip("How fast legs procedural animation fades back in during recovery")]
        [SerializeField] private float _legsFadeInDuration = 0.3f;

        protected CharacterMovement _characterMovement;
        protected MeleeAttackCooldown _cooldown;
        protected Animator _animator;
        protected LegsAnimator _legsAnimator;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            var character = gameObject.GetComponentInParent<Character>();
            _characterMovement = character?.FindAbility<CharacterMovement>();
            _animator = character?.CharacterAnimator;
            _cooldown = gameObject.GetComponentInParent<MeleeAttackCooldown>();
            _legsAnimator = gameObject.GetComponentInParent<LegsAnimator>();
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            _characterMovement?.SetMovement(Vector2.zero);

            if (_animator != null)
            {
                _animator.SetTrigger("Recovery");
            }

            _legsAnimator?.User_FadeEnabled(_legsFadeInDuration);
        }

        public override void PerformAction()
        {
            _characterMovement?.SetMovement(Vector2.zero);
        }

        public override void OnExitState()
        {
            base.OnExitState();

            if (_cooldown != null)
            {
                _cooldown.LastAttackEndTime = Time.time;
            }
        }
    }
}
