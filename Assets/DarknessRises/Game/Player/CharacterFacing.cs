using System;
using UnityEngine;

namespace DarknessRises.Game.Player
{
    /// <summary>
    /// Which way the character's legs and upper body face. The upper body turns toward the aim; the legs face
    /// where the Route heads while walking, and turn toward the aim while stopped. The upper body never twists
    /// further from the legs than the authored limit: past it, the legs turn with it, so the upper body always
    /// ends up facing the aim. Angles are yaw in degrees, 0 facing +Z, in the range [-180, 180).
    /// </summary>
    public sealed class CharacterFacing
    {
        private const float MinFlatLength = 1e-4f;

        private readonly float bodyTurnSpeed;
        private readonly float upperBodyTurnSpeed;
        private readonly float maxTwist;

        public CharacterFacing(float bodyTurnSpeed, float upperBodyTurnSpeed, float maxTwist, float startYaw)
        {
            if (bodyTurnSpeed < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(bodyTurnSpeed), bodyTurnSpeed, "Turn speed can't be negative.");
            }

            if (upperBodyTurnSpeed < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(upperBodyTurnSpeed), upperBodyTurnSpeed, "Turn speed can't be negative.");
            }

            if (maxTwist < 0f || maxTwist > 180f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTwist), maxTwist, "The twist limit must be between 0 and 180 degrees.");
            }

            this.bodyTurnSpeed = bodyTurnSpeed;
            this.upperBodyTurnSpeed = upperBodyTurnSpeed;
            this.maxTwist = maxTwist;
            BodyYaw = Normalize(startYaw);
            UpperBodyYaw = BodyYaw;
        }

        /// <summary>Which way the legs face.</summary>
        public float BodyYaw { get; private set; }

        /// <summary>Which way the upper body faces.</summary>
        public float UpperBodyYaw { get; private set; }

        /// <summary>Degrees the upper body is turned from the legs, positive clockwise seen from above.</summary>
        public float Twist => Mathf.DeltaAngle(BodyYaw, UpperBodyYaw);

        /// <summary>
        /// Turns the legs and upper body for <paramref name="deltaSeconds"/>.
        /// </summary>
        /// <param name="travelDirection">Where the character walks; zero while it stands still.</param>
        /// <param name="aimDirection">Where the player aims; zero if there's no aim, which keeps the last one.</param>
        public void Tick(float deltaSeconds, Vector3 travelDirection, Vector3 aimDirection)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (TryGetYaw(aimDirection, out var aimYaw))
            {
                UpperBodyYaw = Normalize(Mathf.MoveTowardsAngle(UpperBodyYaw, aimYaw, upperBodyTurnSpeed * deltaSeconds));
            }

            // The legs turn within the arc the twist limit allows around the upper body, never across the back of it,
            // so they take the long way round when the short way would twist the upper body too far.
            var bodyTarget = TryGetYaw(travelDirection, out var travelYaw) ? travelYaw : UpperBodyYaw;
            var legsOffset = Mathf.Clamp(Mathf.DeltaAngle(UpperBodyYaw, BodyYaw), -maxTwist, maxTwist);
            var targetOffset = Mathf.Clamp(Mathf.DeltaAngle(UpperBodyYaw, bodyTarget), -maxTwist, maxTwist);
            legsOffset = Mathf.MoveTowards(legsOffset, targetOffset, bodyTurnSpeed * deltaSeconds);

            BodyYaw = Normalize(UpperBodyYaw + legsOffset);
        }

        /// <summary>
        /// A flat world direction seen from the legs: x is to their right, y is ahead of them.
        /// Feeds the walk blend, so walking with the legs turned away blends into strafing or walking backwards.
        /// </summary>
        public Vector2 ToBodySpace(Vector3 worldDirection)
        {
            var local = Quaternion.Euler(0f, -BodyYaw, 0f) * new Vector3(worldDirection.x, 0f, worldDirection.z);
            return new Vector2(local.x, local.z);
        }

        public static bool TryGetYaw(Vector3 direction, out float yaw)
        {
            if (direction.x * direction.x + direction.z * direction.z < MinFlatLength * MinFlatLength)
            {
                yaw = 0f;
                return false;
            }

            yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            return true;
        }

        private static float Normalize(float yaw)
        {
            return Mathf.Repeat(yaw + 180f, 360f) - 180f;
        }
    }
}
