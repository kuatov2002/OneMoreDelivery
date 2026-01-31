using UnityEngine;
using MoreMountains.Tools;
using System.Collections;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Poison clouds that move randomly
    /// </summary>
    [AddComponentMenu("TopDown Engine/Environment/Poison Cloud Controller")]
    public class PoisonCloudController : MonoBehaviour, MMEventListener<BossPhaseEvent>
    {
        [Header("Cloud Settings")]
        public GameObject CloudPrefab;
        public int CloudCount = 3;
        public float CloudSpeed = 2f;
        public int DamagePerSecond = 5;
        public float CloudRadius = 3f;
        
        [Header("Movement")]
        public Vector3 ArenaMin = new Vector3(-15, 0, -15);
        public Vector3 ArenaMax = new Vector3(15, 0, 15);
        
        public LayerMask TargetLayers;
        
        [Header("Activation")]
        public int[] ActivePhases = new int[] { 2, 3 };

        protected bool _active = false;
        protected GameObject[] _clouds;

        protected virtual void SpawnClouds()
        {
            _clouds = new GameObject[CloudCount];

            for (int i = 0; i < CloudCount; i++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(ArenaMin.x, ArenaMax.x),
                    transform.position.y,
                    Random.Range(ArenaMin.z, ArenaMax.z)
                );

                GameObject cloud = Instantiate(CloudPrefab, randomPos, Quaternion.identity);
                _clouds[i] = cloud;

                PoisonCloud cloudScript = cloud.AddComponent<PoisonCloud>();
                cloudScript.Speed = CloudSpeed;
                cloudScript.DamagePerSecond = DamagePerSecond;
                cloudScript.Radius = CloudRadius;
                cloudScript.ArenaMin = ArenaMin;
                cloudScript.ArenaMax = ArenaMax;
                cloudScript.TargetLayers = TargetLayers;
            }
        }

        protected virtual void DespawnClouds()
        {
            if (_clouds == null) return;

            foreach (GameObject cloud in _clouds)
            {
                if (cloud != null) Destroy(cloud);
            }
            _clouds = null;
        }

        public virtual void StartClouds()
        {
            if (_active) return;
            _active = true;
            SpawnClouds();
        }

        public virtual void StopClouds()
        {
            _active = false;
            DespawnClouds();
        }

        public void OnMMEvent(BossPhaseEvent phaseEvent)
        {
            foreach (int phase in ActivePhases)
            {
                if (phaseEvent.Phase == phase)
                {
                    StartClouds();
                    return;
                }
            }
        }

        protected virtual void OnEnable()
        {
            this.MMEventStartListening<BossPhaseEvent>();
        }

        protected virtual void OnDisable()
        {
            this.MMEventStopListening<BossPhaseEvent>();
            StopClouds();
        }
    }

    public class PoisonCloud : MonoBehaviour
    {
        public float Speed = 2f;
        public int DamagePerSecond = 5;
        public float Radius = 3f;
        public Vector3 ArenaMin;
        public Vector3 ArenaMax;
        public LayerMask TargetLayers;

        protected Vector3 _direction;
        protected float _lastDamageTime;

        protected virtual void Start()
        {
            ChangeDirection();
        }

        protected virtual void Update()
        {
            // Move
            transform.position += _direction * Speed * Time.deltaTime;

            // Bounce off walls
            if (transform.position.x < ArenaMin.x || transform.position.x > ArenaMax.x)
            {
                _direction.x = -_direction.x;
            }
            if (transform.position.z < ArenaMin.z || transform.position.z > ArenaMax.z)
            {
                _direction.z = -_direction.z;
            }

            // Random direction change
            if (Random.value < 0.01f)
            {
                ChangeDirection();
            }

            // Damage
            if (Time.time - _lastDamageTime >= 1f)
            {
                DealDamage();
                _lastDamageTime = Time.time;
            }
        }

        protected virtual void ChangeDirection()
        {
            _direction = new Vector3(
                Random.Range(-1f, 1f),
                0,
                Random.Range(-1f, 1f)
            ).normalized;
        }

        protected virtual void DealDamage()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, Radius, TargetLayers);
            foreach (Collider hit in hits)
            {
                Health health = hit.GetComponent<Health>();
                if (health != null)
                {
                    health.Damage(DamagePerSecond, gameObject, 0.1f, 0.1f, Vector3.zero);
                }
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawSphere(transform.position, Radius);
        }
    }
}