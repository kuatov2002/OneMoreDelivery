using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using MoreMountains.Feedbacks;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Dash ability that deals damage to all enemies in path, with input buffering.
    ///
    /// When the player presses Dash while the ability is on cooldown or the movement
    /// state blocks it, the input is buffered and the dash fires as soon as possible.
    ///
    /// Interrupt system participation:
    ///   • OwnTags          → assign "Evasion", "Movement" in the Inspector.
    ///   • CanInterruptTags → assign "Block", "Attack_Melee", "Attack_Ranged", etc.
    ///   • InterruptibleByTags → typically empty (dash is rarely interrupted mid-flight).
    ///
    /// Animation parameters:
    ///   ChargeDashing      (bool) – true during dash
    ///   ChargeDashStarted  (bool) – true on dash start frame
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/Abilities/Character Charge Dash 3D")]
    public class CharacterChargeDash3D : CharacterAbility
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Dash Parameters")]
        [Tooltip("Distance covered during dash")]
        public float DashDistance = 15f;

        [Tooltip("Duration of the dash in seconds")]
        public float DashDuration = 0.8f;

        [Tooltip("Movement curve during dash")]
        public AnimationCurve DashCurve =
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

        [Header("Damage")]
        [Tooltip("Damage dealt to enemies")]
        public float Damage = 20f;

        [Tooltip("Layers that can be damaged")]
        public LayerMask DamageableLayerMask;

        [Tooltip("Should damage each target only once per dash?")]
        public bool DamageOncePerTarget = true;

        [Header("Detection")]
        [Tooltip("Radius for damage detection")]
        public float DetectionRadius = 1.5f;

        [Tooltip("Detection frequency in seconds (0 = every frame)")]
        public float DetectionInterval = 0.1f;

        [Header("Cooldown")]
        public MMCooldown Cooldown;

        [Header("Invincibility")]
        [Tooltip("Invincible during dash?")]
        public bool InvincibleWhileDashing = true;

        [Header("Input Buffer")]
        [Tooltip("Buffer a dash request made while the ability is temporarily unavailable")]
        public AbilityInputBuffer InputBuffer = new AbilityInputBuffer { BufferDuration = 0.3f };

        [Header("Feedbacks")]
        public MMFeedbacks DashStartFeedback;
        public MMFeedbacks DashStopFeedback;
        public MMFeedbacks HitFeedback;

        // ── State ─────────────────────────────────────────────────────────────

        protected bool    _dashing;
        protected float   _dashTimer;
        protected Vector3 _dashOrigin;
        protected Vector3 _dashDestination;
        protected Vector3 _dashDirection;
        protected HashSet<GameObject> _damagedTargets;
        protected Coroutine           _detectionCoroutine;
        protected bool   _dashStartedThisFrame;

        // Captures the player's intended direction slightly before the dash fires.
        // Updated periodically so it reflects recent intent, not the exact frame
        // of the button press (which may lag behind visible movement).
        private Vector3 _intentionDirection;
        private float   _intentionTimer;
        private const float IntentionRefreshRate = 0.05f; // 50 ms

        // ── IsActive ──────────────────────────────────────────────────────────

        // FIX: Override explicitly so the interrupt system has a precise contract.
        // The base class returns (_movement.CurrentState != Idle).
        // While dashing the state IS Dashing (!= Idle), so it accidentally works —
        // but "accidentally works" is not acceptable in shared systems code.
        public override bool IsActive => _dashing;

        // ── Animator parameters ───────────────────────────────────────────────

        protected const string _chargeDashingParameterName     = "ChargeDashing";
        protected const string _chargeDashStartedParameterName = "ChargeDashStarted";

        protected int _chargeDashingParameter;
        protected int _chargeDashStartedParameter;

        // ── Initialization ────────────────────────────────────────────────────

        protected override void Initialization()
        {
            base.Initialization();

            Cooldown.Initialization();

            DashStartFeedback?.Initialization(gameObject);
            DashStopFeedback?.Initialization(gameObject);
            HitFeedback?.Initialization(gameObject);

            _damagedTargets = new HashSet<GameObject>();
        }

        // ── Input ─────────────────────────────────────────────────────────────

        protected override void HandleInput()
        {
            if (_inputManager == null) return;

            if (_inputManager.DashButton.State.CurrentState != MMInput.ButtonStates.ButtonDown)
            {
                return;
            }

            bool canDash = AbilityAuthorized
                           && Cooldown.Ready()
                           && _condition.CurrentState == CharacterStates.CharacterConditions.Normal;

            if (canDash)
            {
                StartDash();
            }
            else
            {
                // Store for later — cooldown or a movement state is blocking us.
                InputBuffer.Request();
            }
        }

        // ── Process ───────────────────────────────────────────────────────────

        public override void ProcessAbility()
        {
            base.ProcessAbility();

            // Periodically snapshot the controller's movement direction so we have
            // a recent "intended direction" to use when the dash finally fires.
            // This is distinct from reading direction at the exact frame of the button
            // press, which can be stale if the player pressed the button a moment
            // before the buffer flush.
            _intentionTimer -= Time.deltaTime;
            if (_intentionTimer <= 0f)
            {
                Vector3 dir = _controller.CurrentDirection;
                if (dir.magnitude > 0.1f)
                {
                    _intentionDirection = dir.normalized;
                }
                _intentionTimer = IntentionRefreshRate;
            }

            Cooldown.Update();

            // Flush a buffered dash as soon as all conditions are met.
            if (!_dashing
                && InputBuffer.IsActive
                && AbilityAuthorized
                && Cooldown.Ready()
                && _condition.CurrentState == CharacterStates.CharacterConditions.Normal)
            {
                if (InputBuffer.ConsumeIfActive())
                {
                    StartDash();
                }
            }

            if (!_dashing) return;

            if (_dashTimer < DashDuration)
            {
                float   t           = _dashTimer / DashDuration;
                float   curveValue  = DashCurve.Evaluate(t);
                Vector3 newPosition = Vector3.Lerp(_dashOrigin, _dashDestination, curveValue);

                _controller.MovePosition(newPosition);
                _dashTimer += Time.deltaTime;
            }
            else
            {
                StopDash();
            }
        }

        // ── Dash ─────────────────────────────────────────────────────────────

        protected virtual void StartDash()
        {
            // FIX: Cooldown check must come before RequestAbilityActivation.
            // Previously the check appeared AFTER the call, meaning we would fire
            // interrupts on other abilities and then immediately bail out here —
            // the damage was already done (e.g. a block was cancelled) even though
            // the dash never actually started.
            if (!Cooldown.Ready()) return;

            // FIX: _dashDirection was being assigned twice.
            // First from _intentionDirection (the correct snapshot), then 
            // overwritten by _controller.CurrentDirection after Cooldown.Start().
            // The intention snapshot exists precisely so we can read a direction
            // that reflects recent input without the noise of the exact button frame.
            // We resolve direction once, here, before anything else changes state.
            _dashDirection = _intentionDirection.magnitude > 0.1f
                ? _intentionDirection
                : transform.forward;

            // Announce intent to the interrupt system AFTER validating all local
            // preconditions. This ensures we only trigger interrupts on other
            // abilities when we are actually going to follow through.
            // CanInterruptTags on this ability drives which abilities get interrupted
            // (e.g. "Block" → stops the shield; "Attack_Melee" → cancels a swing).
            if (!_character.RequestAbilityActivation(this))
            {
                // An uninterruptible ability blocked us.
                // Buffer the input so we retry as soon as it's done.
                InputBuffer.Request();
                return;
            }

            Cooldown.Start();

            StopAllWeapons();

            _movement.ChangeState(CharacterStates.MovementStates.Dashing);

            _dashing              = true;
            _dashTimer            = 0f;
            _dashOrigin           = transform.position;
            _dashStartedThisFrame = true;

            _damagedTargets.Clear();

            _dashDestination = _dashOrigin + _dashDirection * DashDistance;

            _controller.FreeMovement = false;
            _controller3D?.DetachFromMovingPlatform();

            if (InvincibleWhileDashing)
            {
                _health?.DamageDisabled();
            }

            DashStartFeedback?.PlayFeedbacks(transform.position);
            PlayAbilityStartFeedbacks();

            if (_detectionCoroutine != null)
            {
                StopCoroutine(_detectionCoroutine);
            }
            _detectionCoroutine = StartCoroutine(DetectAndDamageRoutine());
        }

        protected virtual void StopDash()
        {
            if (!_dashing) return;

            Cooldown.Stop();
            _movement.ChangeState(CharacterStates.MovementStates.Idle);

            _dashing                 = false;
            _controller.FreeMovement = true;

            if (InvincibleWhileDashing)
            {
                _health?.DamageEnabled();
            }

            if (_detectionCoroutine != null)
            {
                StopCoroutine(_detectionCoroutine);
                _detectionCoroutine = null;
            }

            DashStopFeedback?.PlayFeedbacks(transform.position);
            StopStartFeedbacks();
            PlayAbilityStopFeedbacks();
        }

        // ── Damage detection ──────────────────────────────────────────────────

        protected virtual IEnumerator DetectAndDamageRoutine()
        {
            WaitForSeconds wait = DetectionInterval > 0f
                ? new WaitForSeconds(DetectionInterval)
                : null;

            while (_dashing)
            {
                DetectAndDamage();

                if (wait != null) yield return wait;
                else              yield return null;
            }
        }

        protected virtual void DetectAndDamage()
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position, DetectionRadius, DamageableLayerMask);

            foreach (Collider hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                if (DamageOncePerTarget && _damagedTargets.Contains(hit.gameObject)) continue;

                Health targetHealth = hit.GetComponent<Health>();
                if (targetHealth == null) continue;

                targetHealth.Damage(Damage, gameObject, 0.2f, 0.2f, _dashDirection);

                if (DamageOncePerTarget)
                {
                    _damagedTargets.Add(hit.gameObject);
                }

                HitFeedback?.PlayFeedbacks(hit.transform.position);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void StopAllWeapons()
        {
            if (_handleWeaponList == null) return;

            foreach (CharacterHandleWeapon hw in _handleWeaponList)
            {
                hw?.ForceStop();
            }
        }

        // ── Animator ─────────────────────────────────────────────────────────

        protected override void InitializeAnimatorParameters()
        {
            RegisterAnimatorParameter(
                _chargeDashingParameterName,
                AnimatorControllerParameterType.Bool,
                out _chargeDashingParameter);

            RegisterAnimatorParameter(
                _chargeDashStartedParameterName,
                AnimatorControllerParameterType.Bool,
                out _chargeDashStartedParameter);
        }

        public override void UpdateAnimator()
        {
            MMAnimatorExtensions.UpdateAnimatorBool(
                _animator, _chargeDashingParameter, _dashing,
                _character._animatorParameters, _character.RunAnimatorSanityChecks);

            MMAnimatorExtensions.UpdateAnimatorBool(
                _animator, _chargeDashStartedParameter, _dashStartedThisFrame,
                _character._animatorParameters, _character.RunAnimatorSanityChecks);

            _dashStartedThisFrame = false;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void OnDeath()
        {
            base.OnDeath();
            InputBuffer.Clear();

            if (_dashing)
            {
                StopDash();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            InputBuffer.Clear();

            if (_dashing)
            {
                StopDash();
            }
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

        protected virtual void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, DetectionRadius);

            if (_dashing)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(_dashOrigin, _dashDestination);
            }
        }
    }
}