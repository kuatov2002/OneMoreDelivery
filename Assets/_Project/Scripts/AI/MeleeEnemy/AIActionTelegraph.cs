using FIMSpace.FProceduralAnimation;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Telegraph action: stops movement, tracks the target during the first
    /// portion of the windup (Dark Souls-style), then locks the direction
    /// for the rest — giving the player a dodge window.
    /// Rotates the character root transform directly, no CharacterOrientation3D needed.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Telegraph")]
    public class AIActionTelegraph : AIAction
    {
        [Header("Hand Glow")]
        [Tooltip("Reference to the AttackTelegraphGlow placed on a hand bone")]
        [SerializeField] private AttackTelegraphGlow _telegraphGlow;

        [Header("Timing")]
        [Tooltip("Duration of the telegraph phase in seconds")]
        [SerializeField] private float _telegraphDuration = 0.7f;

        [Header("Tracking (Dark Souls-style)")]
        [Tooltip("Fraction of telegraph duration during which the enemy tracks the player (0-1). After this the direction is locked.")]
        [Range(0f, 1f)]
        [SerializeField] private float _trackingRatio = 0.65f;

        [Tooltip("How fast the enemy rotates toward the player during tracking (degrees/sec)")]
        [SerializeField] private float _trackingRotationSpeed = 360f;

        [Header("Legs Animator")]
        [Tooltip("Fade out duration for legs procedural animation during telegraph")]
        [SerializeField] private float _legsFadeOutDuration = 0.15f;

        protected CharacterMovement _characterMovement;
        protected Animator _animator;
        protected Transform _characterRoot;
        protected LegsAnimator _legsAnimator;

        private Vector3 _lockedDirection;
        private float _enterTime;
        private bool _directionLocked;

        /// <summary>
        /// The attack direction locked at the end of tracking. Used by AIActionMeleeAttackCone.
        /// </summary>
        public Vector3 LockedDirection => _lockedDirection;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            var character = gameObject.GetComponentInParent<Character>();
            _characterMovement = character?.FindAbility<CharacterMovement>();
            _animator = character?.CharacterAnimator;
            _characterRoot = character != null ? character.transform : transform;
            _legsAnimator = gameObject.GetComponentInParent<LegsAnimator>();
        }

        public override void OnEnterState()
        {
            base.OnEnterState();

            _characterMovement?.SetMovement(Vector2.zero);
            _directionLocked = false;

            if (_brain.Target != null)
            {
                Vector3 dirToTarget = _brain.Target.position - _characterRoot.position;
                dirToTarget.y = 0f;
                if (dirToTarget.sqrMagnitude > 0.001f)
                {
                    _lockedDirection = dirToTarget.normalized;
                    _characterRoot.rotation = Quaternion.LookRotation(_lockedDirection);
                }
            }

            if (_animator != null)
                _animator.SetTrigger("Telegraph");

            if (_telegraphGlow != null)
                _telegraphGlow.Show();

            _legsAnimator?.User_FadeToDisabled(_legsFadeOutDuration);

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

            // Track the player during the first portion of the telegraph
            if (!_directionLocked && _brain.Target != null)
            {
                if (progress < _trackingRatio)
                {
                    Vector3 dirToTarget = _brain.Target.position - _characterRoot.position;
                    dirToTarget.y = 0f;

                    if (dirToTarget.sqrMagnitude > 0.001f)
                    {
                        Vector3 desiredDir = dirToTarget.normalized;

                        float maxStep = _trackingRotationSpeed * Time.deltaTime;
                        _lockedDirection = Vector3.RotateTowards(_lockedDirection, desiredDir, maxStep * Mathf.Deg2Rad, 0f);

                        _characterRoot.rotation = Quaternion.LookRotation(_lockedDirection);
                    }
                }
                else
                {
                    // Lock direction — dodge window starts here
                    _directionLocked = true;
                }
            }

            if (_telegraphGlow != null)
                _telegraphGlow.Progress = progress;
        }

        public override void OnExitState()
        {
            base.OnExitState();

            if (_telegraphGlow != null)
                _telegraphGlow.Hide();
        }
    }
}
