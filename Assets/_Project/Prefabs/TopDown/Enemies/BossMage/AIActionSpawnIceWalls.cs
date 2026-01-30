// ═══════════════════════════════════════════════════════════════════
// ACTIONS для Мага
// ═══════════════════════════════════════════════════════════════════

using MoreMountains.Tools;
using UnityEngine;
using System.Collections.Generic;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Action that spawns ice walls that block part of the arena
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Spawn Ice Walls")]
    public class AIActionSpawnIceWalls : AIAction
    {
        [Header("Ice Wall Settings")]
        [Tooltip("Ice wall prefab")]
        public GameObject IceWallPrefab;
        
        [Tooltip("Spawn points for ice walls")]
        public Transform[] WallSpawnPoints;
        
        [Tooltip("How long walls stay active")]
        public float WallDuration = 15f;
        
        [Header("VFX")]
        public GameObject SpawnVFX;

        public bool WallsActive { get; protected set; }
        
        protected List<GameObject> _spawnedWalls = new List<GameObject>();
        protected float _wallSpawnTime;

        public override void PerformAction()
        {
            if (!WallsActive)
            {
                SpawnWalls();
            }
            else
            {
                // Check if walls should despawn
                if (Time.time - _wallSpawnTime >= WallDuration)
                {
                    DespawnWalls();
                }
            }
        }

        protected virtual void SpawnWalls()
        {
            if (IceWallPrefab == null || WallSpawnPoints == null) return;

            foreach (Transform spawnPoint in WallSpawnPoints)
            {
                // Spawn VFX
                if (SpawnVFX != null)
                {
                    Instantiate(SpawnVFX, spawnPoint.position, Quaternion.identity);
                }

                // Spawn wall
                GameObject wall = Instantiate(IceWallPrefab, spawnPoint.position, spawnPoint.rotation);
                _spawnedWalls.Add(wall);
            }

            WallsActive = true;
            _wallSpawnTime = Time.time;
        }

        protected virtual void DespawnWalls()
        {
            foreach (GameObject wall in _spawnedWalls)
            {
                if (wall != null)
                {
                    // Despawn VFX
                    if (SpawnVFX != null)
                    {
                        Instantiate(SpawnVFX, wall.transform.position, Quaternion.identity);
                    }
                    
                    Destroy(wall);
                }
            }

            _spawnedWalls.Clear();
            WallsActive = false;
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            WallsActive = false;
        }

        public override void OnExitState()
        {
            base.OnExitState();
            DespawnWalls();
        }
    }
}