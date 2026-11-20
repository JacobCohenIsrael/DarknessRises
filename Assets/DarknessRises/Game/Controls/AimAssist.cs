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
    /// Aim assist for one kind of input (touch or mouse): picks the nearest visible Enemy within the authored radius of
    /// the aim point (measured on the floor plane) and bends the aim direction toward it by the strength for the Level.
    /// The pull fades out toward the edge of the radius, so it never jumps.
    /// </summary>
    public sealed class AimAssist
    {
        private readonly AimAssistSettings settings;

        /// <param name="levelIndex">The Level fought in (0 is the first): later Levels pull harder, up to the cap.</param>
        public AimAssist(AimAssistSettings settings, int levelIndex = 0)
        {
            this.settings = settings;
            Strength = settings.StrengthAt(levelIndex);
        }

        /// <summary>How far the aim turns toward an Enemy close to the aim point, in this Level: 0 not at all, 1 straight at it.</summary>
        public float Strength { get; }

        /// <summary>Metres from the aim point (on the floor plane) an Enemy may be to pull the aim.</summary>
        public float Radius => settings.Radius;

        /// <summary>
        /// The strength of the pull toward an Enemy <paramref name="distance"/> metres from the aim point (on the floor
        /// plane): full within the full-pull part of the radius, fading to 0 at the radius.
        /// </summary>
        public float PullAt(float distance)
        {
            var radius = settings.Radius;
            if (distance >= radius || radius <= 0f)
            {
                return 0f;
            }

            var fullPull = radius * settings.FullPullFraction;
            if (distance <= fullPull)
            {
                return Strength;
            }

            return Strength * (radius - distance) / (radius - fullPull);
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
        /// turned toward the target's by the pull (0 keeps it, 1 aims straight at the target), at the aim point's distance.
        /// The pull is the strength, faded by how far the target is from the aim point (<see cref="PullAt"/>).
        /// </summary>
        public Vector3 Bend(Vector3 from, Vector3 aimPoint, Vector3 target)
        {
            var offset = target - aimPoint;
            offset.y = 0f;
            var pull = PullAt(offset.magnitude);
            if (pull <= 0f)
            {
                return aimPoint;
            }

            var aim = aimPoint - from;
            aim.y = 0f;
            var toTarget = target - from;
            toTarget.y = 0f;
            if (aim.sqrMagnitude <= 0f || toTarget.sqrMagnitude <= 0f)
            {
                return aimPoint;
            }

            var distance = aim.magnitude;
            var bent = Vector3.Slerp(aim / distance, toTarget.normalized, pull);
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
