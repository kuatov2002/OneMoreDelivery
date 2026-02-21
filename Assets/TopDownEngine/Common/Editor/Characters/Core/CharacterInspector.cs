using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.Rendering;

namespace MoreMountains.TopDownEngine
{
    [CustomEditor(typeof(Character), true)]
    [CanEditMultipleObjects]
    public class CharacterInspector : Editor
    {
        public enum Modes { TwoD, ThreeD }

        // ─────────────────────────────────────────────────────────────────────
        //  State color maps
        // ─────────────────────────────────────────────────────────────────────

        private static readonly Dictionary<CharacterStates.CharacterConditions, Color> ConditionColors =
            new Dictionary<CharacterStates.CharacterConditions, Color>
            {
                { CharacterStates.CharacterConditions.Normal,             new Color(0.40f, 0.85f, 0.40f) },
                { CharacterStates.CharacterConditions.ControlledMovement, new Color(0.40f, 0.75f, 1.00f) },
                { CharacterStates.CharacterConditions.Frozen,             new Color(0.60f, 0.90f, 1.00f) },
                { CharacterStates.CharacterConditions.Paused,             new Color(1.00f, 0.85f, 0.30f) },
                { CharacterStates.CharacterConditions.Dead,               new Color(0.85f, 0.20f, 0.20f) },
                { CharacterStates.CharacterConditions.Stunned,            new Color(0.90f, 0.50f, 0.10f) },
            };

        private static readonly Dictionary<CharacterStates.MovementStates, Color> MovementColors =
            new Dictionary<CharacterStates.MovementStates, Color>
            {
                { CharacterStates.MovementStates.Null,             new Color(0.40f, 0.40f, 0.40f) },
                { CharacterStates.MovementStates.Idle,             new Color(0.75f, 0.75f, 0.75f) },
                { CharacterStates.MovementStates.Walking,          new Color(0.40f, 0.85f, 0.40f) },
                { CharacterStates.MovementStates.Running,          new Color(0.30f, 1.00f, 0.50f) },
                { CharacterStates.MovementStates.Jumping,          new Color(0.40f, 0.75f, 1.00f) },
                { CharacterStates.MovementStates.DoubleJumping,    new Color(0.20f, 0.60f, 1.00f) },
                { CharacterStates.MovementStates.Falling,          new Color(1.00f, 0.70f, 0.20f) },
                { CharacterStates.MovementStates.FallingDownHole,  new Color(0.85f, 0.20f, 0.20f) },
                { CharacterStates.MovementStates.Dashing,          new Color(1.00f, 0.50f, 0.90f) },
                { CharacterStates.MovementStates.Crouching,        new Color(0.80f, 0.80f, 0.40f) },
                { CharacterStates.MovementStates.Crawling,         new Color(0.70f, 0.70f, 0.30f) },
                { CharacterStates.MovementStates.Attacking,        new Color(1.00f, 0.30f, 0.30f) },
                { CharacterStates.MovementStates.SpecialAttacking, new Color(1.00f, 0.10f, 0.50f) },
                { CharacterStates.MovementStates.Pushing,          new Color(0.90f, 0.60f, 0.30f) },
                { CharacterStates.MovementStates.Jetpacking,       new Color(0.50f, 0.90f, 1.00f) },
            };

        private static readonly Dictionary<Weapon.WeaponStates, Color> WeaponStateColors =
            new Dictionary<Weapon.WeaponStates, Color>
            {
                { Weapon.WeaponStates.WeaponIdle,             new Color(0.60f, 0.60f, 0.60f) },
                { Weapon.WeaponStates.WeaponStart,            new Color(1.00f, 0.80f, 0.20f) },
                { Weapon.WeaponStates.WeaponDelayBeforeUse,   new Color(1.00f, 0.70f, 0.10f) },
                { Weapon.WeaponStates.WeaponUse,              new Color(1.00f, 0.25f, 0.25f) },
                { Weapon.WeaponStates.WeaponDelayBetweenUses, new Color(1.00f, 0.55f, 0.20f) },
                { Weapon.WeaponStates.WeaponStop,             new Color(0.70f, 0.70f, 0.70f) },
                { Weapon.WeaponStates.WeaponReloadNeeded,     new Color(0.90f, 0.40f, 0.10f) },
                { Weapon.WeaponStates.WeaponReloadStart,      new Color(0.40f, 0.70f, 1.00f) },
                { Weapon.WeaponStates.WeaponReload,           new Color(0.30f, 0.60f, 1.00f) },
                { Weapon.WeaponStates.WeaponReloadStop,       new Color(0.25f, 0.50f, 1.00f) },
                { Weapon.WeaponStates.WeaponInterrupted,      new Color(0.80f, 0.20f, 0.80f) },
            };

