using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Telegraph action: stops movement, locks facing direction toward target,
    /// plays windup animation. Shows a ground circle telegraph indicator
    /// and a glowing sphere on the hand so the player can read the attack.
    /// Direction stays locked for the subsequent Attack state.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Telegraph")]
    public class AIActionTelegraph : AIAction
    {
        [Header("Ground Indicator")]
        [Tooltip("Reference to the AttackTelegraphCircle (child of this enemy)")]
        [SerializeField] private AttackTelegraphCircle _telegraphCircle;

        [Header("Hand Glow")]
        [Tooltip("Reference to the AttackTelegraphGlow placed on a hand bone")]
        [SerializeField] private AttackTelegraphGlow _telegraphGlow;

        [Header("Timing")]
        [Tooltip("Duration of the telegraph phase in seconds (should match the state transition threshold)")]
        [SerializeField] private float _telegraphDuration = 0.7f;

        protected CharacterMovement _characterMovement;
        protected CharacterOrientation3D _orientation;
        protected Animator _animator;

        private AIActionMeleeAttackCone _attackCone;
        private float _enterTime;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            var character = gameObject.GetComponentInParent<Character>();
            _characterMovement = character?.FindAbility<CharacterMovement>();
            _orientation = character?.FindAbility<CharacterOrientation3D>();
            _animator = character?.CharacterAnimator;

            _attackCone = GetComponent<AIActionMeleeAttackCone>();
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
                _animator.SetTrigger("Telegraph");

            ShowTelegraph();
            _enterTime = Time.time;
        }

        public override void PerformAction()
        {
            _characterMovement?.SetMovement(Vector2.zero);

            float progress = 0f;
            if (_telegraphDuration > 0f)
            {
                float elapsed = Time.time - _enterTime;
                progress = Mathf.Clamp01(elapsed / _telegraphDuration);
            }

            // Drive both effects with the same progress
            if (_telegraphCircle != null)
                _telegraphCircle.FillProgress = progress;

            if (_telegraphGlow != null)
                _telegraphGlow.Progress = progress;
        }

        public override void OnExitState()
        {
            base.OnExitState();

            if (_telegraphCircle != null)
                _telegraphCircle.Hide();

            if (_telegraphGlow != null)
                _telegraphGlow.Hide();
        }

        private void ShowTelegraph()
        {
            // Ground circle
            if (_telegraphCircle != null)
            {
                float arc = _attackCone != null ? _attackCone.ConeAngle : 60f;
                float radius = _attackCone != null ? _attackCone.AttackRange : 2.5f;

                Vector3 dir = _orientation != null
                    ? _orientation.ForcedRotationDirection
                    : transform.forward;

                _telegraphCircle.Show(arc, radius, dir);
            }

            // Hand glow
            if (_telegraphGlow != null)
                _telegraphGlow.Show();
        }
    }
}
