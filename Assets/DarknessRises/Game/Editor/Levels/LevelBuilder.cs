using System.Collections.Generic;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Editor.Audio;
using DarknessRises.Game.Editor.Bosses;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Editor.Enemies;
using DarknessRises.Game.Editor.Hud;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Levels;
using DarknessRises.Game.Rooms;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Waves;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static DarknessRises.Game.Editor.Levels.RoomPieces;

namespace DarknessRises.Game.Editor.Levels
{
    /// <summary>
    /// Builds the prototype Level scene: three hand-built Rooms in a row, joined by doorways, then the Colossus's huge
    /// Boss Room, with the Route walking in through the entrance door, past each Room's Stations, to the Boss's Station
    /// and out the far door. The game boots into it.
    /// Also authors the Level definition's Waves and Stragglers when it has none, so tuning survives a rebuild.
    /// </summary>
    /// <remarks>
    /// The fixed camera looks north (+z), so the Rooms run west to east, doorways sit in east and west walls,
    /// windows in north walls (in view), and every south wall is cut down low (see <see cref="RoomPieces.CameraSideWallHeight"/>).
    /// </remarks>
    public static class LevelBuilder
    {
        public const string ScenePath = "Assets/DarknessRises/Scenes/Level.unity";

        /// <summary>The Addressables group holding this Level's scene.</summary>
        public const string GroupName = "Level 1";
        private const string NavMeshPath = "Assets/DarknessRises/Scenes/Level/NavMesh.asset";
        private const string LevelDefinitionPath = "Assets/DarknessRises/Configuration/Levels/Level1.asset";

        private const float DoorWidth = 2.4f;
        private const float WindowWidth = 2.2f;
        private const float DoorLeafThickness = 0.08f;

        // Where the character starts, outside the entrance door, and where the beats' lights stand.
        private static readonly Vector3 Start = new(-6f, 0f, 5f);
        private static readonly Vector3 EntranceLightPosition = new(-3.5f, 2.4f, 5f);
        private static readonly Vector3 ExitLightPosition = new(81f, 2.6f, 8f);

        private sealed class RoomPlan
        {
            public string Name;
            public float XMin, ZMin, XMax, ZMax;
            public Color FloorTint;
            public float[] WindowsX = System.Array.Empty<float>();
            public float LightningYaw;
            public float WestDoorZ = float.NaN;
            public float EastDoorZ = float.NaN;
            public (Vector3 centre, Vector3 size, bool crate)[] Obstacles;
            public Vector3[] SpawnPoints = System.Array.Empty<Vector3>();
            public float Height = WallHeight;
            public float WindowSill = WindowSillHeight;
            public float WindowTop = WindowTopHeight;

            public bool HasWindows => WindowsX.Length > 0;
        }