        // ─────────────────────────────────────────────────────────────────────
        //  Foldout state
        // ─────────────────────────────────────────────────────────────────────

        private bool _abilitiesFoldout = false;

        // ─────────────────────────────────────────────────────────────────────
        //  Inspector
        // ─────────────────────────────────────────────────────────────────────

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Character character = (Character)target;

            if (Application.isPlaying && character.CharacterState != null)
            {
                DrawLiveState(character);
                EditorGUILayout.Space(6);
            }

            if (character.CharacterAnimator == null)
            {
                if (character.GetComponent<Animator>() != null)
                    character.CharacterAnimator = character.GetComponent<Animator>();
            }

            if (character.CharacterType == Character.CharacterTypes.Player)
            {
                DrawDefaultInspector();
            }

            if (character.CharacterType == Character.CharacterTypes.AI)
            {
                character.PlayerID = "";
                Editor.DrawPropertiesExcluding(serializedObject, new string[] { "PlayerID" });
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Autobuild", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The Character Autobuild button will automatically add all the components needed for a functioning Character, " +
                "and set their settings, layer, tags. Be careful, if you've already customized your character, this will reset its settings!",
                MessageType.Warning, true);

            if (GUILayout.Button("AutoBuild Player Character 2D")) GenerateCharacter(Character.CharacterTypes.Player, Modes.TwoD);
            if (GUILayout.Button("AutoBuild Player Character 3D")) GenerateCharacter(Character.CharacterTypes.Player, Modes.ThreeD);
            if (GUILayout.Button("AutoBuild AI Character 2D"))     GenerateCharacter(Character.CharacterTypes.AI,     Modes.TwoD);
            if (GUILayout.Button("AutoBuild AI Character 3D"))     GenerateCharacter(Character.CharacterTypes.AI,     Modes.ThreeD);

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
                Repaint();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Live state
        // ─────────────────────────────────────────────────────────────────────

        private void DrawLiveState(Character character)
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 10,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };

            DrawSectionHealth(character, headerStyle);
            EditorGUILayout.Space(4);
            DrawSectionCondition(character, headerStyle);
            EditorGUILayout.Space(4);
            DrawSectionMovement(character, headerStyle);
            EditorGUILayout.Space(4);
            DrawSectionController(character, headerStyle);
            EditorGUILayout.Space(4);
            DrawSectionWeapons(character, headerStyle);
            EditorGUILayout.Space(4);
            DrawSectionShield(character, headerStyle);
            EditorGUILayout.Space(4);
            DrawSectionAbilities(character, headerStyle);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Sections
        // ─────────────────────────────────────────────────────────────────────

        private void DrawSectionHealth(Character character, GUIStyle headerStyle)
        {
            Health health = character.CharacterHealth;
            if (health == null) return;

            EditorGUILayout.LabelField("HEALTH", headerStyle);

            float ratio = health.MaximumHealth > 0 ? health.CurrentHealth / health.MaximumHealth : 0f;
            Color healthColor = Color.Lerp(new Color(0.9f, 0.15f, 0.15f), new Color(0.15f, 0.85f, 0.3f), ratio);
            DrawProgressBar($"{health.CurrentHealth:F0} / {health.MaximumHealth:F0}", health.CurrentHealth, health.MaximumHealth, healthColor, showLabel: false);

            if (health.Invulnerable)
                DrawColoredLabel("⚡ Invulnerable", new Color(0.9f, 0.8f, 0.1f), height: 16);
        }

        private void DrawSectionCondition(Character character, GUIStyle headerStyle)
        {
            var condition = character.ConditionState.CurrentState;
            EditorGUILayout.LabelField("CONDITION", headerStyle);
            DrawColoredLabel(condition.ToString(),
                ConditionColors.TryGetValue(condition, out var cc) ? cc : Color.white);
        }

