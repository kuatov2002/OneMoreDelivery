using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Decision that returns true after specified time has passed since state entry
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Time In State")]
    public class AIDecisionTimeInState : AIDecision
    {
        [Tooltip("Time threshold in seconds")]
        public float TimeThreshold = 15f;

        public override bool Decide()
        {
            return _brain.TimeInThisState >= TimeThreshold;
        }
    }
}