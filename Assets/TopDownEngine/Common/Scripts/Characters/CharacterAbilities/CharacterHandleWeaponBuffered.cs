using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Drop-in replacement for CharacterHandleWeapon that adds broad input buffering.
    ///
    /// The stock CharacterHandleWeapon already buffers input when the WEAPON itself
    /// is busy (WeaponState != Idle). This subclass adds a second, outer buffer that
    /// activates when the CHARACTER is unavailable — i.e. AbilityAuthorized is false
    /// (due to BlockingMovementStates, BlockingConditionStates, BlockingWeaponStates)
    /// or the character condition is not Normal.
    ///
    /// As soon as both the character AND the weapon are free the buffered shot fires.
    ///
    /// Behaviour:
    ///   • Shoot pressed, character blocked  → outer buffer stores the request.
    ///   • Every frame the character becomes available → ConsumeIfActive → ShootStart().
    ///   • Shoot released while buffering    → outer buffer is cleared (intent cancelled).
    ///   • Outer buffer expires on its own if the window passes without opportunity.
    ///   • Death / Disable                  → outer buffer is cleared immediately.
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

            // ── Button released — cancel any pending buffer ───────────────────
            if (shootReleased)
            {
                OuterInputBuffer.Clear();
            }

            // ── Button down ───────────────────────────────────────────────────
            if (shootDown)
            {
                if (characterAvailable)
                {
                    // Character is free; delegate to the normal shoot path.
                    // The base class will handle its own inner (per-weapon) buffering.
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