using UnityEngine;
using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Mirror portals that teleport player randomly
    /// </summary>
    [AddComponentMenu("TopDown Engine/Environment/Mirror Portal Controller")]
    public class MirrorPortalController : MonoBehaviour, MMEventListener<BossPhaseEvent>
    {
        [Header("Portal Settings")]
        public GameObject PortalPrefab;
        public Transform[] PortalPositions;
        
        [Tooltip("Time before portal activates after player enters")]
        public float TeleportDelay = 0.5f;
        
        public GameObject TeleportVFX;
        
        [Header("Activation")]
        public int[] ActivePhases = new int[] { 2, 3 };

        protected bool _active = false;
        protected List<GameObject> _portals = new List<GameObject>();

        protected virtual void SpawnPortals()
        {
            foreach (Transform pos in PortalPositions)
            {
                GameObject portal = Instantiate(PortalPrefab, pos.position, pos.rotation);
                _portals.Add(portal);

                MirrorPortal portalScript = portal.GetComponent<MirrorPortal>();
                if (portalScript == null)
                {
                    portalScript = portal.AddComponent<MirrorPortal>();
                }
                portalScript.AllPortalPositions = PortalPositions;
                portalScript.TeleportDelay = TeleportDelay;
                portalScript.TeleportVFX = TeleportVFX;
            }
        }

        protected virtual void DespawnPortals()
        {
            foreach (GameObject portal in _portals)
            {
                if (portal != null) Destroy(portal);
            }
            _portals.Clear();
        }

        public virtual void StartPortals()
        {
            if (_active) return;
            _active = true;
            SpawnPortals();
        }

        public virtual void StopPortals()
        {
            _active = false;
            DespawnPortals();
        }

        public void OnMMEvent(BossPhaseEvent phaseEvent)
        {
            foreach (int phase in ActivePhases)
            {
                if (phaseEvent.Phase == phase)
                {
                    StartPortals();
                    return;
                }
            }
        }

        protected virtual void OnEnable()
        {
            this.MMEventStartListening<BossPhaseEvent>();
        }

        protected virtual void OnDisable()
        {
            this.MMEventStopListening<BossPhaseEvent>();
            StopPortals();
        }
    }

    public class MirrorPortal : MonoBehaviour
    {
        public Transform[] AllPortalPositions;
        public float TeleportDelay = 0.5f;
        public GameObject TeleportVFX;

        protected bool _canTeleport = true;

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!_canTeleport) return;

            Character character = other.GetComponent<Character>();
            if (character != null)
            {
                StartCoroutine(TeleportCharacter(character));
            }
        }

        protected virtual IEnumerator TeleportCharacter(Character character)
        {
            _canTeleport = false;

            // VFX out
            if (TeleportVFX != null)
            {
                Instantiate(TeleportVFX, character.transform.position, Quaternion.identity);
            }

            yield return new WaitForSeconds(TeleportDelay);

            // Random position (not current one)
            List<Transform> available = new List<Transform>(AllPortalPositions);
            available.RemoveAll(t => Vector3.Distance(t.position, transform.position) < 1f);

            if (available.Count > 0)
            {
                Transform destination = available[Random.Range(0, available.Count)];
                character.transform.position = destination.position;

                // VFX in
                if (TeleportVFX != null)
                {
                    Instantiate(TeleportVFX, destination.position, Quaternion.identity);
                }
            }

            yield return new WaitForSeconds(2f);
            _canTeleport = true;
        }
    }
}