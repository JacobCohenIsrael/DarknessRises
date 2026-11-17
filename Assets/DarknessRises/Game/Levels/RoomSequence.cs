using System;
using System.Collections.Generic;
using DarknessRises.Game.Routes;
using UnityEngine;

namespace DarknessRises.Game.Levels
{
    /// <summary>
    /// The Level's Rooms along its Route: each Room starts at the doorway the Route enters it through,
    /// and the character is in whichever Room it last walked into. Plain C#; checking never allocates.
    /// </summary>
    public sealed class RoomSequence
    {
        private readonly float[] entryDistances;

        /// <param name="entryDistances">
        /// Metres along the Route at which each Room begins, in order. The first Room begins at the start of the Route (0).
        /// </param>
        public RoomSequence(IReadOnlyList<float> entryDistances)
        {
            if (entryDistances == null)
            {
                throw new ArgumentNullException(nameof(entryDistances));
            }

            if (entryDistances.Count == 0)
            {
                throw new ArgumentException("A Level needs at least one Room.", nameof(entryDistances));
            }

            if (entryDistances[0] != 0f)
            {
                throw new ArgumentException("The first Room begins where the Route does.", nameof(entryDistances));
            }

            this.entryDistances = new float[entryDistances.Count];
            for (var i = 0; i < entryDistances.Count; i++)
            {
                if (i > 0 && entryDistances[i] <= entryDistances[i - 1])
                {
                    throw new ArgumentException("Rooms must be listed in the order the Route enters them.", nameof(entryDistances));
                }

                this.entryDistances[i] = entryDistances[i];
            }
        }

        /// <summary>Raised with a Room's index when the character walks into it through its doorway.</summary>
        public event Action<int> RoomEntered;

        public int RoomCount => entryDistances.Length;

        /// <summary>The Room the character is in. It starts in the first.</summary>
        public int Current { get; private set; }

        /// <summary>Metres along the Route at which the Room begins.</summary>
        public float EntryDistanceOf(int room) => entryDistances[room];

        /// <summary>The Room that holds the point <paramref name="distance"/> metres along the Route.</summary>
        public int RoomAt(float distance)
        {
            for (var i = entryDistances.Length - 1; i > 0; i--)
            {
                if (distance >= entryDistances[i])
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>Follows the character to <paramref name="distance"/>, entering each Room it walked into on the way.</summary>
        public void Follow(float distance)
        {
            var room = RoomAt(distance);
            while (Current < room)
            {
                Current++;
                RoomEntered?.Invoke(Current);
            }
        }

        /// <summary>
        /// Where the Rooms begin along <paramref name="path"/>: the first at its start, then each at the Route point
        /// closest to the doorway into it.
        /// </summary>
        /// <param name="doorways">The doorway into each Room after the first, in order. Each sits on a point of the Route.</param>
        public static float[] EntryDistances(RoutePath path, IReadOnlyList<Vector3> doorways)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (doorways == null)
            {
                throw new ArgumentNullException(nameof(doorways));
            }

            var distances = new float[doorways.Count + 1];
            for (var d = 0; d < doorways.Count; d++)
            {
                var closest = 0;
                var closestSqr = float.PositiveInfinity;
                for (var p = 0; p < path.PointCount; p++)
                {
                    var sqr = (path.PointAt(p) - doorways[d]).sqrMagnitude;
                    if (sqr < closestSqr)
                    {
                        closestSqr = sqr;
                        closest = p;
                    }
                }

                distances[d + 1] = path.DistanceAtPoint(closest);
            }

            return distances;
        }
    }
}
