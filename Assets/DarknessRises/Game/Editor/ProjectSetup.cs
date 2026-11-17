using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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

            OnlyTheMainLightCastsShadows();

            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] Applied project settings.");
        }

        /// <summary>
        /// Lightning (the main light) is the only light that casts real-time shadows, so every URP asset
        /// turns off shadows for additional lights. That keeps a stray shadowed point light from costing a shadow pass on mobile.
        /// </summary>
        private static void OnlyTheMainLightCastsShadows()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets" }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                var serialized = new SerializedObject(asset);
                serialized.FindProperty("m_MainLightShadowsSupported").boolValue = true;
                serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>Batch-mode entry point: applies the project setup, then (re)builds the test Room.</summary>
        public static void ApplyAndBuildTestRoom()
        {
            Apply();
            TestRoomBuilder.Build();
        }
    }
}
