using System;
using MoreMountains.TopDownEngine;
using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField] private EnterZone[] _enterZones;
    [SerializeField] private Door[] _enterDoors;
    [SerializeField] private Door[] _exitDoors;

    public event Action OnRoomEntered;
    public event Action OnRoomCleared;

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
    }

    private void Start()
    {
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

        WaveSpawner spawner = GetComponent<WaveSpawner>();
        if (spawner != null)
            spawner.StartWaves(ClearRoom);
        else
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