        // Rooms run west to east. Each Room's west wall stands back to back with the previous Room's east wall.
        private static readonly RoomPlan[] Rooms =
        {
            new()
            {
                Name = "Hall",
                XMin = 0f, ZMin = 0f, XMax = 18f, ZMax = 14f,
                FloorTint = new Color(0.62f, 0.56f, 0.5f),
                WindowsX = new[] { 3.5f, 9f, 14.5f },
                LightningYaw = 165f,
                WestDoorZ = 5f,
                EastDoorZ = 10f,
                Obstacles = new[]
                {
                    (new Vector3(4f, 0.6f, 10.5f), new Vector3(1.6f, 1.2f, 1.4f), true),
                    (new Vector3(5.6f, 0.45f, 11.2f), new Vector3(0.9f, 0.9f, 0.9f), true),
                    (new Vector3(9.5f, WallHeight / 2f, 11.5f), new Vector3(1f, WallHeight, 1f), false),
                    (new Vector3(15f, 0.5f, 12.7f), new Vector3(2.2f, 1f, 1.2f), true),
                    (new Vector3(9.5f, 0.45f, 1.6f), new Vector3(1.2f, 0.9f, 1.2f), true),
                    (new Vector3(16.2f, WallHeight / 2f, 3.5f), new Vector3(1f, WallHeight, 1f), false),
                    (new Vector3(3f, 0.4f, 1.6f), new Vector3(1.4f, 0.8f, 1f), true),
                    (new Vector3(10.6f, 0.5f, 8f), new Vector3(1f, 1f, 1f), true),
                },
                SpawnPoints = new[]
                {
                    new Vector3(1f, 0f, 13f),
                    new Vector3(12.5f, 0f, 13.2f),
                    new Vector3(17.2f, 0f, 1f),
                    new Vector3(6.5f, 0f, 0.8f),
                    new Vector3(17.2f, 0f, 13.2f),
                },
            },
            new()
            {
                Name = "Vault",
                XMin = 18.8f, ZMin = 2f, XMax = 32.8f, ZMax = 18f,
                FloorTint = new Color(0.42f, 0.42f, 0.44f),
                WestDoorZ = 10f,
                EastDoorZ = 6f,
                Obstacles = new[]
                {
                    (new Vector3(21.5f, WallHeight / 2f, 5f), new Vector3(1f, WallHeight, 1f), false),
                    (new Vector3(21.5f, WallHeight / 2f, 15.8f), new Vector3(1f, WallHeight, 1f), false),
                    (new Vector3(26f, WallHeight / 2f, 9.5f), new Vector3(1.2f, WallHeight, 1.2f), false),
                    (new Vector3(26.5f, WallHeight / 2f, 16f), new Vector3(1f, WallHeight, 1f), false),
                    (new Vector3(31.2f, WallHeight / 2f, 10.5f), new Vector3(1f, WallHeight, 1f), false),
                    (new Vector3(31f, WallHeight / 2f, 16f), new Vector3(1f, WallHeight, 1f), false),
                    (new Vector3(25f, 0.5f, 4.2f), new Vector3(1.8f, 1f, 1.2f), true),
                    (new Vector3(29.8f, 0.45f, 3.4f), new Vector3(1.2f, 0.9f, 1.2f), true),
                },
                SpawnPoints = new[]
                {
                    new Vector3(19.6f, 0f, 17.2f),
                    new Vector3(32f, 0f, 17.2f),
                    new Vector3(19.6f, 0f, 2.8f),
                    new Vector3(24f, 0f, 17.4f),
                    new Vector3(32f, 0f, 13.5f),
                },
            },
            new()
            {
                Name = "Ward",
                XMin = 33.6f, ZMin = -2f, XMax = 53.6f, ZMax = 14f,
                FloorTint = new Color(0.66f, 0.68f, 0.7f),
                WindowsX = new[] { 37f, 43.6f, 50f },
                LightningYaw = 195f,
                WestDoorZ = 6f,
                EastDoorZ = 8f,
                Obstacles = new[]
                {
                    (new Vector3(38f, WallHeight / 2f, 10f), new Vector3(1f, WallHeight, 1f), false),
                    (new Vector3(44f, WallHeight / 2f, 9f), new Vector3(1f, WallHeight, 1f), false),
                    (new Vector3(50.5f, WallHeight / 2f, 12.5f), new Vector3(1f, WallHeight, 1f), false),
                    (new Vector3(52.4f, WallHeight / 2f, 0.6f), new Vector3(1f, WallHeight, 1f), false),
                    (new Vector3(43f, 0.5f, -0.4f), new Vector3(2f, 1f, 1.2f), true),
                    (new Vector3(36.4f, 0.45f, 0.6f), new Vector3(1.2f, 0.9f, 1.2f), true),
                    (new Vector3(41f, 0.5f, 7.2f), new Vector3(1.4f, 1f, 1f), true),
                    (new Vector3(47.5f, 0.6f, 12.4f), new Vector3(1.6f, 1.2f, 1.2f), true),
                },
                SpawnPoints = new[]
                {
                    new Vector3(34.6f, 0f, 13f),
                    new Vector3(41.5f, 0f, 13.2f),
                    new Vector3(52.8f, 0f, 13.2f),
                    new Vector3(52.8f, 0f, 3f),
                    new Vector3(40f, 0f, -1.2f),
                    new Vector3(47f, 0f, -1.2f),
                },
            },
        };

        // The Colossus's Boss Room, east of the last Room: huge, dark and tall, with tall windows in its north wall.
        // Only a few pillars stand at its edges, out of the way of the Positions in its middle.
        private const float BossRoomHeight = 7f;

