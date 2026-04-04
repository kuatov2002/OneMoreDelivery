using UnityEngine;

/// <summary>
/// Per-enemy component that interfaces with the room's CombatTokenManager.
/// Place on the same GameObject as AIBrain.
/// </summary>
public class CombatTokenHolder : MonoBehaviour
{
    private CombatTokenManager _manager;

    public bool HasToken { get; private set; }

    /// <summary>
    /// Returns true if this enemy is allowed to start an attack.
    /// True when: already holds a token, no manager assigned, or a token is available.
    /// </summary>
    public bool CanAttack => HasToken || _manager == null || _manager.HasTokenAvailable;

    public void SetManager(CombatTokenManager manager)
    {
        _manager = manager;
    }

    public bool TryAcquire()
    {
        if (HasToken) return true;
        if (_manager == null) return true;

        HasToken = _manager.TryAcquire(gameObject);
        return HasToken;
    }

    public void Release()
    {
        if (!HasToken) return;
        HasToken = false;
        _manager?.Release(gameObject);
    }

    private void OnDisable()
    {
        Release();
    }
}
