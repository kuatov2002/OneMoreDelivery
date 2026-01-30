using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Decision that returns true if enough time passed since last teleport
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Teleport Cooldown")]
    public class AIDecisionTeleportCooldown : AIDecision
    {
        [Tooltip("Cooldown duration in seconds")]
        public float CooldownDuration = 10f;

        protected float _lastTeleportTime = -1000f;

        public override bool Decide()
        {
            return (Time.time - _lastTeleportTime) >= CooldownDuration;
        }

        public void RegisterTeleport()
        {
            _lastTeleportTime = Time.time;
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            if (_lastTeleportTime < 0)
            {
                _lastTeleportTime = Time.time;
            }
        }
    }
}