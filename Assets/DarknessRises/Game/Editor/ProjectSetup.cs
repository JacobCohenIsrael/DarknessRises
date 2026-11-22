using DarknessRises.Game.Editor.Boot;
using DarknessRises.Game.Editor.Content;
using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor
{
    /// <summary>
    /// Project-wide settings for the remake, kept as code so they can be re-applied and reviewed.
    /// Run headless with:
    /// <c>unity run . -- -executeMethod DarknessRises.Game.Editor.ProjectSetup.ApplyAndBuildAll</c>
    /// </summary>
    public static class ProjectSetup
    {
        private const string CompanyName = "TheGamefather";
        private const string ProductName = "Darkness Rises";
        private const string ProjectWideInputActionsKey = "com.unity.input.settings.actions";
        private const string ConfigurationFolder = "Assets/DarknessRises/Configuration";

        [MenuItem("Darkness Rises/Apply Project Setup")]
        public static void Apply()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;

            // Mobile landscape only.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // The game's own named actions arrive with the first ticket that needs input;
            // the template's sample project-wide actions (with Move/Jump) don't fit a game where the player never steers.
            EditorBuildSettings.RemoveConfigObject(ProjectWideInputActionsKey);

            Lightning.LightningShadowSetup.Apply();
            Splash.SplashScreenSetup.Apply();
            AddressableContent.Setup();

            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] Applied project settings.");
        }

        /// <summary>
        /// Marks the content every scene shares, once the builders have made it, and labels all content with the
        /// destinations that can need it.
        /// </summary>
        private static void MarkSharedContent()
        {
            AddressableContent.MarkShared(ConfigurationFolder, Weapons.PistolBuilder.ControlsPath);
            AddressableContent.LabelDestinations();
        }

        /// <summary>Batch-mode entry point: applies the project setup, then (re)builds the test Room, the Shop and the boot scene.</summary>
        public static void ApplyAndBuildTestRoom()
        {
            Apply();
            TestRoomBuilder.Build();
            Shop.ShopBuilder.Build();
            MarkSharedContent();
            BootBuilder.Build();
        }

        /// <summary>Batch-mode entry point: applies the project setup, then (re)builds the test Room, the Level, the Shop and the boot scene.</summary>
        public static void ApplyAndBuildAll()
        {
            Apply();
            TestRoomBuilder.Build();
            Levels.LevelBuilder.Build();
            Shop.ShopBuilder.Build();
            MarkSharedContent();
            BootBuilder.Build();
        }
    }
}
