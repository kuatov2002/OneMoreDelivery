using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Extends CharacterChargeDash3D with a charge-based cooldown system.
    /// Instead of a single cooldown, the player has multiple dash charges
    /// that recover sequentially (one at a time).
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/Abilities/Shtefan Charge Dash")]
    public class ShtefanChargeDash : CharacterChargeDash3D
    {
        [Header("Charge System")]
        [Tooltip("Maximum number of dash charges")]
        public int MaxCharges = 3;

        [Tooltip("Time in seconds for each charge to recover")]
        public float ChargeRecoveryTime = 3f;

        /// <summary>
        /// Number of charges currently available.
        /// </summary>
        public int CurrentCharges { get; private set; }

        /// <summary>
        /// Current recovery timer for the next charge (0 to ChargeRecoveryTime).
        /// 0 when all charges are full.
        /// </summary>
        public float CurrentRecoveryTimer { get; private set; }

        /// <summary>
        /// How many charges are currently recovering (queued).
        /// </summary>
        public int RecoveringCount => MaxCharges - CurrentCharges;

        // ── Initialization ──────────────────────────────────────────────────

        protected override void Initialization()
        {
            Cooldown.Unlimited = true; // bypass built-in single cooldown
            base.Initialization();

            CurrentCharges = MaxCharges;
            CurrentRecoveryTimer = 0f;
        }

        // ── Input ───────────────────────────────────────────────────────────

        protected override void HandleInput()
        {
            if (_inputManager == null) return;
            if (_inputManager.DashButton.State.CurrentState != MMInput.ButtonStates.ButtonDown)
                return;

            bool canDash = AbilityAuthorized
                           && CurrentCharges > 0
                           && _condition.CurrentState == CharacterStates.CharacterConditions.Normal;

            if (canDash)
            {
                StartDash();
            }
            else
            {
                InputBuffer.Request();
            }
        }

        // ── Dash ────────────────────────────────────────────────────────────

        protected override void StartDash()
        {
            if (CurrentCharges <= 0) return;
            if (_dashing) return; // don't restart mid-dash

            base.StartDash();

            // Only consume a charge if the dash actually started
            if (_dashing)
            {
                ConsumeCharge();
            }
        }

        // ── Process ─────────────────────────────────────────────────────────

        public override void ProcessAbility()
        {
            // Sequential charge recovery — only one charge recovers at a time
            if (CurrentCharges < MaxCharges)
            {
                CurrentRecoveryTimer -= Time.deltaTime;
                if (CurrentRecoveryTimer <= 0f)
                {
                    CurrentCharges++;
                    // If still missing charges, start the next timer
                    CurrentRecoveryTimer = CurrentCharges < MaxCharges ? ChargeRecoveryTime : 0f;
                }
            }

            // Flush buffered input with charge check
            if (!_dashing
                && InputBuffer.IsActive
                && AbilityAuthorized
                && CurrentCharges > 0
                && _condition.CurrentState == CharacterStates.CharacterConditions.Normal)
            {
                if (InputBuffer.ConsumeIfActive())
                {
                    StartDash();
                }
            }

            if (!_dashing) return;

            // Dash movement (replicated from base to avoid base's buffer logic)
            if (_dashTimer < DashDuration)
            {
                float   t          = _dashTimer / DashDuration;
                float   curveValue = DashCurve.Evaluate(t);
                Vector3 newPos     = Vector3.Lerp(_dashOrigin, _dashDestination, curveValue);

                Vector3 castOrigin = transform.position + Vector3.up * VerticalOffset;
                Vector3 step       = newPos - castOrigin;
                float   stepLen    = step.magnitude;

                bool blocked = stepLen > 0.001f
                    && Physics.SphereCast(
                        castOrigin, CharacterRadius, step / stepLen,
                        out _, stepLen,
                        ObstacleLayerMask, QueryTriggerInteraction.Ignore);

                if (!blocked)
                {
                    _controller.MovePosition(newPos);
                }

                _dashTimer += Time.deltaTime;
            }
            else
            {
                StopDash();
            }
        }

        // ── Charge helpers ──────────────────────────────────────────────────

        private void ConsumeCharge()
        {
            CurrentCharges--;
            // Start recovery timer if this is the first missing charge
            if (CurrentRecoveryTimer <= 0f)
            {
                CurrentRecoveryTimer = ChargeRecoveryTime;
            }
        }
    }
}
