using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Decision that cycles through tactics on timer
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Tactic Timer")]
    public class AIDecisionTacticTimer : AIDecision
    {
        [Tooltip("Time in current tactic before switching")]
        public float TacticDuration = 5f;
        
        [Tooltip("Random variation (+/- seconds)")]
        public float RandomVariation = 2f;

        protected float _tacticStartTime;
        protected float _currentDuration;

        public override void OnEnterState()
        {
            base.OnEnterState();
            _tacticStartTime = Time.time;
            _currentDuration = TacticDuration + Random.Range(-RandomVariation, RandomVariation);
        }

        public override bool Decide()
        {
            return (Time.time - _tacticStartTime) >= _currentDuration;
        }
    }
}