using MoreMountains.Tools;
using UnityEngine;

// NOTE: This class was originally named `AIDecisionTimeInState` which collided
// with `MoreMountains.TopDownEngine.AIDecisionTimeInState` (built into TDE).
// Both classes lived in the same namespace → CS0433 ambiguity when referenced
// from any external assembly (e.g. scripting runners, editor tools).
//
// Renamed to `AIDecisionSimpleTimeInState` to disambiguate.
// Unity MonoBehaviour references bind by script asset GUID (stored in prefab
// `m_Script` field), not by class name, so existing prefabs
// (BossArcher, Duelist, Zombie) still bind correctly after the rename.
namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Simple single-threshold variant of AIDecisionTimeInState.
    /// Returns true after `TimeThreshold` seconds have passed since state entry.
    /// The TDE built-in version uses AfterTimeMin/AfterTimeMax (randomized).
    /// This is kept as a separate class so existing prefab bindings are preserved.
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Simple Time In State")]
    public class AIDecisionSimpleTimeInState : AIDecision
    {
        [Tooltip("Time threshold in seconds")]
        public float TimeThreshold = 15f;

        public override bool Decide()
        {
            return _brain.TimeInThisState >= TimeThreshold;
        }
    }
}