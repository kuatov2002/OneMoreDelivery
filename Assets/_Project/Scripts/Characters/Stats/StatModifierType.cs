namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Modifiable character statistics.
    /// Multiplier stats operate on a 100-point base (100 = 100%).
    /// Probability stats (CritChance, DodgeChance) use base 0 and represent
    /// direct percentages (25 = 25% chance).
    /// </summary>
    public enum StatType
    {
        AttackPower,
        AttackSpeed,
        MovementSpeed,
        MaxHealth,
        CritChance,
        CritDamage,
        DodgeChance
    }

    /// <summary>
    /// How a modifier affects its target stat.
    /// Flat         : adds or subtracts raw percentage points (e.g. +25 → stat becomes 125).
    /// PercentAdd   : adds a fraction of the base-after-flat total (e.g. 0.2 = +20%).
    ///                Multiple PercentAdd modifiers are summed before multiplying, so two
    ///                +20% modifiers yield ×1.4, not ×1.44 (additive, not compound).
    /// Multiplicative: multiplies the result after all additive passes (e.g. 1.5 = ×1.5).
    ///                Multiple Multiplicative modifiers compound (×1.5 × ×1.2 = ×1.8).
    /// </summary>
    public enum StatModifierType
    {
        Flat,
        PercentAdd,
        Multiplicative
    }
}