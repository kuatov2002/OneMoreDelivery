using DTT.AreaOfEffectRegions;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Telegraph action: stops movement, locks facing direction toward target,
    /// plays windup animation. Shows an ArcRegion attack indicator during the windup
    /// so the player can see the incoming attack area.
    /// Direction stays locked for the subsequent Attack state.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Telegraph")]
    public class AIActionTelegraph : AIAction
    {
        [Header("Attack Indicator")]
        [Tooltip("Reference to the ArcRegion indicator (child of this enemy)")]
        [SerializeField] private ArcRegionBase _attackIndicator;

        [Tooltip("Duration of the telegraph phase in seconds (should match the state transition threshold)")]
        [SerializeField] private float _telegraphDuration = 0.7f;

        protected CharacterMovement _characterMovement;
        protected CharacterOrientation3D _orientation;
        protected Animator _animator;

        private AIActionMeleeAttackCone _attackCone;
        private ArcRegion _arcRegion;
        private float _enterTime;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            var character = gameObject.GetComponentInParent<Character>();
            _characterMovement = character?.FindAbility<CharacterMovement>();
            _orientation = character?.FindAbility<CharacterOrientation3D>();
            _animator = character?.CharacterAnimator;

            _attackCone = GetComponent<AIActionMeleeAttackCone>();
            _arcRegion = _attackIndicator as ArcRegion;

            if (_attackIndicator != null)
                _attackIndicator.gameObject.SetActive(false);
        }

        public override void OnEnterState()
        {
            base.OnEnterState();

            _characterMovement?.SetMovement(Vector2.zero);

            if (_brain.Target != null && _orientation != null)
            {
                Vector3 dirToTarget = _brain.Target.position - transform.position;
                dirToTarget.y = 0f;
                if (dirToTarget.sqrMagnitude > 0.001f)
                {
                    _orientation.ForcedRotation = true;
                    _orientation.ForcedRotationDirection = dirToTarget.normalized;
                }
            }

            if (_animator != null)
                _animator.SetTrigger("Telegraph");

            ShowAttackIndicator();
            _enterTime = Time.time;
        }

        public override void PerformAction()
        {
            _characterMovement?.SetMovement(Vector2.zero);

            if (_arcRegion != null && _telegraphDuration > 0f)
            {
                float elapsed = Time.time - _enterTime;
                _arcRegion.FillProgress = Mathf.Clamp01(elapsed / _telegraphDuration);
            }
        }

        public override void OnExitState()
        {
            base.OnExitState();

            if (_attackIndicator != null)
                _attackIndicator.gameObject.SetActive(false);
        }

        private void ShowAttackIndicator()
        {
            if (_attackIndicator == null) return;

            if (_attackCone != null)
            {
                _attackIndicator.Arc = _attackCone.ConeAngle;
                _attackIndicator.Radius = _attackCone.AttackRange;
            }

            if (_orientation != null)
            {
                Vector3 dir = _orientation.ForcedRotationDirection;
                float worldAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                _attackIndicator.Angle = worldAngle - _attackIndicator.transform.eulerAngles.y;
            }

            if (_arcRegion != null)
                _arcRegion.FillProgress = 0f;

            _attackIndicator.gameObject.SetActive(true);
        }
    }
}
