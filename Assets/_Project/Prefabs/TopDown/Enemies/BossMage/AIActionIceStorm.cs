using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Action that casts ice storm AoE around the mage
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Ice Storm")]
    public class AIActionIceStorm : AIAction
    {
        [Header("Ice Storm Settings")]
        [Tooltip("Radius of the ice storm")]
        public float StormRadius = 8f;
        
        [Tooltip("Damage per second")]
        public int DamagePerSecond = 10;
        
        [Tooltip("Storm duration")]
        public float StormDuration = 3f;
        
        [Tooltip("Cooldown between storms")]
        public float Cooldown = 8f;
        
        [Tooltip("Slow percentage (0-1)")]
        public float SlowAmount = 0.5f;
        
        [Header("Targeting")]
        public LayerMask TargetLayers;
        
        [Header("VFX")]
        public GameObject StormVFX;

        protected bool _stormActive;
        protected float _stormStartTime;
        protected float _lastStormTime;
        protected float _lastDamageTime;
        protected GameObject _stormEffect;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();
            _lastStormTime = -Cooldown;
        }

        public override void PerformAction()
        {
            if (_stormActive)
            {
                // Deal damage over time
                if (Time.time - _lastDamageTime >= 1f)
                {
                    DealDamage();
                    _lastDamageTime = Time.time;
                }

                // Check if storm should end
                if (Time.time - _stormStartTime >= StormDuration)
                {
                    EndStorm();
                }
            }
            else
            {
                // Check if can cast new storm
                if (Time.time - _lastStormTime >= Cooldown)
                {
                    StartStorm();
                }
            }
        }

        protected virtual void StartStorm()
        {
            _stormActive = true;
            _stormStartTime = Time.time;
            _lastDamageTime = Time.time;

            // Spawn VFX
            if (StormVFX != null)
            {
                _stormEffect = Instantiate(StormVFX, transform.position+ Vector3.up*0.5f, Quaternion.identity);
            }
        }

        protected virtual void DealDamage()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, StormRadius, TargetLayers);

            foreach (Collider hit in hits)
            {
                Health health = hit.GetComponent<Health>();
                if (health != null)
                {
                    health.Damage(DamagePerSecond, gameObject, 0.2f, 0.1f, Vector3.zero);
                }

                // Apply slow
                CharacterMovement movement = hit.GetComponent<CharacterMovement>();
                if (movement != null)
                {
                    // Slow effect (you'd need to implement this properly)
                    StartCoroutine(ApplySlowCoroutine(movement));
                }
            }
        }

        protected virtual System.Collections.IEnumerator ApplySlowCoroutine(CharacterMovement movement)
        {
            float originalSpeed = movement.MovementSpeedMultiplier;
            movement.MovementSpeedMultiplier *= (1f - SlowAmount);
            yield return new WaitForSeconds(1f);
            movement.MovementSpeedMultiplier = originalSpeed;
        }

        protected virtual void EndStorm()
        {
            _stormActive = false;
            _lastStormTime = Time.time;

            if (_stormEffect != null)
            {
                Destroy(_stormEffect);
            }
        }

        public override void OnExitState()
        {
            base.OnExitState();
            if (_stormActive)
            {
                EndStorm();
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawSphere(transform.position, StormRadius);
        }
    }
}