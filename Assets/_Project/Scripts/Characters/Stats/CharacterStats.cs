using System;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Central stat management component for a TopDownEngine character.
    ///
    /// Multiplier stats (AttackPower, AttackSpeed, MovementSpeed, MaxHealth) have a
    /// base of 100 (100%). Probability stats (CritChance, DodgeChance) have a base
    /// of 0. CritDamage has a base of 150 (1.5x).
    ///
    /// Callers push <see cref="StatModifier"/> objects onto a stat to raise or lower it.
    /// The component reacts to every change and propagates the new value to the
    /// appropriate engine subsystem automatically.
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
    ///     // Add 25% crit chance
    ///     stats.AddModifier(StatType.CritChance, new StatModifier("ring_crit", 25f));
    ///
    ///     // Add 15% dodge chance
    ///     stats.AddModifier(StatType.DodgeChance, new StatModifier("cloak_dodge", 15f));
    /// </code>
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/Core/Character Stats")]
    public class CharacterStats : CharacterAbility
    {
        // ── Stat Instances ─────────────────────────────────────────────────────
        // Not serialized — managed entirely at runtime through the public API.

        private readonly CharacterStat _attackPower   = new CharacterStat();       // base 100
        private readonly CharacterStat _attackSpeed   = new CharacterStat();       // base 100
        private readonly CharacterStat _movementSpeed = new CharacterStat();       // base 100
        private readonly CharacterStat _maxHealth     = new CharacterStat();       // base 100
        private readonly CharacterStat _critChance    = new CharacterStat(5f);     // base 5%
        private readonly CharacterStat _critDamage    = new CharacterStat(150f);   // base 150% = 1.5x
        private readonly CharacterStat _dodgeChance   = new CharacterStat(0f);     // base 0%

        // ── Inspector Debug Display ────────────────────────────────────────────
        // Read-only float mirrors so designers can observe live values in Play Mode.

        [Header("Live Stat Values (read-only, updated at runtime)")]
        [Tooltip("Current AttackPower stat value. 100 = base damage.")]
        [MMReadOnly] [SerializeField] private float _attackPowerDisplay   = 100f;

        [Tooltip("Current AttackSpeed stat value. 100 = base attack interval.")]
        [MMReadOnly] [SerializeField] private float _attackSpeedDisplay   = 100f;

        [Tooltip("Current MovementSpeed stat value. 100 = base walk speed.")]
        [MMReadOnly] [SerializeField] private float _movementSpeedDisplay = 100f;

        [Tooltip("Current MaxHealth stat value. 100 = base maximum health.")]
        [MMReadOnly] [SerializeField] private float _maxHealthDisplay     = 100f;

        [Header("Combat Stats (read-only)")]
        [Tooltip("Current CritChance. Direct percentage: 25 = 25% chance to crit.")]
        [MMReadOnly] [SerializeField] private float _critChanceDisplay    = 5f;

        [Tooltip("Current CritDamage. 150 = 1.5x damage on crit, 200 = 2x.")]
        [MMReadOnly] [SerializeField] private float _critDamageDisplay    = 150f;

        [Tooltip("Current DodgeChance. Direct percentage: 15 = 15% chance to dodge.")]
        [MMReadOnly] [SerializeField] private float _dodgeChanceDisplay   = 0f;

        // ── Cached Engine Base Values ──────────────────────────────────────────

        private float _baseWalkSpeed;
        private float _baseMaxHealth;

        // ── Weapon Base-Data Caches ────────────────────────────────────────────

        private readonly Dictionary<int, WeaponBaseData>        _weaponBaseData = new Dictionary<int, WeaponBaseData>();
        private readonly Dictionary<int, DamageOnTouchBaseData> _dotBaseData    = new Dictionary<int, DamageOnTouchBaseData>();

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
                case StatType.CritChance:    return _critChance;
                case StatType.CritDamage:    return _critDamage;
                case StatType.DodgeChance:   return _dodgeChance;
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

        // ── Crit & Dodge ─────────────────────────────────────────────────────

        /// <summary>
        /// Rolls crit for a single hit. Returns the (possibly multiplied) damage.
        /// CritChance is a direct percentage (0-100). CritDamage is a multiplier
        /// expressed as percentage points (150 = 1.5x).
        /// </summary>
        /// <param name="baseDamage">The raw damage before crit.</param>
        /// <param name="wasCrit">True if the roll was a critical hit.</param>
        /// <returns>Final damage after crit processing.</returns>
        public float ProcessCrit(float baseDamage, out bool wasCrit)
        {
            float chance = _critChance.CurrentValue;
            if (chance > 0f && UnityEngine.Random.Range(0f, 100f) < chance)
            {
                wasCrit = true;
                return baseDamage * (_critDamage.CurrentValue / 100f);
            }
            wasCrit = false;
            return baseDamage;
        }

        /// <summary>
        /// Rolls dodge for an incoming hit. Returns true if the hit was dodged.
        /// DodgeChance is a direct percentage (0-100).
        /// </summary>
        public bool RollDodge()
        {
            float chance = _dodgeChance.CurrentValue;
            return chance > 0f && UnityEngine.Random.Range(0f, 100f) < chance;
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

            // Subscribe to stat change notifications.
            _attackPower.OnValueChanged   += OnAttackPowerChanged;
            _attackSpeed.OnValueChanged   += OnAttackSpeedChanged;
            _movementSpeed.OnValueChanged += OnMovementSpeedChanged;
            _maxHealth.OnValueChanged     += OnMaxHealthChanged;
            _critChance.OnValueChanged    += OnCritChanceChanged;
            _critDamage.OnValueChanged    += OnCritDamageChanged;
            _dodgeChance.OnValueChanged   += OnDodgeChanceChanged;

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

        private void OnCritChanceChanged(float newValue)
        {
            _critChanceDisplay = newValue;
        }

        private void OnCritDamageChanged(float newValue)
        {
            _critDamageDisplay = newValue;
        }

        private void OnDodgeChanceChanged(float newValue)
        {
            _dodgeChanceDisplay = newValue;
        }

        private void OnWeaponChanged()
        {
            // Do NOT clear the base-data caches here.
            // Both dictionaries are keyed by GetInstanceID(), so each entry is
            // already bound to a specific weapon / DamageOnTouch object.
            // A brand-new weapon will produce a cache miss on its fresh instance ID
            // and be recorded correctly.
            ApplyWeaponStats();
        }

        // ── Subsystem Application ──────────────────────────────────────────────

        private void ApplyMovementSpeed()
        {
            if (_characterMovement == null) return;

            if (_baseWalkSpeed <= 0f)
                _baseWalkSpeed = _characterMovement.WalkSpeed;

            if (_baseWalkSpeed <= 0f) return;

            float newSpeed = _baseWalkSpeed * _movementSpeed.Multiplier;

            _characterMovement.WalkSpeed     = newSpeed;
            _characterMovement.MovementSpeed = newSpeed;
        }

        private void ApplyMaxHealth()
        {
            if (_health == null) return;

            if (_baseMaxHealth <= 0f)
                _baseMaxHealth = _health.MaximumHealth;

            if (_baseMaxHealth <= 0f) return;

            float prevMax = _health.MaximumHealth;
            float newMax  = _baseMaxHealth * _maxHealth.Multiplier;

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
            if (!_weaponBaseData.TryGetValue(weaponID, out WeaponBaseData weaponBase))
            {
                weaponBase = new WeaponBaseData(weapon.TimeBetweenUses);
                _weaponBaseData[weaponID] = weaponBase;
            }

            weapon.TimeBetweenUses = Mathf.Max(0.05f, weaponBase.TimeBetweenUses / _attackSpeed.Multiplier);

            // ── Attack Power ──────────────────────────────────────────────────
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

        protected override void OnRespawn()
        {
            base.OnRespawn();

            if (_characterMovement != null)
                _baseWalkSpeed = _characterMovement.WalkSpeed;
            if (_health != null)
                _baseMaxHealth = _health.MaximumHealth;

            ApplyMovementSpeed();
            ApplyMaxHealth();
            ApplyWeaponStats();
        }

        // ── Cleanup ────────────────────────────────────────────────────────────

        protected override void OnDisable()
        {
            base.OnDisable();

            _attackPower.OnValueChanged   -= OnAttackPowerChanged;
            _attackSpeed.OnValueChanged   -= OnAttackSpeedChanged;
            _movementSpeed.OnValueChanged -= OnMovementSpeedChanged;
            _maxHealth.OnValueChanged     -= OnMaxHealthChanged;
            _critChance.OnValueChanged    -= OnCritChanceChanged;
            _critDamage.OnValueChanged    -= OnCritDamageChanged;
            _dodgeChance.OnValueChanged   -= OnDodgeChanceChanged;

            if (_handleWeaponAbilities != null)
                foreach (CharacterHandleWeapon hwp in _handleWeaponAbilities)
                    if (hwp != null)
                        hwp.OnWeaponChange -= OnWeaponChanged;
        }

        // ── Inner Types ────────────────────────────────────────────────────────

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
