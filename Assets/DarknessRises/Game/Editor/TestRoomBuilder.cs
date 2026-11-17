using DarknessRises.Game.Cameras;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Editor.Hud;
using DarknessRises.Game.Editor.Weapons;
using DarknessRises.Game.Player;
using DarknessRises.Game.Routes;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DarknessRises.Game.Editor
{
    /// <summary>
    /// Builds the pitch-black test Room scene and the root configuration it reads.
    /// Existing definition assets are kept, so tuning done in the Inspector survives a rebuild.
    /// </summary>
    public static class TestRoomBuilder
    {
        private const string Root = "Assets/DarknessRises";
        private const string ConfigurationFolder = Root + "/Configuration";
        private const string MaterialsFolder = Root + "/Materials";
        private const string ScenesFolder = Root + "/Scenes";
        private const string LightingFolder = Root + "/Lighting";

        public const string ScenePath = ScenesFolder + "/TestRoom.unity";
        public const string GameConfigurationPath = ConfigurationFolder + "/GameConfiguration.asset";
        private const string PlayerDefinitionPath = ConfigurationFolder + "/PlayerDefinition.asset";
        private const string CameraDefinitionPath = ConfigurationFolder + "/CameraDefinition.asset";
        private const string RouteDefinitionPath = ConfigurationFolder + "/RouteDefinition.asset";
        private const string LightingSettingsPath = LightingFolder + "/Darkness.lighting";

        private const string LitShaderName = "Universal Render Pipeline/Lit";

        // Placeholder Room layout, in metres.
        private const float RoomHalfSize = 10f;
        private const float WallHeight = 3f;
        private const float WallThickness = 0.5f;
        private const float PillarSize = 1f;
        private const float CharacterHeight = 2f;
        private const float CharacterRadius = 0.4f;

        private static readonly Vector3[] PillarPositions =
        {
            new(2.5f, 0f, 1.5f),
            new(-6f, 0f, 4f),
            new(5f, 0f, -5f),
            new(-4f, 0f, -6.5f),
            new(7f, 0f, 7f),
        };

        // The test Route winds between the pillars, stopping at two Stations, and ends in the far corner.
        private static readonly Vector3[] RouteWaypoints =
        {
            new(-8f, 0f, -8f),
            new(-8f, 0f, -1f),
            new(-1f, 0f, -1f),
            new(-1f, 0f, 6f),
            new(4f, 0f, 4f),
            new(8f, 0f, 1f),
            new(8f, 0f, -8f),
        };

        private static readonly int[] StationWaypoints = { 2, 4 };

        [MenuItem("Darkness Rises/Rebuild Test Room")]
        public static void Build()
        {
            EnsureFolder(ConfigurationFolder);
            EnsureFolder(MaterialsFolder);
            EnsureFolder(ScenesFolder);
            EnsureFolder(LightingFolder);
            HudBuilder.RequireTextMeshProEssentials();

            // A new single scene unloads unused assets, so load the assets only after creating it.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var configuration = LoadOrCreateConfiguration();
            var floorMaterial = LoadOrCreateMaterial("Floor", new Color(0.32f, 0.3f, 0.28f));
            var wallMaterial = LoadOrCreateMaterial("Wall", new Color(0.26f, 0.26f, 0.28f));
            var characterMaterial = LoadOrCreateMaterial("Character", new Color(0.55f, 0.55f, 0.6f));

            MakeDark();

            BuildRoom(floorMaterial, wallMaterial);
            var routeWalker = BuildRoute();
            var character = BuildCharacter(characterMaterial, out var aura);
            var cameraView = BuildCameras(out var cameraTarget);
            var gameRoot = BuildGameRoot(configuration, character, aura, cameraView, cameraTarget, routeWalker);
            SetReference(gameRoot, "primaryWeapon", PistolBuilder.BuildPrimaryWeapon(character, Camera.main));
            SetReference(gameRoot, "ammoHud", HudBuilder.BuildAmmoHud());

            // Show the authored values in the Scene view too, not only in Play mode.
            gameRoot.Apply();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[TestRoomBuilder] Built {ScenePath}.");
        }

        private static GameConfiguration LoadOrCreateConfiguration()
        {
            var player = LoadOrCreate<PlayerDefinition>(PlayerDefinitionPath);
            var fixedAngleCamera = LoadOrCreate<CameraDefinition>(CameraDefinitionPath);
            var route = LoadOrCreate<RouteDefinition>(RouteDefinitionPath);
            var configuration = LoadOrCreate<GameConfiguration>(GameConfigurationPath);

            var serialized = new SerializedObject(configuration);
            serialized.FindProperty("player").objectReferenceValue = player;
            serialized.FindProperty("fixedAngleCamera").objectReferenceValue = fixedAngleCamera;
            serialized.FindProperty("route").objectReferenceValue = route;
            serialized.FindProperty("pistol").objectReferenceValue = PistolBuilder.LoadOrCreatePistol();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Re-save the definitions so fields added since they were created are written to disk with their defaults.
            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(fixedAngleCamera);
            EditorUtility.SetDirty(route);
            return configuration;
        }

        private static void MakeDark()
        {
            RenderSettings.skybox = null;
            RenderSettings.sun = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.ambientIntensity = 0f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.reflectionIntensity = 0f;
            RenderSettings.fog = false;

            var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
            if (lighting == null)
            {
                lighting = new LightingSettings { name = "Darkness" };
                AssetDatabase.CreateAsset(lighting, LightingSettingsPath);
            }

            // Everything is real-time: nothing to bake.
            lighting.bakedGI = false;
            lighting.realtimeGI = false;
            EditorUtility.SetDirty(lighting);
            Lightmapping.lightingSettings = lighting;
        }

        private static void BuildRoom(Material floorMaterial, Material wallMaterial)
        {
            var room = new GameObject("Room");

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(room.transform);
            floor.transform.localPosition = new Vector3(0f, -WallThickness / 2f, 0f);
            floor.transform.localScale = new Vector3(RoomHalfSize * 2f, WallThickness, RoomHalfSize * 2f);
            Paint(floor, floorMaterial);

            var walls = new GameObject("Walls").transform;
            walls.SetParent(room.transform);
            var span = RoomHalfSize * 2f + WallThickness * 2f;
            var edge = RoomHalfSize + WallThickness / 2f;
            AddBlock("North Wall", walls, new Vector3(0f, WallHeight / 2f, edge), new Vector3(span, WallHeight, WallThickness), wallMaterial);
            AddBlock("South Wall", walls, new Vector3(0f, WallHeight / 2f, -edge), new Vector3(span, WallHeight, WallThickness), wallMaterial);
            AddBlock("East Wall", walls, new Vector3(edge, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, span), wallMaterial);
            AddBlock("West Wall", walls, new Vector3(-edge, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, span), wallMaterial);

            var obstacles = new GameObject("Obstacles").transform;
            obstacles.SetParent(room.transform);
            for (var i = 0; i < PillarPositions.Length; i++)
            {
                AddBlock(
                    $"Pillar {i + 1}",
                    obstacles,
                    PillarPositions[i] + Vector3.up * (WallHeight / 2f),
                    new Vector3(PillarSize, WallHeight, PillarSize),
                    wallMaterial);
            }

            foreach (var child in room.GetComponentsInChildren<Transform>())
            {
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.BatchingStatic);
            }
        }

        private static RouteWalker BuildRoute()
        {
            var route = new GameObject("Route", typeof(RouteLayout), typeof(RouteWalker));
            var waypoints = new Transform[RouteWaypoints.Length];
            var stationCount = 0;
            for (var i = 0; i < RouteWaypoints.Length; i++)
            {
                var isStation = System.Array.IndexOf(StationWaypoints, i) >= 0;
                var waypoint = new GameObject(isStation ? $"Station {++stationCount}" : $"Waypoint {i + 1}");
                if (isStation)
                {
                    waypoint.AddComponent<StationMarker>();
                }

                waypoint.transform.SetParent(route.transform);
                waypoint.transform.localPosition = RouteWaypoints[i];
                waypoints[i] = waypoint.transform;
            }

            var layout = route.GetComponent<RouteLayout>();
            var serialized = new SerializedObject(layout);
            var list = serialized.FindProperty("waypoints");
            list.arraySize = waypoints.Length;
            for (var i = 0; i < waypoints.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            var walker = route.GetComponent<RouteWalker>();
            SetReference(walker, "route", layout);
            return walker;
        }

        private static Transform BuildCharacter(Material material, out AuraView aura)
        {
            var character = new GameObject("Character").transform;
            character.position = RouteWaypoints[0];

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Placeholder Body";
            body.transform.SetParent(character);
            body.transform.localPosition = Vector3.up * (CharacterHeight / 2f);
            body.transform.localScale = new Vector3(CharacterRadius * 2f, CharacterHeight / 2f, CharacterRadius * 2f);
            Paint(body, material);

            var auraObject = new GameObject("Aura", typeof(Light), typeof(AuraView));
            auraObject.transform.SetParent(character);
            aura = auraObject.GetComponent<AuraView>();
            SetReference(aura, "auraLight", auraObject.GetComponent<Light>());

            return character;
        }

        private static FixedAngleCameraView BuildCameras(out Transform cameraTarget)
        {
            // The camera follows this point a little ahead of the character along the Route.
            cameraTarget = new GameObject("Camera Target").transform;
            cameraTarget.position = RouteWaypoints[0];

            var mainCamera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CinemachineBrain));
            mainCamera.tag = "MainCamera";
            var unityCamera = mainCamera.GetComponent<Camera>();
            unityCamera.clearFlags = CameraClearFlags.SolidColor;
            unityCamera.backgroundColor = Color.black;
            unityCamera.GetUniversalAdditionalCameraData();

            var fixedAngle = new GameObject(
                "Fixed Angle Camera",
                typeof(CinemachineCamera),
                typeof(CinemachineFollow),
                typeof(FixedAngleCameraView));
            var view = fixedAngle.GetComponent<FixedAngleCameraView>();
            SetReference(view, "cinemachineCamera", fixedAngle.GetComponent<CinemachineCamera>());
            SetReference(view, "follow", fixedAngle.GetComponent<CinemachineFollow>());
            return view;
        }

        private static GameRoot BuildGameRoot(
            GameConfiguration configuration,
            Transform character,
            AuraView aura,
            FixedAngleCameraView cameraView,
            Transform cameraTarget,
            RouteWalker routeWalker)
        {
            var gameRoot = new GameObject("Game Root").AddComponent<GameRoot>();
            var serialized = new SerializedObject(gameRoot);
            serialized.FindProperty("configuration").objectReferenceValue = configuration;
            serialized.FindProperty("character").objectReferenceValue = character;
            serialized.FindProperty("aura").objectReferenceValue = aura;
            serialized.FindProperty("fixedAngleCamera").objectReferenceValue = cameraView;
            serialized.FindProperty("cameraTarget").objectReferenceValue = cameraTarget;
            serialized.FindProperty("routeWalker").objectReferenceValue = routeWalker;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return gameRoot;
        }

        private static void AddBlock(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.localPosition = position;
            block.transform.localScale = size;
            Paint(block, material);
        }

        private static void Paint(GameObject gameObject, Material material)
        {
            var renderer = gameObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }

        private static void SetReference(Object owner, string propertyName, Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material LoadOrCreateMaterial(string name, Color baseColor)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find(LitShaderName)) { name = name };
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
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
