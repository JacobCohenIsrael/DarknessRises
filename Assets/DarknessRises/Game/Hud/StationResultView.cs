using System;
using DarknessRises.Game.Scoring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The result card under the Clear banner: the Station's kills, accuracy and time, the Score it earned and its
    /// Grade. Its numbers are written once, when it shows, with TMP's number formatting; the Grade's letter comes from
    /// four fixed strings. It pops in and fades like the banners (<see cref="BannerPop"/>) and never takes a tap.
    /// </summary>
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

        private ScoreHudSettings settings;
        private BannerPop pop;
        private float shownAlpha = -1f;
        private float shownScale = -1f;

        /// <summary>Whether the card is on screen, fading or not.</summary>
        public bool IsVisible => pop != null && pop.IsVisible;

        /// <summary>Lays the card out as <paramref name="hudSettings"/> say; it pops and fades as the banners do.</summary>
        public void Use(ScoreHudSettings hudSettings, StationBannerSettings bannerSettings)
        {
            settings = hudSettings ?? throw new ArgumentNullException(nameof(hudSettings));
            if (bannerSettings == null)
            {
                throw new ArgumentNullException(nameof(bannerSettings));
            }

            pop = new BannerPop(bannerSettings.PopSeconds, bannerSettings.PopScale, bannerSettings.FadeOutSeconds);
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
            pointsValue.SetText(PointsFormat, result.Points);
            gradeLetter.text = GradeLetters[Mathf.Clamp((int)result.Grade, 0, GradeLetters.Length - 1)];
            gradeLetter.color = settings.GradeColor(result.Grade);
            pop.Show();
            shownAlpha = -1f;
            Apply();
        }

        /// <summary>Fades the card out.</summary>
        public void Hide()
        {
            pop?.Hide();
        }

        private void Update()
        {
            if (pop == null)
            {
                return;
            }

            pop.Tick(Time.deltaTime);
            Apply();
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
            }

            var scale = pop.Scale;
            if (!Mathf.Approximately(scale, shownScale))
            {
                shownScale = scale;
                card.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
