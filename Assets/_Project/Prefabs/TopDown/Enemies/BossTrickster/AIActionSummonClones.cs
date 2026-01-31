using MoreMountains.Tools;
using UnityEngine;
using System.Collections.Generic;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Summons clones of the boss that are indistinguishable
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Summon Clones")]
    public class AIActionSummonClones : AIAction
    {
        [Header("Clone Settings")]
        [Tooltip("Clone prefab (should be identical to boss)")]
        public GameObject ClonePrefab;
        
        [Tooltip("Number of clones to summon")]
        public int CloneCount = 2;
        
        [Tooltip("Spawn positions")]
        public Transform[] SpawnPoints;
        
        [Tooltip("Clone HP percentage of original (0-1)")]
        public float CloneHealthPercentage = 0.33f;
        
        [Header("VFX")]
        public GameObject SummonVFX;
        public GameObject CloneDieVFX;

        public int AliveClones { get; protected set; }
        
        protected List<GameObject> _clones = new List<GameObject>();
        protected bool _hasSummoned = false;

        public override void PerformAction()
        {
            if (_hasSummoned) return;
            if (ClonePrefab == null) return;

            SummonClones();
            _hasSummoned = true;
        }

        protected virtual void SummonClones()
        {
            // Clear dead clones
            _clones.RemoveAll(clone => clone == null);

            Health bossHealth = _brain.Owner.GetComponent<Health>();
            float cloneHP = bossHealth != null ? bossHealth.MaximumHealth * CloneHealthPercentage : 100;

            for (int i = 0; i < CloneCount; i++)
            {
                Transform spawnPoint = SpawnPoints[i % SpawnPoints.Length];

                // VFX
                if (SummonVFX != null)
                {
                    Instantiate(SummonVFX, spawnPoint.position, Quaternion.identity);
                }

                // Spawn clone
                GameObject clone = Instantiate(ClonePrefab, spawnPoint.position, spawnPoint.rotation);
                _clones.Add(clone);

                // Setup clone
                Health cloneHealth = clone.GetComponent<Health>();
                if (cloneHealth != null)
                {
                    cloneHealth.MaximumHealth = cloneHP;
                    cloneHealth.CurrentHealth = cloneHP;
                    cloneHealth.OnDeath += () => OnCloneDeath(clone);
                }

                // Set target
                AIBrain cloneBrain = clone.GetComponent<AIBrain>();
                if (cloneBrain != null && _brain.Target != null)
                {
                    cloneBrain.Target = _brain.Target;
                }
            }

            AliveClones = _clones.Count;
        }

        protected virtual void OnCloneDeath(GameObject clone)
        {
            AliveClones--;
            
            if (CloneDieVFX != null)
            {
                Instantiate(CloneDieVFX, clone.transform.position, Quaternion.identity);
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            _hasSummoned = false;
        }

        public override void OnExitState()
        {
            base.OnExitState();
            // Don't destroy clones - they stay alive
        }
    }
}