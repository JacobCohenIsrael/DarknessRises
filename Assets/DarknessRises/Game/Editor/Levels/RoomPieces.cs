using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DarknessRises.Game.Editor.Levels
{
    /// <summary>
    /// Building blocks for hand-built Rooms: floors, walls broken by doorways and windows, and Obstacles.
    /// Walls and floors never cast shadows, so a Lightning flash lights the whole Room and only Obstacles throw shadows.
    /// </summary>
    public static class RoomPieces
    {
        /// <summary>Metres from floor to ceiling line of a full wall.</summary>
        public const float WallHeight = 3.2f;

        public const float WallThickness = 0.4f;

        /// <summary>
        /// Height of every wall on the camera's side of a Room (its south wall). The fixed camera looks north over it,
        /// so a full-height wall there would black out the floor between it and the character. Low enough to see over,
        /// high enough to still read as the Room's edge in a flash.
        /// </summary>
        public const float CameraSideWallHeight = 0.6f;

        public const float DoorHeight = 2.6f;
        public const float WindowSillHeight = 0.8f;
        public const float WindowTopHeight = 2.6f;

        /// <summary>A gap in a wall: a doorway from the floor up, or a window between sill and lintel.</summary>
        public readonly struct Opening
        {
            public Opening(float centre, float width, bool isWindow)
            {
                Centre = centre;
                Width = width;
                IsWindow = isWindow;
                Bottom = isWindow ? WindowSillHeight : 0f;
                Top = isWindow ? WindowTopHeight : DoorHeight;
            }

            /// <summary>A window with its own sill and top, such as the Boss Room's tall windows.</summary>
            public Opening(float centre, float width, float sill, float top)
            {
                Centre = centre;
                Width = width;
                IsWindow = true;
                Bottom = sill;
                Top = top;
            }

            /// <summary>World coordinate along the wall (x for north and south walls, z for east and west walls).</summary>
            public float Centre { get; }

            public float Width { get; }
            public bool IsWindow { get; }
            public float Bottom { get; }
            public float Top { get; }
        }

        public static GameObject Block(string name, Transform parent, Vector3 centre, Vector3 size, Material material, bool castsShadows)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = centre;
            block.transform.localScale = size;
            var renderer = block.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castsShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(block, StaticEditorFlags.BatchingStatic);
            return block;
        }

        /// <summary>A floor slab whose top is at y = 0, covering x and z from min to max.</summary>
        public static void Floor(string name, Transform parent, float xMin, float zMin, float xMax, float zMax, Material material)
        {
            Block(
                name,
                parent,
                new Vector3((xMin + xMax) / 2f, -WallThickness / 2f, (zMin + zMax) / 2f),
                new Vector3(xMax - xMin, WallThickness, zMax - zMin),
                material,
                castsShadows: false);
        }

        /// <summary>
        /// A wall along the x axis (north or south side of a Room) at <paramref name="z"/>, from <paramref name="xMin"/>
        /// to <paramref name="xMax"/>, broken by <paramref name="openings"/>. Each window gets a sky quad outside it,
        /// added to <paramref name="windows"/>.
        /// </summary>
        public static void WallAlongX(
            string name,
            Transform parent,
            float z,
            float xMin,
            float xMax,
            float height,
            float outwardZ,
            IReadOnlyList<Opening> openings,
            Material wallMaterial,
            Material skyMaterial,
            List<Renderer> windows)
        {
            Wall(name, parent, Vector3.right, new Vector3(0f, 0f, z), xMin, xMax, height, new Vector3(0f, 0f, outwardZ), openings, wallMaterial, skyMaterial, windows);
        }

        /// <summary>A wall along the z axis (east or west side of a Room) at <paramref name="x"/>. See <see cref="WallAlongX"/>.</summary>
        public static void WallAlongZ(
            string name,
            Transform parent,
            float x,
            float zMin,
            float zMax,
            float height,
            float outwardX,
            IReadOnlyList<Opening> openings,
            Material wallMaterial,
            Material skyMaterial,
            List<Renderer> windows)
        {
            Wall(name, parent, Vector3.forward, new Vector3(x, 0f, 0f), zMin, zMax, height, new Vector3(outwardX, 0f, 0f), openings, wallMaterial, skyMaterial, windows);
        }

        private static void Wall(
            string name,
            Transform parent,
            Vector3 along,
            Vector3 line,
            float from,
            float to,
            float height,
            Vector3 outward,
            IReadOnlyList<Opening> openings,
            Material wallMaterial,
            Material skyMaterial,
            List<Renderer> windows)
        {
            var wall = new GameObject(name).transform;
            wall.SetParent(parent, false);

            var start = from;
            var piece = 0;
            for (var i = 0; i < openings.Count; i++)
            {
                var opening = openings[i];
                var openingStart = opening.Centre - opening.Width / 2f;
                var openingEnd = opening.Centre + opening.Width / 2f;
                Span(wall, $"Pier {++piece}", along, line, start, openingStart, 0f, height, wallMaterial);
                if (opening.Bottom > 0f)
                {
                    Span(wall, $"Sill {i + 1}", along, line, openingStart, openingEnd, 0f, opening.Bottom, wallMaterial);
                }

                if (opening.Top < height)
                {
                    Span(wall, $"Lintel {i + 1}", along, line, openingStart, openingEnd, opening.Top, height, wallMaterial);
                }

                if (opening.IsWindow)
                {
                    windows.Add(WindowSky($"Window Sky {i + 1}", wall, along, line, opening, outward, skyMaterial));
                }

                start = openingEnd;
            }

            Span(wall, $"Pier {++piece}", along, line, start, to, 0f, height, wallMaterial);
        }

        private static void Span(Transform wall, string name, Vector3 along, Vector3 line, float from, float to, float bottom, float top, Material material)
        {
            if (to - from <= 0.01f || top - bottom <= 0.01f)
            {
                return;
            }

            var across = Vector3.Cross(Vector3.up, along);
            var centre = line + along * ((from + to) / 2f) + Vector3.up * ((bottom + top) / 2f);
            var size = Abs(along * (to - from)) + Vector3.up * (top - bottom) + Abs(across * WallThickness);
            Block(name, wall, centre, size, material, castsShadows: false);
        }

        private static Renderer WindowSky(string name, Transform wall, Vector3 along, Vector3 line, Opening window, Vector3 outward, Material material)
        {
            const float overlap = 0.2f;
            const float gapBehindWall = 0.05f;

            var sky = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sky.name = name;
            Object.DestroyImmediate(sky.GetComponent<Collider>());
            sky.transform.SetParent(wall, false);

            // Just outside the wall, facing into the Room, a little larger than the opening.
            var outwardUnit = outward.normalized;
            var centre = line + along * window.Centre + Vector3.up * ((window.Bottom + window.Top) / 2f)
                         + outwardUnit * (WallThickness / 2f + gapBehindWall);
            sky.transform.SetLocalPositionAndRotation(centre, Quaternion.LookRotation(outwardUnit, Vector3.up));
            sky.transform.localScale = new Vector3(window.Width + overlap, window.Top - window.Bottom + overlap, 1f);

            var renderer = sky.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static Vector3 Abs(Vector3 v) => new(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }
}
