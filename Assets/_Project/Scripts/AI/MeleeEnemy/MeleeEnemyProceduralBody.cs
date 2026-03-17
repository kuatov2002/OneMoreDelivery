using FIMSpace.FTools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Procedural full-body animation for MeleeEnemy — Dark Souls-style.
    /// Runs in LateUpdate after Animator, applying spring-driven additive rotations
    /// to spine, chest, arms, neck and head based on current combat state.
    /// Emphasizes weight, momentum and follow-through.
    /// </summary>
    [DefaultExecutionOrder(5)]
    public class MeleeEnemyProceduralBody : MonoBehaviour
    {
        public enum BodyState { Idle, Telegraph, Attack, Recovery, Stagger }

        [Header("Bone References")]
        [SerializeField] private Transform _spine;
        [SerializeField] private Transform _chest;
        [SerializeField] private Transform _neck;
        [SerializeField] private Transform _head;

        [Space]
        [SerializeField] private Transform _rightShoulder;
        [SerializeField] private Transform _rightArm;
        [SerializeField] private Transform _rightForeArm;

        [Space]
        [SerializeField] private Transform _leftShoulder;
        [SerializeField] private Transform _leftArm;
        [SerializeField] private Transform _leftForeArm;

        [Header("Global Blend")]
        [Range(0f, 1f)]
        [SerializeField] private float _blend = 1f;

        // ──────────────────────── Idle ────────────────────────
        [Header("Idle — Breathing")]
        [SerializeField] private float _breathFrequency = 0.9f;
        [SerializeField] private float _breathSpineX = 2.5f;
        [SerializeField] private float _breathChestX = 1.5f;
        [SerializeField] private float _breathShoulderY = 1.2f;

        [Header("Idle — Weight Shift")]
        [SerializeField] private float _swayFrequency = 0.45f;
        [SerializeField] private float _swaySpineY = 1.8f;
        [SerializeField] private float _swaySpineZ = 0.8f;
        [SerializeField] private float _swayHeadX = 0.6f;

        // ──────────────────────── Telegraph ────────────────────────
        [Header("Telegraph — Spine Coil")]
        [Tooltip("Spine rotates: lean back (X-), twist away from attack side (Y-), slight side tilt (Z)")]
        [SerializeField] private Vector3 _telegraphSpine = new Vector3(-12f, -25f, -5f);
        [SerializeField] private Vector3 _telegraphChest = new Vector3(-8f, -18f, -3f);

        [Header("Telegraph — Arms")]
        [Tooltip("Weapon arm winds back: shoulder up, arm extends behind")]
        [SerializeField] private Vector3 _telegraphWeaponShoulder = new Vector3(-8f, -12f, 0f);
        [SerializeField] private Vector3 _telegraphWeaponArm = new Vector3(-45f, 0f, -25f);
        [SerializeField] private Vector3 _telegraphWeaponForeArm = new Vector3(-20f, 0f, 0f);

        [Tooltip("Off arm braces forward")]
        [SerializeField] private Vector3 _telegraphOffShoulder = new Vector3(5f, 8f, 0f);
        [SerializeField] private Vector3 _telegraphOffArm = new Vector3(15f, 0f, 12f);
        [SerializeField] private Vector3 _telegraphOffForeArm = new Vector3(10f, 0f, 0f);

        [Header("Telegraph — Head (counter-rotates to keep looking at target)")]
        [SerializeField] private Vector3 _telegraphNeck = new Vector3(5f, 15f, 3f);
        [SerializeField] private Vector3 _telegraphHead = new Vector3(3f, 12f, 0f);

        [Header("Telegraph — Easing")]
        [Tooltip("How fast the coil builds up (seconds to reach full pose)")]
        [SerializeField] private float _telegraphBuildUpTime = 0.6f;
        [Tooltip("Easing power: 1=linear, 2=quadratic (slow start, fast end), 3=cubic")]
        [SerializeField] private float _telegraphEasePower = 2.2f;

        // ──────────────────────── Attack ────────────────────────
        [Header("Attack — Spine Whip")]
        [Tooltip("Spine snaps: forward lean (X+), twist toward attack (Y+), tilt (Z)")]
        [SerializeField] private Vector3 _attackSpine = new Vector3(22f, 30f, 5f);
        [SerializeField] private Vector3 _attackChest = new Vector3(15f, 25f, 3f);

        [Header("Attack — Arms")]
        [SerializeField] private Vector3 _attackWeaponShoulder = new Vector3(10f, 15f, 0f);
        [SerializeField] private Vector3 _attackWeaponArm = new Vector3(65f, 0f, 30f);
        [SerializeField] private Vector3 _attackWeaponForeArm = new Vector3(25f, 0f, 10f);
        [SerializeField] private Vector3 _attackOffShoulder = new Vector3(-5f, -8f, 0f);
        [SerializeField] private Vector3 _attackOffArm = new Vector3(-10f, 0f, -8f);
        [SerializeField] private Vector3 _attackOffForeArm = new Vector3(-5f, 0f, 0f);

        [Header("Attack — Head (follows body)")]
        [SerializeField] private Vector3 _attackNeck = new Vector3(5f, 8f, 0f);
        [SerializeField] private Vector3 _attackHead = new Vector3(3f, 5f, 0f);

        // ──────────────────────── Recovery ────────────────────────
        [Header("Recovery — Overextended Pose (holds briefly after attack)")]
        [SerializeField] private Vector3 _recoverySpine = new Vector3(10f, 12f, 0f);
        [SerializeField] private Vector3 _recoveryChest = new Vector3(5f, 8f, 0f);
        [SerializeField] private Vector3 _recoveryWeaponArm = new Vector3(25f, 0f, 10f);
        [Tooltip("Time in seconds the overextended pose holds before fading")]
        [SerializeField] private float _recoveryHoldTime = 0.25f;
        [Tooltip("Time for the pose to fade from overextended to neutral")]
        [SerializeField] private float _recoveryFadeTime = 0.6f;

        // ──────────────────────── Stagger ────────────────────────
        [Header("Stagger — Recoil")]
        [SerializeField] private Vector3 _staggerSpine = new Vector3(-25f, 0f, 0f);
        [SerializeField] private Vector3 _staggerChest = new Vector3(-20f, 0f, 0f);
        [SerializeField] private Vector3 _staggerNeck = new Vector3(-8f, 0f, 0f);
        [SerializeField] private Vector3 _staggerHead = new Vector3(-12f, 0f, 0f);
        [SerializeField] private Vector3 _staggerArms = new Vector3(-20f, 0f, 25f);
        [SerializeField] private float _staggerRandomTilt = 12f;

        // ──────────────────────── Spring Parameters ────────────────────────
        [Header("Spring — Idle (gentle, heavy)")]
        [SerializeField] private float _idleAcceleration = 600f;
        [SerializeField] private float _idleAccelLimit = 150f;
        [SerializeField] private float _idleDamping = 14f;
        [SerializeField] private float _idleBrakePower = 0.3f;

        [Header("Spring — Telegraph (builds tension)")]
        [SerializeField] private float _telegraphAcceleration = 2500f;
        [SerializeField] private float _telegraphAccelLimit = 700f;
        [SerializeField] private float _telegraphDamping = 9f;
        [SerializeField] private float _telegraphBrakePower = 0.15f;

        [Header("Spring — Attack (explosive snap)")]
        [SerializeField] private float _attackAcceleration = 12000f;
        [SerializeField] private float _attackAccelLimit = 5000f;
        [SerializeField] private float _attackDamping = 5f;
        [SerializeField] private float _attackBrakePower = 0.1f;

        [Header("Spring — Recovery (heavy settle)")]
        [SerializeField] private float _recoveryAcceleration = 1500f;
        [SerializeField] private float _recoveryAccelLimit = 400f;
        [SerializeField] private float _recoveryDamping = 16f;
        [SerializeField] private float _recoveryBrakePower = 0.35f;

        [Header("Spring — Stagger (violent then settle)")]
        [SerializeField] private float _staggerAcceleration = 9000f;
        [SerializeField] private float _staggerAccelLimit = 3000f;
        [SerializeField] private float _staggerDamping = 7f;
        [SerializeField] private float _staggerBrakePower = 0.2f;

        private BodyState _state = BodyState.Idle;
        private float _stateTime;
        private Vector3 _staggerRandomOffset;

        private BoneMuscle _spineM, _chestM, _neckM, _headM;
        private BoneMuscle _rShoulderM, _rArmM, _rForeArmM;
        private BoneMuscle _lShoulderM, _lArmM, _lForeArmM;

        private struct BoneMuscle
        {
            public Transform Bone;
            public FMuscle_Eulers Muscle;
            public bool Valid;

            public void Init(Transform bone)
            {
                Bone = bone;
                Valid = bone != null;
                if (!Valid) return;
                Muscle = new FMuscle_Eulers();
                Muscle.Initialize(Vector3.zero);
            }

            public void SetSpring(float acceleration, float accelLimit, float damping, float brakePower)
            {
                if (!Valid) return;
                Muscle.Acceleration = acceleration;
                Muscle.AccelerationLimit = accelLimit;
                Muscle.Damping = damping;
                Muscle.BrakePower = brakePower;
            }

            public void Update(float delta, Vector3 targetOffset, float blend)
            {
                if (!Valid) return;
                Muscle.Update(delta, targetOffset);
                Vector3 euler = Muscle.ProceduralEulerAngles * blend;
                Bone.localRotation = Bone.localRotation * Quaternion.Euler(euler);
            }
        }

        private void Awake()
        {
            _spineM.Init(_spine);
            _chestM.Init(_chest);
            _neckM.Init(_neck);
            _headM.Init(_head);
            _rShoulderM.Init(_rightShoulder);
            _rArmM.Init(_rightArm);
            _rForeArmM.Init(_rightForeArm);
            _lShoulderM.Init(_leftShoulder);
            _lArmM.Init(_leftArm);
            _lForeArmM.Init(_leftForeArm);
        }

        public void SetState(BodyState state)
        {
            if (_state == state) return;
            _state = state;
            _stateTime = 0f;

            if (state == BodyState.Stagger)
            {
                _staggerRandomOffset = new Vector3(
                    Random.Range(-_staggerRandomTilt, _staggerRandomTilt),
                    Random.Range(-_staggerRandomTilt * 0.5f, _staggerRandomTilt * 0.5f),
                    Random.Range(-_staggerRandomTilt, _staggerRandomTilt)
                );
            }

            ApplySpringParams();
        }

        private void ApplySpringParams()
        {
            float acc, accLim, damp, brake;

            switch (_state)
            {
                case BodyState.Idle:
                    acc = _idleAcceleration; accLim = _idleAccelLimit;
                    damp = _idleDamping; brake = _idleBrakePower;
                    break;
                case BodyState.Telegraph:
                    acc = _telegraphAcceleration; accLim = _telegraphAccelLimit;
                    damp = _telegraphDamping; brake = _telegraphBrakePower;
                    break;
                case BodyState.Attack:
                    acc = _attackAcceleration; accLim = _attackAccelLimit;
                    damp = _attackDamping; brake = _attackBrakePower;
                    break;
                case BodyState.Recovery:
                    acc = _recoveryAcceleration; accLim = _recoveryAccelLimit;
                    damp = _recoveryDamping; brake = _recoveryBrakePower;
                    break;
                case BodyState.Stagger:
                    acc = _staggerAcceleration; accLim = _staggerAccelLimit;
                    damp = _staggerDamping; brake = _staggerBrakePower;
                    break;
                default:
                    acc = _idleAcceleration; accLim = _idleAccelLimit;
                    damp = _idleDamping; brake = _idleBrakePower;
                    break;
            }

            _spineM.SetSpring(acc, accLim, damp, brake);
            _chestM.SetSpring(acc, accLim, damp, brake);
            _neckM.SetSpring(acc, accLim, damp, brake);
            _headM.SetSpring(acc, accLim, damp, brake);
            _rShoulderM.SetSpring(acc, accLim, damp, brake);
            _rArmM.SetSpring(acc, accLim, damp, brake);
            _rForeArmM.SetSpring(acc, accLim, damp, brake);
            _lShoulderM.SetSpring(acc, accLim, damp, brake);
            _lArmM.SetSpring(acc, accLim, damp, brake);
            _lForeArmM.SetSpring(acc, accLim, damp, brake);
        }

        private void LateUpdate()
        {
            if (_blend <= 0f) return;

            _stateTime += Time.deltaTime;
            float dt = Time.deltaTime;

            Vector3 spine, chest, neck, head;
            Vector3 rShoulder, rArm, rForeArm;
            Vector3 lShoulder, lArm, lForeArm;

            switch (_state)
            {
                case BodyState.Idle:
                    CalculateIdle(out spine, out chest, out neck, out head,
                        out rShoulder, out rArm, out rForeArm,
                        out lShoulder, out lArm, out lForeArm);
                    break;
                case BodyState.Telegraph:
                    CalculateTelegraph(out spine, out chest, out neck, out head,
                        out rShoulder, out rArm, out rForeArm,
                        out lShoulder, out lArm, out lForeArm);
                    break;
                case BodyState.Attack:
                    CalculateAttack(out spine, out chest, out neck, out head,
                        out rShoulder, out rArm, out rForeArm,
                        out lShoulder, out lArm, out lForeArm);
                    break;
                case BodyState.Recovery:
                    CalculateRecovery(out spine, out chest, out neck, out head,
                        out rShoulder, out rArm, out rForeArm,
                        out lShoulder, out lArm, out lForeArm);
                    break;
                case BodyState.Stagger:
                    CalculateStagger(out spine, out chest, out neck, out head,
                        out rShoulder, out rArm, out rForeArm,
                        out lShoulder, out lArm, out lForeArm);
                    break;
                default:
                    spine = chest = neck = head = Vector3.zero;
                    rShoulder = rArm = rForeArm = Vector3.zero;
                    lShoulder = lArm = lForeArm = Vector3.zero;
                    break;
            }

            _spineM.Update(dt, spine, _blend);
            _chestM.Update(dt, chest, _blend);
            _neckM.Update(dt, neck, _blend);
            _headM.Update(dt, head, _blend);
            _rShoulderM.Update(dt, rShoulder, _blend);
            _rArmM.Update(dt, rArm, _blend);
            _rForeArmM.Update(dt, rForeArm, _blend);
            _lShoulderM.Update(dt, lShoulder, _blend);
            _lArmM.Update(dt, lArm, _blend);
            _lForeArmM.Update(dt, lForeArm, _blend);
        }

        // ──────────────────────── State Calculations ────────────────────────

        private void CalculateIdle(
            out Vector3 spine, out Vector3 chest, out Vector3 neck, out Vector3 head,
            out Vector3 rShoulder, out Vector3 rArm, out Vector3 rForeArm,
            out Vector3 lShoulder, out Vector3 lArm, out Vector3 lForeArm)
        {
            float t = _stateTime;
            float breathCycle = Mathf.Sin(t * _breathFrequency * Mathf.PI * 2f);
            float breathCycle2 = Mathf.Sin(t * _breathFrequency * Mathf.PI * 2f + 0.4f);
            float swayCycle = Mathf.Sin(t * _swayFrequency * Mathf.PI * 2f);
            float swayCycle2 = Mathf.Sin(t * _swayFrequency * 0.7f * Mathf.PI * 2f);

            // Spine: breathe forward/back + sway side-to-side
            spine = new Vector3(
                breathCycle * _breathSpineX,
                swayCycle * _swaySpineY,
                swayCycle2 * _swaySpineZ
            );

            // Chest: counter-breathe (gives accordion feel), follow sway
            chest = new Vector3(
                -breathCycle2 * _breathChestX,
                -swayCycle * _swaySpineY * 0.4f,
                0f
            );

            // Neck/head: subtle compensation so head stays relatively stable
            neck = new Vector3(
                -breathCycle * _breathSpineX * 0.15f,
                -swayCycle * _swaySpineY * 0.2f,
                0f
            );

            head = new Vector3(
                _swayHeadX * Mathf.Sin(t * 0.3f * Mathf.PI * 2f),
                0f,
                0f
            );

            // Shoulders rise and fall with breathing
            rShoulder = new Vector3(0f, breathCycle * _breathShoulderY, 0f);
            lShoulder = new Vector3(0f, -breathCycle2 * _breathShoulderY, 0f);

            // Arms hang — very subtle pendulum from sway
            rArm = new Vector3(0f, 0f, swayCycle * 1f);
            rForeArm = Vector3.zero;
            lArm = new Vector3(0f, 0f, -swayCycle * 1f);
            lForeArm = Vector3.zero;
        }

        private void CalculateTelegraph(
            out Vector3 spine, out Vector3 chest, out Vector3 neck, out Vector3 head,
            out Vector3 rShoulder, out Vector3 rArm, out Vector3 rForeArm,
            out Vector3 lShoulder, out Vector3 lArm, out Vector3 lForeArm)
        {
            // Eased build-up: slow start → fast end (like coiling a spring)
            float raw = Mathf.Clamp01(_stateTime / _telegraphBuildUpTime);
            float t = Mathf.Pow(raw, _telegraphEasePower);

            // Spine coils: leans back + twists away from attack side
            spine = _telegraphSpine * t;
            chest = _telegraphChest * t;

            // Head counter-rotates to keep eyes on target (menacing)
            neck = _telegraphNeck * t;
            head = _telegraphHead * t;

            // Weapon arm (right) winds back dramatically
            rShoulder = _telegraphWeaponShoulder * t;
            rArm = _telegraphWeaponArm * t;
            rForeArm = _telegraphWeaponForeArm * t;

            // Off arm (left) braces forward for balance
            lShoulder = _telegraphOffShoulder * t;
            lArm = _telegraphOffArm * t;
            lForeArm = _telegraphOffForeArm * t;
        }

        private void CalculateAttack(
            out Vector3 spine, out Vector3 chest, out Vector3 neck, out Vector3 head,
            out Vector3 rShoulder, out Vector3 rArm, out Vector3 rForeArm,
            out Vector3 lShoulder, out Vector3 lArm, out Vector3 lForeArm)
        {
            // Instant target — spring's high acceleration snaps everything from
            // the telegraph coil to the attack pose. The momentum overshoot
            // from the spring gives natural follow-through.
            spine = _attackSpine;
            chest = _attackChest;
            neck = _attackNeck;
            head = _attackHead;

            rShoulder = _attackWeaponShoulder;
            rArm = _attackWeaponArm;
            rForeArm = _attackWeaponForeArm;

            lShoulder = _attackOffShoulder;
            lArm = _attackOffArm;
            lForeArm = _attackOffForeArm;
        }

        private void CalculateRecovery(
            out Vector3 spine, out Vector3 chest, out Vector3 neck, out Vector3 head,
            out Vector3 rShoulder, out Vector3 rArm, out Vector3 rForeArm,
            out Vector3 lShoulder, out Vector3 lArm, out Vector3 lForeArm)
        {
            // Overextended pose → neutral. Shows weight and exhaustion.
            // Hold the overextended pose briefly, then fade to neutral.
            float holdT = Mathf.Clamp01(_stateTime / _recoveryHoldTime);
            float fadeStart = _recoveryHoldTime;
            float fadeT = _stateTime > fadeStart
                ? Mathf.Clamp01((_stateTime - fadeStart) / _recoveryFadeTime)
                : 0f;

            // Smooth ease-out for the fade
            float fade = 1f - fadeT * fadeT;

            // During hold phase, show the overextended pose at full strength
            // During fade phase, smoothly reduce to zero
            float strength = holdT < 1f ? holdT : fade;

            spine = _recoverySpine * strength;
            chest = _recoveryChest * strength;
            neck = Vector3.zero;
            head = Vector3.zero;

            rShoulder = Vector3.zero;
            rArm = _recoveryWeaponArm * strength;
            rForeArm = Vector3.zero;

            lShoulder = Vector3.zero;
            lArm = Vector3.zero;
            lForeArm = Vector3.zero;
        }

        private void CalculateStagger(
            out Vector3 spine, out Vector3 chest, out Vector3 neck, out Vector3 head,
            out Vector3 rShoulder, out Vector3 rArm, out Vector3 rForeArm,
            out Vector3 lShoulder, out Vector3 lArm, out Vector3 lForeArm)
        {
            // Violent recoil + random variation for organic feel
            spine = _staggerSpine + _staggerRandomOffset;
            chest = _staggerChest + _staggerRandomOffset * 0.7f;

            // Whiplash: head snaps back harder than spine
            neck = _staggerNeck + new Vector3(_staggerRandomOffset.x * 0.3f, _staggerRandomOffset.y, 0f);
            head = _staggerHead + new Vector3(_staggerRandomOffset.x * 0.4f, _staggerRandomOffset.y * 0.5f, _staggerRandomOffset.z * 0.3f);

            // Arms fly outward
            rShoulder = new Vector3(0f, -_staggerRandomTilt * 0.4f, 0f);
            rArm = _staggerArms;
            rForeArm = new Vector3(_staggerArms.x * 0.4f, 0f, _staggerArms.z * 0.3f);

            lShoulder = new Vector3(0f, _staggerRandomTilt * 0.4f, 0f);
            lArm = new Vector3(_staggerArms.x, 0f, -_staggerArms.z);
            lForeArm = new Vector3(_staggerArms.x * 0.4f, 0f, -_staggerArms.z * 0.3f);
        }
    }
}
