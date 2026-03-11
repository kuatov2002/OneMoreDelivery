using MoreMountains.TopDownEngine;
using UnityEngine;

/// <summary>
/// ScriptableObject representing a stat-based gift choice.
/// Implements IBuffChoice for display in the BuffSelectionModule UI.
/// Holds the stat type and modifier value to apply when selected.
///
/// Create via: Assets → Create → Game/Stat Gift
/// </summary>
[CreateAssetMenu(fileName = "NewStatGift", menuName = "Game/Stat Gift")]
public class StatGiftChoice : ScriptableObject, IBuffChoice
{
    [Header("Display")]
    public string displayName;
    [TextArea(1, 3)]
    public string description;
    public Sprite icon;
    public Color backgroundColor = Color.white;

    [Header("Stat Modifier")]
    [Tooltip("Which stat this gift modifies.")]
    public StatType statType;

    [Tooltip("Flat value added to the stat. For multiplier stats (base 100): +25 = 125%. " +
             "For probability stats (base 0): +15 = 15% chance.")]
    public float value;

    // ── IBuffChoice ──────────────────────────────────────────────────────

    public string GetName() => displayName;
    public string GetDescription() => description;
    public Sprite GetIcon() => icon;
    public Color GetBackgroundColor() => backgroundColor;
}
