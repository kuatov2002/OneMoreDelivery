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

    private readonly HashSet<GameObject> _activeAttackers = new();

    public bool HasTokenAvailable => _activeAttackers.Count < MaxActiveAttackers;

    public bool TryAcquire(GameObject requester)
    {
        if (_activeAttackers.Contains(requester)) return true;
        if (_activeAttackers.Count >= MaxActiveAttackers) return false;
        _activeAttackers.Add(requester);
        return true;
    }

    public void Release(GameObject requester)
    {
        _activeAttackers.Remove(requester);
    }
}
