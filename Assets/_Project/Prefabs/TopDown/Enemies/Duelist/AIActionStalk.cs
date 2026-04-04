using FIMSpace.FProceduralAnimation;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Predatory stalking — the enemy moves in organic arcs around the target,
    /// not perfect circles. Uses Perlin noise to continuously drift the orbit
    /// angle, creating unpredictable paths with natural weight and momentum.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Stalk")]
    public class AIActionStalk : AIAction
    {
        [Header("Distance")]
        [Tooltip("Ideal orbit distance from the target")]
        public float PreferredDistance = 3.5f;

        [Tooltip("How strongly the enemy corrects toward PreferredDistance")]
        public float DistanceCorrectionStrength = 3f;

        [Header("Movement")]
        [Tooltip("Base movement speed multiplier (1 = full run speed)")]
        [Range(0.2f, 1f)]
        public float BaseSpeedFactor = 0.45f;

        [Header("Orbit Angle Drift (Perlin)")]
        [Tooltip("How fast the orbit angle drifts over time (lower = smoother arcs)")]
        public float AngleNoiseSpeed = 0.4f;

        [Tooltip("Max angle offset from pure lateral in degrees — controls how wide the arcs are")]
        public float AngleNoiseAmplitude = 65f;

        [Header("Radial Weave (Perlin)")]
        [Tooltip("How fast the enemy weaves in/out toward the target")]
        public float RadialNoiseSpeed = 0.55f;

        [Tooltip("How many units the preferred distance wobbles")]
        public float RadialNoiseAmplitude = 1.5f;

        [Header("Speed Variation (Perlin)")]
        [Tooltip("How fast the speed varies")]
        public float SpeedNoiseSpeed = 0.7f;

        [Tooltip("Minimum speed multiplier")]
        [Range(0.1f, 1f)]
        public float SpeedNoiseMin = 0.4f;

        [Header("Direction Flips")]
        [Tooltip("Min time before flipping orbit direction")]
        public float FlipIntervalMin = 2f;

        [Tooltip("Max time before flipping orbit direction")]
        public float FlipIntervalMax = 5f;

        [Header("Hesitation")]
        [Tooltip("Chance to pause briefly when flipping direction")]
        [Range(0f, 1f)]
        public float PauseChance = 0.3f;

        [Tooltip("Min pause duration")]
        public float PauseDurationMin = 0.25f;

        [Tooltip("Max pause duration")]
        public float PauseDurationMax = 0.6f;

        [Header("Enemy Separation")]
        [Tooltip("How far enemies push each other apart")]
        public float SeparationRadius = 3f;

        [Tooltip("Strength of the repulsion force between enemies")]
        public float SeparationStrength = 1.5f;

        [Header("Momentum")]
        [Tooltip("How fast the movement direction blends (lower = heavier feel)")]
        public float DirectionSmoothing = 4f;

        [Header("Rotation")]
        [Tooltip("How fast the enemy rotates to face movement direction (degrees/sec)")]
        public float RotationSpeed = 360f;

        [Tooltip("How fast the enemy turns toward the target during pauses (degrees/sec)")]
        public float PauseRotationSpeed = 180f;

        protected CharacterMovement _characterMovement;
        protected Transform _characterRoot;
        protected LegsAnimator _legsAnimator;
        protected MeleeEnemyProceduralBody _proceduralBody;

        // cached nearby enemies for separation
        private static readonly Collider[] _overlapBuffer = new Collider[16];

        private int _orbitDir;                  // +1 or -1
        private float _nextFlipTime;

        // per-instance noise offsets so multiple enemies look different
        private float _noiseOffsetAngle;
        private float _noiseOffsetRadial;
        private float _noiseOffsetSpeed;

        // smoothed direction for momentum
        private Vector3 _smoothedDir;

        // pause
        private bool _isPaused;
        private float _pauseEnd;

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

            _legsAnimator?.User_SetIsMoving(true);
            _proceduralBody?.SetState(MeleeEnemyProceduralBody.BodyState.Idle);

            _orbitDir = Random.value > 0.5f ? 1 : -1;
            _noiseOffsetAngle = Random.Range(0f, 1000f);
            _noiseOffsetRadial = Random.Range(0f, 1000f);
            _noiseOffsetSpeed = Random.Range(0f, 1000f);
            _smoothedDir = Vector3.zero;

            _isPaused = false;
            ScheduleNextFlip();
        }

        public override void PerformAction()
        {
            if (_brain.Target == null) return;

            // --- direction flip timer ---
            if (Time.time >= _nextFlipTime)
                Flip();

            // --- pause: stop and turn to stare at the player ---
            if (_isPaused)
            {
                if (Time.time >= _pauseEnd)
                    _isPaused = false;

                _smoothedDir = Vector3.Lerp(_smoothedDir, Vector3.zero, Time.deltaTime * DirectionSmoothing * 2f);
                ApplyMovement(_smoothedDir);
                RotateToward(GetDirectionToTarget(), PauseRotationSpeed);
                return;
            }

            // --- vectors to target ---
            Vector3 toTarget = _brain.Target.position - _characterRoot.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            Vector3 dirToTarget = dist > 0.001f ? toTarget / dist : _characterRoot.forward;

            // --- lateral base (perpendicular) ---
            Vector3 lateral = Vector3.Cross(Vector3.up, dirToTarget) * _orbitDir;

            // --- drift the orbit angle with perlin noise ---
            // this bends the lateral direction forward or backward over time
            // creating arcs instead of circles
            float angleNoise = Mathf.PerlinNoise(
                Time.time * AngleNoiseSpeed + _noiseOffsetAngle, _noiseOffsetAngle) * 2f - 1f;
            float angleOffset = angleNoise * AngleNoiseAmplitude;

            // rotate lateral direction around Y by the noise angle
            Quaternion arcRotation = Quaternion.AngleAxis(angleOffset, Vector3.up);
            Vector3 arcDir = arcRotation * lateral;

            // --- radial correction with noise wobble ---
            float radialNoise = Mathf.PerlinNoise(
                Time.time * RadialNoiseSpeed + _noiseOffsetRadial, _noiseOffsetRadial) * 2f - 1f;
            float effectivePreferred = PreferredDistance + radialNoise * RadialNoiseAmplitude;

            float distError = dist - effectivePreferred;
            // blend radial correction into the arc direction
            // stronger correction when further from preferred distance
            float radialWeight = Mathf.Clamp01(Mathf.Abs(distError) / PreferredDistance) * DistanceCorrectionStrength;
            Vector3 radialPush = dirToTarget * Mathf.Sign(distError) * radialWeight;

            // --- separation from other enemies ---
            Vector3 separation = ComputeSeparation();

            Vector3 desiredDir = (arcDir + radialPush + separation).normalized;

            // --- speed variation ---
            float speedNoise = Mathf.PerlinNoise(
                Time.time * SpeedNoiseSpeed + _noiseOffsetSpeed, _noiseOffsetSpeed);
            float speed = Mathf.Lerp(SpeedNoiseMin, 1f, speedNoise) * BaseSpeedFactor;

            // --- apply momentum smoothing ---
            Vector3 targetMoveDir = desiredDir * speed;
            _smoothedDir = Vector3.Lerp(_smoothedDir, targetMoveDir, Time.deltaTime * DirectionSmoothing);

            ApplyMovement(_smoothedDir);

            // face movement direction so the forward walk animation looks correct
            if (_smoothedDir.sqrMagnitude > 0.01f)
                RotateToward(_smoothedDir.normalized, RotationSpeed);
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

        private void ApplyMovement(Vector3 dir)
        {
            Vector2 moveVec = new Vector2(dir.x, dir.z);
            _characterMovement?.SetMovement(moveVec);

            bool isMoving = dir.sqrMagnitude > 0.01f;
            if (_legsAnimator != null)
            {
                _legsAnimator.User_SetIsMoving(isMoving);
                if (isMoving)
                    _legsAnimator.User_SetDesiredMovementDirection(dir.normalized);
            }
        }

        private Vector3 GetDirectionToTarget()
        {
            if (_brain.Target == null) return _characterRoot.forward;
            Vector3 dir = _brain.Target.position - _characterRoot.position;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.001f ? dir.normalized : _characterRoot.forward;
        }

        private void RotateToward(Vector3 direction, float speed)
        {
            if (direction.sqrMagnitude < 0.001f) return;
            Quaternion targetRot = Quaternion.LookRotation(direction);
            float maxStep = speed * Time.deltaTime;
            _characterRoot.rotation = Quaternion.RotateTowards(_characterRoot.rotation, targetRot, maxStep);
        }

        private Vector3 ComputeSeparation()
        {
            Vector3 push = Vector3.zero;
            int count = Physics.OverlapSphereNonAlloc(
                _characterRoot.position, SeparationRadius, _overlapBuffer);

            for (int i = 0; i < count; i++)
            {
                // skip self
                if (_overlapBuffer[i].transform.root == _characterRoot.root) continue;

                // only repel other enemies (they have AIBrain)
                if (_overlapBuffer[i].GetComponentInParent<MoreMountains.Tools.AIBrain>() == null) continue;

                Vector3 away = _characterRoot.position - _overlapBuffer[i].transform.position;
                away.y = 0f;
                float dist = away.magnitude;

                if (dist < 0.01f) continue;

                // stronger push the closer they are
                float strength = 1f - Mathf.Clamp01(dist / SeparationRadius);
                push += (away / dist) * strength * SeparationStrength;
            }

            return push;
        }

        private void Flip()
        {
            _orbitDir = -_orbitDir;

            if (Random.value < PauseChance)
            {
                _isPaused = true;
                _pauseEnd = Time.time + Random.Range(PauseDurationMin, PauseDurationMax);
            }

            ScheduleNextFlip();
        }

        private void ScheduleNextFlip()
        {
            _nextFlipTime = Time.time + Random.Range(FlipIntervalMin, FlipIntervalMax);
        }
    }
}
