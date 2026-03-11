using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Fast melee dash attack with poison daggers
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Poison Dash")]
    public class AIActionPoisonDash : AIAction
    {
        [Header("Dash Settings")]
        [Tooltip("Dash distance")]
        public float DashDistance = 10f;
        
        [Tooltip("Dash speed")]
        public float DashSpeed = 20f;
        
        [Tooltip("Cooldown")]
        public float Cooldown = 5f;
        
        [Header("Damage")]
        [Tooltip("Damage on collision")]
        public int DashDamage = 30;
        
        [Tooltip("Poison damage per second")]
        public int PoisonDPS = 5;
        
        [Tooltip("Poison duration")]
        public float PoisonDuration = 5f;
        
        [Header("VFX")]
        public GameObject DashTrailVFX;
        
        public LayerMask DamageableLayers;

        protected float _lastDashTime;
        protected bool _isDashing = false;
        protected Vector3 _dashDirection;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();
            _lastDashTime = -Cooldown;
        }

        public override void PerformAction()
        {
            if (_isDashing) return;
            if (Time.time - _lastDashTime < Cooldown) return;
            if (_brain.Target == null) return;

            StartDash();
        }

        protected virtual void StartDash()
        {
            _dashDirection = (_brain.Target.position - transform.position).normalized;
            _isDashing = true;
            _lastDashTime = Time.time;

            if (DashTrailVFX != null)
            {
                GameObject trail = Instantiate(DashTrailVFX, transform.position, Quaternion.identity);
                trail.transform.SetParent(transform);
            }

            StartCoroutine(DashCoroutine());
        }

        protected virtual System.Collections.IEnumerator DashCoroutine()
        {
            float dashTime = DashDistance / DashSpeed;
            float elapsed = 0f;
            Vector3 startPos = transform.position;

            while (elapsed < dashTime)
            {
                transform.position += _dashDirection * DashSpeed * Time.deltaTime;

                // Check collisions
                Collider[] hits = Physics.OverlapSphere(transform.position, 1f, DamageableLayers);
                foreach (Collider hit in hits)
                {
                    Health health = hit.GetComponent<Health>();
                    if (health != null)
                    {
                        health.Damage(DashDamage, gameObject, 0.2f, 0.1f, _dashDirection * 5f);
                        
                        // Apply poison
                        StartCoroutine(ApplyPoisonCoroutine(health));
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            _isDashing = false;
        }

        protected virtual System.Collections.IEnumerator ApplyPoisonCoroutine(Health health)
        {
            int ticks = Mathf.RoundToInt(PoisonDuration);
            for (int i = 0; i < ticks; i++)
            {
                if (health != null)
                {
                    health.Damage(PoisonDPS, gameObject, 0.1f, 0.1f, Vector3.zero);
                }
                yield return new WaitForSeconds(1f);
            }
        }
    }
}