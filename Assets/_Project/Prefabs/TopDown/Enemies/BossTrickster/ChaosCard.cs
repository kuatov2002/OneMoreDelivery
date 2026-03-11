using MoreMountains.TopDownEngine;
using UnityEngine;

/// <summary>
    /// Component for chaos card projectile
    /// </summary>
    public class ChaosCard : MonoBehaviour
    {
        public AIActionThrowChaosCards.CardEffect Effect;

        protected DamageOnTouch _damageOnTouch;

        protected virtual void Start()
        {
            _damageOnTouch = GetComponent<DamageOnTouch>();
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            ApplyEffect(other.gameObject);
        }

        protected virtual void ApplyEffect(GameObject target)
        {
            Health health = target.GetComponent<Health>();
            if (health == null) return;

            switch (Effect)
            {
                case AIActionThrowChaosCards.CardEffect.Damage:
                    // Normal damage (handled by DamageOnTouch)
                    break;

                case AIActionThrowChaosCards.CardEffect.Poison:
                    // Apply poison DoT
                    break;

                case AIActionThrowChaosCards.CardEffect.Slow:
                    // Apply slow
                    break;

                case AIActionThrowChaosCards.CardEffect.Heal:
                    // Heal enemies (chaos!)
                    if (target.CompareTag("Player"))
                    {
                        health.CurrentHealth = Mathf.Min(health.CurrentHealth + 2, health.MaximumHealth);
                    }
                    break;
            }
        }

        protected virtual System.Collections.IEnumerator ApplyPoisonCoroutine(Health health)
        {
            for (int i = 0; i < 5; i++)
            {
                if (health != null)
                {
                    health.Damage(5, gameObject, 0.1f, 0.1f, Vector3.zero);
                }
                yield return new WaitForSeconds(1f);
            }
        }

        protected virtual System.Collections.IEnumerator ApplySlowCoroutine(CharacterMovement movement)
        {
            float original = movement.MovementSpeedMultiplier;
            movement.MovementSpeedMultiplier *= 0.5f;
            yield return new WaitForSeconds(3f);
            movement.MovementSpeedMultiplier = original;
        }
    }