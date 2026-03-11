using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Drop-in replacement for CharacterHandleWeapon that adds broad input buffering
    /// and full participation in the ability interrupt system.
    ///
    /// The stock CharacterHandleWeapon already buffers input when the WEAPON itself
    /// is busy (WeaponState != Idle).  This subclass adds a second, outer buffer that
    /// activates when the CHARACTER is unavailable — i.e. AbilityAuthorized is false
    /// (due to BlockingMovementStates, BlockingConditionStates, BlockingWeaponStates)
    /// or the character condition is not Normal.
    ///
    /// As soon as both the character AND the weapon are free the buffered shot fires.
    ///
    /// Interrupt system participation:
    ///   • OwnTags          → assign "Attack_Ranged" in the Inspector.
    ///   • CanInterruptTags → assign tags you want shooting to cancel (e.g. "Block"),
    ///                        or leave empty if shooting cannot initiate interrupts.
    ///   • InterruptibleByTags → assign "Evasion", "Counter", etc.
    ///
    /// Behaviour:
    ///   • Shoot pressed, character blocked  → outer buffer stores the request.
    ///   • Every frame the character becomes available → ConsumeIfActive → ShootStart().
    ///   • Shoot released while buffering    → outer buffer is cleared (intent cancelled).
    ///   • Outer buffer expires on its own if the window passes without opportunity.
    ///   • Death / Disable                   → outer buffer is cleared immediately.
    ///   • Interrupted by another ability    → weapon force-stopped.
    ///
    /// The existing per-weapon BufferInput / MaximumBufferDuration settings continue
    /// to work exactly as before and are orthogonal to this mechanism.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/Abilities/Character Handle Weapon Buffered")]
    public class CharacterHandleWeaponBuffered : CharacterHandleWeapon
    {
        [Header("Outer Input Buffer")]
        [Tooltip(
            "Buffers a shoot request made while the character itself is temporarily " +
            "unavailable (blocking movement/condition states). " +
            "This is separate from the per-weapon BufferInput setting above.")]
        public AbilityInputBuffer OuterInputBuffer = new AbilityInputBuffer { BufferDuration = 0.3f };

        // ── IsActive ──────────────────────────────────────────────────────────

        // FIX: The base class returns (_movement.CurrentState != Idle).
        // Firing a weapon does NOT change the movement state — the character keeps
        // Walking or Idle — so the base implementation always returns false while
        // shooting. This means the interrupt system never sees this ability as active,
        // so nothing can interrupt an ongoing attack via tags.
        // Override to inspect the weapon state directly.
        public override bool IsActive
        {
            get
            {
                if (CurrentWeapon == null) return false;
                Weapon.WeaponStates state = CurrentWeapon.WeaponState.CurrentState;
                return state != Weapon.WeaponStates.WeaponIdle
                    && state != Weapon.WeaponStates.WeaponStop;
            }
        }

        // ── Interrupt system ──────────────────────────────────────────────────

        // FIX: When another ability (e.g. a dash with CanInterruptTags containing
        // "Attack_Ranged") interrupts us, we must stop the weapon.
        // Without this override the base implementation does nothing, meaning the
        // weapon would continue firing through the interruption.
        public override void OnInterruptedBy(CharacterAbility interruptor)
        {
            OuterInputBuffer.Clear();
            ForceStop();
        }
        
        public override void ForceStop()
        {
            _buffering = false;          // kill the base-class per-weapon buffer
            OuterInputBuffer.Clear();    // kill the outer (character-level) buffer
            base.ForceStop();
        }

        // ── Hit reaction ────────────────────────────────────────────────────

        // FIX: The base OnHit calls CurrentWeapon.Interrupt() which only changes
        // the weapon state to WeaponInterrupted and (in MeleeWeapon) stops the
        // attack coroutine. But DisableDamageArea() is deferred to the next
        // LateUpdate when CaseWeaponInterrupted runs. This leaves the melee
        // damage area collider enabled until then — the weapon hitbox stays
        // active even though the stagger animation is already playing.
        // Calling ForceStop() immediately invokes TurnWeaponOff → DisableDamageArea,
        // ensuring the hitbox is killed the same frame the character gets hit.
        protected override void OnHit()
        {
            base.OnHit();  // fires Interrupt() via CharacterHandleWeapon.OnHit
            if (GettingHitInterruptsAttack && CurrentWeapon != null)
            {
                ForceStop();
            }
        }
        // ── Input ─────────────────────────────────────────────────────────────

        protected override void HandleInput()
        {
            // Weapon absent — nothing to do.
            if (CurrentWeapon == null)
            {
                OuterInputBuffer.Clear();
                return;
            }

            bool shootDown =
                _inputManager.ShootButton.State.CurrentState == MMInput.ButtonStates.ButtonDown
                || _inputManager.ShootAxis == MMInput.ButtonStates.ButtonDown;

            bool shootHeld =
                _inputManager.ShootButton.State.CurrentState == MMInput.ButtonStates.ButtonPressed
                || _inputManager.ShootAxis == MMInput.ButtonStates.ButtonPressed;

            bool shootReleased =
                _inputManager.ShootButton.State.CurrentState == MMInput.ButtonStates.ButtonUp
                || _inputManager.ShootAxis == MMInput.ButtonStates.ButtonUp;

            bool characterAvailable =
                AbilityAuthorized
                && _condition.CurrentState == CharacterStates.CharacterConditions.Normal;

            // ── Button released — cancel any pending outer buffer ─────────────
            if (shootReleased)
            {
                OuterInputBuffer.Clear();
            }

            // ── Button down ───────────────────────────────────────────────────
            if (shootDown)
            {
                if (characterAvailable)
                {
                    // Character is free — delegate to the normal shoot path which
                    // calls RequestAbilityActivation internally.
                    ShootStart();
                }
                else
                {
                    // Character is blocked — remember the intent.
                    OuterInputBuffer.Request();
                }
            }

            // ── Auto fire (held, Auto trigger mode) ───────────────────────────
            if (shootHeld
                && characterAvailable
                && ContinuousPress
                && CurrentWeapon.TriggerMode == Weapon.TriggerModes.Auto)
            {
                ShootStart();
            }

            // ── Auto combo (held) ─────────────────────────────────────────────
            if (shootHeld && characterAvailable && ContinuousPress && CurrentWeapon.IsAutoComboWeapon)
            {
                ShootStart();
            }

            // ── Secondary axis threshold shoot ────────────────────────────────
            if (characterAvailable
                && UseSecondaryAxisThresholdToShoot
                && _inputManager.SecondaryMovement.magnitude > _inputManager.Threshold.magnitude)
            {
                ShootStart();
            }

            // ── Force-always-shoot ────────────────────────────────────────────
            if (characterAvailable && ForceAlwaysShoot)
            {
                ShootStart();
            }

            // ── Reload ────────────────────────────────────────────────────────
            if (_inputManager.ReloadButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
            {
                Reload();
            }

            // ── Release ───────────────────────────────────────────────────────
            if (shootReleased && characterAvailable)
            {
                ShootStop();
                CurrentWeapon.WeaponInputReleased();
            }

            // ── Weapon idle while input off ───────────────────────────────────
            if (characterAvailable
                && CurrentWeapon.WeaponState.CurrentState == Weapon.WeaponStates.WeaponDelayBetweenUses
                && _inputManager.ShootAxis == MMInput.ButtonStates.Off
                && _inputManager.ShootButton.State.CurrentState == MMInput.ButtonStates.Off
                && !(UseSecondaryAxisThresholdToShoot
                     && _inputManager.SecondaryMovement.magnitude > _inputManager.Threshold.magnitude))
            {
                CurrentWeapon.WeaponInputStop();
            }
        }

        // ── ShootStart ────────────────────────────────────────────────────────

        // FIX: The base ShootStart never calls RequestAbilityActivation, so firing
        // does not participate in the interrupt system as an initiator.
        // If CanInterruptTags is populated (e.g. "Block"), shooting will now
        // correctly signal intent to interrupt, and the target ability will decide
        // whether to allow it based on its own InterruptibleByTags.
        public override void ShootStart()
        {
            if (!AbilityAuthorized
                || CurrentWeapon == null
                || _condition.CurrentState != CharacterStates.CharacterConditions.Normal)
            {
                return;
            }

            // Announce intent to the interrupt system.
            // If CanInterruptTags is empty this is a no-op (no abilities are checked).
            // If CanInterruptTags contains e.g. "Block" and the shield is active, this
            // will either interrupt the shield (if it allows it) or return false (if not).
            if (!_character.RequestAbilityActivation(this))
            {
                // Something uninterruptible is blocking us — buffer for retry.
                OuterInputBuffer.Request();
                return;
            }

            // Delegate the actual firing to the base implementation which handles
            // the per-weapon buffer (BufferInput / MaximumBufferDuration).
            base.ShootStart();
        }

        // ── Process ───────────────────────────────────────────────────────────

        public override void ProcessAbility()
        {
            // Run the base logic (HandleCharacterState, HandleFeedbacks,
            // UpdateAmmoDisplay, HandleBuffer).
            base.ProcessAbility();

            FlushOuterBuffer();
        }

        /// <summary>
        /// Fires the buffered shot as soon as the character becomes available.
        /// </summary>
        private void FlushOuterBuffer()
        {
            if (!OuterInputBuffer.IsActive) return;

            if (CurrentWeapon == null)
            {
                OuterInputBuffer.Clear();
                return;
            }

            bool characterAvailable =
                AbilityAuthorized
                && _condition.CurrentState == CharacterStates.CharacterConditions.Normal;

            if (!characterAvailable) return;

            if (OuterInputBuffer.ConsumeIfActive())
            {
                // ShootStart now calls RequestAbilityActivation, which may re-buffer
                // if something uninterruptible is still in the way — that is correct.
                ShootStart();
            }
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void OnDeath()
        {
            base.OnDeath();
            OuterInputBuffer.Clear();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            OuterInputBuffer.Clear();
        }
    }
}