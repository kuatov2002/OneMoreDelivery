using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Action that performs melee strike with weapon
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Melee Strike")]
    public class AIActionMeleeStrike : AIAction
    {
        [Header("Settings")]
        [Tooltip("Damage dealt by melee strike")]
        public int MeleeDamage = 25;
        
        [Tooltip("Attack range")]
        public float AttackRange = 3f;
        
        [Tooltip("Time between strikes")]
        public float StrikeCooldown = 2f;
        
        [Tooltip("Knockback force")]
        public float KnockbackForce = 5f;
        
        [Header("Binding")]
        public LayerMask TargetLayers;

        protected float _lastStrikeTime;
        protected Animator _animator;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();
            
            _animator = GetComponentInParent<Animator>();
            _lastStrikeTime = -StrikeCooldown;
        }

        public override void PerformAction()
        {
            if (Time.time - _lastStrikeTime < StrikeCooldown) return;
            if (_brain.Target == null) return;
            
            float distanceToTarget = Vector3.Distance(transform.position, _brain.Target.position);
            if (distanceToTarget > AttackRange) return;

            Strike();
        }

        protected virtual void Strike()
        {
            // Play animation
            if (_animator != null)
            {
                _animator.SetTrigger("MeleeAttack");
            }

            // Deal damage
            Collider[] hits = Physics.OverlapSphere(transform.position, AttackRange, TargetLayers);
            
            foreach (Collider hit in hits)
            {
                Health health = hit.GetComponent<Health>();
                if (health != null)
                {
                    Vector3 knockbackDirection = (hit.transform.position - transform.position).normalized;
                    health.Damage(MeleeDamage, gameObject, 0.2f, 0.1f, knockbackDirection * KnockbackForce);
                }
            }

            _lastStrikeTime = Time.time;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, AttackRange);
        }
    }
}