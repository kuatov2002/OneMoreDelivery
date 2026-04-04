using System;
using MoreMountains.TopDownEngine;
using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField] private EnterZone[] _enterZones;
    [SerializeField] private Health[] _enemies;
    [SerializeField] private Door[] _enterDoors;
    [SerializeField] private Door[] _exitDoors;

    public event Action OnRoomEntered;
    public event Action OnRoomCleared;

    private int _aliveEnemyCount;
    private bool _isCleared;

    #region Lifecycle

    private void OnEnable()
    {
        foreach (EnterZone zone in _enterZones)
            zone.EventOnPlayerEntered += HandlePlayerEnteredRoom;
    }

    private void OnDisable()
    {
        foreach (EnterZone zone in _enterZones)
            zone.EventOnPlayerEntered -= HandlePlayerEnteredRoom;

        UnsubscribeFromAllEnemies();
    }

    private void Start()
    {
        _aliveEnemyCount = _enemies.Length;
        SubscribeToAllEnemies();
        AssignCombatTokenManager();

        OpenDoors(_enterDoors);
        CloseDoors(_exitDoors);
    }

    #endregion

    // ── Door helpers ─────────────────────────────────────────────────────────

    private static void OpenDoors(Door[] doors)
    {
        foreach (Door door in doors)
            door.OpenDoor();
    }

    private static void CloseDoors(Door[] doors)
    {
        foreach (Door door in doors)
            door.CloseDoor();
    }

    // ── Door entry ───────────────────────────────────────────────────────────

    private void HandlePlayerEnteredRoom()
    {
        if (_isCleared) return;

        CloseDoors(_enterDoors);
        CloseDoors(_exitDoors);

        OnRoomEntered?.Invoke();
    }

    // ── Combat token assignment ────────────────────────────────────────────────

    private void AssignCombatTokenManager()
    {
        var tokenManager = GetComponent<CombatTokenManager>();
        if (tokenManager == null) return;

        foreach (Health enemy in _enemies)
        {
            if (enemy == null) continue;
            var holder = enemy.GetComponent<CombatTokenHolder>();
            if (holder != null)
                holder.SetManager(tokenManager);
        }
    }

    // ── Enemy tracking ───────────────────────────────────────────────────────

    private void SubscribeToAllEnemies()
    {
        foreach (Health enemy in _enemies)
            if (enemy != null)
                enemy.OnDeath += HandleEnemyDeath;
    }

    private void UnsubscribeFromAllEnemies()
    {
        foreach (Health enemy in _enemies)
            if (enemy != null)
                enemy.OnDeath -= HandleEnemyDeath;
    }

    private void HandleEnemyDeath()
    {
        _aliveEnemyCount--;

        if (_aliveEnemyCount <= 0)
            ClearRoom();
    }

    // ── Room cleared ─────────────────────────────────────────────────────────

    private void ClearRoom()
    {
        if (_isCleared) return;
        _isCleared = true;

        OpenDoors(_exitDoors);
        OnRoomCleared?.Invoke();
    }
}