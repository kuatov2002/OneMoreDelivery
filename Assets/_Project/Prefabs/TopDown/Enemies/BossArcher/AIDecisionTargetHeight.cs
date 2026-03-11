using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Decision that checks if target is on a specific Y level (for vertical arenas)
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Target Height")]
    public class AIDecisionTargetHeight : AIDecision
    {
        public enum HeightComparison { Below, Above, Same }
        
        [Tooltip("Height comparison mode")]
        public HeightComparison Comparison = HeightComparison.Below;
        
        [Tooltip("Height threshold")]
        public float HeightThreshold = 3f;
        
        [Tooltip("Tolerance for 'Same' comparison")]
        public float Tolerance = 1f;

        public override bool Decide()
        {
            if (_brain.Target == null) return false;

            float heightDifference = _brain.Target.position.y - _brain.Owner.transform.position.y;

            switch (Comparison)
            {
                case HeightComparison.Below:
                    return heightDifference < -HeightThreshold;
                    
                case HeightComparison.Above:
                    return heightDifference > HeightThreshold;
                    
                case HeightComparison.Same:
                    return Mathf.Abs(heightDifference) <= Tolerance;
                    
                default:
                    return false;
            }
        }
    }
}