        private void DrawSectionMovement(Character character, GUIStyle headerStyle)
        {
            var movement = character.MovementState.CurrentState;
            var previous = character.MovementState.PreviousState;

            EditorGUILayout.LabelField("MOVEMENT STATE", headerStyle);
            DrawColoredLabel(movement.ToString(),
                MovementColors.TryGetValue(movement, out var mc) ? mc : Color.white);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("prev:", GUILayout.Width(30));
            DrawColoredLabel(previous.ToString(),
                MovementColors.TryGetValue(previous, out var pc) ? pc * 0.65f : Color.gray,
                height: 16);
            EditorGUILayout.EndHorizontal();

            var movementAbility = character.FindAbility<CharacterMovement>();
            if (movementAbility != null)
            {
                EditorGUILayout.Space(2);
                DrawProgressBar("Speed",      movementAbility.MovementSpeed,           30f, new Color(0.3f, 0.8f, 1f));
                DrawProgressBar("Multiplier", movementAbility.MovementSpeedMultiplier,  3f, new Color(0.9f, 0.7f, 0.3f));

                if (movementAbility.MovementForbidden)
                    DrawColoredLabel("⚠  Movement Forbidden", new Color(1f, 0.3f, 0.3f), height: 18);
            }
        }

        private void DrawSectionController(Character character, GUIStyle headerStyle)
        {
            var controller = character.GetComponent<TopDownController>();
            if (controller == null) return;

            EditorGUILayout.LabelField("CONTROLLER", headerStyle);

            bool grounded = controller.Grounded;
            DrawColoredLabel(grounded ? "Grounded" : "Airborne",
                grounded ? new Color(0.4f, 0.85f, 0.4f) : new Color(1f, 0.6f, 0.2f));

            DrawProgressBar("Velocity", controller.CurrentMovement.magnitude, 20f, new Color(0.3f, 0.8f, 1f));
        }

        private void DrawSectionWeapons(Character character, GUIStyle headerStyle)
        {
            var handleWeapons = character.FindAbilities<CharacterHandleWeapon>();
            if (handleWeapons == null || handleWeapons.Count == 0) return;

            EditorGUILayout.LabelField("WEAPONS", headerStyle);

            foreach (var handleWeapon in handleWeapons)
            {
                Weapon weapon = handleWeapon.CurrentWeapon;

                if (weapon == null)
                {
                    DrawColoredLabel($"[Slot {handleWeapon.HandleWeaponID}]  No weapon", new Color(0.35f, 0.35f, 0.35f), height: 18);
                    continue;
                }

                var weaponState = weapon.WeaponState.CurrentState;
                Color stateColor = WeaponStateColors.TryGetValue(weaponState, out var wc) ? wc : Color.white;

                // Weapon name + state on one row
                EditorGUILayout.BeginHorizontal();

                var nameRect = EditorGUILayout.GetControlRect(false, 20f, GUILayout.Width(130));
                EditorGUI.DrawRect(nameRect, new Color(0.2f, 0.2f, 0.2f));
                EditorGUI.LabelField(nameRect, weapon.name, new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal    = { textColor = new Color(0.85f, 0.85f, 0.85f) },
                    fontStyle = FontStyle.Bold,
                    padding   = new RectOffset(4, 0, 0, 0)
                });

                var stateRect = EditorGUILayout.GetControlRect(false, 20f);
                EditorGUI.DrawRect(stateRect, stateColor * 0.85f);
                EditorGUI.LabelField(stateRect, weaponState.ToString().Replace("Weapon", ""), new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal    = { textColor = Color.white },
                    fontStyle = FontStyle.Bold
                });

                EditorGUILayout.EndHorizontal();

