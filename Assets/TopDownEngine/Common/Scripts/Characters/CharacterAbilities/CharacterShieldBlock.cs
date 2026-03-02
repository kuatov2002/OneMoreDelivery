using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using MoreMountains.Feedbacks;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Shield block ability with parry mechanic and input buffering.
    ///
    /// When the player presses the block button while the ability is temporarily
    /// unavailable (e.g. the character is in a blocking movement state), the input
    /// is buffered and the block activates as soon as the restriction lifts.
    ///
    /// Animation parameters:
    ///   Blocking      (bool)    – true while holding block
    ///   BlockStarted  (trigger) – triggered on block start
    ///   Parried       (trigger) – triggered on successful parry
    ///   ParryWindow   (bool)    – true during parry window
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/Abilities/Character Shield Block")]
    public class CharacterShieldBlock : CharacterAbility
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Block Settings")]
        [Tooltip("Direction the shield protects (local space)")]
        public Vector3 ShieldDirection = Vector3.forward;

        [Tooltip("Protection arc angle in degrees (180 = half circle)")]
        public float ProtectionArc = 120f;

        [Tooltip("Can move while blocking?")]
        public bool AllowMovementWhileBlocking = true;

        [Tooltip("Movement speed multiplier while blocking")]
        [MMCondition("AllowMovementWhileBlocking", true)]
        public float MovementSpeedMultiplier = 0.3f;

        [Header("Damage Reduction")]
        [Tooltip("Damage reduction percentage (0-1)")]
        [Range(0f, 1f)]
        public float DamageReduction = 0.8f;

        [Tooltip("Block all damage from protected direction?")]
        public bool PerfectBlock = false;

        [Header("Parry")]
        [Tooltip("Enable parry mechanic?")]
        public bool ParryEnabled = true;

        [Tooltip("Parry window duration after block starts (seconds)")]
        [MMCondition("ParryEnabled", true)]
        public float ParryWindowDuration = 0.3f;

        [Tooltip("Damage multiplier returned to attacker on parry")]
        [MMCondition("ParryEnabled", true)]
        public float ParryDamageMultiplier = 1.5f;

        [Tooltip("Stun attacker on successful parry?")]
        [MMCondition("ParryEnabled", true)]
        public bool StunOnParry = true;

        [Tooltip("Stun duration in seconds")]
        [MMCondition("StunOnParry", true)]
        public float StunDuration = 1.5f;

        [Header("Input Buffer")]
        [Tooltip("Buffer a block request made while the ability is temporarily unavailable")]
        public AbilityInputBuffer InputBuffer = new AbilityInputBuffer { BufferDuration = 0.25f };

        [Header("Feedbacks")]
        public MMFeedbacks BlockStartFeedback;
        public MMFeedbacks BlockStopFeedback;
        public MMFeedbacks BlockHitFeedback;
        public MMFeedbacks ParrySuccessFeedback;

        // ── State ─────────────────────────────────────────────────────────────

        protected bool _blocking;
        protected bool _parryWindowActive;
        protected float _parryWindowTimer;
        protected float _originalMovementSpeed;
        protected CharacterMovement _characterMovement;

        // ── Animator parameters ───────────────────────────────────────────────

        protected const string _blockingParameterName    = "Blocking";
        protected const string _blockStartedParameterName = "BlockStarted";
        protected const string _parriedParameterName     = "Parried";
        protected const string _parryWindowParameterName = "ParryWindow";

        protected int _blockingParameter;
        protected int _blockStartedParameter;
        protected int _parriedParameter;
        protected int _parryWindowParameter;

        // ── Initialization ────────────────────────────────────────────────────

        protected override void Initialization()
        {
            base.Initialization();

            _characterMovement = _character?.FindAbility<CharacterMovement>();

            BlockStartFeedback?.Initialization(gameObject);
            BlockStopFeedback?.Initialization(gameObject);
            BlockHitFeedback?.Initialization(gameObject);
            ParrySuccessFeedback?.Initialization(gameObject);
        }

        // ── Input ─────────────────────────────────────────────────────────────

        protected override void HandleInput()
        {
            if (_inputManager == null) return;

            bool buttonDown = _inputManager.SecondaryShootButton.State.CurrentState
                              == MMInput.ButtonStates.ButtonDown;
            bool buttonUp   = _inputManager.SecondaryShootButton.State.CurrentState
                              == MMInput.ButtonStates.ButtonUp;

            if (buttonDown)
            {
                // Ability is fully available — start immediately.
                if (AbilityAuthorized
                    && _condition.CurrentState == CharacterStates.CharacterConditions.Normal)
                {
                    StartBlocking();
                }
                else
                {
                    // Temporarily blocked — store the request.
                    InputBuffer.Request();
                }
            }

            if (buttonUp)
            {
                InputBuffer.Clear();

                if (_blocking)
                {
                    StopBlocking();
                }
            }

            // Not Authorized and currently blocking — force-stop.
            if (_blocking
                && (!AbilityAuthorized
                    || _condition.CurrentState != CharacterStates.CharacterConditions.Normal))
            {
                StopBlocking();
            }
        }

        // ── Process ───────────────────────────────────────────────────────────

        public override void ProcessAbility()
        {
            base.ProcessAbility();

            // Try to flush a buffered block request.
            if (!_blocking
                && InputBuffer.IsActive
                && AbilityAuthorized
                && _condition.CurrentState == CharacterStates.CharacterConditions.Normal)
            {
                if (InputBuffer.ConsumeIfActive())
                {
                    StartBlocking();
                }
            }

            if (_blocking)
            {
                UpdateParryWindow();
                CheckForceStopConditions();
            }
        }

        // ── Force-stop conditions ─────────────────────────────────────────────

        public override void OnInterruptedBy(CharacterAbility interruptor)
        {
            // Нас прервали — убираем блок без cooldown penalty,
            // потому что это намеренное прерывание игрока, а не таймаут
            StopBlocking();
        }
        
        protected virtual void CheckForceStopConditions()
        {
            if (_movement.CurrentState != CharacterStates.MovementStates.SpecialAttacking)
            {
                StopBlocking();
                return;
            }

            if (IsAnyWeaponActive())
            {
                StopBlocking(weaponInterrupt: true);
                StartCoroutine(ResetMovementAfterWeaponRoutine());
            }
        }

        protected virtual IEnumerator ResetMovementAfterWeaponRoutine()
        {
            yield return null; // let the weapon start

            while (IsAnyWeaponActive())
            {
                yield return null;
            }

            if (_movement.CurrentState == CharacterStates.MovementStates.SpecialAttacking)
            {
                _movement.ChangeState(CharacterStates.MovementStates.Idle);
            }
        }

        // ── Block / Stop ──────────────────────────────────────────────────────

        protected virtual void StartBlocking()
        {
            if (_blocking) return;

            StopAllWeapons();

            _blocking = true;
            _movement.ChangeState(CharacterStates.MovementStates.SpecialAttacking);

            if (ParryEnabled)
            {
                _parryWindowActive = true;
                _parryWindowTimer  = 0f;
            }

            ApplyMovementRestriction();

            MMAnimatorExtensions.UpdateAnimatorTrigger(
                _animator, _blockStartedParameter, _character._animatorParameters);

            BlockStartFeedback?.PlayFeedbacks(transform.position);
            PlayAbilityStartFeedbacks();
        }

        protected virtual void StopBlocking(bool weaponInterrupt = false)
        {
            if (!_blocking) return;

            _blocking          = false;
            _parryWindowActive = false;

            if (!weaponInterrupt
                && _movement.CurrentState == CharacterStates.MovementStates.SpecialAttacking)
            {
                _movement.ChangeState(CharacterStates.MovementStates.Idle);
            }

            RestoreMovement();

            BlockStopFeedback?.PlayFeedbacks(transform.position);
            StopStartFeedbacks();
            PlayAbilityStopFeedbacks();
        }

        // ── Parry ─────────────────────────────────────────────────────────────

        protected virtual void UpdateParryWindow()
        {
            if (!_parryWindowActive) return;

            _parryWindowTimer += Time.deltaTime;
            if (_parryWindowTimer >= ParryWindowDuration)
            {
                _parryWindowActive = false;
            }
        }

        // ── Public damage API ─────────────────────────────────────────────────

        /// <summary>
        /// Call this from the Health component when taking damage.
        /// Returns the modified damage value after block / parry processing.
        /// </summary>
        public virtual float ProcessIncomingDamage(
            float damage, Vector3 damageDirection, GameObject instigator)
        {
            if (!_blocking) return damage;

            Vector3 attackDir       = (damageDirection - transform.position).normalized;
            Vector3 shieldWorldDir  = transform.TransformDirection(ShieldDirection);
            float   angle           = Vector3.Angle(shieldWorldDir, attackDir);

            if (angle > ProtectionArc / 2f)
            {
                return damage; // unprotected side
            }

            if (_parryWindowActive && ParryEnabled)
            {
                return ExecuteParry(damage, instigator);
            }

            BlockHitFeedback?.PlayFeedbacks(transform.position);
            return PerfectBlock ? 0f : damage * (1f - DamageReduction);
        }

        protected virtual float ExecuteParry(float damage, GameObject attacker)
        {
            _parryWindowActive = false;

            ParrySuccessFeedback?.PlayFeedbacks(transform.position);
            MMAnimatorExtensions.UpdateAnimatorTrigger(
                _animator, _parriedParameter, _character._animatorParameters);

            if (attacker != null)
            {
                Health attackerHealth = attacker.GetComponent<Health>();
                if (attackerHealth != null)
                {
                    attackerHealth.Damage(
                        damage * ParryDamageMultiplier,
                        gameObject, 0.2f, 0.2f, -ShieldDirection);

                    if (StunOnParry)
                    {
                        Character attackerCharacter = attacker.GetComponent<Character>();
                        attackerCharacter?.ChangeCharacterConditionTemporarily(
                            CharacterStates.CharacterConditions.Stunned,
                            StunDuration, true, false);
                    }
                }
            }

            return -1f;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private bool IsAnyWeaponActive()
        {
            if (_handleWeaponList == null) return false;

            foreach (CharacterHandleWeapon hw in _handleWeaponList)
            {
                if (hw.CurrentWeapon == null) continue;

                Weapon.WeaponStates s = hw.CurrentWeapon.WeaponState.CurrentState;
                if (s != Weapon.WeaponStates.WeaponIdle && s != Weapon.WeaponStates.WeaponStop)
                {
                    return true;
                }
            }

            return false;
        }

        private void StopAllWeapons()
        {
            if (_handleWeaponList == null) return;

            foreach (CharacterHandleWeapon hw in _handleWeaponList)
            {
                hw?.ForceStop();
            }
        }

        private void ApplyMovementRestriction()
        {
            if (_characterMovement == null) return;

            if (!AllowMovementWhileBlocking)
            {
                _characterMovement.MovementForbidden = true;
            }
            else
            {
                _originalMovementSpeed = _characterMovement.MovementSpeedMultiplier;
                _characterMovement.MovementSpeedMultiplier = MovementSpeedMultiplier;
            }
        }

        private void RestoreMovement()
        {
            if (_characterMovement == null) return;

            _characterMovement.MovementForbidden = false;

            if (AllowMovementWhileBlocking)
            {
                _characterMovement.MovementSpeedMultiplier = _originalMovementSpeed;
            }
        }

        // ── Animator ─────────────────────────────────────────────────────────

        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(
                _blockingParameterName,
                AnimatorControllerParameterType.Bool,
                out _blockingParameter);

            RegisterAnimatorParameter(
                _blockStartedParameterName,
                AnimatorControllerParameterType.Trigger,
                out _blockStartedParameter);

            RegisterAnimatorParameter(
                _parriedParameterName,
                AnimatorControllerParameterType.Trigger,
                out _parriedParameter);

            RegisterAnimatorParameter(
                _parryWindowParameterName,
                AnimatorControllerParameterType.Bool,
                out _parryWindowParameter);
        }

        public override void UpdateAnimator()
        {
            MMAnimatorExtensions.UpdateAnimatorBool(
                _animator, _blockingParameter, _blocking,
                _character._animatorParameters, _character.RunAnimatorSanityChecks);

            MMAnimatorExtensions.UpdateAnimatorBool(
                _animator, _parryWindowParameter, _parryWindowActive,
                _character._animatorParameters, _character.RunAnimatorSanityChecks);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void OnDeath()
        {
            base.OnDeath();
            InputBuffer.Clear();

            if (_blocking)
            {
                StopBlocking();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            InputBuffer.Clear();

            if (_blocking)
            {
                StopBlocking();
            }
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

        protected virtual void OnDrawGizmos()
        {
            if (!_blocking) return;

            Gizmos.color = _parryWindowActive ? Color.yellow : Color.blue;

            Vector3 worldDir = transform.TransformDirection(ShieldDirection);

            for (int i = 0; i <= 20; i++)
            {
                float   angle     = (ProtectionArc / 20f) * i;
                Vector3 direction = Quaternion.Euler(0, angle - ProtectionArc / 2f, 0) * worldDir;
                Gizmos.DrawRay(transform.position, direction * 2f);
            }
        }
    }
}