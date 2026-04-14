using UnityEngine;
using MoreMountains.TopDownEngine;

/// <summary>
/// Thin controller that exposes RunState lifecycle methods to the Unity inspector.
/// Place in the hub or bootstrap scene. Wire StartRun() and EndRun() to UI buttons
/// or SceneEvents via UnityEvent in the inspector.
///
/// Requires RunState to already exist in the scene (or persist from a previous scene).
/// </summary>
public class RunLifecycleController : MonoBehaviour
{
    private void Awake()
    {
        if (RunState.Instance == null)
        {
            Debug.LogError("[RunLifecycleController] RunState.Instance is null. " +
                           "Ensure a RunState GameObject exists and has initialized before this component.", this);
            enabled = false;
        }
    }

    /// <summary>
    /// Begins a new run. Resets Gold, room/biome counters, and all run modifiers.
    /// VampireSouls are preserved. Safe to call from a UnityEvent or UI button.
    /// </summary>
    public void StartRun()
    {
        RunState.Instance.StartRun();
    }

    /// <summary>
    /// Ends the current run.
    /// Pass true for victory (Gold kept), false for defeat (Gold lost).
    /// Safe to call from a UnityEvent or UI button.
    /// </summary>
    public void EndRun(bool victory)
    {
        RunState.Instance.EndRun(victory);
    }
}
