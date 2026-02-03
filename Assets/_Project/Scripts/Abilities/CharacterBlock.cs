using UnityEngine;
using System.Collections;
using MoreMountains.Tools;
using MoreMountains.Feedbacks;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Ability for knight's block with parry mechanic
    /// First frames work as parry, then becomes normal block (damage reduction)
    /// Uses SpecialAttacking state to avoid conflicts with other abilities
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/Abilities/Character Block")]
    public class CharacterBlock : CharacterAbility
    {
        [Header("Cooldown")] 
        [Tooltip("Cooldown between block uses")]
        public MMCooldown Cooldown;

        [Header("Block Settings")]
        [Tooltip("Invulnerability duration after releasing block")]
        public float InvulnerabilityAfterBlockDuration = 0.3f;

        [Header("Feedbacks")] 
        [Tooltip("Feedback when entering block/parry state")]
        public MMFeedbacks BlockStartFeedback;

        [Tooltip("Feedback during normal block")]
        public MMFeedbacks BlockHitFeedback;

        [Tooltip("Feedback when block ends")] 
        public MMFeedbacks BlockStopFeedback;

        protected bool _blocking = false;
        protected float _blockTimer = 0f;
        protected float _invulnerabilityTimer = 0f;
        protected bool _invulnerabilityActive = false;

        protected const string _blockingAnimationParameterName = "Blocking";
        protected int _blockingAnimationParameter;

        public override string HelpBoxText()
        {
            return
                "Knight's block ability with parry mechanic. Hold the block button - first frames parry attacks (reflect damage + bonuses), then normal block (damage reduction). Uses SpecialAttacking state to prevent conflicts with other abilities.";
        }

        protected override void Initialization()
        {
            base.Initialization();
            Cooldown.Initialization();
            BlockStartFeedback?.Initialization(gameObject);
            BlockHitFeedback?.Initialization(gameObject);
            BlockStopFeedback?.Initialization(gameObject);
        }

        protected override void HandleInput()
        {
            base.HandleInput();

            // Check if ability is authorized and character is in normal condition
            if (!AbilityAuthorized || _condition.CurrentState != CharacterStates.CharacterConditions.Normal) 
                return;

            // Start blocking on button down if not already blocking and cooldown is ready
            if (_inputManager.SecondaryShootButton.State.CurrentState == MMInput.ButtonStates.ButtonDown && 
                !_blocking && Cooldown.Ready()) 
            {
                BlockStart();
            }

            // Stop blocking on button up (immediately, but keep invulnerability)
            if (_inputManager.SecondaryShootButton.State.CurrentState == MMInput.ButtonStates.ButtonUp && _blocking) 
            {
                BlockStop();
            }
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();
            Cooldown.Update();

            if (_blocking)
            {
                _blockTimer += Time.deltaTime;

                // Auto-stop block after duration
                if (Cooldown.CurrentDurationLeft <= 0) 
                {
                    BlockStop();
                }
            }

            // Handle invulnerability timer after block ends
            if (_invulnerabilityActive && !_blocking)
            {
                _invulnerabilityTimer += Time.deltaTime;
                
                if (_invulnerabilityTimer >= InvulnerabilityAfterBlockDuration)
                {
                    RemoveInvulnerability();
                }
            }
        }
        
        protected virtual void OnTriggerEnter(Collider collision)
        {
            if (!_blocking) return;
    
            // Check if it's a damage source
            DamageOnTouch damageOnTouch = collision.GetComponent<DamageOnTouch>();
            if (damageOnTouch != null)
            {
                BlockHitFeedback?.PlayFeedbacks(transform.position);
                
                // Apply knockback and damage to the attacker
                ApplyKnockbackToAttacker(damageOnTouch);
            }
        }

        /// <summary>
        /// Applies knockback and damage to the attacker
        /// </summary>
        protected virtual void ApplyKnockbackToAttacker(DamageOnTouch attackerDamageOnTouch)
        {
            // Trigger the "hit non-damageable" feedback on the DamageOnTouch component
            attackerDamageOnTouch.HitNonDamageableFeedback?.PlayFeedbacks(transform.position);
        }

        protected virtual void BlockStart()
        {
            Cooldown.Start();
            _blocking = true;
            _blockTimer = 0f;
    
            // БЛОКИРУЕМ УРОН СРАЗУ!
            if (_health != null)
            {
                _health.Invulnerable = true;
                _invulnerabilityActive = true;
            }

            _movement.ChangeState(CharacterStates.MovementStates.SpecialAttacking);

            if (_characterMovement != null) 
            {
                _characterMovement.MovementForbidden = true;
            }

            BlockStartFeedback?.PlayFeedbacks(transform.position);
            PlayAbilityStartFeedbacks();
        }

        protected virtual void BlockStop()
        {
            if (!_blocking) 
                return;

            Cooldown.Stop();
            _blocking = false;
            _blockTimer = 0f;

            // НЕ снимаем неуязвимость сразу - запускаем таймер
            _invulnerabilityTimer = 0f;

            if (_characterMovement != null) 
            {
                _characterMovement.MovementForbidden = false;
            }

            _movement.ChangeState(CharacterStates.MovementStates.Idle);
            BlockStopFeedback?.PlayFeedbacks(transform.position);
            PlayAbilityStopFeedbacks();
        }

        protected virtual void RemoveInvulnerability()
        {
            if (_health != null)
            {
                _health.Invulnerable = false;
            }
            _invulnerabilityActive = false;
            _invulnerabilityTimer = 0f;
        }

        /// <summary>
        /// Resets the ability, stops blocking if active
        /// </summary>
        public override void ResetAbility()
        {
            base.ResetAbility();

            if (_blocking) 
            {
                BlockStop();
            }

            // Immediately remove invulnerability on reset
            if (_invulnerabilityActive)
            {
                RemoveInvulnerability();
            }
        }

        /// <summary>
        /// Initializes animator parameters
        /// </summary>
        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(_blockingAnimationParameterName, AnimatorControllerParameterType.Bool,
                out _blockingAnimationParameter);
        }

        /// <summary>
        /// Updates the animator with current blocking state
        /// </summary>
        public override void UpdateAnimator()
        {
            MMAnimatorExtensions.UpdateAnimatorBool(_animator, _blockingAnimationParameter, _blocking,
                _character._animatorParameters, _character.RunAnimatorSanityChecks);
        }

        /// <summary>
        /// Stop blocking on death
        /// </summary>
        protected override void OnDeath()
        {
            base.OnDeath();

            if (_blocking) 
            {
                BlockStop();
            }

            // Remove invulnerability immediately on death
            if (_invulnerabilityActive)
            {
                RemoveInvulnerability();
            }
        }
    }
}