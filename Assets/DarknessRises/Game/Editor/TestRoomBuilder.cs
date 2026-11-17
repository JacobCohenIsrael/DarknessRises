using DarknessRises.Game.Cameras;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Debugging;
using DarknessRises.Game.Editor.Audio;
using DarknessRises.Game.Editor.Enemies;
using DarknessRises.Game.Editor.Grenades;
using DarknessRises.Game.Editor.Health;
using DarknessRises.Game.Editor.Hud;
using DarknessRises.Game.Editor.Levels;
using DarknessRises.Game.Editor.Player;
using DarknessRises.Game.Editor.Skills;
using DarknessRises.Game.Editor.Waves;
using DarknessRises.Game.Editor.Weapons;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Player;
using DarknessRises.Game.Rooms;
using DarknessRises.Game.Routes;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private const string LightningDefinitionPath = ConfigurationFolder + "/LightningDefinition.asset";

        // Placeholder Rumble from the old Free Pack sounds (approved as placeholders).
        private const string RumbleClipPath = "Assets/Free Pack/Earthquake 3 - Big.wav";

        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";

        // Placeholder Room layout, in metres.
        private const float RoomHalfSize = 10f;
        private const float WallHeight = 3f;
        private const float WallThickness = 0.5f;
        private const float PillarSize = 1f;

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
        // Windows in the east wall, by their centre along the wall (z), in metres.
        private static readonly float[] WindowCentres = { -5.5f, 0f, 5.5f };
        private const float WindowWidth = 2.2f;
        private const float WindowSillHeight = 0.7f;
        private const float WindowTopHeight = 2.5f;

        // Lightning comes in through the east windows, travelling west and a little towards the camera,
        // so Obstacle shadows stretch across the screen.
        private const float LightningYawDegrees = -110f;

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
            var windowSkyMaterial = LoadOrCreateUnlitMaterial("WindowSky", Color.black);

            MakeDark();

            var room = BuildRoom(floorMaterial, wallMaterial, windowSkyMaterial);

            // Baked before anything else is in the scene, so only the Room shapes where Enemies can walk.
            WavesBuilder.BakeNavMesh(room.transform);
            var routeWaves = WavesBuilder.BuildWaves(room.transform);
            var routeWalker = BuildRoute();
            var gameRoot = BuildGameplay(configuration, room, routeWalker, RouteWaypoints[0]);
            SetReference(gameRoot, "routeWaves", routeWaves);

            // Show the authored values in the Scene view too, not only in Play mode.
            gameRoot.Apply();

            EditorSceneManager.SaveScene(scene, ScenePath);
            BuildScenes.Include(ScenePath, first: false);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TestRoomBuilder] Built {ScenePath}.");
        }

        /// <summary>
        /// Everything a playable scene needs besides its Rooms and Route: the character and its Weapon, the cameras,
        /// Lightning, the HUD and the Game Root that wires them. The character and camera start at <paramref name="start"/>.
        /// </summary>
        internal static GameRoot BuildGameplay(GameConfiguration configuration, Room room, RouteWalker routeWalker, Vector3 start)
        {
            var characterMaterial = LoadOrCreateMaterial("Character", new Color(0.55f, 0.55f, 0.6f));
            var character = BuildCharacter(characterMaterial, start, out var aura);
            var cameraView = BuildCameras(start, out var cameraTarget);
            var lightning = BuildLightning();
            var gameRoot = BuildGameRoot(configuration, character, aura, cameraView, cameraTarget, routeWalker);
            var primaryWeapon = PistolBuilder.BuildPrimaryWeapon(character, Camera.main);
            SetReference(gameRoot, "primaryWeapon", primaryWeapon);
            SetReference(gameRoot, "characterAnimation", CharacterBuilder.Animate(character, primaryWeapon));
            SetReference(gameRoot, "ammoHud", HudBuilder.BuildAmmoHud());
            GrenadeBuilder.Build(gameRoot, character);
            SetReference(gameRoot, "room", room);
            SetReference(gameRoot, "lightning", lightning);
            SetReference(gameRoot, "toggleLightning", LoadDebugToggleLightningAction());
            SkillsBuilder.Build(gameRoot, character, character.GetComponent<PlayerControls>(), Object.FindAnyObjectByType<Canvas>());
            HealthBuilder.Build(gameRoot, character, cameraView);
            return gameRoot;
        }

        internal static GameConfiguration LoadOrCreateConfiguration()
        {
            var player = LoadOrCreate<PlayerDefinition>(PlayerDefinitionPath);
            var fixedAngleCamera = LoadOrCreate<CameraDefinition>(CameraDefinitionPath);
            var route = LoadOrCreate<RouteDefinition>(RouteDefinitionPath);
            var lightning = LoadOrCreateLightning();
            var configuration = LoadOrCreate<GameConfiguration>(GameConfigurationPath);

            var serialized = new SerializedObject(configuration);
            serialized.FindProperty("player").objectReferenceValue = player;
            serialized.FindProperty("fixedAngleCamera").objectReferenceValue = fixedAngleCamera;
            serialized.FindProperty("route").objectReferenceValue = route;
            serialized.FindProperty("pistol").objectReferenceValue = PistolBuilder.LoadOrCreatePistol();
            serialized.FindProperty("lightning").objectReferenceValue = lightning;
            serialized.FindProperty("flare").objectReferenceValue = SkillsBuilder.LoadOrCreateFlare();
            serialized.FindProperty("slowMo").objectReferenceValue = SkillsBuilder.LoadOrCreateSlowMo();
            var audio = AudioBuilder.LoadOrCreateAudio();
            serialized.FindProperty("audio").objectReferenceValue = audio;
            serialized.FindProperty("grenade").objectReferenceValue = GrenadeBuilder.LoadOrCreateGrenade();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            WavesBuilder.AuthorPlaceholderWaves(route);
            AudioBuilder.RouteEnemyAudio(EnemyBuilder.LoadOrCreateShambler(), audio);

            // Re-save the definitions so fields added since they were created are written to disk with their defaults.
            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(fixedAngleCamera);
            EditorUtility.SetDirty(route);
            EditorUtility.SetDirty(lightning);
            return configuration;
        }

        private static LightningDefinition LoadOrCreateLightning()
        {
            var lightning = LoadOrCreate<LightningDefinition>(LightningDefinitionPath);
            var serialized = new SerializedObject(lightning);
            var clip = serialized.FindProperty("rumble.clip");
            if (clip.objectReferenceValue == null)
            {
                clip.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(RumbleClipPath);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return lightning;
        }

        /// <summary>
        /// The Lightning debug toggle's action in the game's input actions. Adds it (Debug/ToggleLightning on L)
        /// if it's missing and keeps every other action as it is.
        /// </summary>
        private static InputActionReference LoadDebugToggleLightningAction()
        {
            const string controlsPath = PistolBuilder.ControlsPath;
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(controlsPath);
            if (actions.FindAction(LightningDebugToggle.ActionPath) == null)
            {
                var edited = InputActionAsset.FromJson(actions.ToJson());
                var debugMap = edited.FindActionMap(LightningDebugToggle.MapName) ?? edited.AddActionMap(LightningDebugToggle.MapName);
                debugMap.AddAction(LightningDebugToggle.ActionName, InputActionType.Button, "<Keyboard>/l");
                System.IO.File.WriteAllText(controlsPath, edited.ToJson());
                Object.DestroyImmediate(edited);
                AssetDatabase.ImportAsset(controlsPath);
                actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(controlsPath);
            }

            var action = actions.FindAction(LightningDebugToggle.ActionPath, throwIfNotFound: true);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(controlsPath))
            {
                if (asset is InputActionReference reference && reference.action != null && reference.action.id == action.id)
                {
                    return reference;
                }
            }

            throw new System.InvalidOperationException($"No action reference for {LightningDebugToggle.ActionPath} in {controlsPath}.");
        }

        internal static void MakeDark()
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

        private static Room BuildRoom(Material floorMaterial, Material wallMaterial, Material windowSkyMaterial)
        {
            var room = new GameObject("Room", typeof(Room));

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
            // The south wall faces the camera, so it's cut down low enough to see over (#12).
            AddBlock(
                "South Wall",
                walls,
                new Vector3(0f, RoomPieces.CameraSideWallHeight / 2f, -edge),
                new Vector3(span, RoomPieces.CameraSideWallHeight, WallThickness),
                wallMaterial);
            var windows = BuildEastWallWithWindows(walls, edge, span, wallMaterial, windowSkyMaterial);
            AddBlock("West Wall", walls, new Vector3(-edge, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, span), wallMaterial);

            // Only Obstacles (and the character) throw Lightning shadows. Walls and floor don't, so a flash lights the whole Room.
            foreach (var renderer in walls.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            floor.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

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

            // Each flash recolours the window skies per renderer, so they stay out of static batches.
            foreach (var window in windows)
            {
                GameObjectUtility.SetStaticEditorFlags(window.gameObject, 0);
            }

            var lightningDirection = new GameObject("Lightning Direction").transform;
            lightningDirection.SetParent(room.transform);
            lightningDirection.SetLocalPositionAndRotation(new Vector3(edge, WallHeight, 0f), Quaternion.Euler(0f, LightningYawDegrees, 0f));

            var roomComponent = room.GetComponent<Room>();
            var serialized = new SerializedObject(roomComponent);
            serialized.FindProperty("letsLightningIn").boolValue = true;
            serialized.FindProperty("lightningDirection").objectReferenceValue = lightningDirection;
            var windowsProperty = serialized.FindProperty("windows");
            windowsProperty.arraySize = windows.Length;
            for (var i = 0; i < windows.Length; i++)
            {
                windowsProperty.GetArrayElementAtIndex(i).objectReferenceValue = windows[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return roomComponent;
        }

        /// <summary>
        /// The east wall, broken by windows: full-height piers between them, a sill below and a lintel above each,
        /// and a sky quad just outside that lights up with each flash.
        /// </summary>
        private static Renderer[] BuildEastWallWithWindows(
            Transform walls,
            float edge,
            float span,
            Material wallMaterial,
            Material windowSkyMaterial)
        {
            var eastWall = new GameObject("East Wall").transform;
            eastWall.SetParent(walls);
            var windows = new Renderer[WindowCentres.Length];
            var pierStart = -span / 2f;

            for (var i = 0; i < WindowCentres.Length; i++)
            {
                var windowStart = WindowCentres[i] - WindowWidth / 2f;
                var windowEnd = WindowCentres[i] + WindowWidth / 2f;
                AddWallSpan($"Pier {i + 1}", eastWall, edge, pierStart, windowStart, 0f, WallHeight, wallMaterial);
                AddWallSpan($"Sill {i + 1}", eastWall, edge, windowStart, windowEnd, 0f, WindowSillHeight, wallMaterial);
                AddWallSpan($"Lintel {i + 1}", eastWall, edge, windowStart, windowEnd, WindowTopHeight, WallHeight, wallMaterial);
                windows[i] = AddWindowSky($"Window Sky {i + 1}", eastWall, edge, WindowCentres[i], windowSkyMaterial);
                pierStart = windowEnd;
            }

            AddWallSpan($"Pier {WindowCentres.Length + 1}", eastWall, edge, pierStart, span / 2f, 0f, WallHeight, wallMaterial);
            return windows;
        }

        private static void AddWallSpan(string name, Transform parent, float x, float fromZ, float toZ, float fromY, float toY, Material material)
        {
            AddBlock(
                name,
                parent,
                new Vector3(x, (fromY + toY) / 2f, (fromZ + toZ) / 2f),
                new Vector3(WallThickness, toY - fromY, toZ - fromZ),
                material);
        }

        private static Renderer AddWindowSky(string name, Transform parent, float wallX, float centreZ, Material material)
        {
            var sky = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sky.name = name;
            Object.DestroyImmediate(sky.GetComponent<Collider>());
            sky.transform.SetParent(parent);

            // Just outside the wall, facing into the Room, a little larger than the opening.
            const float overlap = 0.2f;
            const float gapBehindWall = 0.05f;
            sky.transform.SetLocalPositionAndRotation(
                new Vector3(wallX + WallThickness / 2f + gapBehindWall, (WindowSillHeight + WindowTopHeight) / 2f, centreZ),
                Quaternion.Euler(0f, 90f, 0f));
            sky.transform.localScale = new Vector3(WindowWidth + overlap, WindowTopHeight - WindowSillHeight + overlap, 1f);

            var renderer = sky.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static LightningView BuildLightning()
        {
            var lightning = new GameObject("Lightning", typeof(LightningView));

            var keyLight = new GameObject("Key Light (shadows)", typeof(Light)).GetComponent<Light>();
            keyLight.transform.SetParent(lightning.transform);
            keyLight.type = LightType.Directional;
            keyLight.GetUniversalAdditionalLightData();

            var fillLight = new GameObject("Fill Light", typeof(Light)).GetComponent<Light>();
            fillLight.transform.SetParent(lightning.transform);
            fillLight.type = LightType.Directional;
            fillLight.GetUniversalAdditionalLightData();

            var rumble = new GameObject("Rumble", typeof(AudioSource)).GetComponent<AudioSource>();
            rumble.transform.SetParent(lightning.transform);
            rumble.playOnAwake = false;

            // Lightning is the main light: the one light URP renders real-time shadows for.
            RenderSettings.sun = keyLight;

            var view = lightning.GetComponent<LightningView>();
            SetReference(view, "keyLight", keyLight);
            SetReference(view, "fillLight", fillLight);
            SetReference(view, "rumbleSource", rumble);
            return view;
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

        private static Transform BuildCharacter(Material material, Vector3 start, out AuraView aura)
        {
            var character = new GameObject("Character").transform;
            character.position = start;

            CharacterBuilder.BuildPlaceholderModel(character, material);

            var auraObject = new GameObject("Aura", typeof(Light), typeof(AuraView));
            auraObject.transform.SetParent(character);
            aura = auraObject.GetComponent<AuraView>();
            SetReference(aura, "auraLight", auraObject.GetComponent<Light>());

            return character;
        }

        private static FixedAngleCameraView BuildCameras(Vector3 start, out Transform cameraTarget)
        {
            // The camera follows this point a little ahead of the character along the Route.
            cameraTarget = new GameObject("Camera Target").transform;
            cameraTarget.position = start;

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

        internal static void SetReference(Object owner, string propertyName, Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static Material LoadOrCreateMaterial(string name, Color baseColor)
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

        internal static Material LoadOrCreateUnlitMaterial(string name, Color baseColor)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find(UnlitShaderName)) { name = name };
            material.SetColor("_BaseColor", baseColor);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        internal static T LoadOrCreate<T>(string path) where T : ScriptableObject
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

        internal static void EnsureFolder(string path)
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
