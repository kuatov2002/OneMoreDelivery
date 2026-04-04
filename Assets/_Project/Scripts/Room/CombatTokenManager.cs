using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Limits how many enemies in a room can attack simultaneously.
/// Place on the same GameObject as Room.
/// </summary>
public class CombatTokenManager : MonoBehaviour
{
    [Tooltip("Max enemies that can be in Telegraph/Attack/Recovery at the same time")]
    public int MaxActiveAttackers = 2;

    [Tooltip("Delay after a token is released before it can be acquired again")]
    public float TokenCooldown = 0.8f;

    private readonly HashSet<GameObject> _activeAttackers = new();
    private float _lastReleaseTime = -100f;

    public bool HasTokenAvailable =>
        _activeAttackers.Count < MaxActiveAttackers
        && Time.time - _lastReleaseTime >= TokenCooldown;

    public bool TryAcquire(GameObject requester)
    {
        if (_activeAttackers.Contains(requester)) return true;
        if (!HasTokenAvailable) return false;
        _activeAttackers.Add(requester);
        return true;
    }

    public void Release(GameObject requester)
    {
        if (_activeAttackers.Remove(requester))
            _lastReleaseTime = Time.time;
    }
}
