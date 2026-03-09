using System;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Central stat management component for a TopDownEngine character.
    ///
    /// Each of the four stats (AttackPower, AttackSpeed, MovementSpeed, MaxHealth) has a
    /// fixed base of 100 (representing 100%). Callers push <see cref="StatModifier"/> objects
    /// onto a stat to raise or lower it. The component reacts to every change and propagates
    /// the new multiplier to the appropriate engine subsystem automatically.
    ///
    /// Attach this component to the same GameObject as the Character.
    ///
    /// Usage example:
    /// <code>
    ///     var stats = character.GetComponent&lt;CharacterStats&gt;();
    ///
    ///     // Give a buff: +50% attack power from a potion
    ///     stats.AddModifier(StatType.AttackPower, new StatModifier("potion_atk", 50f));
    ///
    ///     // Remove the buff when it expires
    ///     stats.RemoveModifier(StatType.AttackPower, "potion_atk");
    ///
    ///     // Apply a permanent -20% movement speed debuff from armour
    ///     stats.AddModifier(StatType.MovementSpeed, new StatModifier("heavy_armour", -20f));
    /// </code>
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/Core/Character Stats")]
    public class CharacterStats : CharacterAbility
    {
        // ── Stat Instances ─────────────────────────────────────────────────────
        // Not serialized — managed entirely at runtime through the public API.
        // The four readonly fields guarantee one instance per stat per character.

        private readonly CharacterStat _attackPower   = new CharacterStat();
        private readonly CharacterStat _attackSpeed   = new CharacterStat();
        private readonly CharacterStat _movementSpeed = new CharacterStat();
        private readonly CharacterStat _maxHealth     = new CharacterStat();

        // ── Inspector Debug Display ────────────────────────────────────────────
        // Read-only float mirrors so designers can observe live values in Play Mode.

        [Header("Live Stat Values (read-only, updated at runtime)")]
        [Tooltip("Current AttackPower stat value. 100 = base damage.")]
        [MMReadOnly] [SerializeField] private float _attackPowerDisplay   = CharacterStat.BaseValue;

        [Tooltip("Current AttackSpeed stat value. 100 = base attack interval.")]
        [MMReadOnly] [SerializeField] private float _attackSpeedDisplay   = CharacterStat.BaseValue;

        [Tooltip("Current MovementSpeed stat value. 100 = base walk speed.")]
        [MMReadOnly] [SerializeField] private float _movementSpeedDisplay = CharacterStat.BaseValue;

        [Tooltip("Current MaxHealth stat value. 100 = base maximum health.")]
        [MMReadOnly] [SerializeField] private float _maxHealthDisplay     = CharacterStat.BaseValue;

        // ── Cached Engine Base Values ──────────────────────────────────────────
        // Captured once at initialization so we always scale from the original
        // designer-defined values, not from a previously-modified state.

        private float _baseWalkSpeed;
        private float _baseMaxHealth;

        // ── Weapon Base-Data Caches ────────────────────────────────────────────
        // Keyed by instance ID so each physical weapon object retains its own
        // original values independently of how many times stats change.

        private readonly Dictionary<int, WeaponBaseData>        _weaponBaseData = new Dictionary<int, WeaponBaseData>();
        private readonly Dictionary<int, DamageOnTouchBaseData> _dotBaseData    = new Dictionary<int, DamageOnTouchBaseData>();

        // ── Test Input ────────────────────────────────────────────────────────
        // Remove this entire section (fields + HandleInput override) when done testing.

        private const string _testModifierID = "debug_spacebar_atk";
        private bool _testBuffActive = false;

        // ── Cached Ability References ──────────────────────────────────────────

        private List<CharacterHandleWeapon> _handleWeaponAbilities;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Returns the <see cref="CharacterStat"/> for the given type.</summary>
        public CharacterStat GetStat(StatType type)
        {
            switch (type)
            {
                case StatType.AttackPower:   return _attackPower;
                case StatType.AttackSpeed:   return _attackSpeed;
                case StatType.MovementSpeed: return _movementSpeed;
                case StatType.MaxHealth:     return _maxHealth;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        /// <summary>
        /// Adds (or replaces) a modifier on the specified stat.
        /// Duplicate IDs are silently replaced.
        /// </summary>
        public void AddModifier(StatType type, StatModifier modifier)
            => GetStat(type).AddModifier(modifier);

        /// <summary>
        /// Removes the modifier with the given ID from the specified stat.
        /// Returns false if no modifier with that ID was found.
        /// </summary>
        public bool RemoveModifier(StatType type, string modifierID)
            => GetStat(type).RemoveModifier(modifierID);

        /// <summary>Removes all active modifiers from the specified stat.</summary>
        public void ClearModifiers(StatType type)
            => GetStat(type).ClearModifiers();

        /// <summary>
        /// Returns the current multiplier for the given stat (e.g. 1.5 = 150%).
        /// Shorthand for <c>GetStat(type).Multiplier</c>.
        /// </summary>
        public float GetMultiplier(StatType type)
            => GetStat(type).Multiplier;

        // ── Test: Spacebar toggles +50% AttackPower buff ───────────────────────
        // Пробел нажат  → добавляет модификатор "debug_spacebar_atk" (+50).
        // Пробел нажат снова → удаляет тот же модификатор.
        // Удалите этот метод и поля _testModifierID / _testBuffActive когда
        // тестирование будет завершено.
        protected override void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_testBuffActive)
                {
                    RemoveModifier(StatType.AttackPower, _testModifierID);
                    _testBuffActive = false;
                    Debug.Log("[CharacterStats] Test buff REMOVED. AttackPower = "
                              + _attackPower.CurrentValue);
                }
                else
                {
                    AddModifier(StatType.AttackPower,
                                new StatModifier(_testModifierID, 50f));
                    _testBuffActive = true;
                    Debug.Log("[CharacterStats] Test buff APPLIED (+50). AttackPower = "
                              + _attackPower.CurrentValue);
                }
            }
        }

        // ── Initialization ─────────────────────────────────────────────────────

        /// <summary>
        /// Called by the engine after all components are ready.
        /// Caches base values and subscribes to change events.
        /// </summary>
        protected override void Initialization()
        {
            base.Initialization();

            // Capture designer-specified base values before any stat modifiers apply.
            if (_characterMovement != null)
                _baseWalkSpeed = _characterMovement.WalkSpeed;

            if (_health != null)
                _baseMaxHealth = _health.MaximumHealth;

            // Subscribe to stat change notifications using named methods (not lambdas)
            // so they can be cleanly unsubscribed in OnDisable.
            _attackPower.OnValueChanged   += OnAttackPowerChanged;
            _attackSpeed.OnValueChanged   += OnAttackSpeedChanged;
            _movementSpeed.OnValueChanged += OnMovementSpeedChanged;
            _maxHealth.OnValueChanged     += OnMaxHealthChanged;

            // Subscribe to weapon swap events so stats are re-applied on equip.
            _handleWeaponAbilities = _character?.FindAbilities<CharacterHandleWeapon>();
            if (_handleWeaponAbilities != null)
                foreach (CharacterHandleWeapon hwp in _handleWeaponAbilities)
                    hwp.OnWeaponChange += OnWeaponChanged;
        }

        // ── Stat Change Handlers ───────────────────────────────────────────────

        private void OnAttackPowerChanged(float newValue)
        {
            _attackPowerDisplay = newValue;
            ApplyWeaponStats();
        }

        private void OnAttackSpeedChanged(float newValue)
        {
            _attackSpeedDisplay = newValue;
            ApplyWeaponStats();
        }

        private void OnMovementSpeedChanged(float newValue)
        {
            _movementSpeedDisplay = newValue;
            ApplyMovementSpeed();
        }

        private void OnMaxHealthChanged(float newValue)
        {
            _maxHealthDisplay = newValue;
            ApplyMaxHealth();
        }

        private void OnWeaponChanged()
        {
            // Do NOT clear the base-data caches here.
            //
            // Both dictionaries are keyed by GetInstanceID(), so each entry is
            // already bound to a specific weapon / DamageOnTouch object.
            // Clearing on every weapon-change event would cause the next
            // ApplyWeaponStatsTo call to re-capture dot.MinDamageCaused as the
            // "base" — but that value may already be a multiplied result, which
            // produces compounding damage across every buff cycle.
            //
            // A brand-new weapon will produce a cache miss on its fresh instance ID
            // and be recorded correctly. A re-equipped weapon retains its original
            // baseline. Unity instance IDs are unique for the lifetime of an object,
            // so stale collisions cannot occur in practice.
            ApplyWeaponStats();
        }

        // ── Subsystem Application ──────────────────────────────────────────────

        private void ApplyMovementSpeed()
        {
            if (_characterMovement == null) return;

            // Lazy capture: if Initialization ran before CharacterMovement was ready,
            // grab the base value now when we first have a valid non-zero speed.
            if (_baseWalkSpeed <= 0f)
                _baseWalkSpeed = _characterMovement.WalkSpeed;

            // Still zero means the component is not ready yet — bail out.
            if (_baseWalkSpeed <= 0f) return;

            float newSpeed = _baseWalkSpeed * _movementSpeed.Multiplier;

            // WalkSpeed  — the "reset-to" value used on respawn by ResetAbility().
            // MovementSpeed — the runtime property read by SetMovement() every frame.
            // Both must be updated so respawns also respect the current stat.
            _characterMovement.WalkSpeed     = newSpeed;
            _characterMovement.MovementSpeed = newSpeed;
        }

        private void ApplyMaxHealth()
        {
            if (_health == null) return;

            // Lazy capture: if Initialization ran before Health was ready (the engine
            // may call Revive() before Start()), grab the base value now.
            if (_baseMaxHealth <= 0f)
                _baseMaxHealth = _health.MaximumHealth;

            // Still zero means Health is not yet fully initialized — bail out
            // rather than clamping via Mathf.Max(1f, 0) which would silently
            // set MaximumHealth = 1 and corrupt the character's HP pool.
            if (_baseMaxHealth <= 0f) return;

            float prevMax = _health.MaximumHealth;
            float newMax  = _baseMaxHealth * _maxHealth.Multiplier;

            // Scale current HP proportionally so the character is not suddenly killed
            // (or over-healed) when max health changes at runtime.
            float scaledCurrent = (prevMax > 0f)
                ? _health.CurrentHealth * (newMax / prevMax)
                : newMax;

            _health.MaximumHealth = newMax;
            _health.SetHealth(Mathf.Clamp(scaledCurrent, 0f, newMax));
        }

        private void ApplyWeaponStats()
        {
            if (_handleWeaponAbilities == null) return;

            foreach (CharacterHandleWeapon hwp in _handleWeaponAbilities)
                if (hwp.CurrentWeapon != null)
                    ApplyWeaponStatsTo(hwp.CurrentWeapon);
        }

        /// <summary>
        /// Applies the current AttackPower and AttackSpeed multipliers to one weapon.
        /// Records the weapon's original values on first encounter so subsequent changes
        /// always scale from the true designer baseline.
        /// </summary>
        private void ApplyWeaponStatsTo(Weapon weapon)
        {
            int weaponID = weapon.GetInstanceID();

            // ── Attack Speed ──────────────────────────────────────────────────
            // Higher stat → lower TimeBetweenUses → faster attacks.
            // Division by multiplier: 200% speed halves the interval; 50% doubles it.
            if (!_weaponBaseData.TryGetValue(weaponID, out WeaponBaseData weaponBase))
            {
                weaponBase = new WeaponBaseData(weapon.TimeBetweenUses);
                _weaponBaseData[weaponID] = weaponBase;
            }

            // Floor at 0.05 s to prevent near-infinite fire rates.
            weapon.TimeBetweenUses = Mathf.Max(0.05f, weaponBase.TimeBetweenUses / _attackSpeed.Multiplier);

            // ── Attack Power ──────────────────────────────────────────────────
            // Locate all DamageOnTouch zones parented to this weapon (active or not,
            // because melee zones are disabled between swings).
            DamageOnTouch[] damageAreas = weapon.GetComponentsInChildren<DamageOnTouch>(includeInactive: true);
            foreach (DamageOnTouch dot in damageAreas)
            {
                int dotID = dot.GetInstanceID();

                if (!_dotBaseData.TryGetValue(dotID, out DamageOnTouchBaseData dotBase))
                {
                    dotBase = new DamageOnTouchBaseData(dot.MinDamageCaused, dot.MaxDamageCaused);
                    _dotBaseData[dotID] = dotBase;
                }

                dot.MinDamageCaused = dotBase.MinDamage * _attackPower.Multiplier;
                dot.MaxDamageCaused = dotBase.MaxDamage * _attackPower.Multiplier;
            }
        }

        // ── Respawn ────────────────────────────────────────────────────────────

        /// <summary>
        /// Re-applies all stats after respawn.
        ///
        /// IMPORTANT: Health.Revive() resets MaximumHealth to the inspector value
        /// (step 1) and then fires OnRevive (step 3), so by the time this method
        /// runs the engine has already restored the "correct" designer base values.
        /// We must re-capture them here before multiplying, otherwise we would be
        /// scaling from a stale (or zero) cached base and corrupt the HP pool.
        /// </summary>
        protected override void OnRespawn()
        {
            base.OnRespawn();

            // Re-capture base values from the freshly-reset engine state.
            if (_characterMovement != null)
                _baseWalkSpeed = _characterMovement.WalkSpeed;
            if (_health != null)
                _baseMaxHealth = _health.MaximumHealth;

            ApplyMovementSpeed();
            ApplyMaxHealth();
            ApplyWeaponStats();
        }

        // ── Cleanup ────────────────────────────────────────────────────────────

        /// <summary>Unsubscribes all delegates to prevent memory leaks.</summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            _attackPower.OnValueChanged   -= OnAttackPowerChanged;
            _attackSpeed.OnValueChanged   -= OnAttackSpeedChanged;
            _movementSpeed.OnValueChanged -= OnMovementSpeedChanged;
            _maxHealth.OnValueChanged     -= OnMaxHealthChanged;

            if (_handleWeaponAbilities != null)
                foreach (CharacterHandleWeapon hwp in _handleWeaponAbilities)
                    if (hwp != null)
                        hwp.OnWeaponChange -= OnWeaponChanged;
        }

        // ── Inner Types ────────────────────────────────────────────────────────
        // Stored as readonly structs (zero heap allocation after initial boxing
        // in the Dictionary) to keep the hot path in ApplyWeaponStatsTo cheap.

        private readonly struct WeaponBaseData
        {
            public readonly float TimeBetweenUses;
            public WeaponBaseData(float timeBetweenUses) => TimeBetweenUses = timeBetweenUses;
        }

        private readonly struct DamageOnTouchBaseData
        {
            public readonly float MinDamage;
            public readonly float MaxDamage;
            public DamageOnTouchBaseData(float min, float max)
            {
                MinDamage = min;
                MaxDamage = max;
            }
        }
    }
}