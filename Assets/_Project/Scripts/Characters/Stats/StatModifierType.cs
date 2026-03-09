namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// The four modifiable character statistics.
    /// All stats operate on a 100-point base (100 = 100%).
    /// </summary>
    public enum StatType
    {
        AttackPower,
        AttackSpeed,
        MovementSpeed,
        MaxHealth
    }

    /// <summary>
    /// How a modifier affects its target stat.
    /// Flat  : adds or subtracts raw percentage points (e.g. +25 → stat becomes 125).
    /// </summary>
    public enum StatModifierType
    {
        Flat
    }
}