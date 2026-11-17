using System.Collections.Generic;
using UnityEditor;

namespace DarknessRises.Game.Editor
{
    /// <summary>Keeps each built scene in the build settings without dropping the others.</summary>
    public static class BuildScenes
    {
        /// <param name="path">The scene to include.</param>
        /// <param name="first">Whether it should be the scene the game boots into.</param>
        public static void Include(string path, bool first)
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path != path && !string.IsNullOrEmpty(scene.path))
                {
                    scenes.Add(scene);
                }
            }

            var included = new EditorBuildSettingsScene(path, true);
            if (first)
            {
                scenes.Insert(0, included);
            }
            else
            {
                scenes.Add(included);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
