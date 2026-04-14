using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Represents a single character statistic with a configurable base value.
    /// Multiplier-style stats use base 100 (100%); direct-percentage stats
    /// (like CritChance, DodgeChance) use base 0.
    ///
    /// Modifiers are additive percentage-point deltas stacked on top of the base,
    /// so a +25 modifier on a base-100 stat yields 125 (125%).
    ///
    /// Thread-safety: all mutations must occur on the main Unity thread.
    /// </summary>
    [Serializable]
    public class CharacterStat
    {
        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>
        /// Raised whenever the computed value changes.
        /// Subscribers receive the new final value (not the multiplier).
        /// </summary>
        public event Action<float> OnValueChanged;

        // ── State ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Base value for this stat instance.
        /// Multiplier stats default to 100 (100%); probability stats use 0.
        /// </summary>
        public readonly float BaseValue;

        // Ordered list so iteration is deterministic and cache-friendly.
        private readonly List<StatModifier> _modifiers = new List<StatModifier>(4);

        // Cached result; recomputed only when the modifier list changes.
        private float _cachedValue;
        private bool  _isDirty = false;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Creates a stat with the given base value.
        /// Default is 100 (100%) for backward-compatible multiplier stats.
        /// Use 0 for probability stats (CritChance, DodgeChance) or
        /// 150 for CritDamage (150% = 1.5x base crit multiplier).
        /// </summary>
        public CharacterStat(float baseValue = 100f)
        {
            BaseValue    = baseValue;
            _cachedValue = baseValue;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Current computed value (base + all modifier deltas).
        /// Floored at 0 to prevent negative multipliers from inverting behaviour.
        /// </summary>
        public float CurrentValue
        {
            get
            {
                if (_isDirty) Recompute();
                return _cachedValue;
            }
        }

        /// <summary>
        /// The stat expressed as a 0-inf multiplier.
        /// A CurrentValue of 100 yields 1.0; 150 yields 1.5; 50 yields 0.5.
        /// Meaningful only for multiplier-style stats (base 100).
        /// For probability stats (base 0), use <see cref="CurrentValue"/> directly.
        /// </summary>
        public float Multiplier => CurrentValue / 100f;

        /// <summary>Adds a modifier. Duplicate IDs are silently replaced.</summary>
        public void AddModifier(StatModifier modifier)
        {
            // Replace existing modifier with the same ID to avoid duplicates.
            int existingIndex = FindIndexByID(modifier.ID);
            if (existingIndex >= 0)
                _modifiers[existingIndex] = modifier;
            else
                _modifiers.Add(modifier);

            MarkDirty();
        }

        /// <summary>
        /// Removes the modifier with the given ID.
        /// Returns true if a modifier was found and removed.
        /// </summary>
        public bool RemoveModifier(string id)
        {
            int index = FindIndexByID(id);
            if (index < 0) return false;

            _modifiers.RemoveAt(index);
            MarkDirty();
            return true;
        }

        /// <summary>Removes all currently applied modifiers.</summary>
        public void ClearModifiers()
        {
            if (_modifiers.Count == 0) return;
            _modifiers.Clear();
            MarkDirty();
        }

        /// <summary>Returns true if a modifier with the given ID is currently applied.</summary>
        public bool HasModifier(string id) => FindIndexByID(id) >= 0;

        // ── Internal ──────────────────────────────────────────────────────────

        private void MarkDirty()
        {
            _isDirty = true;
            // Eagerly notify so subscribers react on the same frame.
            if (_isDirty) Recompute();
        }

        private void Recompute()
        {
            // Step 1: BaseValue + sum of all Flat modifiers.
            float flat           = BaseValue;
            float percentAddSum  = 0f;
            float multiplicative = 1f;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                StatModifier m = _modifiers[i];
                switch (m.ModifierType)
                {
                    case StatModifierType.Flat:
                        flat += m.Value;
                        break;
                    case StatModifierType.PercentAdd:
                        percentAddSum += m.Value;   // e.g. 0.2 for +20%
                        break;
                    case StatModifierType.Multiplicative:
                        multiplicative *= m.Value;  // e.g. 1.5 for ×1.5; compound
                        break;
                }
            }

            // Step 2: × (1 + sum of PercentAdd). Additive, not compound.
            // Step 3: × product of Multiplicative. Compound.
            float result = flat * (1f + percentAddSum) * multiplicative;

            _cachedValue = Mathf.Max(0f, result);
            _isDirty     = false;

            OnValueChanged?.Invoke(_cachedValue);
        }

        private int FindIndexByID(string id)
        {
            for (int i = 0; i < _modifiers.Count; i++)
                if (_modifiers[i].ID == id) return i;
            return -1;
        }
    }
}
