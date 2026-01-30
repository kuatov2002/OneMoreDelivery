using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Action that summons floating books that attack the player
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Summon Books")]
    public class AIActionSummonBooks : AIAction
    {
        [Header("Summon Settings")]
        [Tooltip("Floating book prefab")]
        public GameObject BookPrefab;
        
        [Tooltip("Number of books to summon")]
        public int BookCount = 1;
        
        [Tooltip("Spawn radius around mage")]
        public float SpawnRadius = 3f;
        
        [Tooltip("Cooldown between summons")]
        public float SummonCooldown = 10f;
        
        [Header("VFX")]
        public GameObject SummonVFX;

        protected float _lastSummonTime;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();
            _lastSummonTime = -SummonCooldown;
        }

        public override void PerformAction()
        {
            if (Time.time - _lastSummonTime < SummonCooldown) return;
            if (BookPrefab == null) return;

            SummonBooks();
        }

        protected virtual void SummonBooks()
        {
            for (int i = 0; i < BookCount; i++)
            {
                // Random position around mage
                Vector3 randomOffset = Random.insideUnitSphere * SpawnRadius;
                randomOffset.y = transform.position.y; // Keep same height
                Vector3 spawnPos = transform.position + randomOffset;

                // Spawn VFX
                if (SummonVFX != null)
                {
                    Instantiate(SummonVFX, spawnPos, Quaternion.identity);
                }

                // Spawn book
                GameObject book = Instantiate(BookPrefab, spawnPos, Quaternion.identity);
                
                // Set book target
                AIBrain bookBrain = book.GetComponent<AIBrain>();
                if (bookBrain != null && _brain.Target != null)
                {
                    bookBrain.Target = _brain.Target;
                }
            }

            _lastSummonTime = Time.time;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, SpawnRadius);
        }
    }
}