using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Action that teleports to a random position from predefined points
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Teleport")]
    public class AIActionTeleport : AIAction
    {
        [Header("Teleport Points")]
        [Tooltip("Array of possible teleport destinations")]
        public Transform[] TeleportPoints;
        
        [Header("VFX")]
        [Tooltip("Particle effect on teleport out")]
        public GameObject TeleportOutVFX;
        
        [Tooltip("Particle effect on teleport in")]
        public GameObject TeleportInVFX;
        
        [Tooltip("Delay between disappear and appear")]
        public float TeleportDelay = 0.3f;

        protected bool _hasTeleported;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();
        }

        public override void PerformAction()
        {
            if (_hasTeleported) return;
            if (TeleportPoints == null || TeleportPoints.Length == 0) return;

            StartCoroutine(TeleportSequence());
            _hasTeleported = true;
        }

        protected virtual System.Collections.IEnumerator TeleportSequence()
        {
            // Teleport out VFX
            if (TeleportOutVFX != null)
            {
                Instantiate(TeleportOutVFX, transform.position, Quaternion.identity);
            }

            // Hide character temporarily
            Renderer[] renderers = _brain.Owner.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
            }

            yield return new WaitForSeconds(TeleportDelay);

            // Choose random teleport point (not current position)
            Transform chosenPoint = TeleportPoints[Random.Range(0, TeleportPoints.Length)];
            _brain.Owner.transform.position = chosenPoint.position;

            // Teleport in VFX
            if (TeleportInVFX != null)
            {
                Instantiate(TeleportInVFX, transform.position, Quaternion.identity);
            }

            // Show character
            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            _hasTeleported = false;
        }
    }
}