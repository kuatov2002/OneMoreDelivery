using System;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// An immutable modifier applied to a <see cref="CharacterStat"/>.
    /// Identified by a unique string ID so callers can remove it later.
    /// </summary>
    [Serializable]
    public readonly struct StatModifier
    {
        /// <summary>Unique identifier used to remove this modifier later.</summary>
        public readonly string ID;

        /// <summary>
        /// The flat percentage-point delta to apply.
        /// Positive values increase the stat; negative values reduce it.
        /// Example: +25 on a base-100 stat yields a final value of 125 (125%).
        /// </summary>
        public readonly float Value;

        /// <summary>How this modifier is combined with the stat's base value.</summary>
        public readonly StatModifierType ModifierType;

        public StatModifier(string id, float value, StatModifierType modifierType = StatModifierType.Flat)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("StatModifier ID must not be null or empty.", nameof(id));

            ID           = id;
            Value        = value;
            ModifierType = modifierType;
        }
    }
}