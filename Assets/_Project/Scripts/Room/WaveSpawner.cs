using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.TopDownEngine;

/// <summary>
/// Spawns enemies in sequential waves when StartWaves() is called.
/// Place on the same GameObject as Room. Designed to replace pre-placed enemies
/// as the source of truth for room combat.
///
/// Wave order is top-to-bottom in the inspector. Each wave waits for all of its
/// enemies to die before the next wave begins.
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [Serializable]
    public class WaveEntry
    {
        [Tooltip("Enemy prefab to instantiate. Must have a Health component.")]
        public GameObject EnemyPrefab;

        [Tooltip("Where to spawn this enemy. Uses the Transform's position and rotation.")]
        public Transform SpawnPoint;
    }

    [Serializable]
    public class Wave
    {
        [Tooltip("Seconds to wait before spawning this wave.")]
        public float DelayBeforeWave = 0f;

        [Tooltip("All enemies to spawn in this wave simultaneously.")]
        public List<WaveEntry> Entries = new List<WaveEntry>();
    }

    [Tooltip("Waves to run in order. All enemies in a wave must die before the next wave starts.")]
    public List<Wave> Waves = new List<Wave>();

    // CombatTokenManager cached once — present on the same GameObject as Room.
    private CombatTokenManager _tokenManager;

    private void Awake()
    {
        _tokenManager = GetComponent<CombatTokenManager>();
    }

    /// <summary>
    /// Begins the wave sequence. <paramref name="onAllWavesCleared"/> is invoked
    /// once every enemy in every wave is dead. If Waves is empty, fires immediately.
    /// </summary>
    public void StartWaves(Action onAllWavesCleared)
    {
        if (Waves == null || Waves.Count == 0)
        {
            onAllWavesCleared?.Invoke();
            return;
        }

        StartCoroutine(RunWaves(onAllWavesCleared));
    }

    private IEnumerator RunWaves(Action onAllWavesCleared)
    {
        foreach (Wave wave in Waves)
        {
            if (wave.DelayBeforeWave > 0f)
                yield return new WaitForSeconds(wave.DelayBeforeWave);

            List<Health> waveEnemies = SpawnWave(wave);

            yield return WaitForAllDead(waveEnemies);
        }

        onAllWavesCleared?.Invoke();
    }

    private List<Health> SpawnWave(Wave wave)
    {
        var spawned = new List<Health>(wave.Entries.Count);

        foreach (WaveEntry entry in wave.Entries)
        {
            if (entry.EnemyPrefab == null)
            {
                Debug.LogWarning("[WaveSpawner] WaveEntry has no EnemyPrefab assigned — skipped.", this);
                continue;
            }
            if (entry.SpawnPoint == null)
            {
                Debug.LogWarning("[WaveSpawner] WaveEntry has no SpawnPoint assigned — skipped.", this);
                continue;
            }

            GameObject enemy = Instantiate(
                entry.EnemyPrefab,
                entry.SpawnPoint.position,
                entry.SpawnPoint.rotation);

            Health health = enemy.GetComponent<Health>();
            if (health != null)
                spawned.Add(health);
            else
                Debug.LogWarning($"[WaveSpawner] Spawned '{entry.EnemyPrefab.name}' has no Health component — it will not be tracked.", this);

            if (_tokenManager != null)
            {
                CombatTokenHolder holder = enemy.GetComponent<CombatTokenHolder>();
                if (holder != null)
                    holder.SetManager(_tokenManager);
            }
        }

        return spawned;
    }

    private IEnumerator WaitForAllDead(List<Health> enemies)
    {
        if (enemies.Count == 0) yield break;

        int aliveCount = enemies.Count;

        // Local function captured by the delegates below.
        void OnEnemyDeath() => aliveCount--;

        foreach (Health h in enemies)
            h.OnDeath += OnEnemyDeath;

        yield return new WaitUntil(() => aliveCount <= 0);

        // Unsubscribe even if Health was destroyed (null check required).
        foreach (Health h in enemies)
            if (h != null)
                h.OnDeath -= OnEnemyDeath;
    }
}
