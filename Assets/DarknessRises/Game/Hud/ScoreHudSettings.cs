using System;
using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// How the Score shows on the HUD: the running Score and the Combo, the points that pop up over a kill, the hit
    /// marker where a Shot hurts an Enemy, and the result card under the Clear banner. Sizes are in HUD units (1080 tall).
    /// </summary>
    [Serializable]
    public sealed class ScoreHudSettings
    {
        [Header("Score and Combo")]
        [SerializeField, Tooltip("The small word over the Score.")]
        private string scoreCaption = "SCORE";

        [SerializeField] private Color scoreColor = new(0.95f, 0.92f, 0.8f, 0.95f);

        [SerializeField, Tooltip("The small word beside the Combo's multiplier.")]
        private string comboCaption = "COMBO";

        [SerializeField, Tooltip("The Combo's multiplier and its timeout bar.")]
        private Color comboColor = new(1f, 0.72f, 0.2f, 1f);

        [SerializeField, Min(1f), Tooltip("How many times its size the Combo's multiplier jumps to when it steps up.")]
        private float comboKickScale = 1.6f;

        [SerializeField, Min(0f), Tooltip("Seconds that jump takes to settle back.")]
        private float comboKickSeconds = 0.2f;

        [Header("Points over a kill")]
        [SerializeField, Min(1f)] private float popupFontSize = 46f;

        [SerializeField, Tooltip("A kill at the Combo's plain multiplier.")]
        private Color popupColor = new(1f, 1f, 1f, 1f);

        [SerializeField, Tooltip("A kill the Combo multiplied.")]
        private Color popupComboColor = new(1f, 0.72f, 0.2f, 1f);

        [SerializeField, Min(0.05f), Tooltip("Seconds the points stay up.")]
        private float popupSeconds = 0.9f;

        [SerializeField, Min(0f), Tooltip("How far the points float up over their time.")]
        private float popupRise = 90f;

        [SerializeField, Tooltip("Where the points start, relative to the Enemy's feet, in world metres.")]
        private Vector3 popupWorldOffset = new(0f, 2.2f, 0f);

        [Header("Hit marker")]
        [SerializeField, Min(1f), Tooltip("Width and height of the marker that flashes where a Shot hurts an Enemy.")]
        private float hitMarkerSize = 70f;

        [SerializeField] private Color hitMarkerColor = new(1f, 0.25f, 0.2f, 1f);

        [SerializeField, Min(0.02f), Tooltip("Seconds the marker shows, shrinking and fading.")]
        private float hitMarkerSeconds = 0.18f;

        [SerializeField, Min(1f), Tooltip("How many times its size the marker starts at.")]
        private float hitMarkerStartScale = 1.5f;

        [Header("Result card")]
        [SerializeField] private string killsCaption = "KILLS";
        [SerializeField] private string accuracyCaption = "ACCURACY";
        [SerializeField] private string timeCaption = "TIME";

        [SerializeField, Tooltip("From the centre of the screen to the centre of the card. Under the Clear banner.")]
        private Vector2 cardPosition = new(0f, 20f);

        [SerializeField] private Color cardTextColor = new(0.92f, 0.92f, 0.92f, 1f);

        [SerializeField, Tooltip("The strip behind the card, so it reads over a Room lit by Lightning. Clear for none.")]
        private Color cardBandColor = new(0f, 0f, 0f, 0.45f);

        [SerializeField, Tooltip("The Grade letter's colour for C, B, A and S, in that order.")]
        private Color[] gradeColors =
        {
            new(0.7f, 0.7f, 0.7f, 1f),
            new(0.45f, 0.75f, 1f, 1f),
            new(0.55f, 1f, 0.6f, 1f),
            new(1f, 0.85f, 0.2f, 1f),
        };

        public string ScoreCaption => scoreCaption;
        public Color ScoreColor => scoreColor;
        public string ComboCaption => comboCaption;
        public Color ComboColor => comboColor;
        public float ComboKickScale => comboKickScale;
        public float ComboKickSeconds => comboKickSeconds;
        public float PopupFontSize => popupFontSize;
        public Color PopupColor => popupColor;
        public Color PopupComboColor => popupComboColor;
        public float PopupSeconds => popupSeconds;
        public float PopupRise => popupRise;
        public Vector3 PopupWorldOffset => popupWorldOffset;
        public float HitMarkerSize => hitMarkerSize;
        public Color HitMarkerColor => hitMarkerColor;
        public float HitMarkerSeconds => hitMarkerSeconds;
        public float HitMarkerStartScale => hitMarkerStartScale;
        public string KillsCaption => killsCaption;
        public string AccuracyCaption => accuracyCaption;
        public string TimeCaption => timeCaption;
        public Vector2 CardPosition => cardPosition;
        public Color CardTextColor => cardTextColor;
        public Color CardBandColor => cardBandColor;

        /// <summary>The colour of <paramref name="grade"/>'s letter; white when none is authored for it.</summary>
        public Color GradeColor(Scoring.Grade grade)
        {
            var index = (int)grade;
            return index >= 0 && index < gradeColors.Length ? gradeColors[index] : Color.white;
        }
    }
}
