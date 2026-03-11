using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Action that makes boss jump down to a lower level with shockwave
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Jump Down")]
    public class AIActionJumpDown : AIAction
    {
        [Header("Jump Settings")]
        [Tooltip("Target position to jump to")]
        public Transform JumpTarget;
        
        [Tooltip("Jump duration")]
        public float JumpDuration = 1f;
        
        [Tooltip("Jump height arc")]
        public float JumpHeight = 2f;
        
        [Header("Shockwave")]
        [Tooltip("Shockwave radius on landing")]
        public float ShockwaveRadius = 5f;
        
        [Tooltip("Shockwave damage")]
        public int ShockwaveDamage = 30;
        
        [Tooltip("Knockback force")]
        public float KnockbackForce = 10f;
        
        [Tooltip("Layer mask for shockwave targets")]
        public LayerMask ShockwaveTargets;
        
        [Header("VFX")]
        public GameObject LandingVFX;

        protected bool _hasJumped;

        public override void PerformAction()
        {
            if (_hasJumped) return;
            if (JumpTarget == null) return;

            StartCoroutine(JumpSequence());
            _hasJumped = true;
        }

        protected virtual System.Collections.IEnumerator JumpSequence()
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = JumpTarget.position;
            float elapsed = 0f;

            while (elapsed < JumpDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / JumpDuration;
                
                // Parabolic arc
                float height = JumpHeight * 4 * progress * (1 - progress);
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
                currentPos.y += height;
                
                transform.position = currentPos;
                
                yield return null;
            }

            transform.position = endPos;
            
            // Landing effects
            CreateShockwave();
            
            if (LandingVFX != null)
            {
                Instantiate(LandingVFX, transform.position, Quaternion.identity);
            }
        }

        protected virtual void CreateShockwave()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, ShockwaveRadius, ShockwaveTargets);
            
            foreach (Collider hit in hits)
            {
                // Damage
                Health health = hit.GetComponent<Health>();
                if (health != null)
                {
                    health.Damage(ShockwaveDamage, gameObject, 0.2f, 0.1f, Vector3.zero);
                }
                
                // Knockback
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 direction = (hit.transform.position - transform.position).normalized;
                    rb.AddForce(direction * KnockbackForce, ForceMode.Impulse);
                }
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            _hasJumped = false;
        }
    }
}