                // Ammo bar if magazine-based
                if (weapon.MagazineBased)
                {
                    float ammoRatio = weapon.MagazineSize > 0 ? (float)weapon.CurrentAmmoLoaded / weapon.MagazineSize : 0f;
                    Color ammoColor = Color.Lerp(new Color(0.9f, 0.3f, 0.1f), new Color(0.3f, 0.85f, 0.3f), ammoRatio);
                    DrawProgressBar("Ammo", weapon.CurrentAmmoLoaded, weapon.MagazineSize, ammoColor, showLabel: false,
                        customLabel: $"{weapon.CurrentAmmoLoaded} / {weapon.MagazineSize}");
                }
            }
        }

        private void DrawSectionShield(Character character, GUIStyle headerStyle)
        {
            var shield = character.FindAbility<CharacterShieldBlock>();
            if (shield == null) return;

            EditorGUILayout.LabelField("SHIELD", headerStyle);

            // Determine state via MovementState — shield uses SpecialAttacking
            bool isBlocking = character.MovementState.CurrentState == CharacterStates.MovementStates.SpecialAttacking;

            if (isBlocking)
            {
                // Check parry window via reflection — field is protected, so we check via serialized name
                // If you want direct access, make _parryWindowActive internal in CharacterShieldBlock
                DrawColoredLabel("🛡  Blocking", new Color(0.20f, 0.55f, 1.00f));
            }
            else
            {
                DrawColoredLabel("Shield Inactive", new Color(0.35f, 0.35f, 0.35f), height: 18);
            }
        }

        private void DrawSectionAbilities(Character character, GUIStyle headerStyle)
        {
            var abilities = character.GetComponents<CharacterAbility>();
            if (abilities == null || abilities.Length == 0) return;

            _abilitiesFoldout = EditorGUILayout.Foldout(_abilitiesFoldout, "ABILITIES", true, new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            });

            if (!_abilitiesFoldout) return;

            foreach (var ability in abilities)
            {
                bool initialized = ability.AbilityInitialized;
                bool permitted   = ability.AbilityPermitted;
                bool enabled     = ability.enabled;

                Color rowColor;
                string statusIcon;

                if (!enabled)
                {
                    rowColor   = new Color(0.25f, 0.25f, 0.25f);
                    statusIcon = "✕";
                }
                else if (!permitted)
                {
                    rowColor   = new Color(0.55f, 0.35f, 0.10f);
                    statusIcon = "⊘";
                }
                else if (!initialized)
                {
                    rowColor   = new Color(0.40f, 0.40f, 0.10f);
                    statusIcon = "…";
                }
                else
                {
                    rowColor   = new Color(0.18f, 0.42f, 0.22f);
                    statusIcon = "✓";
                }

                EditorGUILayout.BeginHorizontal();

                // Status badge
                var badgeRect = EditorGUILayout.GetControlRect(false, 18f, GUILayout.Width(22));
                EditorGUI.DrawRect(badgeRect, rowColor);
                EditorGUI.LabelField(badgeRect, statusIcon, new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal    = { textColor = Color.white },
                    fontStyle = FontStyle.Bold
                });

                // Ability name
                var nameRect = EditorGUILayout.GetControlRect(false, 18f);
                EditorGUI.DrawRect(nameRect, new Color(0.18f, 0.18f, 0.18f));
                EditorGUI.LabelField(nameRect, ability.GetType().Name, new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal    = { textColor = enabled ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.45f, 0.45f, 0.45f) },
                    padding   = new RectOffset(4, 0, 0, 0)
                });

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(1);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Drawing helpers
        // ─────────────────────────────────────────────────────────────────────

        private static void DrawColoredLabel(string text, Color bgColor, float height = 22)
        {
            var rect = EditorGUILayout.GetControlRect(false, height);
            EditorGUI.DrawRect(rect, bgColor * 0.85f);

            var border = rect;
            border.height = 1;
            EditorGUI.DrawRect(border, bgColor);
            border.y = rect.yMax - 1;
            EditorGUI.DrawRect(border, bgColor);

            EditorGUI.LabelField(rect, text, new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white },
                fontSize  = 11
            });
        }

        private static void DrawProgressBar(string label, float value, float max, Color fillColor,
            float height = 18, bool showLabel = true, string customLabel = null)
        {
            EditorGUILayout.BeginHorizontal();

            if (showLabel)
                EditorGUILayout.LabelField(label, GUILayout.Width(70));

            var rect = EditorGUILayout.GetControlRect(false, height);
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

            float ratio = max > 0 ? Mathf.Clamp01(value / max) : 0f;
            if (ratio > 0)
            {
                var fill = new Rect(rect.x, rect.y, rect.width * ratio, rect.height);
                EditorGUI.DrawRect(fill, fillColor);
            }

            string displayText = customLabel ?? $"{value:F2}";
            EditorGUI.LabelField(rect, displayText, new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white },
                fontStyle = FontStyle.Bold
            });

            EditorGUILayout.EndHorizontal();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Autobuild (оригинальный код без изменений)
        // ─────────────────────────────────────────────────────────────────────

        protected virtual void GenerateCharacter(Character.CharacterTypes type, Modes mode)
        {
            Character character = (Character)target;

            Debug.LogFormat(character.name + " : Character Autobuild Start");

            if (type == Character.CharacterTypes.Player)
            {
                character.CharacterType = Character.CharacterTypes.Player;
                character.gameObject.layer = LayerMask.NameToLayer("Player");
                character.gameObject.tag   = "Player";
                character.PlayerID         = "Player1";
            }

            if (type == Character.CharacterTypes.AI)
            {
                character.CharacterType    = Character.CharacterTypes.AI;
                character.gameObject.layer = LayerMask.NameToLayer("Enemies");
            }

            if (mode == Modes.TwoD)
            {
                Rigidbody2D rigidbody2D = character.GetComponent<Rigidbody2D>() ?? character.gameObject.AddComponent<Rigidbody2D>();
                rigidbody2D.bodyType               = RigidbodyType2D.Dynamic;
                rigidbody2D.simulated              = true;
                rigidbody2D.useAutoMass            = false;
                rigidbody2D.mass                   = 1;
                rigidbody2D.linearDamping          = 1;
                rigidbody2D.angularDamping         = 0.05f;
                rigidbody2D.gravityScale           = 0;
                rigidbody2D.interpolation          = RigidbodyInterpolation2D.Interpolate;
                rigidbody2D.sleepMode              = RigidbodySleepMode2D.StartAwake;
                rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rigidbody2D.constraints            = RigidbodyConstraints2D.FreezeRotation;

                SortingGroup sortingGroup = character.GetComponent<SortingGroup>() ?? character.gameObject.AddComponent<SortingGroup>();
                sortingGroup.sortingLayerName = "Characters";

                BoxCollider2D boxcollider2D = character.GetComponent<BoxCollider2D>() ?? character.gameObject.AddComponent<BoxCollider2D>();
                boxcollider2D.isTrigger = false;

                TopDownController2D topDownController2D = character.GetComponent<TopDownController2D>() ?? character.gameObject.AddComponent<TopDownController2D>();
                topDownController2D.Gravity         = -30;
                topDownController2D.GroundLayerMask = LayerMask.GetMask("Ground");
                topDownController2D.HoleLayerMask   = LayerMask.GetMask("Hole");

                if (character.GetComponent<CharacterOrientation2D>() == null) character.gameObject.AddComponent<CharacterOrientation2D>();
                if (character.GetComponent<CharacterDash2D>()        == null) character.gameObject.AddComponent<CharacterDash2D>();
                if (character.GetComponent<CharacterJump2D>()        == null) character.gameObject.AddComponent<CharacterJump2D>();
            }

            if (mode == Modes.ThreeD)
            {
                CharacterController characterController = character.GetComponent<CharacterController>() ?? character.gameObject.AddComponent<CharacterController>();
                characterController.slopeLimit      = 45f;
                characterController.stepOffset      = 0.3f;
                characterController.skinWidth       = 0.08f;
                characterController.minMoveDistance = 0.001f;
                characterController.radius          = 0.5f;

                Rigidbody rigidbody = character.GetComponent<Rigidbody>() ?? character.gameObject.AddComponent<Rigidbody>();
                rigidbody.mass                   = 1;
                rigidbody.linearDamping          = 0;
                rigidbody.angularDamping         = 0.05f;
                rigidbody.interpolation          = RigidbodyInterpolation.None;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rigidbody.useGravity             = true;
                rigidbody.isKinematic            = true;

                TopDownController3D topDownController3D = character.GetComponent<TopDownController3D>() ?? character.gameObject.AddComponent<TopDownController3D>();
                topDownController3D.Gravity            = 40;
                topDownController3D.ObstaclesLayerMask = LayerMask.GetMask("Obstacles", "Ground", "ObstaclesDoors", "MovingPlatform", "FallingPlatform");

                if (character.GetComponent<CharacterOrientation3D>() == null) character.gameObject.AddComponent<CharacterOrientation3D>();
                if (character.GetComponent<CharacterCrouch>()        == null) character.gameObject.AddComponent<CharacterCrouch>();
                if (character.GetComponent<CharacterJump3D>()        == null) character.gameObject.AddComponent<CharacterJump3D>();
                if (character.GetComponent<CharacterDash3D>()        == null) character.gameObject.AddComponent<CharacterDash3D>();
            }

            if (character.GetComponent<CharacterMovement>() == null) character.gameObject.AddComponent<CharacterMovement>();
            if (character.GetComponent<CharacterRun>()      == null) character.gameObject.AddComponent<CharacterRun>();

            if (type == Character.CharacterTypes.Player)
            {
                if (character.GetComponent<CharacterButtonActivation>() == null) character.gameObject.AddComponent<CharacterButtonActivation>();
                if (character.GetComponent<CharacterPause>()            == null) character.gameObject.AddComponent<CharacterPause>();
                if (character.GetComponent<CharacterTimeControl>()      == null) character.gameObject.AddComponent<CharacterTimeControl>();
            }

            Health health = character.GetComponent<Health>() ?? character.gameObject.AddComponent<Health>();
            health.MaximumHealth = 100;
            health.CurrentHealth = 100;

            Debug.LogFormat(character.name + " : Character Autobuild Complete");
        }
    }
}