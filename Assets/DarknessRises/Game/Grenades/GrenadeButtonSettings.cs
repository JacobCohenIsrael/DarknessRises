using System;
using UnityEngine;

namespace DarknessRises.Game.Grenades
{
    /// <summary>The touch Grenade button: how it's picked up, its size, where it starts, and how it looks.</summary>
    [Serializable]
    public sealed class GrenadeButtonSettings
    {
        [SerializeField, Min(0.05f), Tooltip("Seconds a finger stays on the button before it's picked up to be moved.")]
        private float holdSeconds = 0.5f;

        [SerializeField, Min(1f), Tooltip("The button's diameter in HUD reference pixels (1920x1080).")]
        private float size = 150f;

        [SerializeField, Tooltip("Where the button's centre starts, 0 to 1 across the screen's safe area. Above the ammo readout.")]
        private Vector2 defaultPosition = new(0.9f, 0.34f);

        [SerializeField, Tooltip("The button's colour while Idle with a charge to throw.")]
        private Color idleColor = new(0.16f, 0.16f, 0.17f, 0.6f);

        [SerializeField, Tooltip("The button's colour while armed, pulsing toward the Idle colour.")]
        private Color armedColor = new(1f, 0.55f, 0.15f, 0.9f);

        [SerializeField, Min(0.05f), Tooltip("Seconds per pulse while armed.")]
        private float armedPulseSeconds = 0.6f;

        [SerializeField, Tooltip("The button's colour with no charges: greyed out.")]
        private Color emptyColor = new(0.3f, 0.3f, 0.3f, 0.3f);

        [SerializeField, Min(1f), Tooltip("How much bigger the button draws while it's picked up.")]
        private float movingScale = 1.15f;

        public float HoldSeconds => holdSeconds;
        public float Size => size;
        public Vector2 DefaultPosition => defaultPosition;
        public Color IdleColor => idleColor;
        public Color ArmedColor => armedColor;
        public float ArmedPulseSeconds => armedPulseSeconds;
        public Color EmptyColor => emptyColor;
        public float MovingScale => movingScale;
    }
}
