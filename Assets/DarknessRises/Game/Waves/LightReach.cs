using UnityEngine;

namespace DarknessRises.Game.Waves
{
    /// <summary>
    /// Whether a light's reach covers a point: a directional light (Lightning) covers the whole Room, a point light
    /// covers its range, and a spot light covers its range inside its cone. Walls are not taken into account,
    /// so a Spawn point behind a wall from a light still counts as lit; that errs on the side of never spawning in view.
    /// </summary>
    public static class LightReach
    {
        public static bool Covers(
            LightType type,
            Vector3 lightPosition,
            Vector3 lightForward,
            float range,
            float spotAngleDegrees,
            Vector3 point)
        {
            switch (type)
            {
                case LightType.Directional:
                    return true;

                case LightType.Point:
                    return (point - lightPosition).sqrMagnitude <= range * range;

                case LightType.Spot:
                    var toPoint = point - lightPosition;
                    var distanceSquared = toPoint.sqrMagnitude;
                    if (distanceSquared > range * range)
                    {
                        return false;
                    }

                    return distanceSquared <= 0f || Vector3.Angle(lightForward, toPoint) <= spotAngleDegrees * 0.5f;

                default:
                    // Area lights are baked only; nothing in the game uses them in real time.
                    return false;
            }
        }
    }
}
