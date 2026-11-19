using DarknessRises.Game.Controls;
using DarknessRises.Game.Gestures;
using DarknessRises.Game.Weapons;
using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor.Gestures
{
    /// <summary>
    /// Builds touch controls: the Gesture definition (thresholds and Aim assist) and, on the character, the Gesture input
    /// that presses the Gameplay actions and the Aim assist the Primary weapon bends its aim with.
    /// The Touch control scheme's bindings live in the source-controlled input actions asset.
    /// </summary>
    public static class GestureBuilder
    {
        private const string GestureDefinitionPath = "Assets/DarknessRises/Configuration/GestureDefinition.asset";

        // The Hold used to wait 0.3 s before it fired. It's shorter now, so a pressed finger shoots almost at once (#72).
        private const float FormerHoldSeconds = 0.3f;
        private const float HoldSeconds = 0.15f;

        /// <summary>Loads the Gesture definition, creating it with its defaults if it's missing.</summary>
        public static GestureDefinition LoadOrCreateGestures()
        {
            var gestures = TestRoomBuilder.LoadOrCreate<GestureDefinition>(GestureDefinitionPath);

            // Only the former default moves; a time tuned in the Inspector stays.
            var serialized = new SerializedObject(gestures);
            var holdSeconds = serialized.FindProperty("thresholds.holdSeconds");
            if (Mathf.Approximately(holdSeconds.floatValue, FormerHoldSeconds))
            {
                holdSeconds.floatValue = HoldSeconds;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            // Re-save so fields added since the asset was created are written with their defaults.
            EditorUtility.SetDirty(gestures);
            return gestures;
        }

        /// <summary>Adds Gesture input and Aim assist to the character and hands them to the Game Root and the Primary weapon.</summary>
        internal static void Build(GameRoot gameRoot, Transform character)
        {
            var gestureInput = character.gameObject.AddComponent<GestureInput>();
            TestRoomBuilder.SetReference(gestureInput, "controls", character.GetComponent<PlayerControls>());
            var aimAssist = character.gameObject.AddComponent<AimAssistView>();
            TestRoomBuilder.SetReference(gameRoot, "gestureInput", gestureInput);
            TestRoomBuilder.SetReference(gameRoot, "aimAssist", aimAssist);
            TestRoomBuilder.SetReference(character.GetComponent<PrimaryWeaponView>(), "aimAssist", aimAssist);
        }
    }
}
