using System;
using System.Globalization;
using DarknessRises.Game.Scoring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The result card under the Clear banner: the Station's kills, accuracy and time, the Score it earned and its
    /// Grade. When the fight's accuracy earns a bonus, it stamps in after a moment ("ACCURACY x1.5") and the Score counts
    /// up to the total (<see cref="BonusReveal"/>). While the Run's Score boost is on offer, a button under it offers the
    /// boost for watching an ad; the card then stays up a little longer after the Go cue, and never holds the game up.
    /// Numbers are written with TMP's number formatting when they change; the Grade's letter comes from four fixed
    /// strings. It pops in and fades like the banners (<see cref="BannerPop"/>); only the offer's button takes a tap.
    /// </summary>
    /// <remarks>
    /// It runs on game time, like the banners: a pause (or the ad it offers) freezes it with the Level, so nothing on
    /// it plays out unseen behind the pause menu or the ad.
    /// </remarks>
    public sealed class StationResultView : MonoBehaviour
    {
        private const string CountFormat = "{0}";
        private const string PercentFormat = "{0}%";
        private const string SecondsFormat = "{0:1}s";
        private const string PointsFormat = "+{0}";

        // By Grade, from C to S.
        private static readonly string[] GradeLetters = { "C", "B", "A", "S" };

        [SerializeField] private CanvasGroup group;

        [SerializeField, Tooltip("Holds the whole card. Scaled by the pop.")]
        private RectTransform card;

        [SerializeField, Tooltip("The dark strip behind the card.")]
        private Image band;

        [SerializeField] private TMP_Text killsCaption;
        [SerializeField] private TMP_Text killsValue;
        [SerializeField] private TMP_Text accuracyCaption;
        [SerializeField] private TMP_Text accuracyValue;
        [SerializeField] private TMP_Text timeCaption;
        [SerializeField] private TMP_Text timeValue;
        [SerializeField] private TMP_Text pointsValue;

        [SerializeField, Tooltip("The Grade's letter, large.")]
        private TMP_Text gradeLetter;

        [SerializeField, Tooltip("The accuracy bonus, stamped in beside the Score.")]
        private TMP_Text accuracyStamp;

        [SerializeField, Tooltip("Offers the Score boost for watching an ad. Shown only while the boost is on offer.")]
        private Button boostButton;

        [SerializeField] private TMP_Text boostButtonLabel;

        [SerializeField, Tooltip("Under the button: how long the boost lasts.")]
        private TMP_Text boostHint;

        private readonly CountUp pointsCount = new();
        private ScoreHudSettings settings;
        private BannerPop pop;
        private BonusReveal reveal;
        private ScoreBoost boost;
        private Action onBoostChanged;
        private int total;
        private bool isHiding;
        private bool shownOffer;
        private float shownAlpha = -1f;
        private float shownScale = -1f;
        private float shownStampSize = -1f;

        /// <summary>Raised when the player taps the Score boost offer.</summary>
        public event Action BoostPicked;

        /// <summary>Whether the card is on screen, fading or not.</summary>
        public bool IsVisible => pop != null && pop.IsVisible;

        /// <summary>Whether the card offers the Score boost now.</summary>
        public bool IsOffering => shownOffer;

        /// <summary>Whether the accuracy bonus has stamped in on the card showing.</summary>
        public bool IsStamped => reveal != null && reveal.IsStamped;

        /// <summary>
        /// Lays the card out as <paramref name="hudSettings"/> say; it pops and fades as the banners do. It offers
        /// <paramref name="scoreBoost"/> while that's on offer; null offers nothing.
        /// </summary>
        public void Use(ScoreHudSettings hudSettings, StationBannerSettings bannerSettings, ScoreBoost scoreBoost = null)
        {
            settings = hudSettings ?? throw new ArgumentNullException(nameof(hudSettings));
            if (bannerSettings == null)
            {
                throw new ArgumentNullException(nameof(bannerSettings));
            }

            Unwatch();
            pop = new BannerPop(bannerSettings.PopSeconds, bannerSettings.PopScale, bannerSettings.FadeOutSeconds);
            if (reveal != null)
            {
                reveal.CountStarted -= OnCountStarted;
            }

            reveal = settings.ToBonusReveal();
            reveal.CountStarted += OnCountStarted;
            card.anchoredPosition = settings.CardPosition;
            band.rectTransform.anchoredPosition = new Vector2(0f, settings.CardPosition.y);
            band.color = settings.CardBandColor;
            killsCaption.text = settings.KillsCaption;
            accuracyCaption.text = settings.AccuracyCaption;
            timeCaption.text = settings.TimeCaption;
            foreach (var label in new[] { killsCaption, killsValue, accuracyCaption, accuracyValue, timeCaption, timeValue, pointsValue })
            {
                label.color = settings.CardTextColor;
            }

            accuracyStamp.color = settings.AccuracyStampColor;
            accuracyStamp.enabled = false;

            boost = scoreBoost;
            if (boost != null)
            {
                onBoostChanged ??= ShowOffer;
                boost.Started += onBoostChanged;
                boost.Ended += onBoostChanged;
                boostButtonLabel.SetText(settings.BoostOfferFormat, boost.Factor);
                boostHint.SetText(settings.BoostOfferHintFormat, Mathf.RoundToInt(boost.DurationSeconds));
                boostButton.targetGraphic.color = settings.BoostButtonColor;
            }

            boostButton.onClick.RemoveListener(OnBoostButton);
            boostButton.onClick.AddListener(OnBoostButton);
            pointsCount.ValueChanged -= ShowPoints;
            pointsCount.ValueChanged += ShowPoints;
            ShowOffer();
            Apply();
        }

        /// <summary>Pops the card in with <paramref name="result"/> on it.</summary>
        public void Show(in StationResult result)
        {
            if (pop == null)
            {
                return;
            }

            killsValue.SetText(CountFormat, result.Kills);
            accuracyValue.SetText(PercentFormat, Mathf.RoundToInt(result.Accuracy * 100f));
            timeValue.SetText(SecondsFormat, result.Seconds);
            pointsCount.Set(result.Points);
            ShowPoints(result.Points);
            gradeLetter.text = GradeLetters[Mathf.Clamp((int)result.Grade, 0, GradeLetters.Length - 1)];
            gradeLetter.color = settings.GradeColor(result.Grade);

            // Once per Station, never per frame: the multiplier as the tier authors it ("1.5", "1.25", "2").
            total = result.Total;
            if (result.HasBonus)
            {
                accuracyStamp.text = string.Format(
                    CultureInfo.InvariantCulture,
                    settings.AccuracyStampFormat,
                    result.AccuracyMultiplier.ToString("0.##", CultureInfo.InvariantCulture));
                reveal.Begin();
            }
            else
            {
                reveal.Stop();
            }

            isHiding = false;
            pop.Show();
            shownAlpha = -1f;
            shownStampSize = -1f;
            ShowOffer();
            Apply();
        }

        /// <summary>
        /// Fades the card out: at once, or after the authored linger while it offers the Score boost, so the offer is
        /// there long enough to reach. Asking again while it's already on its way out changes nothing.
        /// </summary>
        public void Hide()
        {
            if (pop == null || isHiding)
            {
                return;
            }

            isHiding = true;
            pop.Hide(shownOffer ? settings.OfferLingerSeconds : 0f);
        }

        private void OnBoostButton()
        {
            if (shownOffer)
            {
                BoostPicked?.Invoke();
            }
        }

        private void OnCountStarted()
        {
            pointsCount.CountTo(total, reveal.CountSeconds);
        }

        private void ShowPoints(int points) => pointsValue.SetText(PointsFormat, points);

        // The offer shows while the boost is on offer: never while one runs (the player's rule).
        private void ShowOffer()
        {
            var offered = boost != null && boost.IsOffered;
            shownOffer = offered;
            if (boostButton.gameObject.activeSelf != offered)
            {
                boostButton.gameObject.SetActive(offered);
            }

            shownAlpha = -1f;
        }

        private void Update()
        {
            if (pop == null)
            {
                return;
            }

            var deltaSeconds = Time.deltaTime;
            pop.Tick(deltaSeconds);
            reveal.Tick(deltaSeconds);
            pointsCount.Tick(deltaSeconds);
            Apply();
        }

        private void OnDestroy()
        {
            Unwatch();
            if (reveal != null)
            {
                reveal.CountStarted -= OnCountStarted;
            }

            pointsCount.ValueChanged -= ShowPoints;
            if (boostButton != null)
            {
                boostButton.onClick.RemoveListener(OnBoostButton);
            }
        }

        private void Unwatch()
        {
            if (boost != null)
            {
                boost.Started -= onBoostChanged;
                boost.Ended -= onBoostChanged;
                boost = null;
            }
        }

        // Touches the canvas only when what it shows changes, so a hidden card costs nothing.
        private void Apply()
        {
            var alpha = pop.Alpha;
            if (!Mathf.Approximately(alpha, shownAlpha))
            {
                shownAlpha = alpha;
                group.alpha = alpha;
                var visible = alpha > 0f;
                if (card.gameObject.activeSelf != visible)
                {
                    card.gameObject.SetActive(visible);
                    band.enabled = visible;
                }

                // Only the offer takes a tap, and only while it can be seen.
                var takesTaps = visible && shownOffer;
                group.blocksRaycasts = takesTaps;
                group.interactable = takesTaps;
            }

            var scale = pop.Scale;
            if (!Mathf.Approximately(scale, shownScale))
            {
                shownScale = scale;
                card.localScale = new Vector3(scale, scale, 1f);
            }

            if (accuracyStamp.enabled != reveal.IsStamped)
            {
                accuracyStamp.enabled = reveal.IsStamped;
            }

            var stampSize = reveal.StampSize;
            if (!Mathf.Approximately(stampSize, shownStampSize))
            {
                shownStampSize = stampSize;
                accuracyStamp.rectTransform.localScale = new Vector3(stampSize, stampSize, 1f);
            }
        }
    }
}
