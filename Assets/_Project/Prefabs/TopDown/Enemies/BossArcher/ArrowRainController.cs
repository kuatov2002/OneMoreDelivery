using UnityEngine;
using MoreMountains.Tools;
using System.Collections;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Spawns arrow rain periodically for a set duration.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Environment/Arrow Rain Controller")]
    public class ArrowRainController : MonoBehaviour
    {
        [Header("Arrow Settings")]
        [Tooltip("Arrow projectile prefab")]
        public Projectile ArrowPrefab;
        
        [Tooltip("How many arrows per wave")]
        public int ArrowsPerWave = 10;
        
        [Tooltip("Time between waves")]
        public float WaveInterval = 5f;
        
        [Tooltip("Height from which arrows spawn")]
        public float SpawnHeight = 15f;
        
        [Header("Area")]
        [Tooltip("Center of the rain area")]
        public Transform RainCenter;
        
        [Tooltip("Radius of the rain area")]
        public float RainRadius = 10f;
        
        [Header("Warning")]
        [Tooltip("Warning indicator prefab (shows where arrow will land)")]
        public GameObject WarningIndicator;
        
        [Tooltip("Warning duration before arrow lands")]
        public float WarningDuration = 1f;

        [Header("Timing")]
        [Tooltip("Duration of rain when active")]
        public float RainDuration = 10f;
        
        [Tooltip("Interval between rain periods")]
        public float RainCooldown = 20f;
        
        [Tooltip("Start raining automatically on Start")]
        public bool AutoStart = true;

        protected bool _isRaining = false;
        protected Coroutine _rainCycleCoroutine;
        protected Coroutine _waveCoroutine;

        protected virtual void Start()
        {
            if (RainCenter == null)
            {
                RainCenter = transform;
            }

            if (AutoStart)
            {
                StartRainCycle();
            }
        }

        protected virtual IEnumerator RainCycle()
        {
            while (true)
            {
                _isRaining = true;
                _waveCoroutine = StartCoroutine(SpawnWaves());
                
                yield return new WaitForSeconds(RainDuration);
                
                _isRaining = false;
                if (_waveCoroutine != null)
                {
                    StopCoroutine(_waveCoroutine);
                }
                
                yield return new WaitForSeconds(RainCooldown);
            }
        }

        protected virtual IEnumerator SpawnWaves()
        {
            while (_isRaining)
            {
                SpawnArrowWave();
                yield return new WaitForSeconds(WaveInterval);
            }
        }

        protected virtual void SpawnArrowWave()
        {
            for (int i = 0; i < ArrowsPerWave; i++)
            {
                StartCoroutine(SpawnSingleArrow());
            }
        }

        protected virtual IEnumerator SpawnSingleArrow()
        {
            Vector2 randomCircle = Random.insideUnitCircle * RainRadius;
            Vector3 targetPos = RainCenter.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            Vector3 spawnPos = targetPos + Vector3.up * SpawnHeight;

            GameObject warning = null;
            if (WarningIndicator != null)
            {
                warning = Instantiate(WarningIndicator, targetPos, Quaternion.identity);
            }

            yield return new WaitForSeconds(WarningDuration);

            if (warning != null)
            {
                Destroy(warning);
            }

            if (ArrowPrefab != null)
            {
                GameObject arrow = Instantiate(ArrowPrefab.gameObject, spawnPos, Quaternion.Euler(90, 0, 0));
                
                Projectile projectile = arrow.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.SetDirection(Vector3.down, Quaternion.Euler(90, 0, 0), true);
                }
            }
        }

        public virtual void StartRainCycle()
        {
            if (_rainCycleCoroutine != null)
            {
                StopCoroutine(_rainCycleCoroutine);
            }
            
            _rainCycleCoroutine = StartCoroutine(RainCycle());
        }

        public virtual void StopRainCycle()
        {
            if (_rainCycleCoroutine != null)
            {
                StopCoroutine(_rainCycleCoroutine);
            }
            
            if (_waveCoroutine != null)
            {
                StopCoroutine(_waveCoroutine);
            }
            
            _isRaining = false;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (RainCenter == null) return;
            
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawSphere(RainCenter.position, RainRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(RainCenter.position, RainRadius);
        }

        protected virtual void OnDestroy()
        {
            StopRainCycle();
        }
    }
}