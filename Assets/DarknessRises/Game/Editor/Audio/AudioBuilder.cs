using System;
using System.Reflection;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Enemies;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace DarknessRises.Game.Editor.Audio
{
    /// <summary>
    /// Builds the game's audio routing: a mixer with an Enemies group whose pitch is exposed, so it can follow the
    /// Enemy time scale, and the audio definition that names them. Existing assets are kept.
    /// </summary>
    /// <remarks>
    /// Unity has no public API for authoring mixers, so a missing mixer is created through the Editor's internal
    /// mixer controller, by reflection. That only runs when the committed mixer asset is missing.
    /// </remarks>
    public static class AudioBuilder
    {
        private const string Root = "Assets/DarknessRises";
        private const string AudioFolder = Root + "/Audio";
        private const string ConfigurationFolder = Root + "/Configuration";

        public const string MixerPath = AudioFolder + "/GameMixer.mixer";
        private const string AudioDefinitionPath = ConfigurationFolder + "/AudioDefinition.asset";
        private const string EnemiesGroupName = "Enemies";

        private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        /// <summary>Loads the audio definition, creating it and the mixer if they're missing.</summary>
        public static AudioDefinition LoadOrCreateAudio()
        {
            EnsureFolder(AudioFolder);
            var definition = AssetDatabase.LoadAssetAtPath<AudioDefinition>(AudioDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<AudioDefinition>();
                AssetDatabase.CreateAsset(definition, AudioDefinitionPath);
            }

            var serialized = new SerializedObject(definition);
            var mixer = serialized.FindProperty("mixer");
            if (mixer.objectReferenceValue == null)
            {
                mixer.objectReferenceValue = LoadOrCreateMixer(serialized.FindProperty("enemyPitchParameter").stringValue);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(definition);
            return definition;
        }

        /// <summary>Sends the Enemy type's sounds through the mixer's Enemies group, unless it's already routed.</summary>
        public static void RouteEnemyAudio(EnemyDefinition enemyType, AudioDefinition audio)
        {
            var serialized = new SerializedObject(enemyType);
            var outputGroup = serialized.FindProperty("audio.outputGroup");
            if (outputGroup.objectReferenceValue != null)
            {
                return;
            }

            outputGroup.objectReferenceValue = FindEnemiesGroup(audio.Mixer);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemyType);
        }

        private static AudioMixerGroup FindEnemiesGroup(AudioMixer mixer)
        {
            var groups = mixer.FindMatchingGroups(EnemiesGroupName);
            if (groups.Length == 0)
            {
                throw new InvalidOperationException($"{AssetDatabase.GetAssetPath(mixer)} has no {EnemiesGroupName} group.");
            }

            return groups[0];
        }

        private static AudioMixer LoadOrCreateMixer(string enemyPitchParameter)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (existing != null)
            {
                return existing;
            }

            var editorAssembly = typeof(UnityEditor.Editor).Assembly;
            var controllerType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerController", throwOnError: true);
            var groupType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerGroupController", throwOnError: true);
            var pathType = editorAssembly.GetType("UnityEditor.Audio.AudioGroupParameterPath", throwOnError: true);

            var controller = controllerType.GetMethod("CreateMixerControllerAtPath", Members).Invoke(null, new object[] { MixerPath });
            var master = controllerType.GetProperty("masterGroup", Members).GetValue(controller);
            var enemies = controllerType.GetMethod("CreateNewGroup", Members).Invoke(controller, new object[] { EnemiesGroupName, false });
            controllerType.GetMethod("AddChildToParent", Members).Invoke(controller, new[] { enemies, master });

            // One view showing both groups, so the Audio Mixer window has something to open on.
            var viewType = editorAssembly.GetType("UnityEditor.Audio.MixerGroupView", throwOnError: true);
            var groupId = groupType.GetProperty("groupID", Members);
            var view = Activator.CreateInstance(viewType);
            viewType.GetField("name", Members).SetValue(view, "View");
            viewType.GetField("guids", Members).SetValue(view, new[] { (GUID)groupId.GetValue(master), (GUID)groupId.GetValue(enemies) });
            var views = Array.CreateInstance(viewType, 1);
            views.SetValue(view, 0);
            controllerType.GetProperty("views", Members).SetValue(controller, views);

            // Expose the Enemies group's pitch under the definition's parameter name.
            var pitchGuid = groupType.GetMethod("GetGUIDForPitch", Members).Invoke(enemies, null);
            var pitchPath = Activator.CreateInstance(pathType, enemies, pitchGuid);
            controllerType.GetMethod("AddExposedParameter", Members).Invoke(controller, new[] { pitchPath });

            var exposedProperty = controllerType.GetProperty("exposedParameters", Members);
            var exposed = (Array)exposedProperty.GetValue(controller);
            for (var i = 0; i < exposed.Length; i++)
            {
                var parameter = exposed.GetValue(i);
                var guidField = parameter.GetType().GetField("guid", Members);
                if (!guidField.GetValue(parameter).Equals(pitchGuid))
                {
                    continue;
                }

                parameter.GetType().GetField("name", Members).SetValue(parameter, enemyPitchParameter);
                exposed.SetValue(parameter, i);
            }

            exposedProperty.SetValue(controller, exposed);
            controllerType.GetMethod("OnChangedExposedParameter", Members).Invoke(controller, null);

            EditorUtility.SetDirty((Object)controller);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
