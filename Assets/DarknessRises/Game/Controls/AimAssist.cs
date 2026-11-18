using System;
using DarknessRises.Game.Visibility;
using UnityEngine;

namespace DarknessRises.Game.Controls
{
    /// <summary>An Enemy Aim assist could bend toward: where it is, and the point light would have to reach to show it.</summary>
    public readonly struct AimCandidate
    {
        public AimCandidate(Vector3 position, Vector3 body)
        {
            Position = position;
            Body = body;
        }

        /// <summary>Where it stands; Shots are bent toward this point, flattened.</summary>
        public Vector3 Position { get; }

        /// <summary>The middle of its body, which a light must reach for it to be visible.</summary>
        public Vector3 Body { get; }
    }

    /// <summary>
    /// Aim assist, on touch only: picks the nearest visible Enemy within the authored radius of the aim point
    /// (measured on the floor plane) and bends the aim direction toward it by the authored strength.
    /// </summary>
    public sealed class AimAssist
    {
        private readonly AimAssistSettings settings;

        public AimAssist(AimAssistSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// The index of the candidate to bend toward: the nearest to <paramref name="aimPoint"/> within the radius
        /// that <paramref name="lit"/> shows. -1 when there's none.
        /// </summary>
        public int PickTarget(Vector3 aimPoint, ReadOnlySpan<AimCandidate> candidates, ReadOnlySpan<LitSpot> lit)
        {
            var best = -1;
            var bestDistanceSquared = settings.Radius * settings.Radius;
            for (var i = 0; i < candidates.Length; i++)
            {
                var offset = candidates[i].Position - aimPoint;
                offset.y = 0f;
                var distanceSquared = offset.sqrMagnitude;
                if (distanceSquared <= bestDistanceSquared && IsVisible(candidates[i].Body, lit))
                {
                    best = i;
                    bestDistanceSquared = distanceSquared;
                }
            }

            return best;
        }

        /// <summary>
        /// The aim point bent toward <paramref name="target"/>: the direction from <paramref name="from"/> to the aim point
        /// turned toward the target's by the strength (0 keeps it, 1 aims straight at the target), at the aim point's distance.
        /// </summary>
        public Vector3 Bend(Vector3 from, Vector3 aimPoint, Vector3 target)
        {
            var aim = aimPoint - from;
            aim.y = 0f;
            var toTarget = target - from;
            toTarget.y = 0f;
            if (aim.sqrMagnitude <= 0f || toTarget.sqrMagnitude <= 0f)
            {
                return aimPoint;
            }

            var distance = aim.magnitude;
            var bent = Vector3.Slerp(aim / distance, toTarget.normalized, settings.Strength);
            var point = from + bent * distance;
            point.y = aimPoint.y;
            return point;
        }

        /// <summary>Whether any of <paramref name="lit"/> lights <paramref name="body"/>.</summary>
        public static bool IsVisible(Vector3 body, ReadOnlySpan<LitSpot> lit)
        {
            for (var i = 0; i < lit.Length; i++)
            {
                if (lit[i].Lights(body))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
