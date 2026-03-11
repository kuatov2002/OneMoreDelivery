using UnityEngine;
using MoreMountains.Tools;
using System.Collections;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Floor cards that activate when stepped on
    /// </summary>
    [AddComponentMenu("TopDown Engine/Environment/Card Trap Controller")]
    public class CardTrapController : MonoBehaviour, MMEventListener<BossPhaseEvent>
    {
        [System.Serializable]
        public class CardTrap
        {
            public Transform Position;
            public GameObject WarningVFX;
            public GameObject ActivationVFX;
            public int Damage = 20;
            public float Radius = 2f;
        }

        [Header("Traps")]
        public CardTrap[] Traps;
        
        [Tooltip("Time between trap activations")]
        public float ActivationInterval = 3f;
        
        [Tooltip("Warning time before activation")]
        public float WarningTime = 1f;
        
        public LayerMask TargetLayers;
        
        [Header("Activation")]
        public int[] ActivePhases = new int[] { 3 };

        protected bool _active = false;
        protected Coroutine _trapCoroutine;

        protected virtual IEnumerator TrapLoop()
        {
            while (_active)
            {
                // Random trap
                CardTrap trap = Traps[Random.Range(0, Traps.Length)];
                
                StartCoroutine(ActivateTrap(trap));

                yield return new WaitForSeconds(ActivationInterval);
            }
        }

        protected virtual IEnumerator ActivateTrap(CardTrap trap)
        {
            // Warning
            GameObject warning = null;
            if (trap.WarningVFX != null)
            {
                warning = Instantiate(trap.WarningVFX, trap.Position.position, Quaternion.identity);
            }

            yield return new WaitForSeconds(WarningTime);

            // Remove warning
            if (warning != null) Destroy(warning);

            // Activation VFX
            if (trap.ActivationVFX != null)
            {
                Instantiate(trap.ActivationVFX, trap.Position.position, Quaternion.identity);
            }

            // Damage
            Collider[] hits = Physics.OverlapSphere(trap.Position.position, trap.Radius, TargetLayers);
            foreach (Collider hit in hits)
            {
                Health health = hit.GetComponent<Health>();
                if (health != null)
                {
                    health.Damage(trap.Damage, gameObject, 0.2f, 0.1f, Vector3.zero);
                }
            }
        }

        public virtual void StartTraps()
        {
            if (_active) return;
            _active = true;
            _trapCoroutine = StartCoroutine(TrapLoop());
        }

        public virtual void StopTraps()
        {
            _active = false;
            if (_trapCoroutine != null) StopCoroutine(_trapCoroutine);
        }

        public void OnMMEvent(BossPhaseEvent phaseEvent)
        {
            foreach (int phase in ActivePhases)
            {
                if (phaseEvent.Phase == phase)
                {
                    StartTraps();
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
            StopTraps();
        }
    }
}