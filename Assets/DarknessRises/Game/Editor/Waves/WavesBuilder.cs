using DarknessRises.Game.Editor.Enemies;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Waves;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace DarknessRises.Game.Editor.Waves
{
    /// <summary>
    /// Gives the test Room what its Waves need: a baked NavMesh for Enemies to hunt on, Spawn points around its edge,
    /// the scene object that runs the Waves, and (when none are authored yet) placeholder Shambler Waves for each Station.
    /// </summary>
    public static class WavesBuilder
    {
        private const string NavMeshPath = "Assets/DarknessRises/Scenes/TestRoom/NavMesh.asset";

        // Inside the walls, out of the Stations' Aura, each facing into the Room. In metres.
        private static readonly Vector3[] SpawnPointPositions =
        {
            new(-9f, 0f, 9f),
            new(2.5f, 0f, 9f),
            new(9f, 0f, 5f),
            new(9f, 0f, -4f),
            new(0f, 0f, -9f),
            new(-9f, 0f, 3.5f),
        };

        // Placeholder fights: (delay seconds, spawn interval seconds, Shamblers) per Wave, per Station.
        private static readonly (float delay, float interval, int count)[][] PlaceholderStationWaves =
        {
            new[] { (0.8f, 1.5f, 3), (2f, 1.2f, 4) },
            new[] { (0.8f, 1.2f, 4), (2f, 1f, 6) },
        };

        /// <summary>Bakes the Room's NavMesh. Call it before anything that isn't part of the Room is built.</summary>
        public static void BakeNavMesh(Transform room)
        {
            var surface = room.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();

            var folder = System.IO.Path.GetDirectoryName(NavMeshPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder).Replace('\\', '/'), System.IO.Path.GetFileName(folder));
            }

            AssetDatabase.DeleteAsset(NavMeshPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
        }

        /// <summary>Adds the Room's Spawn points and the object that runs the Waves through them.</summary>
        public static RouteWavesView BuildWaves(Transform room)
        {
            var spawnPointsObject = new GameObject("Spawn Points", typeof(RoomSpawnPoints));
            spawnPointsObject.transform.SetParent(room, false);

            var points = new Transform[SpawnPointPositions.Length];
            for (var i = 0; i < points.Length; i++)
            {
                var point = new GameObject($"Spawn Point {i + 1}").transform;
                point.SetParent(spawnPointsObject.transform, false);
                point.localPosition = SpawnPointPositions[i];
                var inward = -SpawnPointPositions[i];
                inward.y = 0f;
                point.localRotation = Quaternion.LookRotation(inward, Vector3.up);
                points[i] = point;
            }

            var spawnPoints = spawnPointsObject.GetComponent<RoomSpawnPoints>();
            var serializedPoints = new SerializedObject(spawnPoints);
            var list = serializedPoints.FindProperty("points");
            list.arraySize = points.Length;
            for (var i = 0; i < points.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            }

            serializedPoints.ApplyModifiedPropertiesWithoutUndo();

            var view = new GameObject("Waves").AddComponent<RouteWavesView>();
            var serializedView = new SerializedObject(view);
            serializedView.FindProperty("spawnPoints").objectReferenceValue = spawnPoints;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>Authors placeholder Shambler Waves for each Station, unless the Route already has Waves.</summary>
        public static void AuthorPlaceholderWaves(RouteDefinition route)
        {
            // Loaded even when Waves are authored, so a missing Shambler prefab is rebuilt.
            var shambler = EnemyBuilder.LoadOrCreateShambler();
            var serialized = new SerializedObject(route);
            var stations = serialized.FindProperty("stations");
            if (stations.arraySize > 0)
            {
                return;
            }

            stations.arraySize = PlaceholderStationWaves.Length;
            for (var s = 0; s < PlaceholderStationWaves.Length; s++)
            {
                var waves = stations.GetArrayElementAtIndex(s).FindPropertyRelative("waves");
                waves.arraySize = PlaceholderStationWaves[s].Length;
                for (var w = 0; w < PlaceholderStationWaves[s].Length; w++)
                {
                    var (delay, interval, count) = PlaceholderStationWaves[s][w];
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

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
