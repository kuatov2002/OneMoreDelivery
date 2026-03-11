using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Special transition that uses random state decision
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Cycle State")]
    public class AIActionCycleState : AIAction
    {
        [Tooltip("Reference to random state decision")]
        public AIDecisionRandomState RandomStateDecision;

        protected bool _hasCycled = false;

        public override void PerformAction()
        {
            if (_hasCycled) return;

            if (RandomStateDecision != null)
            {
                string nextState = RandomStateDecision.GetChosenState();
                if (!string.IsNullOrEmpty(nextState))
                {
                    _brain.TransitionToState(nextState);
                }
            }

            _hasCycled = true;
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            _hasCycled = false;
        }
    }
}