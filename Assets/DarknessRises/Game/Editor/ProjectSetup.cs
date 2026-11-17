using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor
{
    /// <summary>
    /// Project-wide settings for the remake, kept as code so they can be re-applied and reviewed.
    /// Run headless with:
    /// <c>unity run . -- -executeMethod DarknessRises.Game.Editor.ProjectSetup.ApplyAndBuildTestRoom -quit</c>
    /// </summary>
    public static class ProjectSetup
    {
        private const string CompanyName = "TheGamefather";
        private const string ProductName = "Darkness Rises";
        private const string ProjectWideInputActionsKey = "com.unity.input.settings.actions";

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

            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] Applied project settings.");
        }

        /// <summary>Batch-mode entry point: applies the project setup, then (re)builds the test Room.</summary>
        public static void ApplyAndBuildTestRoom()
        {
            Apply();
            TestRoomBuilder.Build();
        }
    }
}
