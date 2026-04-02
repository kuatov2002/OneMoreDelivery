using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Tracks melee attack cooldown with per-enemy desync offset.
    /// Attach to the same GameObject as AIBrain.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Melee Enemy/Melee Attack Cooldown")]
    public class MeleeAttackCooldown : MonoBehaviour
    {
        [Tooltip("Cooldown after Recovery before next Telegraph")]
        public float Cooldown = 2.0f;

        [Tooltip("Max random offset on spawn for multi-enemy desync")]
        public float MaxDesyncOffset = 2.0f;

        [HideInInspector]
        public float LastAttackEndTime;

        public bool IsReady => Time.time - LastAttackEndTime >= Cooldown;

        protected virtual void Awake()
        {
            LastAttackEndTime = Time.time - Cooldown + Random.Range(0f, MaxDesyncOffset);
        }
    }
}
