using System;
using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// How the crosshair looks and behaves: a reticle where the player aims, which kicks with every Shot. With a mouse
    /// it follows the pointer all the time and stands in for the cursor; on touch it appears where the finger lands
    /// with the first Shot and fades a moment after the last. Sizes are in HUD units (1080 tall).
    /// </summary>
    [Serializable]
    public sealed class CrosshairSettings
    {
        [SerializeField, Min(1f), Tooltip("Width and height of the crosshair.")]
        private float size = 96f;

        [SerializeField, Tooltip("Bright enough to read over a Room lit by Lightning, and in the dark.")]
        private Color color = new(1f, 0.95f, 0.75f, 0.9f);

        [SerializeField, Min(1f), Tooltip("How many times its size the crosshair jumps to with each Shot.")]
        private float kickScale = 1.45f;

        [SerializeField, Min(0f), Tooltip("Seconds the jump takes to settle back.")]
        private float kickSeconds = 0.12f;

        [SerializeField, Min(0f), Tooltip("On touch, seconds the crosshair stays after the last Shot once the finger has lifted.")]
        private float touchLingerSeconds = 0.5f;

        [SerializeField, Min(0f), Tooltip("Seconds the crosshair then takes to fade out.")]
        private float fadeSeconds = 0.25f;

        [SerializeField, Tooltip("With a mouse, hide the system cursor while the crosshair stands in for it. It comes back on the pause and death screens.")]
        private bool hideMouseCursor = true;

        [Header("Reload ring")]
        [SerializeField, Min(1f), Tooltip("Width and height of the thin ring around the crosshair that fills while the Primary weapon reloads. " +
            "On touch it stays where the crosshair last was.")]
        private float reloadRingSize = 150f;

        [SerializeField, Tooltip("The ring's fill, clockwise from the top as the reload runs.")]
        private Color reloadRingColor = new(1f, 0.72f, 0.3f, 0.95f);

        [SerializeField, Tooltip("The faint full ring behind the fill, showing how much is left. Clear for none.")]
        private Color reloadTrackColor = new(1f, 1f, 1f, 0.18f);

        public float Size => size;
        public Color Color => color;
        public float KickScale => kickScale;
        public float KickSeconds => kickSeconds;
        public float TouchLingerSeconds => touchLingerSeconds;
        public float FadeSeconds => fadeSeconds;
        public bool HideMouseCursor => hideMouseCursor;
        public float ReloadRingSize => reloadRingSize;
        public Color ReloadRingColor => reloadRingColor;
        public Color ReloadTrackColor => reloadTrackColor;
    }
}
