using System;
using UnityEngine;

namespace DarknessRises.Game.Cameras
{
    /// <summary>
    /// How far along the Route ahead of the character the camera aims, so it frames more of what's ahead than behind.
    /// </summary>
    [Serializable]
    public sealed class LookAheadSettings
    {
        [SerializeField, Min(0f), Tooltip("Metres ahead along the Route the camera aims while the character walks.")]
        private float walkingDistance = 4f;

        [SerializeField, Min(0f), Tooltip(
            "Metres ahead along the Route the camera aims while the character is stopped at a Station or the end of the Route.")]
        private float stoppedDistance;

        [SerializeField, Min(0.01f), Tooltip("Metres per second the look-ahead grows or shrinks when the character starts or stops.")]
        private float changeSpeed = 3f;

        public float WalkingDistance => walkingDistance;
        public float StoppedDistance => stoppedDistance;
        public float ChangeSpeed => changeSpeed;
    }
}
