// ═══════════════════════════════════════════════════════════════════
// DECISIONS для Мага
// ═══════════════════════════════════════════════════════════════════

using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Decision that checks if ice walls are currently active
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Ice Walls Active")]
    public class AIDecisionIceWallsActive : AIDecision
    {
        [Tooltip("Reference to the ice wall spawner")]
        public AIActionSpawnIceWalls IceWallSpawner;

        public override void Initialization()
        {
            if (IceWallSpawner == null)
            {
                IceWallSpawner = GetComponent<AIActionSpawnIceWalls>();
            }
        }

        public override bool Decide()
        {
            if (IceWallSpawner == null) return false;
            return IceWallSpawner.WallsActive;
        }
    }
}