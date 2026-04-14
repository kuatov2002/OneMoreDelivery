using FIMSpace.FProceduralAnimation;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Alerted action: the moment the enemy first notices the player.
    /// Stops, snaps to face the target, plays a short "I see you" pose
    /// through MeleeEnemyProceduralBody before chasing.
    ///
    /// Pairs with a short AIDecisionTimeInState transition (~0.35s) to Chase.
    /// Gives the enemy personality and telegraphs engagement to the player.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Alerted")]
    public class AIActionAlerted : AIAction
    {
        [Header("Rotation")]
        [Tooltip("How fast the enemy snaps to face the target (degrees/sec)")]
        public float RotationSpeed = 720f;

        [Header("SFX Hook (optional)")]
        [Tooltip("Optional feedback to play when first alerted — hook up bark/gasp SFX here")]
        [SerializeField] private MMFeedbacks _alertedFeedback;

        protected CharacterMovement _characterMovement;
        protected Transform _characterRoot;
        protected LegsAnimator _legsAnimator;
        protected MeleeEnemyProceduralBody _proceduralBody;

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

            _characterMovement?.SetMovement(Vector2.zero);
            _proceduralBody?.SetState(MeleeEnemyProceduralBody.BodyState.Alert);

            // Keep legs "planted" during the alert pose
            _legsAnimator?.User_SetIsMoving(false);

            _alertedFeedback?.PlayFeedbacks();
        }

        public override void PerformAction()
        {
            _characterMovement?.SetMovement(Vector2.zero);

            if (_brain.Target == null) return;

            Vector3 dirToTarget = _brain.Target.position - _characterRoot.position;
            dirToTarget.y = 0f;

            if (dirToTarget.sqrMagnitude < 0.001f) return;

            Quaternion targetRot = Quaternion.LookRotation(dirToTarget.normalized);
            float maxStep = RotationSpeed * Time.deltaTime;
            _characterRoot.rotation = Quaternion.RotateTowards(_characterRoot.rotation, targetRot, maxStep);
        }
    }
}
