using System;
using UnityEngine;

namespace DarknessRises.Game.Bosses
{
    /// <summary>How Relocate picks a Position from a direction, and how quick the dash there is (ADR 0006).</summary>
    [Serializable]
    public sealed class RelocateSettings
    {
        [SerializeField, Min(0.01f), Tooltip("Seconds the dash from one Position to the next takes.")]
        private float dashSeconds = 0.25f;

        [SerializeField, Range(1f, 90f), Tooltip(
            "How far, in degrees either side of the pressed direction, a Position may lie and still count as " +
            "\"in that direction\". The nearest one inside wins.")]
        private float maxAngleDegrees = 60f;

        public RelocateSettings()
        {
        }

        public RelocateSettings(float dashSeconds, float maxAngleDegrees)
        {
            this.dashSeconds = dashSeconds;
            this.maxAngleDegrees = maxAngleDegrees;
        }

        public float DashSeconds => dashSeconds;
        public float MaxAngleDegrees => maxAngleDegrees;
    }
}
