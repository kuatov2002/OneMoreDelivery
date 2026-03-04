using UnityEngine;
using System.Collections.Generic;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Shtefan-specific Health subclass that intercepts incoming damage
    /// and routes it through CharacterShieldBlock for block/parry processing
    /// before falling through to the standard Health.Damage() pipeline.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/Core/Shtefan Health")]
    public class ShtefanHealth : Health
    {
        /// <summary>
        /// Cached shield block ability reference — looked up once during
        /// Initialization, not on every Damage() call.
        /// </summary>
        protected CharacterShieldBlock _shieldBlock;

        /// <summary>
        /// On initialization we cache the shield block ability.
        /// </summary>
        public override void Initialization()
        {
            base.Initialization();
            CacheShieldBlock();
        }

        /// <summary>
        /// Caches the CharacterShieldBlock reference from the character.
        /// Called during Initialization. Call again if abilities change at runtime.
        /// </summary>
        protected virtual void CacheShieldBlock()
        {
            _shieldBlock = _character?.FindAbility<CharacterShieldBlock>();
        }

        /// <summary>
        /// Intercepts damage to process shield block / parry before
        /// delegating to the base Health damage pipeline.
        /// </summary>
        public override void Damage(
            float damage,
            GameObject instigator,
            float flickerDuration,
            float invincibilityDuration,
            Vector3 damageDirection,
            List<TypedDamage> typedDamages = null)
        {
            if (_shieldBlock != null)
            {
                float processed = _shieldBlock.ProcessIncomingDamage(
                    damage, damageDirection, instigator);

                // Parry consumed the hit entirely — no damage to Shtefan.
                if (processed < 0f)
                    return;

                // Shield blocked — apply chip damage silently, without triggering
                // OnHit, "Damage" animation, or invincibility frames.
                // The shield plays its own BlockHitFeedback.
                if (processed < damage)
                {
                    if (!CanTakeDamageThisFrame())
                        return;

                    float chipDamage = ComputeDamageOutput(processed, typedDamages, true);

                    if (MasterHealth != null)
                        MasterHealth.SetHealth(MasterHealth.CurrentHealth - chipDamage);
                    else
                        SetHealth(CurrentHealth - chipDamage);

                    LastDamage = chipDamage;
                    LastDamageDirection = damageDirection;
                    UpdateHealthBar(true);
                    return;
                }

                // Shield did not block (wrong angle, not blocking, etc.)
                // Pass possibly modified damage through to the standard pipeline.
                damage = processed;
            }

            base.Damage(damage, instigator, flickerDuration,
                        invincibilityDuration, damageDirection, typedDamages);
        }
    }
}
