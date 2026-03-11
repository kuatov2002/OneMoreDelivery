using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Moves away from target (kiting behavior)
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Move Away")]
    public class AIActionMoveAway : AIAction
    {
        [Tooltip("Movement speed multiplier")]
        public float SpeedMultiplier = 1.2f;
        
        [Tooltip("Minimum distance to maintain")]
        public float MinDistance = 8f;

        protected CharacterMovement _characterMovement;
        protected TopDownController _controller;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            Character character = GetComponentInParent<Character>();
            _characterMovement = character?.FindAbility<CharacterMovement>();
            _controller = character?.GetComponent<TopDownController>();
        }

        public override void PerformAction()
        {
            if (_brain.Target == null) return;
            if (_characterMovement == null || _controller == null) return;

            float distance = Vector3.Distance(transform.position, _brain.Target.position);

            if (distance < MinDistance)
            {
                // Move away
                Vector3 awayDirection = (transform.position - _brain.Target.position).normalized;
                _characterMovement.SetMovement(new Vector2(awayDirection.x, awayDirection.z) * SpeedMultiplier);
            }
            else
            {
                // Stop moving
                _characterMovement.SetMovement(Vector2.zero);
            }
        }

        public override void OnExitState()
        {
            base.OnExitState();
            if (_characterMovement != null)
            {
                _characterMovement.SetMovement(Vector2.zero);
            }
        }
    }
}