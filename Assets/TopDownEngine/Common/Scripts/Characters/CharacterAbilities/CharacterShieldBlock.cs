using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using MoreMountains.Feedbacks;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Shield block ability with parry mechanic
    /// 
    /// Animation parameters:
    /// Blocking (bool) - true while holding block
    /// BlockStarted (trigger) - triggered on block start
    /// Parried (trigger) - triggered on successful parry
    /// ParryWindow (bool) - true during parry window
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/Abilities/Character Shield Block")]
    public class CharacterShieldBlock : CharacterAbility
    {
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

        [Header("Feedbacks")]
        public MMFeedbacks BlockStartFeedback;
        public MMFeedbacks BlockStopFeedback;
        public MMFeedbacks BlockHitFeedback;
        public MMFeedbacks ParrySuccessFeedback;

        protected bool _blocking;
        protected bool _parryWindowActive;
        protected float _parryWindowTimer;
        protected float _originalMovementSpeed;
        protected CharacterMovement _characterMovement;
        
        protected const string _blockingParameterName = "Blocking";
        protected const string _blockStartedParameterName = "BlockStarted";
        protected const string _parriedParameterName = "Parried";
        protected const string _parryWindowParameterName = "ParryWindow";
        
        protected int _blockingParameter;
        protected int _blockStartedParameter;
        protected int _parriedParameter;
        protected int _parryWindowParameter;

        protected override void Initialization()
        {
            base.Initialization();
    
            _characterMovement = _character?.FindAbility<CharacterMovement>();
    
            BlockStartFeedback?.Initialization(gameObject);
            BlockStopFeedback?.Initialization(gameObject);
            BlockHitFeedback?.Initialization(gameObject);
            ParrySuccessFeedback?.Initialization(gameObject);

            // Подписываемся на изменения стейта движения
            if (_movement != null)
            {
                _movement.OnStateChange += OnMovementStateChanged;
            }
        }
        protected virtual void OnMovementStateChanged()
        {
            if (_blocking && _movement.CurrentState != CharacterStates.MovementStates.SpecialAttacking)
            {
                // Если оружие уже активно — weaponInterrupt
                bool weaponActive = false;
                if (_handleWeaponList != null)
                {
                    foreach (CharacterHandleWeapon hw in _handleWeaponList)
                    {
                        if (hw.CurrentWeapon == null) continue;
                        var s = hw.CurrentWeapon.WeaponState.CurrentState;
                        if (s != Weapon.WeaponStates.WeaponIdle && s != Weapon.WeaponStates.WeaponStop)
                        {
                            weaponActive = true;
                            break;
                        }
                    }
                }
                StopBlocking(weaponInterrupt: weaponActive);
            }
        }
        protected override void HandleInput()
        {
            if (!AbilityAuthorized 
                || _condition.CurrentState != CharacterStates.CharacterConditions.Normal)
            {
                if (_blocking)
                {
                    StopBlocking();
                }

                return;
            }
            
            if (_inputManager.SecondaryShootButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
            {
                StartBlocking();
            }
            else if (_inputManager.SecondaryShootButton.State.CurrentState == MMInput.ButtonStates.ButtonUp)
            {
                StopBlocking();
            }
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();
    
            if (_blocking)
            {
                UpdateParryWindow();
                CheckForceStopConditions();
            }
        }
        protected virtual void CheckForceStopConditions()
        {
            if (_movement.CurrentState != CharacterStates.MovementStates.SpecialAttacking)
            {
                StopBlocking();
                return;
            }

            if (_handleWeaponList != null)
            {
                foreach (CharacterHandleWeapon handleWeapon in _handleWeaponList)
                {
                    if (handleWeapon.CurrentWeapon == null) continue;

                    Weapon.WeaponStates weaponState = handleWeapon.CurrentWeapon.WeaponState.CurrentState;
                    bool weaponActive = weaponState != Weapon.WeaponStates.WeaponIdle
                                        && weaponState != Weapon.WeaponStates.WeaponStop;

                    if (weaponActive)
                    {
                        StopBlocking(weaponInterrupt: true);
                        StartCoroutine(ResetMovementAfterWeapon());
                        return;
                    }
                }
            }
        }

        protected virtual IEnumerator ResetMovementAfterWeapon()
        {
            // Один кадр ждём чтобы оружие успело стартовать
            yield return null;

            // Ждём пока оружие не закончит
            bool weaponStillActive = true;
            while (weaponStillActive)
            {
                weaponStillActive = false;
                if (_handleWeaponList != null)
                {
                    foreach (CharacterHandleWeapon hw in _handleWeaponList)
                    {
                        if (hw.CurrentWeapon == null) continue;
                        Weapon.WeaponStates s = hw.CurrentWeapon.WeaponState.CurrentState;
                        if (s != Weapon.WeaponStates.WeaponIdle && s != Weapon.WeaponStates.WeaponStop)
                        {
                            weaponStillActive = true;
                            break;
                        }
                    }
                }
                if (weaponStillActive) yield return null;
            }

            // Сбрасываем только если никто другой стейт не занял
            if (_movement.CurrentState == CharacterStates.MovementStates.SpecialAttacking)
            {
                _movement.ChangeState(CharacterStates.MovementStates.Idle);
            }
        }
        protected virtual void StartBlocking()
        {
            if (_blocking) return;

            if (_handleWeaponList != null)
            {
                foreach (CharacterHandleWeapon handleWeapon in _handleWeaponList)
                {
                    handleWeapon?.ForceStop();
                }
            }
            
            _blocking = true;
            _movement.ChangeState(CharacterStates.MovementStates.SpecialAttacking);

            if (ParryEnabled)
            {
                _parryWindowActive = true;
                _parryWindowTimer = 0f;
            }

            if (!AllowMovementWhileBlocking && _characterMovement != null)
            {
                _characterMovement.MovementForbidden = true;
            }
            else if (_characterMovement != null)
            {
                _originalMovementSpeed = _characterMovement.MovementSpeedMultiplier;
                _characterMovement.MovementSpeedMultiplier = MovementSpeedMultiplier;
            }

            MMAnimatorExtensions.UpdateAnimatorTrigger(
                _animator, 
                _blockStartedParameter, 
                _character._animatorParameters
            );
            BlockStartFeedback?.PlayFeedbacks(transform.position);
            PlayAbilityStartFeedbacks();
        }
        
        protected virtual void StopBlocking(bool weaponInterrupt = false)
        {
            if (!_blocking) return;

            _blocking = false;
            _parryWindowActive = false;

            // Если прерываем ради оружия — не сбрасываем стейт в Idle,
            // CharacterMovement сам перейдёт в Walking/Idle со следующего кадра,
            // минуя конфликт с аниматором атаки
            if (!weaponInterrupt && _movement.CurrentState == CharacterStates.MovementStates.SpecialAttacking)
            {
                _movement.ChangeState(CharacterStates.MovementStates.Idle);
            }

            if (_characterMovement != null)
            {
                _characterMovement.MovementForbidden = false;
                if (AllowMovementWhileBlocking)
                {
                    _characterMovement.MovementSpeedMultiplier = _originalMovementSpeed;
                }
            }

            BlockStopFeedback?.PlayFeedbacks(transform.position);
            StopStartFeedbacks();
            PlayAbilityStopFeedbacks();
        }

        protected virtual void UpdateParryWindow()
        {
            if (!_parryWindowActive) return;

            _parryWindowTimer += Time.deltaTime;
            if (_parryWindowTimer >= ParryWindowDuration)
            {
                _parryWindowActive = false;
            }
        }

        /// <summary>
        /// Call this from Health component when taking damage
        /// Returns modified damage value
        /// </summary>
        public virtual float ProcessIncomingDamage(float damage, Vector3 damageDirection, GameObject instigator)
        {
            if (!_blocking) return damage;

            // Check if attack is from protected direction
            Vector3 attackDirection = (damageDirection - transform.position).normalized;
            Vector3 shieldWorldDirection = transform.TransformDirection(ShieldDirection);
            float angle = Vector3.Angle(shieldWorldDirection, attackDirection);

            if (angle > ProtectionArc / 2f)
            {
                return damage; // Attack from unprotected side
            }

            // Check for parry
            if (_parryWindowActive && ParryEnabled)
            {
                return ExecuteParry(damage, instigator);
            }

            // Regular block
            BlockHitFeedback?.PlayFeedbacks(transform.position);
            
            float finalDamage = PerfectBlock ? 0f : damage * (1f - DamageReduction);
            return finalDamage;
        }

        protected virtual float ExecuteParry(float damage, GameObject attacker)
        {
            _parryWindowActive = false;
            
            ParrySuccessFeedback?.PlayFeedbacks(transform.position);
            MMAnimatorExtensions.UpdateAnimatorTrigger(
                _animator, 
                _parriedParameter, 
                _character._animatorParameters
            );

            // Return damage to attacker
            if (attacker != null)
            {
                Health attackerHealth = attacker.GetComponent<Health>();
                if (attackerHealth != null)
                {
                    float returnDamage = damage * ParryDamageMultiplier;
                    attackerHealth.Damage(
                        returnDamage, 
                        gameObject, 
                        0.2f, 
                        0.2f, 
                        -ShieldDirection
                    );

                    if (StunOnParry)
                    {
                        Character attackerCharacter = attacker.GetComponent<Character>();
                        if (attackerCharacter != null)
                        {
                            attackerCharacter.ChangeCharacterConditionTemporarily(
                                CharacterStates.CharacterConditions.Stunned,
                                StunDuration,
                                true,
                                false
                            );
                        }
                    }
                }
            }

            return 0f; // Perfect parry = no damage
        }

        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(
                _blockingParameterName, 
                AnimatorControllerParameterType.Bool, 
                out _blockingParameter
            );
            RegisterAnimatorParameter(
                _blockStartedParameterName, 
                AnimatorControllerParameterType.Trigger, 
                out _blockStartedParameter
            );
            RegisterAnimatorParameter(
                _parriedParameterName, 
                AnimatorControllerParameterType.Trigger, 
                out _parriedParameter
            );
            RegisterAnimatorParameter(
                _parryWindowParameterName, 
                AnimatorControllerParameterType.Bool, 
                out _parryWindowParameter
            );
        }

        public override void UpdateAnimator()
        {
            MMAnimatorExtensions.UpdateAnimatorBool(
                _animator, 
                _blockingParameter, 
                _blocking, 
                _character._animatorParameters, 
                _character.RunAnimatorSanityChecks
            );
            
            MMAnimatorExtensions.UpdateAnimatorBool(
                _animator, 
                _parryWindowParameter, 
                _parryWindowActive, 
                _character._animatorParameters, 
                _character.RunAnimatorSanityChecks
            );
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            if (_blocking)
            {
                StopBlocking();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_movement != null)
            {
                _movement.OnStateChange -= OnMovementStateChanged;
            }

            if (_blocking)
            {
                StopBlocking();
            }
        }

        protected virtual void OnDrawGizmos()
        {
            if (!_blocking) return;

            Gizmos.color = _parryWindowActive ? Color.yellow : Color.blue;
            
            Vector3 worldDirection = transform.TransformDirection(ShieldDirection);
            Vector3 arcStart = Quaternion.Euler(0, -ProtectionArc / 2f, 0) * worldDirection;
            
            for (int i = 0; i <= 20; i++)
            {
                float angle = (ProtectionArc / 20f) * i;
                Vector3 direction = Quaternion.Euler(0, angle - ProtectionArc / 2f, 0) * worldDirection;
                Gizmos.DrawRay(transform.position, direction * 2f);
            }
        }
    }
}