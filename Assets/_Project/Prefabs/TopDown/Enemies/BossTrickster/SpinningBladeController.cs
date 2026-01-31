using UnityEngine;
using MoreMountains.Tools;
using System.Collections;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Rotating blade hazards
    /// </summary>
    [AddComponentMenu("TopDown Engine/Environment/Spinning Blade Controller")]
    public class SpinningBladeController : MonoBehaviour, MMEventListener<BossPhaseEvent>
    {
        [System.Serializable]
        public class Blade
        {
            public GameObject BladeObject;
            public float RotationSpeed = 360f;
            public int Damage = 25;
            public float DamageRadius = 2f;
            public float DamageInterval = 0.5f;
        }

        [Header("Blades")]
        public Blade[] Blades;
        
        public LayerMask TargetLayers;
        
        [Header("Activation")]
        public int[] ActivePhases = new int[] { 3 };

        protected bool _active = false;
        protected Coroutine[] _bladeCoroutines;

        protected virtual void Start()
        {
            _bladeCoroutines = new Coroutine[Blades.Length];
        }

        protected virtual IEnumerator RotateAndDamage(Blade blade, int index)
        {
            float damageTimer = 0f;

            while (_active)
            {
                // Rotate
                blade.BladeObject.transform.Rotate(Vector3.up, blade.RotationSpeed * Time.deltaTime);

                // Damage
                damageTimer += Time.deltaTime;
                if (damageTimer >= blade.DamageInterval)
                {
                    Collider[] hits = Physics.OverlapSphere(
                        blade.BladeObject.transform.position,
                        blade.DamageRadius,
                        TargetLayers
                    );

                    foreach (Collider hit in hits)
                    {
                        Health health = hit.GetComponent<Health>();
                        if (health != null)
                        {
                            health.Damage(blade.Damage, gameObject, 0.2f, 0.1f, Vector3.zero);
                        }
                    }

                    damageTimer = 0f;
                }

                yield return null;
            }
        }

        public virtual void StartBlades()
        {
            if (_active) return;
            _active = true;

            for (int i = 0; i < Blades.Length; i++)
            {
                _bladeCoroutines[i] = StartCoroutine(RotateAndDamage(Blades[i], i));
            }
        }

        public virtual void StopBlades()
        {
            _active = false;

            for (int i = 0; i < _bladeCoroutines.Length; i++)
            {
                if (_bladeCoroutines[i] != null)
                {
                    StopCoroutine(_bladeCoroutines[i]);
                }
            }
        }

        public void OnMMEvent(BossPhaseEvent phaseEvent)
        {
            foreach (int phase in ActivePhases)
            {
                if (phaseEvent.Phase == phase)
                {
                    StartBlades();
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
            StopBlades();
        }
    }
}