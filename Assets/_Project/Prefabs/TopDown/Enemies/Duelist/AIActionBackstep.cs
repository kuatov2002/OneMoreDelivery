using FIMSpace.FProceduralAnimation;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Backstep action: retreats away from the target for a short duration
    /// while still facing them. Used after Stagger to create a back-and-forth
    /// combat rhythm instead of a stagger-lock punish loop.
    ///
    /// Pairs with a short AIDecisionTimeInState transition (~0.6s) to Stalk.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Backstep")]
    public class AIActionBackstep : AIAction
    {
        [Header("Movement")]
        [Tooltip("Speed multiplier while backstepping (1 = full run speed)")]
        [Range(0.1f, 1.5f)]
        public float SpeedFactor = 0.85f;

        [Tooltip("How quickly movement ramps up (smoothing). Higher = snappier retreat.")]
        public float DirectionSmoothing = 10f;

        [Header("Rotation")]
        [Tooltip("How fast the enemy rotates to keep facing the target (degrees/sec)")]
        public float RotationSpeed = 540f;

        protected CharacterMovement _characterMovement;
        protected Transform _characterRoot;
        protected LegsAnimator _legsAnimator;
        protected MeleeEnemyProceduralBody _proceduralBody;

        private Vector3 _smoothedDir;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            var character = gameObject.GetComponentInParent<Character>();
            _characterMovement = character?.FindAbility<CharacterMovement>();
            _characterRoot = character != null ? character.transform : transform;
            _legsAnimator = gameObject.GetComponentInParent<LegsAnimator>();
            _proceduralBody = gameObject.GetComponentInParent<MeleeEnemyProceduralBody>();
        }

        public override void OnEnterState()
        {
            base.OnEnterState();

            _proceduralBody?.SetState(MeleeEnemyProceduralBody.BodyState.Idle);
            _legsAnimator?.User_SetIsMoving(true);
            _smoothedDir = Vector3.zero;
        }

        public override void PerformAction()
        {
            if (_brain.Target == null) return;

            Vector3 toTarget = _brain.Target.position - _characterRoot.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.001f)
            {
                _characterMovement?.SetMovement(Vector2.zero);
                return;
            }

            Vector3 dirToTarget = toTarget.normalized;
            // Away from target
            Vector3 desiredDir = -dirToTarget * SpeedFactor;

            _smoothedDir = Vector3.Lerp(_smoothedDir, desiredDir, Time.deltaTime * DirectionSmoothing);

            Vector2 moveVec = new Vector2(_smoothedDir.x, _smoothedDir.z);
            _characterMovement?.SetMovement(moveVec);

            // Face the target while retreating (walking backwards)
            Quaternion targetRot = Quaternion.LookRotation(dirToTarget);
            float maxStep = RotationSpeed * Time.deltaTime;
            _characterRoot.rotation = Quaternion.RotateTowards(_characterRoot.rotation, targetRot, maxStep);

            if (_legsAnimator != null && _smoothedDir.sqrMagnitude > 0.01f)
            {
                _legsAnimator.User_SetDesiredMovementDirection(_smoothedDir.normalized);
            }
        }

        public override void OnExitState()
        {
            base.OnExitState();

            _characterMovement?.SetHorizontalMovement(0f);
            _characterMovement?.SetVerticalMovement(0f);

            if (_legsAnimator != null)
            {
                _legsAnimator.User_SetIsMoving(false);
                _legsAnimator.User_SetDesiredMovementDirectionOff();
            }
        }
    }
}
