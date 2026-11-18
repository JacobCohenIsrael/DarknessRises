using DarknessRises.Game.Audio;
using DarknessRises.Game.Debugging;
using DarknessRises.Game.Editor.Audio;
using DarknessRises.Game.Editor.Enemies;
using DarknessRises.Game.Editor.Health;
using DarknessRises.Game.Runs;
using UnityEditor;

namespace DarknessRises.Game.Editor.Runs
{
    /// <summary>
    /// Builds the Ramp: its definition asset, with the Runner as the Enemy type that joins the Waves partway in, and the
    /// debug key that jumps to Ramp level N. An existing definition keeps its tuning; only a missing Runner is filled in.
    /// </summary>
    public static class RampBuilder
    {
        private const string RampDefinitionPath = "Assets/DarknessRises/Configuration/RampDefinition.asset";
        private const string JumpToRampLevelBinding = "<Keyboard>/f2";

        /// <summary>Loads the Ramp definition, creating it, the Runner and its prefab if they're missing.</summary>
        public static RampDefinition LoadOrCreateRamp(AudioDefinition audio)
        {
            var runner = EnemyBuilder.LoadOrCreateRunner();
            AudioBuilder.RouteEnemyAudio(runner, audio);

            var ramp = TestRoomBuilder.LoadOrCreate<RampDefinition>(RampDefinitionPath);
            var serialized = new SerializedObject(ramp);
            var runnerProperty = serialized.FindProperty("runner");
            if (runnerProperty.objectReferenceValue == null)
            {
                runnerProperty.objectReferenceValue = runner;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Re-saved so fields added since it was created are written to disk with their defaults.
            EditorUtility.SetDirty(ramp);
            return ramp;
        }

        /// <summary>Gives the Game Root the Debug/JumpToRampLevel action (F2), adding it to the input actions if it's missing.</summary>
        internal static void WireDebugJump(GameRoot gameRoot)
        {
            var action = HealthBuilder.LoadOrAddDebugAction(RampLevelDebugJump.ActionName, JumpToRampLevelBinding);
            TestRoomBuilder.SetReference(gameRoot, "jumpToRampLevel", action);
        }
    }
}
