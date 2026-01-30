using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Decision that returns true once, then resets when exiting state
    /// Useful for one-time transitions
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Once")]
    public class AIDecisionOnce : AIDecision
    {
        private bool _hasTriggered = false;

        public override bool Decide()
        {
            if (!_hasTriggered)
            {
                _hasTriggered = true;
                return true;
            }
            return false;
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            _hasTriggered = false;
        }

        public override void OnExitState()
        {
            base.OnExitState();
            _hasTriggered = false;
        }
    }
}