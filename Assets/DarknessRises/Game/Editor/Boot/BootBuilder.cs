using DarknessRises.Game.Boot;
using DarknessRises.Game.Editor.Hud;
using DarknessRises.Game.Editor.Levels;
using DarknessRises.Game.Editor.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DarknessRises.Game.Editor.Boot
{
    /// <summary>
    /// Builds the boot scene, the only scene in Build Settings: a Bootstrap that loads the Level and the Shop through
    /// Addressables, a camera that keeps the screen black while no Level is showing, and the build version label.
    /// Build the Level, the Shop and the Test Room first.
    /// </summary>
    public static class BootBuilder
    {
        public const string ScenePath = "Assets/DarknessRises/Scenes/Boot.unity";

        // Outside the Configuration folder, which is Addressables content: the boot scene ships in the build itself.
        private const string BootFolder = "Assets/DarknessRises/Boot";
        private const string BootDefinitionPath = BootFolder + "/BootDefinition.asset";

        // Drawn under everything, and only while no Level is loaded.
        private const float LoadingCameraDepth = -100f;

        [MenuItem("Darkness Rises/Rebuild Boot Scene")]
        public static void Build()
        {
            TestRoomBuilder.EnsureFolder(BootFolder);

            // A new single scene unloads unused assets, so load the assets only after creating it.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            TestRoomBuilder.MakeDark();

            var definition = LoadOrCreateDefinition();

            var loadingCamera = new GameObject("Loading Camera", typeof(Camera)).GetComponent<Camera>();
            loadingCamera.clearFlags = CameraClearFlags.SolidColor;
            loadingCamera.backgroundColor = Color.black;
            loadingCamera.depth = LoadingCameraDepth;
            loadingCamera.cullingMask = 0;
            var cameraData = loadingCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderShadows = false;
            cameraData.renderPostProcessing = false;
            loadingCamera.allowMSAA = false;

            var versionLabel = VersionLabelBuilder.Build();

            var bootstrap = new GameObject("Bootstrap", typeof(Bootstrap)).GetComponent<Bootstrap>();
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("definition").objectReferenceValue = definition;
            serialized.FindProperty("loadingCamera").objectReferenceValue = loadingCamera;
            serialized.FindProperty("versionLabel").objectReferenceValue = versionLabel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            BuildScenes.OnlyBoot(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BootBuilder] Built {ScenePath}.");
        }

        private static BootDefinition LoadOrCreateDefinition()
        {
            var definition = TestRoomBuilder.LoadOrCreate<BootDefinition>(BootDefinitionPath);
            var serialized = new SerializedObject(definition);
            SetScene(serialized.FindProperty("levelScene"), LevelBuilder.ScenePath);
            SetScene(serialized.FindProperty("shopScene"), ShopBuilder.ScenePath);
            SetScene(serialized.FindProperty("testRoomScene"), TestRoomBuilder.ScenePath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void SetScene(SerializedProperty reference, string scenePath)
        {
            reference.FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(scenePath);
        }
    }
}
