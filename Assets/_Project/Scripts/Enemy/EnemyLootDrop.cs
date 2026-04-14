using UnityEngine;
using MoreMountains.TopDownEngine;

/// <summary>
/// Drops Gold into RunState when this enemy dies.
/// Attach to any enemy prefab alongside a Health component.
/// </summary>
public class EnemyLootDrop : MonoBehaviour
{
    [Header("Gold Drop")]
    [Tooltip("Minimum Gold dropped on death (inclusive).")]
    public int GoldMin = 5;

    [Tooltip("Maximum Gold dropped on death (inclusive).")]
    public int GoldMax = 15;

    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
        if (_health == null)
            Debug.LogWarning("[EnemyLootDrop] No Health component found on this GameObject.", this);
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
        if (RunState.Instance == null || !RunState.Instance.IsRunActive) return;

        int gold = Random.Range(GoldMin, GoldMax + 1);
        RunState.Instance.AddGold(gold);
    }
}
