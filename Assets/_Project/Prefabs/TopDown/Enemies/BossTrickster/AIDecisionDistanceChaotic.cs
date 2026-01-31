using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Decision that checks distance but with random offset (unpredictable)
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Distance Chaotic")]
    public class AIDecisionDistanceChaotic : AIDecision
    {
        [Tooltip("Base distance threshold")]
        public float BaseDistance = 5f;
        
        [Tooltip("Random variation range")]
        public float RandomVariation = 2f;
        
        public enum ComparisonMode { CloserThan, FartherThan }
        public ComparisonMode Mode = ComparisonMode.CloserThan;

        protected float _currentThreshold;

        public override void OnEnterState()
        {
            base.OnEnterState();
            // Randomize threshold on state enter
            _currentThreshold = BaseDistance + Random.Range(-RandomVariation, RandomVariation);
        }

        public override bool Decide()
        {
            if (_brain.Target == null) return false;

            float distance = Vector3.Distance(transform.position, _brain.Target.position);

            if (Mode == ComparisonMode.CloserThan)
                return distance < _currentThreshold;
            else
                return distance > _currentThreshold;
        }
    }
}