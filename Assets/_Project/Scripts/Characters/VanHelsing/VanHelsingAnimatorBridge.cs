
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Bridges the TopDownEngine weapon system with Van Helsing's crossbow animator.
    ///
    /// Responsibilities:
    ///   1. Monitors the equipped weapon's state machine and sets the
    ///      "Shooting" / "Reloading" animator bools so the UpperBody layer
    ///      plays the correct crossbow animations.
    ///   2. Computes RelativeForwardSpeed / RelativeLateralSpeed so the
    ///      locomotion blend tree works correctly with independent aiming.
    ///   3. Dynamically scales animation playback speed so foot movement
    ///      matches the character's actual movement speed (no sliding).
    ///
    /// Add this component to the same GameObject that carries the Animator and
    /// CharacterHandleWeapon.  It is intentionally NOT a CharacterAbility —
    /// it does not process input or participate in the ability interrupt system;
    /// it only reads state and forwards it to the Animator.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/Abilities/Van Helsing Animator Bridge")]
    public class VanHelsingAnimatorBridge : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("References (auto-resolved if left empty)")]
        [Tooltip("The Animator on this character.")]
        public Animator TargetAnimator;

        [Tooltip("The weapon handler ability. Resolved automatically if null.")]
        public CharacterHandleWeapon WeaponHandler;

        [Header("Locomotion Speed Sync")]
        [Tooltip("The root-motion speed of the walk animations (measured from Crossbow@WalkForward). " +
                 "Used to compute the playback multiplier so feet don't slide.")]
        public float AnimationRootSpeed = 1.86f;

        [Tooltip("Smoothing applied to the animator speed changes.")]
        public float SpeedSmoothTime = 0.1f;

        // ── Hashed parameter IDs ─────────────────────────────────────────────

        private static readonly int _hashShooting   = Animator.StringToHash("Shooting");
        private static readonly int _hashReloading   = Animator.StringToHash("Reloading");
        private static readonly int _hashRelFwd      = Animator.StringToHash("RelativeForwardSpeed");
        private static readonly int _hashRelLat      = Animator.StringToHash("RelativeLateralSpeed");

        // ── Private state ────────────────────────────────────────────────────

        private TopDownController _controller;
        private CharacterMovement _charMovement;
        private bool _hasFwdParam;
        private bool _hasLatParam;
        private bool _hasShootParam;
        private bool _hasReloadParam;
        private float _modelScale = 1f;
        private float _currentAnimSpeed = 1f;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Start()
        {
            if (TargetAnimator == null)
                TargetAnimator = GetComponent<Animator>();

            if (WeaponHandler == null)
                WeaponHandler = GetComponent<CharacterHandleWeapon>();

            _controller    = GetComponent<TopDownController>();
            _charMovement  = GetComponent<CharacterMovement>();
            _modelScale    = transform.lossyScale.x; // assumes uniform scale

            // Cache which parameters actually exist in the controller
            if (TargetAnimator != null && TargetAnimator.runtimeAnimatorController != null)
            {
                foreach (var p in TargetAnimator.parameters)
                {
                    if (p.nameHash == _hashRelFwd)     _hasFwdParam     = true;
                    if (p.nameHash == _hashRelLat)     _hasLatParam     = true;
                    if (p.nameHash == _hashShooting)   _hasShootParam   = true;
                    if (p.nameHash == _hashReloading)  _hasReloadParam  = true;
                }
            }
        }

        private void Update()
        {
            if (TargetAnimator == null) return;

            UpdateWeaponAnimations();
            UpdateRelativeSpeeds();
            UpdateAnimatorSpeed();
        }

        // ── Weapon state → Animator ──────────────────────────────────────────

        private void UpdateWeaponAnimations()
        {
            if (WeaponHandler == null || WeaponHandler.CurrentWeapon == null)
            {
                if (_hasShootParam)  TargetAnimator.SetBool(_hashShooting, false);
                if (_hasReloadParam) TargetAnimator.SetBool(_hashReloading, false);
                return;
            }

            var state = WeaponHandler.CurrentWeapon.WeaponState.CurrentState;

            bool isShooting = state == Weapon.WeaponStates.WeaponUse
                           || state == Weapon.WeaponStates.WeaponDelayBeforeUse
                           || state == Weapon.WeaponStates.WeaponStart;

            bool isReloading = state == Weapon.WeaponStates.WeaponReloadStart
                            || state == Weapon.WeaponStates.WeaponReload
                            || state == Weapon.WeaponStates.WeaponReloadStop;

            if (_hasShootParam)  TargetAnimator.SetBool(_hashShooting, isShooting);
            if (_hasReloadParam) TargetAnimator.SetBool(_hashReloading, isReloading);
        }

        // ── Relative speed computation ───────────────────────────────────────

        private void UpdateRelativeSpeeds()
        {
            if (!_hasFwdParam && !_hasLatParam) return;
            if (_controller == null) return;

            // Velocity in world space → character-local space
            Vector3 localVelocity = transform.InverseTransformDirection(_controller.Speed);

            // Normalize to -1..1 based on character walk speed
            float maxSpeed = (_charMovement != null)
                ? Mathf.Max(_charMovement.WalkSpeed, 0.01f)
                : 1f;

            float relFwd = Mathf.Clamp(localVelocity.z / maxSpeed, -1f, 1f);
            float relLat = Mathf.Clamp(localVelocity.x / maxSpeed, -1f, 1f);

            if (_hasFwdParam) TargetAnimator.SetFloat(_hashRelFwd, relFwd, SpeedSmoothTime, Time.deltaTime);
            if (_hasLatParam) TargetAnimator.SetFloat(_hashRelLat, relLat, SpeedSmoothTime, Time.deltaTime);
        }

        // ── Animation speed sync (anti-slide) ───────────────────────────────

        // Base layer state hashes for checking current state
        private static readonly int _hashIdleState = Animator.StringToHash("Idle");
        private static readonly int _hashLocoState = Animator.StringToHash("Locomotion");

        private void UpdateAnimatorSpeed()
        {
            if (_controller == null) return;

            // Only adjust speed during Idle or Locomotion states.
            // Dash, Death, Hit, Block should play at normal speed.
            var stateInfo = TargetAnimator.GetCurrentAnimatorStateInfo(0);
            bool isLocomotion = stateInfo.shortNameHash == _hashLocoState
                             || stateInfo.shortNameHash == _hashIdleState;

            if (!isLocomotion)
            {
                _currentAnimSpeed = Mathf.Lerp(_currentAnimSpeed, 1f, Time.deltaTime * 10f);
                TargetAnimator.speed = _currentAnimSpeed;
                return;
            }

            float actualSpeed = _controller.Speed.magnitude;

            if (actualSpeed < 0.1f)
            {
                // Standing still — normal speed for idle animation
                _currentAnimSpeed = Mathf.Lerp(_currentAnimSpeed, 1f, Time.deltaTime * 10f);
            }
            else
            {
                // Moving — scale animation to match movement speed.
                // Visual foot speed = AnimationRootSpeed * modelScale * animatorSpeed
                // We want: Visual foot speed == actualSpeed
                // So: animatorSpeed = actualSpeed / (AnimationRootSpeed * modelScale)
                float targetSpeed = actualSpeed / (AnimationRootSpeed * _modelScale);
                targetSpeed = Mathf.Clamp(targetSpeed, 0.5f, 4f); // safety clamp
                _currentAnimSpeed = Mathf.Lerp(_currentAnimSpeed, targetSpeed, Time.deltaTime * 10f);
            }

            TargetAnimator.speed = _currentAnimSpeed;
        }
    }
}
