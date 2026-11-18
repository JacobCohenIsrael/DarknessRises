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

        /// <summary>Loads the Gesture definition, creating it with its defaults if it's missing.</summary>
        public static GestureDefinition LoadOrCreateGestures()
        {
            var gestures = TestRoomBuilder.LoadOrCreate<GestureDefinition>(GestureDefinitionPath);

            // Re-save so fields added since the asset was created are written with their defaults.
            EditorUtility.SetDirty(gestures);
            return gestures;
        }

        /// <summary>Adds Gesture input and Aim assist to the character and hands them to the Game Root and the Primary weapon.</summary>
        internal static void Build(GameRoot gameRoot, Transform character)
        {
            var gestureInput = character.gameObject.AddComponent<GestureInput>();
            var aimAssist = character.gameObject.AddComponent<AimAssistView>();
            TestRoomBuilder.SetReference(gameRoot, "gestureInput", gestureInput);
            TestRoomBuilder.SetReference(gameRoot, "aimAssist", aimAssist);
            TestRoomBuilder.SetReference(character.GetComponent<PrimaryWeaponView>(), "aimAssist", aimAssist);
        }
    }
}
