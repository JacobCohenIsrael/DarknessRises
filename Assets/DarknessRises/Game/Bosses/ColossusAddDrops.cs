using System;
using System.Collections.Generic;
using DarknessRises.Game.Randomness;
using UnityEngine;

namespace DarknessRises.Game.Bosses
{
    /// <summary>Where a Shambler drops from the Colossus's back: one of the Boss Room's drop points, away from the character.</summary>
    public static class ColossusAddDrops
    {
        /// <summary>
        /// A random drop point at least <paramref name="minMetres"/> from <paramref name="character"/> on the floor,
        /// or the farthest one when none is that far. -1 when there are no drop points. Never allocates.
        /// </summary>
        public static int Pick(IReadOnlyList<Vector3> points, Vector3 character, float minMetres, IRandom random)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var farEnough = 0;
            var farthest = -1;
            var farthestSqr = -1f;
            var minSqr = minMetres * minMetres;
            for (var i = 0; i < points.Count; i++)
            {
                var sqr = FlatSqrDistance(points[i], character);
                if (sqr >= minSqr)
                {
                    farEnough++;
                }

                if (sqr > farthestSqr)
                {
                    farthestSqr = sqr;
                    farthest = i;
                }
            }

            if (farEnough == 0)
            {
                return farthest;
            }

            var pick = Math.Min(farEnough - 1, (int)Math.Floor(random.Range(0f, farEnough)));
            for (var i = 0; i < points.Count; i++)
            {
                if (FlatSqrDistance(points[i], character) < minSqr)
                {
                    continue;
                }

                if (pick == 0)
                {
                    return i;
                }

                pick--;
            }

            return farthest;
        }

        private static float FlatSqrDistance(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
