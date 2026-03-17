using FIMSpace.FProceduralAnimation;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Moves toward the target and rotates the character root to face movement direction.
    /// Drop-in replacement for AIActionMoveTowardsTarget3D that doesn't need CharacterOrientation3D.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Chase Target 3D")]
    public class AIActionChaseTarget3D : AIAction
    {
        [Tooltip("Minimum distance from the target this character can reach")]
        public float MinimumDistance = 1f;

        [Tooltip("How fast the character rotates to face movement direction (degrees/sec)")]
        public float RotationSpeed = 720f;

        protected CharacterMovement _characterMovement;
        protected Transform _characterRoot;
        protected LegsAnimator _legsAnimator;
        protected Vector2 _movementVector;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            var character = gameObject.GetComponentInParent<Character>();
            _characterMovement = character?.FindAbility<CharacterMovement>();
            _characterRoot = character != null ? character.transform : transform;
            _legsAnimator = gameObject.GetComponentInParent<LegsAnimator>();
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            _legsAnimator?.User_SetIsMoving(true);
        }

        public override void PerformAction()
        {
            if (_brain.Target == null) return;

            Vector3 dirToTarget = _brain.Target.position - _characterRoot.position;
            dirToTarget.y = 0f;

            // Move
            _movementVector.x = dirToTarget.x;
            _movementVector.y = dirToTarget.z;
            _characterMovement.SetMovement(_movementVector);

            if (Mathf.Abs(_characterRoot.position.x - _brain.Target.position.x) < MinimumDistance)
                _characterMovement.SetHorizontalMovement(0f);

            if (Mathf.Abs(_characterRoot.position.z - _brain.Target.position.z) < MinimumDistance)
                _characterMovement.SetVerticalMovement(0f);

            // Rotate to face movement direction
            if (dirToTarget.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dirToTarget.normalized);
                float maxStep = RotationSpeed * Time.deltaTime;
                _characterRoot.rotation = Quaternion.RotateTowards(_characterRoot.rotation, targetRot, maxStep);
            }

            if (_legsAnimator != null)
            {
                _legsAnimator.User_SetDesiredMovementDirection(dirToTarget.normalized);
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