        private static readonly RoomPlan BossRoom = new()
        {
            Name = "Boss Room",
            XMin = 54.4f, ZMin = -4f, XMax = 82.4f, ZMax = 20f,
            FloorTint = new Color(0.4f, 0.38f, 0.36f),
            WindowsX = new[] { 58.4f, 63.4f, 68.4f, 73.4f, 78.4f },
            LightningYaw = 172f,
            WestDoorZ = 8f,
            EastDoorZ = 8f,
            Height = BossRoomHeight,
            WindowSill = 0.8f,
            WindowTop = 6f,
            Obstacles = new[]
            {
                (new Vector3(56.6f, BossRoomHeight / 2f, 17.6f), new Vector3(1.2f, BossRoomHeight, 1.2f), false),
                (new Vector3(80.2f, BossRoomHeight / 2f, 17.6f), new Vector3(1.2f, BossRoomHeight, 1.2f), false),
                (new Vector3(56.6f, 0.5f, 0.6f), new Vector3(1.6f, 1f, 1.2f), true),
                (new Vector3(80.2f, 0.45f, 0.4f), new Vector3(1.2f, 0.9f, 1.2f), true),
            },
        };

        /// <summary>The middle of the Boss Room's floor. The Colossus's Positions are measured from here.</summary>
        private static Vector3 BossRoomCentre => new((BossRoom.XMin + BossRoom.XMax) / 2f, 0f, (BossRoom.ZMin + BossRoom.ZMax) / 2f);

        // Every Room the scene builds, the Boss Room last.
        private static RoomPlan[] AllRooms
        {
            get
            {
                var all = new RoomPlan[Rooms.Length + 1];
                Rooms.CopyTo(all, 0);
                all[Rooms.Length] = BossRoom;
                return all;
            }
        }

        // The Route, west to east. Stations stop the character. The second-last Station is the Boss's, on the Boss Room's
        // first Position; the last is the end beat's calm by the far door. The last doorway leads into the Boss Room.
        private static readonly (Vector3 point, bool station, bool doorway)[] RoutePoints =
        {
            (Start, false, false),
            (new Vector3(2f, 0f, 5f), false, false),
            (new Vector3(7f, 0f, 6.5f), true, false),
            (new Vector3(12f, 0f, 4f), false, false),
            (new Vector3(13.5f, 0f, 9f), true, false),
            (new Vector3(16.5f, 0f, 10f), false, false),
            (new Vector3(18.4f, 0f, 10f), false, true),
            (new Vector3(21f, 0f, 10f), false, false),
            (new Vector3(24f, 0f, 13f), true, false),
            (new Vector3(28.8f, 0f, 13f), false, false),
            (new Vector3(28.8f, 0f, 7.5f), true, false),
            (new Vector3(31f, 0f, 6f), false, false),
            (new Vector3(33.2f, 0f, 6f), false, true),
            (new Vector3(36f, 0f, 6f), false, false),
            (new Vector3(40f, 0f, 3f), true, false),
            (new Vector3(46f, 0f, 3f), false, false),
            (new Vector3(48f, 0f, 8.5f), true, false),
            (new Vector3(51.5f, 0f, 8f), false, false),
            (new Vector3(54f, 0f, 8f), false, true),
            (new Vector3(57f, 0f, 8f), false, false),
            (new Vector3(62f, 0f, 3.5f), false, false),
            (new Vector3(68.4f, 0f, 3.5f), true, false),
            (new Vector3(76f, 0f, 3.5f), false, false),
            (new Vector3(80.6f, 0f, 8f), true, false),
            (new Vector3(83.2f, 0f, 8f), false, false),
            (new Vector3(90f, 0f, 8f), false, false),
        };

        // Placeholder fights per Room: (delay seconds, spawn interval seconds, Shamblers) per Wave, per Station.
        private static readonly (float delay, float interval, int count)[][][] PlaceholderStationWaves =
        {
            new[]
            {
                new[] { (1f, 1.5f, 3) },
                new[] { (0.8f, 1.4f, 3), (2f, 1.2f, 4) },
            },
            new[]
            {
                new[] { (0.8f, 1.2f, 4), (1.5f, 1f, 5) },
                new[] { (0.8f, 1f, 5), (1.5f, 0.9f, 6) },
            },
            new[]
            {
                new[] { (0.8f, 1f, 5), (1.5f, 0.8f, 7) },
                new[] { (0.6f, 0.9f, 6), (1.2f, 0.8f, 8) },
            },
        };

        // Placeholder Stragglers per Room: (shortest and longest interval in seconds, most alive at once).
        private static readonly (float min, float max, int maxAlive)[] PlaceholderStragglers =
        {
            (4f, 7f, 1),
            (4f, 6f, 2),
            (3.5f, 6f, 2),
        };

        // Title card and screen fade.
        private const float TitleFontSize = 170f;
        private const float TitleCharacterSpacing = 16f;
        private static readonly Color TitleColor = new(0.96f, 0.9f, 0.7f, 1f);
        private static readonly Vector2 TitleOffset = new(0f, 60f);
        private static readonly Vector2 TitleSize = new(1800f, 240f);

