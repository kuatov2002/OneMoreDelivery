using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Event to broadcast boss phase changes
    /// </summary>
    public struct BossPhaseEvent
    {
        public int Phase;
        public GameObject Boss;

        public BossPhaseEvent(int phase, GameObject boss)
        {
            Phase = phase;
            Boss = boss;
        }

        static BossPhaseEvent e;
        public static void Trigger(int phase, GameObject boss)
        {
            e.Phase = phase;
            e.Boss = boss;
            MMEventManager.TriggerEvent(e);
        }
    }
}