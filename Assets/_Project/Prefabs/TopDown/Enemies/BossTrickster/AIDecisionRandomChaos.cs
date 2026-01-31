using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Decision that randomly returns true/false (for chaos)
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Random Chaos")]
    public class AIDecisionRandomChaos : AIDecision
    {
        [Tooltip("Probability of returning true (0-1)")]
        public float ChaosProbability = 0.3f;
        
        [Tooltip("Check frequency in seconds")]
        public float CheckFrequency = 2f;

        protected float _lastCheckTime;

        public override bool Decide()
        {
            if (Time.time - _lastCheckTime < CheckFrequency)
            {
                return false;
            }

            _lastCheckTime = Time.time;
            return Random.value < ChaosProbability;
        }
    }
}