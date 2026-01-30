using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Action that summons minions at specified spawn points
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Summon Minions")]
    public class AIActionSummonMinions : AIAction
    {
        [Header("Summon Settings")]
        [Tooltip("Minion prefab to spawn")]
        public GameObject MinionPrefab;
        
        [Tooltip("Spawn points for minions")]
        public Transform[] SpawnPoints;
        
        [Tooltip("Number of minions to summon")]
        public int MinionCount = 2;
        
        [Header("VFX")]
        public GameObject SummonVFX;

        protected bool _hasSummoned;

        public override void PerformAction()
        {
            if (_hasSummoned) return;
            if (MinionPrefab == null || SpawnPoints == null || SpawnPoints.Length == 0) return;

            SummonMinions();
            _hasSummoned = true;
        }

        protected virtual void SummonMinions()
        {
            int spawnCount = Mathf.Min(MinionCount, SpawnPoints.Length);
            
            for (int i = 0; i < spawnCount; i++)
            {
                Transform spawnPoint = SpawnPoints[i];
                
                // Spawn VFX
                if (SummonVFX != null)
                {
                    Instantiate(SummonVFX, spawnPoint.position, Quaternion.identity);
                }
                
                // Spawn minion
                GameObject minion = Instantiate(MinionPrefab, spawnPoint.position, spawnPoint.rotation);
                
                // Set minion target to player
                AIBrain minionBrain = minion.GetComponent<AIBrain>();
                if (minionBrain != null && _brain.Target != null)
                {
                    minionBrain.Target = _brain.Target;
                }
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            _hasSummoned = false;
        }
    }
}