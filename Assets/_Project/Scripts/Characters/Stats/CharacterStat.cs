using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Represents a single character statistic with a fixed base value of 100
    /// (meaning 100%). Modifiers are additive percentage-point deltas stacked
    /// on top of that base, so a +25 modifier raises the effective value to 125%.
    ///
    /// Thread-safety: all mutations must occur on the main Unity thread.
    /// </summary>
    [Serializable]
    public class CharacterStat
    {
        // ── Constants ─────────────────────────────────────────────────────────

        /// <summary>Base percentage value for every stat. Fixed at 100.</summary>
        public const float BaseValue = 100f;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>
        /// Raised whenever the computed value changes.
        /// Subscribers receive the new final value (not the multiplier).
        /// </summary>
        public event Action<float> OnValueChanged;

        // ── State ─────────────────────────────────────────────────────────────

        // Ordered list so iteration is deterministic and cache-friendly.
        private readonly List<StatModifier> _modifiers = new List<StatModifier>(4);

        // Cached result; recomputed only when the modifier list changes.
        private float _cachedValue = BaseValue;
        private bool  _isDirty     = false;

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
        /// The stat expressed as a 0–∞ multiplier.
        /// A CurrentValue of 100 yields 1.0; 150 yields 1.5; 50 yields 0.5.
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
            float sum = BaseValue;
            for (int i = 0; i < _modifiers.Count; i++)
                sum += _modifiers[i].Value;       // Flat addition only for now.

            _cachedValue = Mathf.Max(0f, sum);
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