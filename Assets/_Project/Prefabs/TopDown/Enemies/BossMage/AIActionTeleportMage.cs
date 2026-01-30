using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Enhanced teleport with cooldown tracking for decisions
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Teleport Mage")]
    public class AIActionTeleportMage : AIAction
    {
        [Header("Teleport Points")]
        [Tooltip("Crystal positions to teleport to")]
        public Transform[] TeleportCrystals;
        
        [Header("VFX")]
        public GameObject TeleportOutVFX;
        public GameObject TeleportInVFX;
        public float TeleportDelay = 0.3f;

        protected bool _hasTeleported;
        protected AIDecisionTeleportCooldown _cooldownDecision;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();
            
            _cooldownDecision = GetComponent<AIDecisionTeleportCooldown>();
        }

        public override void PerformAction()
        {
            if (_hasTeleported) return;
            if (TeleportCrystals == null || TeleportCrystals.Length == 0) return;

            StartCoroutine(TeleportSequence());
            _hasTeleported = true;
        }

        protected virtual System.Collections.IEnumerator TeleportSequence()
        {
            // VFX out
            if (TeleportOutVFX != null)
            {
                Instantiate(TeleportOutVFX, transform.position, Quaternion.identity);
            }

            // Hide
            Renderer[] renderers = _brain.Owner.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
            }

            yield return new WaitForSeconds(TeleportDelay);

            // Choose random crystal
            Transform crystal = TeleportCrystals[Random.Range(0, TeleportCrystals.Length)];
            _brain.Owner.transform.position = crystal.position;

            // VFX in
            if (TeleportInVFX != null)
            {
                Instantiate(TeleportInVFX, transform.position, Quaternion.identity);
            }

            // Show
            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
            }

            // Register teleport in cooldown decision
            if (_cooldownDecision != null)
            {
                _cooldownDecision.RegisterTeleport();
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            _hasTeleported = false;
        }
    }
}