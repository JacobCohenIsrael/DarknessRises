using UnityEngine;

namespace DarknessRises.Game.Controls
{
    /// <summary>
    /// Turns a pointer ray from the camera into an aim point in the Room.
    /// The aim point lies on a horizontal plane at the Weapon's height, so a level Shot passes visually under the pointer.
    /// </summary>
    public static class AimPlane
    {
        // Rays closer than this to horizontal never reach the plane in any useful distance.
        private const float MinimumVerticalComponent = 1e-4f;

        public static bool TryIntersect(Ray ray, float planeHeight, out Vector3 point)
        {
            var vertical = ray.direction.y;
            if (Mathf.Abs(vertical) < MinimumVerticalComponent)
            {
                point = default;
                return false;
            }

            var distance = (planeHeight - ray.origin.y) / vertical;
            if (distance < 0f)
            {
                point = default;
                return false;
            }

            point = ray.GetPoint(distance);
            return true;
        }

        /// <summary>
        /// The horizontal direction from <paramref name="from"/> to <paramref name="aimPoint"/>.
        /// False when the aim point is (almost) straight above or below, where the direction is undefined.
        /// </summary>
        public static bool TryGetFlatDirection(Vector3 from, Vector3 aimPoint, out Vector3 direction)
        {
            var flat = aimPoint - from;
            flat.y = 0f;
            var lengthSquared = flat.sqrMagnitude;
            if (lengthSquared < MinimumVerticalComponent * MinimumVerticalComponent)
            {
                direction = default;
                return false;
            }

            direction = flat / Mathf.Sqrt(lengthSquared);
            return true;
        }
    }
}
