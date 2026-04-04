using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Returns true when the target is within stalk range — used to
    /// transition from Chase to Stalk (circling) behaviour.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision In Stalk Range")]
    public class AIDecisionInStalkRange : AIDecision
    {
        [Tooltip("Distance at which the enemy stops chasing and begins stalking/circling")]
        public float StalkRange = 5f;

        protected Transform _characterRoot;

        public override void Initialization()
        {
            var character = gameObject.GetComponentInParent<Character>();
            _characterRoot = character != null ? character.transform : transform;
        }

        public override bool Decide()
        {
            if (_brain.Target == null) return false;

            Vector3 toTarget = _brain.Target.position - _characterRoot.position;
            toTarget.y = 0f;

            return toTarget.sqrMagnitude <= StalkRange * StalkRange;
        }
    }
}
