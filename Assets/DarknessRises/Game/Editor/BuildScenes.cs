using UnityEditor;

namespace DarknessRises.Game.Editor
{
    /// <summary>Keeps the build's scene list to the boot scene alone: every other scene loads through Addressables.</summary>
    public static class BuildScenes
    {
        /// <param name="bootScenePath">The boot scene, the one scene the game starts in.</param>
        public static void OnlyBoot(string bootScenePath)
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(bootScenePath, true) };
        }
    }
}
