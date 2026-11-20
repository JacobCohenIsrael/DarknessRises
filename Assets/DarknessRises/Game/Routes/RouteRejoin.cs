using System;
using UnityEngine;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// Where a character that something else moved off its Route (the Boss fight's Relocate) walks back onto it.
    /// It heads in a straight line for the farthest point of the Route it can reach that way before the next stop, so it
    /// walks on towards where the Route goes rather than back to where it left it. A character still on that spot just
    /// walks the Route on. Plain C# (ADR 0003): what blocks a straight walk comes in as <c>isClear</c>.
    /// </summary>
    public static class RouteRejoin
    {
        /// <summary>
        /// Metres along <paramref name="path"/> at which a character standing at <paramref name="from"/> rejoins it: the
        /// farthest point from <paramref name="leftAt"/> up to <paramref name="nextStop"/> (sampled every
        /// <paramref name="sampleMetres"/>) that <paramref name="isClear"/> says it can walk straight to. It is
        /// <paramref name="leftAt"/> itself when the character stands within <paramref name="onRouteMetres"/> of it
        /// on the floor, or when no point further on can be reached.
        /// </summary>
        /// <param name="leftAt">Metres along the Route at which the character left it.</param>
        /// <param name="nextStop">Metres along the Route of the next Station, or the Route's end.</param>
        /// <param name="isClear">Whether a straight walk between two points is clear. Null counts every walk as clear.</param>
        public static float JoinDistance(
            Vector3 from,
            RoutePath path,
            float leftAt,
            float nextStop,
            float onRouteMetres,
            float sampleMetres,
            Func<Vector3, Vector3, bool> isClear)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (nextStop < leftAt)
            {
                throw new ArgumentOutOfRangeException(nameof(nextStop), nextStop, "The next stop can't come before where the character left the Route.");
            }

            if (onRouteMetres < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(onRouteMetres), onRouteMetres, "A distance can't be negative.");
            }

            if (sampleMetres <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleMetres), sampleMetres, "The Route must be sampled some way apart.");
            }

            if (FlatDistance(from, path.PositionAt(leftAt)) <= onRouteMetres)
            {
                return leftAt;
            }

            var samples = Mathf.CeilToInt((nextStop - leftAt) / sampleMetres);
            for (var i = 0; i < samples; i++)
            {
                var distance = nextStop - i * sampleMetres;
                if (isClear == null || isClear(from, path.PositionAt(distance)))
                {
                    return distance;
                }
            }

            return leftAt;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }
    }
}
