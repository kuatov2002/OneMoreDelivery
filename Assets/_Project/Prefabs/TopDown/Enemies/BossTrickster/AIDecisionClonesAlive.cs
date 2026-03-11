using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Decision that checks if clones are alive
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Clones Alive")]
    public class AIDecisionClonesAlive : AIDecision
    {
        [Tooltip("Minimum clones that should be alive")]
        public int MinimumClones = 1;

        protected AIActionSummonClones _cloneAction;

        public override void Initialization()
        {
            _cloneAction = GetComponent<AIActionSummonClones>();
        }

        public override bool Decide()
        {
            if (_cloneAction == null) return false;
            return _cloneAction.AliveClones >= MinimumClones;
        }
    }
}