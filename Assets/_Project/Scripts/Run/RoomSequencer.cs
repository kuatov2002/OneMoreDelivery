using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoreMountains.TopDownEngine;

/// <summary>
/// Drives linear room progression for a run.
/// Persists across scene loads via DontDestroyOnLoad.
///
/// Usage:
///   1. Place one instance in the bootstrap/hub scene.
///   2. Call StartSequence(orderedSceneNames) to begin a run.
///   3. Room.cs calls OnRoomCleared() automatically after each room is cleared.
/// </summary>
public class RoomSequencer : MonoBehaviour
{
    public static RoomSequencer Instance { get; private set; }

    [Tooltip("Scene to load when all rooms are complete (run victory).")]
    [SerializeField] private string HubSceneName = "Hub";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Begins a new run with the given room sequence.
    /// Resets RunState, sets the sequence, then loads the first scene.
    /// </summary>
    public void StartSequence(List<string> roomScenes)
    {
        if (RunState.Instance == null)
        {
            Debug.LogError("[RoomSequencer] RunState.Instance is null. Cannot start sequence.", this);
            return;
        }

        if (roomScenes == null || roomScenes.Count == 0)
        {
            Debug.LogError("[RoomSequencer] roomScenes is null or empty. Cannot start sequence.", this);
            return;
        }

        RunState.Instance.SetRoomSequence(roomScenes);
        RunState.Instance.StartRun();

        LoadCurrentRoom();
    }

    /// <summary>
    /// Called by Room.cs after a room is cleared.
    /// Advances to the next room or ends the run on completion.
    /// </summary>
    public void OnRoomCleared()
    {
        if (RunState.Instance == null) return;

        if (RunState.Instance.HasNextRoom)
        {
            RunState.Instance.AdvanceRoom();
            LoadCurrentRoom();
        }
        else
        {
            RunState.Instance.EndRun(true);
            SceneManager.LoadScene(HubSceneName);
        }
    }

    private void LoadCurrentRoom()
    {
        string scene = RunState.Instance.CurrentRoomScene;
        if (string.IsNullOrEmpty(scene))
        {
            Debug.LogError("[RoomSequencer] CurrentRoomScene is null or empty. Check RoomSequence contents.", this);
            return;
        }

        SceneManager.LoadScene(scene);
    }
}
