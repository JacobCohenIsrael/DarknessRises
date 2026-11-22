using System;
using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// How the Score shows on the HUD: the running Score and the Combo, the Score boost's timer, the points that pop up
    /// over a kill, the hit marker where a Shot hurts an Enemy, the result card under the Clear banner with its accuracy
    /// bonus and Score boost offer, and the best Score on the end screen. Sizes are in HUD units (1080 tall).
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

        [Header("Score boost on the HUD")]
        [SerializeField, Tooltip("The boost beside the Score while it runs. {0} is what it multiplies by.")]
        private string boostFormat = "x{0}";

        [SerializeField, Tooltip("The boost's time left beside it. {0} is the whole seconds left.")]
        private string boostTimerFormat = "{0}s";

        [SerializeField] private Color boostColor = new(0.45f, 1f, 0.55f, 1f);

        [SerializeField, Min(0f), Tooltip("Seconds left at which the boost starts to pulse, to say it's about to end.")]
        private float boostWarningSeconds = 10f;

        [SerializeField, Min(0f), Tooltip("Pulses per second while it's about to end.")]
        private float boostPulsesPerSecond = 2.5f;

        [SerializeField, Min(1f), Tooltip("How many times its size the boost grows to at the top of a pulse.")]
        private float boostPulseScale = 1.3f;

        [SerializeField, Min(0f), Tooltip("Seconds the Score takes to count up by a Station's accuracy bonus, once the card has counted it.")]
        private float scoreCountSeconds = 0.5f;

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

        [Header("Result card: accuracy bonus")]
        [SerializeField, Tooltip("The accuracy bonus stamped on the card. {0} is its multiplier, as \"1.5\".")]
        private string accuracyStampFormat = "ACCURACY x{0}";

        [SerializeField] private Color accuracyStampColor = new(1f, 0.85f, 0.2f, 1f);

        [SerializeField, Min(0f), Tooltip(
            "Seconds after the card pops in before the bonus stamps in, so the Station's own Score is read first.")]
        private float stampDelaySeconds = 0.45f;

        [SerializeField, Min(0f), Tooltip("Seconds the stamp takes to punch down to its size.")]
        private float stampSeconds = 0.18f;

        [SerializeField, Min(1f), Tooltip("How many times its size the stamp starts at.")]
        private float stampScale = 2.2f;

        [SerializeField, Min(0f), Tooltip("Seconds between the stamp landing and the total starting to count up.")]
        private float countDelaySeconds = 0.12f;

        [SerializeField, Min(0f), Tooltip(
            "Seconds the card's Score takes to count up to the total with the bonus. The card shows for the Clear and "
            + "Quiet beats (about 2 s), so all of it fits before the Go cue.")]
        private float countSeconds = 0.6f;

        [Header("Result card: Score boost offer")]
        [SerializeField, Tooltip("The button that offers the Score boost for watching an ad. {0} is what the boost multiplies by.")]
        private string boostOfferFormat = "WATCH AD: x{0} SCORE";

        [SerializeField, Tooltip("Under the button: how long the boost lasts. {0} is its whole seconds.")]
        private string boostOfferHintFormat = "for {0}s of play";

        [SerializeField] private Color boostButtonColor = new(0.07f, 0.3f, 0.24f, 1f);

        [SerializeField, Min(0f), Tooltip(
            "Seconds the card stays up after the Go cue while it offers the boost, so the button is there long enough to "
            + "reach. The character walks on meanwhile: the offer never holds the game up.")]
        private float offerLingerSeconds = 2.5f;

        [Header("End screen: best Score")]
        [SerializeField, Tooltip("The caption beside the best Score of every Run.")]
        private string bestCaption = "BEST";

        [SerializeField, Tooltip("Stamped on the end screen when the Run beat the best Score.")]
        private string newBestText = "NEW BEST!";

        [SerializeField] private Color newBestColor = new(1f, 0.85f, 0.2f, 1f);

        [SerializeField, Min(0f), Tooltip("Seconds after the end screen shows before NEW BEST! stamps in.")]
        private float newBestDelaySeconds = 0.5f;

        [SerializeField, Min(0f), Tooltip("Pulses per second of NEW BEST! once it has landed.")]
        private float newBestPulsesPerSecond = 1.5f;

        [SerializeField, Min(1f), Tooltip("How many times its size NEW BEST! grows to at the top of a pulse.")]
        private float newBestPulseScale = 1.12f;

        [Header("Result card: Grade")]
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
        public string BoostFormat => boostFormat;
        public string BoostTimerFormat => boostTimerFormat;
        public Color BoostColor => boostColor;
        public float BoostWarningSeconds => boostWarningSeconds;
        public float BoostPulsesPerSecond => boostPulsesPerSecond;
        public float BoostPulseScale => boostPulseScale;
        public float ScoreCountSeconds => scoreCountSeconds;
        public string AccuracyStampFormat => accuracyStampFormat;
        public Color AccuracyStampColor => accuracyStampColor;
        public float StampDelaySeconds => stampDelaySeconds;
        public float StampSeconds => stampSeconds;
        public float StampScale => stampScale;
        public float CountDelaySeconds => countDelaySeconds;
        public float CountSeconds => countSeconds;
        public string BoostOfferFormat => boostOfferFormat;
        public string BoostOfferHintFormat => boostOfferHintFormat;
        public Color BoostButtonColor => boostButtonColor;
        public float OfferLingerSeconds => offerLingerSeconds;
        public string BestCaption => bestCaption;
        public string NewBestText => newBestText;
        public Color NewBestColor => newBestColor;
        public float NewBestDelaySeconds => newBestDelaySeconds;
        public float NewBestPulsesPerSecond => newBestPulsesPerSecond;
        public float NewBestPulseScale => newBestPulseScale;

        /// <summary>The timing of the accuracy bonus's reveal on the card.</summary>
        public BonusReveal ToBonusReveal() => new(stampDelaySeconds, stampSeconds, stampScale, countDelaySeconds, countSeconds);

        /// <summary>The colour of <paramref name="grade"/>'s letter; white when none is authored for it.</summary>
        public Color GradeColor(Scoring.Grade grade)
        {
            var index = (int)grade;
            return index >= 0 && index < gradeColors.Length ? gradeColors[index] : Color.white;
        }
    }
}
