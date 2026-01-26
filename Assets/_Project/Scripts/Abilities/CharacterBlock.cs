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
        [Header("Block Settings")] 
        [Tooltip("Duration of the block in seconds")]
        public float BlockDuration = 2f;

        [Tooltip("Damage reduction percentage during normal block (0-1)")]
        public float BlockDamageReduction = 0.6f;

        [Header("Parry Settings")] 
        [Tooltip("Duration of parry window in seconds")]
        public float ParryWindow = 0.3f;

        [Header("Cooldown")] 
        [Tooltip("Cooldown between block uses")]
        public MMCooldown Cooldown;

        [Header("Feedbacks")] 
        [Tooltip("Feedback when entering block/parry state")]
        public MMFeedbacks BlockStartFeedback;

        [Tooltip("Feedback when successfully parrying an attack")]
        public MMFeedbacks ParrySuccessFeedback;

        [Tooltip("Feedback during normal block")]
        public MMFeedbacks BlockHitFeedback;

        [Tooltip("Feedback when block ends")] 
        public MMFeedbacks BlockStopFeedback;

        protected bool _blocking = false;
        protected bool _inParryWindow = false;
        protected float _blockTimer = 0f;
        protected float _parryTimer = 0f;

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
            ParrySuccessFeedback?.Initialization(gameObject);
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

            // Stop blocking on button up
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

                // Handle parry window timing
                if (_inParryWindow)
                {
                    _parryTimer += Time.deltaTime;
                    if (_parryTimer >= ParryWindow) 
                    {
                        ExitParryWindow();
                    }
                }

                // Auto-stop block after duration
                if (_blockTimer >= BlockDuration) 
                {
                    BlockStop();
                }
            }
        }

        /// <summary>
        /// Starts the block, changes movement state to SpecialAttacking
        /// </summary>
        protected virtual void BlockStart()
        {
            // Start cooldown
            Cooldown.Start();
            
            // Set blocking flags
            _blocking = true;
            _inParryWindow = true;
            _blockTimer = 0f;
            _parryTimer = 0f;

            // Change to SpecialAttacking state to prevent conflicts
            _movement.ChangeState(CharacterStates.MovementStates.SpecialAttacking);

            // Disable movement during block
            if (_characterMovement != null) 
            {
                _characterMovement.MovementForbidden = true;
            }

            // Play feedbacks
            BlockStartFeedback?.PlayFeedbacks(transform.position);
            PlayAbilityStartFeedbacks();
        }

        /// <summary>
        /// Exits the parry window, transitioning to normal block
        /// </summary>
        protected virtual void ExitParryWindow()
        {
            _inParryWindow = false;
        }

        /// <summary>
        /// Stops the block and restores previous movement state
        /// </summary>
        protected virtual void BlockStop()
        {
            if (!_blocking) 
                return;

            // Stop cooldown
            Cooldown.Stop();
            
            // Clear blocking flags
            _blocking = false;
            _inParryWindow = false;
            _blockTimer = 0f;
            _parryTimer = 0f;

            // Re-enable movement
            if (_characterMovement != null) 
            {
                _characterMovement.MovementForbidden = false;
            }

            _movement.ChangeState(CharacterStates.MovementStates.Idle);


            // Play feedbacks
            BlockStopFeedback?.PlayFeedbacks(transform.position);
            PlayAbilityStopFeedbacks();
        }

        /// <summary>
        /// Called when character is hit, handles parry or block logic
        /// </summary>
        protected override void OnHit()
        {
            base.OnHit();

            if (!_blocking) 
                return;

            if (_inParryWindow)
            {
                OnParrySuccess();
            }
            else
            {
                OnBlockHit();
            }
        }

        /// <summary>
        /// Handles successful parry - negates damage completely
        /// </summary>
        protected virtual void OnParrySuccess()
        {
            ParrySuccessFeedback?.PlayFeedbacks(transform.position);

            // Temporarily disable damage
            _health.DamageDisabled();
            
            // Re-enable damage after one frame to allow the hit to process
            StartCoroutine(ReenableDamageAfterFrame());
        }

        /// <summary>
        /// Handles normal block - reduces damage
        /// </summary>
        protected virtual void OnBlockHit()
        {
            BlockHitFeedback?.PlayFeedbacks(transform.position);

            // Calculate reduced damage and restore health accordingly
            var reducedDamage = _health.LastDamage * (1f - BlockDamageReduction);
            _health.SetHealth(_health.CurrentHealth + (_health.LastDamage - reducedDamage));
        }

        /// <summary>
        /// Coroutine to re-enable damage after one frame (for parry)
        /// </summary>
        protected virtual IEnumerator ReenableDamageAfterFrame()
        {
            yield return null;
            _health.DamageEnabled();
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
        }

        /// <summary>
        /// Stop blocking when ability is disabled
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            if (_blocking) 
            {
                BlockStop();
            }
        }
    }
}