        // The line under the title: the setup line of a Run's first Level.
        private const float SubtitleFontSize = 50f;
        private const float SubtitleCharacterSpacing = 6f;
        private static readonly Color SubtitleColor = new(0.86f, 0.84f, 0.8f, 1f);
        private static readonly Vector2 SubtitleOffset = new(0f, -90f);
        private static readonly Vector2 SubtitleSize = new(1800f, 90f);

        // The title card's wording before it went arcade (#72). Only these former defaults are replaced; a title
        // written in the Inspector stays.
        private const string FormerTitleFormat = "Level {0}";
        private const string FormerClearedTitleFormat = "Level {0} cleared";
        private const string TitleFormat = "STAGE {0}";
        private const string ClearedTitleFormat = "STAGE {0} CLEAR";

        [MenuItem("Darkness Rises/Rebuild Level")]
        public static void Build()
        {
            HudBuilder.RequireTextMeshProEssentials();

            // A new single scene unloads unused assets, so load the assets only after creating it.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var configuration = TestRoomBuilder.LoadOrCreateConfiguration();
            var definition = LoadOrCreateLevelDefinition();
            var serializedConfiguration = new SerializedObject(configuration);
            serializedConfiguration.FindProperty("level").objectReferenceValue = definition;
            serializedConfiguration.ApplyModifiedPropertiesWithoutUndo();

            TestRoomBuilder.MakeDark();

            var level = new GameObject("Level", typeof(LevelView));
            var geometry = new GameObject("Rooms").transform;
            geometry.SetParent(level.transform, false);
            var builtRooms = BuildRooms(geometry);
            var rooms = new Room[Rooms.Length];
            System.Array.Copy(builtRooms, rooms, rooms.Length);
            var bossRoom = builtRooms[Rooms.Length];
            BuildOutside(geometry);

            // Baked before anything else is in the scene, so only the Rooms shape where Enemies can walk.
            BakeNavMesh(geometry);

            var routeWalker = BuildRoute(level.transform, out var doorways);
            var gameRoot = TestRoomBuilder.BuildGameplay(configuration, rooms[0], routeWalker, Start);
            var levelView = level.GetComponent<LevelView>();

            // The last doorway leads into the Boss Room; the ones before it into the Level's Rooms.
            var roomDoorways = new Transform[doorways.Length - 1];
            System.Array.Copy(doorways, roomDoorways, roomDoorways.Length);
            WireLevel(levelView, routeWalker.GetComponent<RouteLayout>(), rooms, roomDoorways);
            var colossus = ColossusBuilder.BuildRig(bossRoom.transform, BossRoomCentre, definition.Boss);
            TestRoomBuilder.SetReference(levelView, "bossRoom", bossRoom);
            TestRoomBuilder.SetReference(levelView, "bossDoorway", doorways[doorways.Length - 1]);
            TestRoomBuilder.SetReference(levelView, "boss", colossus);
            TestRoomBuilder.SetReference(gameRoot, "level", levelView);

            // Show the authored values in the Scene view too, not only in Play mode.
            gameRoot.Apply();

            AudioRouting.RequireRouted(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            // Loaded additively through Addressables by the boot scene; only the boot scene is in Build Settings.
            AddressableContent.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), AddressableContent.LevelGroup(GroupName));
            AssetDatabase.SaveAssets();
            Debug.Log($"[LevelBuilder] Built {ScenePath}.");
        }

        private static Room[] BuildRooms(Transform parent)
        {
            var wallMaterial = LevelMaterials.Concrete("Concrete Wall", new Color(0.5f, 0.5f, 0.52f));
            var pillarMaterial = LevelMaterials.Concrete("Concrete Pillar", new Color(0.46f, 0.45f, 0.44f));
            var crateMaterial = LevelMaterials.Plain("Crate", new Color(0.45f, 0.36f, 0.26f));
            var thresholdMaterial = LevelMaterials.Concrete("Concrete Threshold", new Color(0.38f, 0.38f, 0.38f));
            var skyMaterial = TestRoomBuilder.LoadOrCreateUnlitMaterial("WindowSky", Color.black);

            var plans = AllRooms;
            var rooms = new Room[plans.Length];
            for (var r = 0; r < plans.Length; r++)
            {
                var plan = plans[r];
                var floorMaterial = LevelMaterials.TiledFloor(
                    $"Floor {plan.Name}",
                    plan.FloorTint,
                    plan.XMax - plan.XMin,
                    plan.ZMax - plan.ZMin,
                    tileMetres: 1f);
                rooms[r] = BuildRoom(plan, parent, floorMaterial, wallMaterial, pillarMaterial, crateMaterial, skyMaterial);

                // The doorway's floor between this Room's east wall and the next Room's west wall.
                if (r + 1 < plans.Length)
                {
                    Floor(
                        $"Threshold {plan.Name} to {plans[r + 1].Name}",
                        parent,
                        plan.XMax,
                        plan.EastDoorZ - DoorWidth / 2f,
                        plans[r + 1].XMin,
                        plan.EastDoorZ + DoorWidth / 2f,
                        thresholdMaterial);
                }
            }

            return rooms;
        }

