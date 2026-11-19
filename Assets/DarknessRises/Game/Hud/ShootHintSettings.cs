using System;
using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The first-run hint over the first Enemy: what it says, how often it's shown and how it looks.
    /// Sizes are in HUD units (1080 tall).
    /// </summary>
    [Serializable]
    public sealed class ShootHintSettings
    {
        [SerializeField, Tooltip("How often the hint is shown: over the first Enemy of every Run, once ever on this device, or never.")]
        private ShootHintFrequency frequency = ShootHintFrequency.OncePerRun;

        [SerializeField, Tooltip("What the hint says on touch.")]
        private string touchText = "TAP TO SHOOT";

        [SerializeField, Tooltip("What the hint says with a mouse.")]
        private string mouseText = "CLICK TO SHOOT";

        [SerializeField] private Color color = new(1f, 0.95f, 0.35f, 1f);

        [SerializeField, Min(1f)] private float fontSize = 56f;

        [SerializeField, Tooltip("Where the hint's arrow points, relative to the Enemy's feet, in world metres: about its eyes.")]
        private Vector3 worldOffset = new(0f, 2.3f, 0f);

        [SerializeField, Min(1f), Tooltip("Width and height of the arrow that points down at the Enemy.")]
        private float arrowSize = 64f;

        [SerializeField, Min(0f), Tooltip("The hint keeps this far inside the screen's edges, so its text is never cut off when the Enemy is near one.")]
        private float screenMargin = 40f;

        [SerializeField, Min(0f), Tooltip("How far the hint bobs up and back.")]
        private float bobDistance = 18f;

        [SerializeField, Min(0f), Tooltip("Bobs per second.")]
        private float bobsPerSecond = 2f;

        [SerializeField, Min(0f), Tooltip("Seconds the hint takes to fade in, and out once the player has fired.")]
        private float fadeSeconds = 0.25f;

        public ShootHintFrequency Frequency => frequency;
        public string TouchText => touchText;
        public string MouseText => mouseText;
        public Color Color => color;
        public float FontSize => fontSize;
        public Vector3 WorldOffset => worldOffset;
        public float ArrowSize => arrowSize;
        public float ScreenMargin => screenMargin;
        public float BobDistance => bobDistance;
        public float BobsPerSecond => bobsPerSecond;
        public float FadeSeconds => fadeSeconds;
    }
}
