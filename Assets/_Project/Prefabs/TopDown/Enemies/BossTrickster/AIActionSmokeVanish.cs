using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Creates smoke screen and becomes invisible/invulnerable
    /// </summary>
    [AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Smoke Vanish")]
    public class AIActionSmokeVanish : AIAction
    {
        [Header("Smoke Settings")]
        [Tooltip("Smoke VFX prefab")]
        public GameObject SmokePrefab;
        
        [Tooltip("Duration of invisibility")]
        public float InvisibilityDuration = 2f;
        
        [Header("Teleport")]
        [Tooltip("Teleport positions")]
        public Transform[] TeleportPoints;

        protected bool _hasVanished = false;
        protected Renderer[] _renderers;
        protected Health _health;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();

            _renderers = _brain.Owner.GetComponentsInChildren<Renderer>();
            _health = _brain.Owner.GetComponent<Health>();
        }

        public override void PerformAction()
        {
            if (_hasVanished) return;

            StartCoroutine(VanishSequence());
            _hasVanished = true;
        }

        protected virtual System.Collections.IEnumerator VanishSequence()
        {
            // Spawn smoke
            if (SmokePrefab != null)
            {
                Instantiate(SmokePrefab, transform.position, Quaternion.identity);
            }

            // Make invulnerable
            if (_health != null)
            {
                _health.Invulnerable = true;
            }

            // Hide
            foreach (var renderer in _renderers)
            {
                renderer.enabled = false;
            }

            yield return new WaitForSeconds(InvisibilityDuration / 2f);

            // Teleport
            if (TeleportPoints != null && TeleportPoints.Length > 0)
            {
                Transform newPos = TeleportPoints[Random.Range(0, TeleportPoints.Length)];
                _brain.Owner.transform.position = newPos.position;
            }

            yield return new WaitForSeconds(InvisibilityDuration / 2f);

            // Reappear
            if (SmokePrefab != null)
            {
                Instantiate(SmokePrefab, transform.position, Quaternion.identity);
            }

            foreach (var renderer in _renderers)
            {
                renderer.enabled = true;
            }

            if (_health != null)
            {
                _health.Invulnerable = false;
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            _hasVanished = false;
        }
    }
}