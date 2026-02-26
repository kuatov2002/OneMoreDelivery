using UnityEngine;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// Serializable, allocation-free input buffer for use in CharacterAbility subclasses.
    ///
    /// Usage pattern:
    ///   HandleInput  → if button pressed but ability unavailable → _buffer.Request()
    ///   ProcessAbility → if ability now available → if (_buffer.ConsumeIfActive()) { DoAbility(); }
    ///   OnDeath / OnDisable                       → _buffer.Clear()
    /// </summary>
    [System.Serializable]
    public sealed class AbilityInputBuffer
    {
        [Tooltip("Enable input buffering for this ability")]
        public bool Enabled = true;

        [Tooltip("How long (seconds) a buffered input stays valid")]
        [Min(0f)]
        public float BufferDuration = 0.25f;

        // ── state ─────────────────────────────────────────────────────────────
        private float _expiresAt = -1f;

        /// <summary>True if a valid, unexpired input is stored.</summary>
        public bool IsActive => Enabled && Time.time < _expiresAt;

        // ── API ───────────────────────────────────────────────────────────────

        /// <summary>Store an input request, resetting the expiry window.</summary>
        public void Request()
        {
            if (!Enabled) return;
            _expiresAt = Time.time + BufferDuration;
        }

        /// <summary>
        /// If the buffer is active, consume it and return true.
        /// Returns false without side-effects when the buffer is empty or expired.
        /// </summary>
        public bool ConsumeIfActive()
        {
            if (!IsActive) return false;
            Clear();
            return true;
        }

        /// <summary>Discard any stored input immediately.</summary>
        public void Clear() => _expiresAt = -1f;
    }
}