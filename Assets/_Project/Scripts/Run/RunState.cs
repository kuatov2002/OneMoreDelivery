using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Singleton that tracks all run-scoped and persistent state across scene loads.
    ///
    /// Survives scene transitions via DontDestroyOnLoad. Place one instance in the
    /// first scene (or a bootstrap scene) and it will persist for the lifetime of
    /// the application.
    ///
    /// Run-scoped data (Gold, modifiers, room/biome counters) is reset on StartRun().
    /// Persistent data (VampireSouls) is never reset.
    /// </summary>
    public class RunState : MonoBehaviour
    {
        public static RunState Instance { get; private set; }

        // ── Persistent (never reset between runs) ───────────────────────────────
        /// <summary>Meta-currency that persists across all runs and deaths.</summary>
        public int VampireSouls { get; private set; }

        // ── Run-scoped (reset on StartRun) ──────────────────────────────────────
        /// <summary>Gold accumulated this run. Lost on death (EndRun(false)).</summary>
        public int Gold { get; private set; }

        /// <summary>Zero-indexed biome the player is currently in.</summary>
        public int CurrentBiome { get; private set; }

        /// <summary>Zero-indexed room within the current biome.</summary>
        public int CurrentRoom { get; private set; }

        /// <summary>True while a run is in progress.</summary>
        public bool IsRunActive { get; private set; }

        // ── Room sequence ───────────────────────────────────────────────────────
        private List<string> _roomSequence = new List<string>();

        /// <summary>Scene name for the current room index. Null if sequence is unset or exhausted.</summary>
        public string CurrentRoomScene =>
            _roomSequence != null && CurrentRoom < _roomSequence.Count
                ? _roomSequence[CurrentRoom]
                : null;

        /// <summary>True if at least one more room exists after the current one.</summary>
        public bool HasNextRoom =>
            _roomSequence != null && CurrentRoom + 1 < _roomSequence.Count;

        // ── Run modifiers ───────────────────────────────────────────────────────
        private readonly List<RunModifierEntry> _runModifiers = new List<RunModifierEntry>();
        private ReadOnlyCollection<RunModifierEntry> _runModifiersReadOnly;

        // ── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _runModifiersReadOnly = new ReadOnlyCollection<RunModifierEntry>(_runModifiers);
        }

        // ── Run control ─────────────────────────────────────────────────────────

        /// <summary>
        /// Begins a new run. Resets Gold, biome/room counters, and all run modifiers.
        /// VampireSouls are never touched.
        /// </summary>
        public void StartRun()
        {
            Gold = 0;
            CurrentBiome = 0;
            CurrentRoom = 0;
            IsRunActive = true;
            _runModifiers.Clear();
        }

        /// <summary>
        /// Ends the current run. On defeat (<paramref name="victory"/> = false), Gold is lost.
        /// VampireSouls are always preserved.
        /// </summary>
        public void EndRun(bool victory)
        {
            IsRunActive = false;
            if (!victory)
                Gold = 0;
        }

        // ── Gold ────────────────────────────────────────────────────────────────

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
        }

        /// <summary>
        /// Attempts to spend <paramref name="amount"/> Gold.
        /// Returns false (and leaves Gold unchanged) if the player cannot afford it.
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (amount <= 0 || Gold < amount) return false;
            Gold -= amount;
            return true;
        }

        // ── Vampire Souls ───────────────────────────────────────────────────────

        public void AddSouls(int amount)
        {
            if (amount <= 0) return;
            VampireSouls += amount;
        }

        // ── Run modifiers ───────────────────────────────────────────────────────

        /// <summary>
        /// Records a stat modifier for this run so CharacterStats can reapply it
        /// after a scene load. Call this whenever a gift is granted to the player.
        /// </summary>
        /// <param name="statType">Which stat the modifier targets.</param>
        /// <param name="modifier">The modifier to store and reapply.</param>
        public void AddRunModifier(StatType statType, StatModifier modifier)
        {
            _runModifiers.Add(new RunModifierEntry(statType, modifier));
        }

        /// <summary>
        /// Returns a read-only view of all modifiers accumulated this run.
        /// CharacterStats calls this during Initialization to reapply them after scene load.
        /// </summary>
        public IReadOnlyList<RunModifierEntry> GetRunModifiers() => _runModifiersReadOnly;

        // ── Room sequence control ────────────────────────────────────────────────

        /// <summary>
        /// Sets the ordered list of room scene names for the current run.
        /// Call this before <see cref="StartRun"/> so CurrentRoom = 0 aligns with index 0.
        /// </summary>
        public void SetRoomSequence(List<string> scenes)
        {
            _roomSequence = scenes ?? new List<string>();
        }

        // ── Navigation ──────────────────────────────────────────────────────────

        /// <summary>Advances to the next room within the current biome.</summary>
        public void AdvanceRoom() => CurrentRoom++;

        /// <summary>Advances to the next biome and resets the room counter to 0.</summary>
        public void AdvanceBiome()
        {
            CurrentBiome++;
            CurrentRoom = 0;
        }
    }

    /// <summary>
    /// Pairs a <see cref="StatModifier"/> with the <see cref="StatType"/> it targets.
    /// Required because StatModifier itself has no target stat — RunState needs both
    /// to reconstruct the full modifier call on scene load.
    /// </summary>
    [Serializable]
    public readonly struct RunModifierEntry
    {
        public readonly StatType StatType;
        public readonly StatModifier Modifier;

        public RunModifierEntry(StatType statType, StatModifier modifier)
        {
            StatType = statType;
            Modifier = modifier;
        }
    }
}