        private static Room BuildRoom(
            RoomPlan plan,
            Transform parent,
            Material floorMaterial,
            Material wallMaterial,
            Material pillarMaterial,
            Material crateMaterial,
            Material skyMaterial)
        {
            var roomObject = new GameObject($"Room {plan.Name}", typeof(Room));
            roomObject.transform.SetParent(parent, false);
            var room = roomObject.transform;

            Floor("Floor", room, plan.XMin, plan.ZMin, plan.XMax, plan.ZMax, floorMaterial);

            var walls = new GameObject("Walls").transform;
            walls.SetParent(room, false);
            var windows = new List<Renderer>();
            var xFrom = plan.XMin - WallThickness;
            var xTo = plan.XMax + WallThickness;
            var noOpenings = System.Array.Empty<Opening>();

            var northOpenings = new Opening[plan.WindowsX.Length];
            for (var i = 0; i < northOpenings.Length; i++)
            {
                northOpenings[i] = new Opening(plan.WindowsX[i], WindowWidth, plan.WindowSill, plan.WindowTop);
            }

            WallAlongX("North Wall", walls, plan.ZMax + WallThickness / 2f, xFrom, xTo, plan.Height, 1f, northOpenings, wallMaterial, skyMaterial, windows);
            WallAlongX("South Wall (cut away)", walls, plan.ZMin - WallThickness / 2f, xFrom, xTo, CameraSideWallHeight, -1f, noOpenings, wallMaterial, skyMaterial, windows);
            WallAlongZ("West Wall", walls, plan.XMin - WallThickness / 2f, plan.ZMin, plan.ZMax, plan.Height, -1f, Doorway(plan.WestDoorZ), wallMaterial, skyMaterial, windows);
            WallAlongZ("East Wall", walls, plan.XMax + WallThickness / 2f, plan.ZMin, plan.ZMax, plan.Height, 1f, Doorway(plan.EastDoorZ), wallMaterial, skyMaterial, windows);

            var obstacles = new GameObject("Obstacles").transform;
            obstacles.SetParent(room, false);
            for (var i = 0; i < plan.Obstacles.Length; i++)
            {
                var (centre, size, crate) = plan.Obstacles[i];
                Block(crate ? $"Crate {i + 1}" : $"Pillar {i + 1}", obstacles, centre, size, crate ? crateMaterial : pillarMaterial, castsShadows: true);
            }

            var spawnPoints = BuildSpawnPoints(plan, room);

            Transform lightningDirection = null;
            if (plan.HasWindows)
            {
                // Lightning comes in through the north windows, heading south towards the camera, so Obstacle shadows
                // stretch down the screen.
                lightningDirection = new GameObject("Lightning Direction").transform;
                lightningDirection.SetParent(room, false);
                lightningDirection.SetLocalPositionAndRotation(
                    new Vector3((plan.XMin + plan.XMax) / 2f, plan.Height, plan.ZMax),
                    Quaternion.Euler(0f, plan.LightningYaw, 0f));
            }

            var component = roomObject.GetComponent<Room>();
            var serialized = new SerializedObject(component);
            serialized.FindProperty("letsLightningIn").boolValue = plan.HasWindows;
            serialized.FindProperty("lightningDirection").objectReferenceValue = lightningDirection;
            serialized.FindProperty("spawnPoints").objectReferenceValue = spawnPoints;
            var windowsProperty = serialized.FindProperty("windows");
            windowsProperty.arraySize = windows.Count;
            for (var i = 0; i < windows.Count; i++)
            {
                windowsProperty.GetArrayElementAtIndex(i).objectReferenceValue = windows[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return component;
        }

        private static Opening[] Doorway(float centreZ)
        {
            return float.IsNaN(centreZ) ? System.Array.Empty<Opening>() : new[] { new Opening(centreZ, DoorWidth, isWindow: false) };
        }

        private static RoomSpawnPoints BuildSpawnPoints(RoomPlan plan, Transform room)
        {
            var spawnPointsObject = new GameObject("Spawn Points", typeof(RoomSpawnPoints));
            spawnPointsObject.transform.SetParent(room, false);
            var centre = new Vector3((plan.XMin + plan.XMax) / 2f, 0f, (plan.ZMin + plan.ZMax) / 2f);

            var points = new Transform[plan.SpawnPoints.Length];
            for (var i = 0; i < points.Length; i++)
            {
                var point = new GameObject($"Spawn Point {i + 1}").transform;
                point.SetParent(spawnPointsObject.transform, false);
                point.localPosition = plan.SpawnPoints[i];
                var inward = centre - plan.SpawnPoints[i];
                inward.y = 0f;
                point.localRotation = Quaternion.LookRotation(inward, Vector3.up);
                points[i] = point;
            }

            var spawnPoints = spawnPointsObject.GetComponent<RoomSpawnPoints>();
            var serialized = new SerializedObject(spawnPoints);
            var list = serialized.FindProperty("points");
            list.arraySize = points.Length;
            for (var i = 0; i < points.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return spawnPoints;
        }

        // The ground outside the entrance and the far door, and the doors themselves standing open.
        private static void BuildOutside(Transform parent)
        {
            var ground = LevelMaterials.Concrete("Concrete Ground", new Color(0.34f, 0.33f, 0.31f));
            var door = LevelMaterials.Plain("Door", new Color(0.32f, 0.24f, 0.18f));
            var first = Rooms[0];
            var last = BossRoom;

            Floor("Ground Outside Entrance", parent, first.XMin - 9f, first.WestDoorZ - 3f, first.XMin - WallThickness, first.WestDoorZ + 3f, ground);
            Floor("Ground Outside Exit", parent, last.XMax + WallThickness, last.EastDoorZ - 3f, last.XMax + 9f, last.EastDoorZ + 3f, ground);
            Floor("Entrance Threshold", parent, first.XMin - WallThickness, first.WestDoorZ - DoorWidth / 2f, first.XMin, first.WestDoorZ + DoorWidth / 2f, ground);
            Floor("Exit Threshold", parent, last.XMax, last.EastDoorZ - DoorWidth / 2f, last.XMax + WallThickness, last.EastDoorZ + DoorWidth / 2f, ground);

            // Each door is swung open outwards against the wall, on its north hinge.
            var leafHeight = DoorHeight - 0.05f;
            var leafWidth = DoorWidth - 0.1f;
            Block(
                "Entrance Door",
                parent,
                new Vector3(first.XMin - WallThickness - leafWidth / 2f, leafHeight / 2f, first.WestDoorZ + DoorWidth / 2f + DoorLeafThickness),
                new Vector3(leafWidth, leafHeight, DoorLeafThickness),
                door,
                castsShadows: true);
            Block(
                "Exit Door",
                parent,
                new Vector3(last.XMax + WallThickness + leafWidth / 2f, leafHeight / 2f, last.EastDoorZ + DoorWidth / 2f + DoorLeafThickness),
                new Vector3(leafWidth, leafHeight, DoorLeafThickness),
                door,
                castsShadows: true);
        }

        private static void BakeNavMesh(Transform rooms)
        {
            var surface = rooms.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();

            TestRoomBuilder.EnsureFolder(System.IO.Path.GetDirectoryName(NavMeshPath).Replace('\\', '/'));
            AssetDatabase.DeleteAsset(NavMeshPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
        }

        private static RouteWalker BuildRoute(Transform parent, out Transform[] doorways)
        {
            var route = new GameObject("Route", typeof(RouteLayout), typeof(RouteWalker));
            route.transform.SetParent(parent, false);
            var waypoints = new Transform[RoutePoints.Length];
            var doorwayList = new List<Transform>();
            var stationCount = 0;
            for (var i = 0; i < RoutePoints.Length; i++)
            {
                var (point, isStation, isDoorway) = RoutePoints[i];
                var isExit = isStation && i == LastStationIndex();
                var name = isExit ? "Station (end beat)"
                    : isStation ? $"Station {++stationCount}"
                    : isDoorway ? $"Doorway {doorwayList.Count + 1}"
                    : $"Waypoint {i + 1}";
                var waypoint = new GameObject(name);
                if (isStation)
                {
                    waypoint.AddComponent<StationMarker>();
                }

                waypoint.transform.SetParent(route.transform, false);
                waypoint.transform.localPosition = point;
                waypoints[i] = waypoint.transform;
                if (isDoorway)
                {
                    doorwayList.Add(waypoint.transform);
                }

                WarnIfBlocked(point, isStation);
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
            TestRoomBuilder.SetReference(walker, "route", layout);
            doorways = doorwayList.ToArray();
            return walker;
        }

        private static int LastStationIndex()
        {
            for (var i = RoutePoints.Length - 1; i >= 0; i--)
            {
                if (RoutePoints[i].station)
                {
                    return i;
                }
            }

            return -1;
        }

        // A check for whoever edits the layout: the character walks through Obstacles, so keep them off the Route.
        private static void WarnIfBlocked(Vector3 point, bool isStation)
        {
            const float clearance = 0.9f;
            foreach (var room in AllRooms)
            {
                foreach (var (centre, size, _) in room.Obstacles)
                {
                    var dx = Mathf.Max(Mathf.Abs(point.x - centre.x) - size.x / 2f, 0f);
                    var dz = Mathf.Max(Mathf.Abs(point.z - centre.z) - size.z / 2f, 0f);
                    if (dx * dx + dz * dz < clearance * clearance)
                    {
                        Debug.LogWarning($"[LevelBuilder] {(isStation ? "Station" : "Waypoint")} at {point} is inside or next to an Obstacle in {room.Name}.");
                    }
                }
            }
        }

        private static void WireLevel(LevelView view, RouteLayout route, Room[] rooms, Transform[] doorways)
        {
            var entranceLight = AddBeatLight("Entrance Light", view.transform, EntranceLightPosition);
            var exitLight = AddBeatLight("Exit Light", view.transform, ExitLightPosition);
            var hud = Object.FindAnyObjectByType<AmmoHudView>().transform.parent;
            var screenFade = BuildScreenFade(hud);
            var titleCard = BuildTitleCard(hud);

            var serialized = new SerializedObject(view);
            serialized.FindProperty("route").objectReferenceValue = route;
            var roomsProperty = serialized.FindProperty("rooms");
            roomsProperty.arraySize = rooms.Length;
            for (var i = 0; i < rooms.Length; i++)
            {
                roomsProperty.GetArrayElementAtIndex(i).objectReferenceValue = rooms[i];
            }

            var doorwaysProperty = serialized.FindProperty("doorways");
            doorwaysProperty.arraySize = doorways.Length;
            for (var i = 0; i < doorways.Length; i++)
            {
                doorwaysProperty.GetArrayElementAtIndex(i).objectReferenceValue = doorways[i];
            }

            serialized.FindProperty("entranceLight").objectReferenceValue = entranceLight;
            serialized.FindProperty("exitLight").objectReferenceValue = exitLight;
            serialized.FindProperty("titleCard").objectReferenceValue = titleCard;
            serialized.FindProperty("screenFade").objectReferenceValue = screenFade;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // A shadowless point light for a beat, off until the beat switches it on. Only Lightning casts shadows.
        private static Light AddBeatLight(string name, Transform parent, Vector3 position)
        {
            var light = new GameObject(name, typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = position;
            light.type = LightType.Point;
            light.shadows = LightShadows.None;
            light.intensity = 0f;
            light.enabled = false;
            light.GetUniversalAdditionalLightData();
            return light;
        }

        private static ScreenFadeView BuildScreenFade(Transform hud)
        {
            var fade = new GameObject("Screen Fade", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(ScreenFadeView));
            var rect = fade.GetComponent<RectTransform>();
            rect.SetParent(hud, false);
            Stretch(rect);
            var cover = fade.GetComponent<Image>();
            cover.color = Color.black;
            cover.raycastTarget = false;
            cover.enabled = false;
            var group = fade.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var view = fade.GetComponent<ScreenFadeView>();
            TestRoomBuilder.SetReference(view, "group", group);
            TestRoomBuilder.SetReference(view, "cover", cover);
            return view;
        }

        private static TitleCardView BuildTitleCard(Transform hud)
        {
            var card = new GameObject("Title Card", typeof(RectTransform), typeof(CanvasGroup), typeof(TitleCardView));
            var rect = card.GetComponent<RectTransform>();
            rect.SetParent(hud, false);
            Stretch(rect);
            var group = card.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var label = AddCardText(rect, "Title", "STAGE 1", TitleFontSize, TitleCharacterSpacing, TitleColor, TitleOffset, TitleSize);
            label.fontStyle = FontStyles.Bold;
            var subtitle = AddCardText(
                rect, "Subtitle", string.Empty, SubtitleFontSize, SubtitleCharacterSpacing, SubtitleColor, SubtitleOffset, SubtitleSize);

            var view = card.GetComponent<TitleCardView>();
            TestRoomBuilder.SetReference(view, "group", group);
            TestRoomBuilder.SetReference(view, "label", label);
            TestRoomBuilder.SetReference(view, "subtitle", subtitle);
            return view;
        }

        // A centred line of the title card. Never a raycast target: taps go through the card and fire.
        private static TextMeshProUGUI AddCardText(
            RectTransform card, string name, string text, float fontSize, float characterSpacing, Color color, Vector2 offset, Vector2 size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(card, false);
            textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = offset;
            textRect.sizeDelta = size;
            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.characterSpacing = characterSpacing;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            label.enabled = false;
            return label;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static LevelDefinition LoadOrCreateLevelDefinition()
        {
            TestRoomBuilder.EnsureFolder(System.IO.Path.GetDirectoryName(LevelDefinitionPath).Replace('\\', '/'));
            var definition = TestRoomBuilder.LoadOrCreate<LevelDefinition>(LevelDefinitionPath);
            AuthorPlaceholderFights(definition);

            // The Colossus waits in the Boss Room, unless the Level already names a Boss.
            var colossus = ColossusBuilder.LoadOrCreateColossus(AddressableContent.LevelGroup(GroupName));
            var serialized = new SerializedObject(definition);
            var boss = serialized.FindProperty("boss");
            if (boss.objectReferenceValue == null)
            {
                boss.objectReferenceValue = colossus;
            }

            ReplaceIfFormer(serialized.FindProperty("titleFormat"), FormerTitleFormat, TitleFormat);
            ReplaceIfFormer(serialized.FindProperty("clearedTitleFormat"), FormerClearedTitleFormat, ClearedTitleFormat);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Re-saved so fields added since it was created are written to disk with their defaults.
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void ReplaceIfFormer(SerializedProperty text, string former, string current)
        {
            if (text.stringValue == former)
            {
                text.stringValue = current;
            }
        }

        /// <summary>Authors placeholder Shambler Waves and Stragglers for each Room, unless the Level already has Rooms.</summary>
        private static void AuthorPlaceholderFights(LevelDefinition definition)
        {
            // Loaded even when the fights are authored, so a missing Shambler prefab is rebuilt.
            var shambler = EnemyBuilder.LoadOrCreateShambler();
            var serialized = new SerializedObject(definition);
            var rooms = serialized.FindProperty("rooms");
            if (rooms.arraySize > 0)
            {
                return;
            }

            rooms.arraySize = Rooms.Length;
            for (var r = 0; r < Rooms.Length; r++)
            {
                var room = rooms.GetArrayElementAtIndex(r);
                room.FindPropertyRelative("name").stringValue = Rooms[r].Name;

                var stations = room.FindPropertyRelative("stations");
                var roomWaves = PlaceholderStationWaves[r];
                stations.arraySize = roomWaves.Length;
                for (var s = 0; s < roomWaves.Length; s++)
                {
                    var waves = stations.GetArrayElementAtIndex(s).FindPropertyRelative("waves");
                    waves.arraySize = roomWaves[s].Length;
                    for (var w = 0; w < roomWaves[s].Length; w++)
                    {
                        var (delay, interval, count) = roomWaves[s][w];
                        var wave = waves.GetArrayElementAtIndex(w);
                        wave.FindPropertyRelative("delaySeconds").floatValue = delay;
                        wave.FindPropertyRelative("spawnIntervalSeconds").floatValue = interval;
                        var groups = wave.FindPropertyRelative("groups");
                        groups.arraySize = 1;
                        var group = groups.GetArrayElementAtIndex(0);
                        group.FindPropertyRelative("enemyType").objectReferenceValue = shambler;
                        group.FindPropertyRelative("count").intValue = count;
                    }
                }

                var (min, max, maxAlive) = PlaceholderStragglers[r];
                var stragglers = room.FindPropertyRelative("stragglers");
                stragglers.FindPropertyRelative("enemyType").objectReferenceValue = shambler;
                stragglers.FindPropertyRelative("minIntervalSeconds").floatValue = min;
                stragglers.FindPropertyRelative("maxIntervalSeconds").floatValue = max;
                stragglers.FindPropertyRelative("maxAlive").intValue = maxAlive;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
