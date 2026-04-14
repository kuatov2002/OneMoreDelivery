using UnityEngine;
using MoreMountains.TopDownEngine;

/// <summary>
/// Drops Vampire Souls into RunState when this enemy dies.
/// Attach to enemy prefabs alongside a Health component.
/// Set SoulsAmount to 0 for regular enemies (no drop).
///
/// GDD reference values (set on prefabs, not hardcoded here):
///   Regular enemy : 0
///   Elite enemy   : 25-40
///   Boss          : 150
///   Final boss    : 500
/// </summary>
public class SoulsLootDrop : MonoBehaviour
{
    [Tooltip("Vampire Souls awarded on death. 0 = no drop.")]
    public int SoulsAmount = 0;

    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
        if (_health == null)
            Debug.LogWarning("[SoulsLootDrop] No Health component found on this GameObject.", this);
    }

    private void OnEnable()
    {
        if (_health != null)
            _health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (SoulsAmount <= 0) return;
        RunState.Instance?.AddSouls(SoulsAmount);
    }
}
