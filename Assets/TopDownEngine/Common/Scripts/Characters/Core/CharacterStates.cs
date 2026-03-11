using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// The various states you can use to check if your character is doing something at the current frame.
    /// </summary>
    public class CharacterStates
    {
        /// <summary>
        /// The possible character conditions — high-level "what is happening to the character".
        /// </summary>
        public enum CharacterConditions
        {
            Normal,
            ControlledMovement,
            Frozen,
            Paused,
            Dead,
            Stunned
        }

        /// <summary>
        /// The possible movement states — what locomotion action is currently active.
        /// These usually correspond to their own ability class, but it's not mandatory.
        /// </summary>
        public enum MovementStates
        {
            Null,
            Idle,
            Falling,
            Walking,
            Running,
            Crouching,
            Crawling,
            Dashing,
            Jetpacking,
            Jumping,
            Pushing,
            DoubleJumping,
            Attacking,
            SpecialAttacking,
            FallingDownHole
        }
    }
}