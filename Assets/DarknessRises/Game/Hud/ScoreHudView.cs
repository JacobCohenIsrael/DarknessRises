using System;
using DarknessRises.Game.Scoring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The Run's Score, the Combo and the Score boost on the HUD. The Score's digits are written only when the Score
    /// changes, with TMP's number formatting, so nothing is allocated; a Station's accuracy bonus counts up onto it
    /// arcade-style. The Combo's multiplier texts are made once; it shows only while it multiplies, with a bar that runs
    /// down to its timeout. The boost shows beside the Score while it runs ("x2" and its whole seconds left, written only
    /// when they change), and pulses as it's about to end. None of it is a raycast target.
    /// </summary>
    public sealed class ScoreHudView : MonoBehaviour
    {
        private const string ScoreFormat = "{0}";
        private const string MultiplierPrefix = "x";

        [SerializeField] private TMP_Text scoreCaption;
        [SerializeField] private TMP_Text scoreValue;

        [SerializeField, Tooltip("Holds everything of the Combo. Shown only while the multiplier is above 1.")]
        private GameObject comboRoot;

        [SerializeField] private TMP_Text comboCaption;

        [SerializeField, Tooltip("The multiplier, kicked larger when it steps up.")]
        private TMP_Text comboValue;

        [SerializeField, Tooltip("Runs down as the Combo's timeout does.")]
        private Image comboBar;

        [SerializeField, Tooltip("Holds the Score boost beside the Score. Shown only while a boost runs; pulsed as it ends.")]
        private RectTransform boostRoot;

        [SerializeField, Tooltip("What the boost multiplies by: \"x2\".")]
        private TMP_Text boostValue;

        [SerializeField, Tooltip("The boost's whole seconds left.")]
        private TMP_Text boostTimer;

        private readonly CountUp scoreCount = new();
        private ScoreHudSettings settings;
        private Combo combo;
        private ScoreBoost boost;
        private Action<int> onMultiplierChanged;
        private Action<int> onScoreChanged;
        private string[] multiplierTexts;
        private float kickSecondsLeft;
        private float shownFill = -1f;
        private int shownBoostSeconds = -1;
        private float pulseSeconds;
        private float countDelaySecondsLeft;
        private float countSeconds;
        private int countTarget;
        private bool isCountWaiting;

        /// <summary>The Score showing now: it lags the Run's while a bonus counts up.</summary>
        public int ShownScore => scoreCount.Value;

        /// <summary>
        /// Starts showing <paramref name="score"/> and <paramref name="shownCombo"/>, whose multiplier never goes above
        /// <paramref name="maxMultiplier"/>.
        /// </summary>
        public void Show(ScoreHudSettings hudSettings, Combo shownCombo, int maxMultiplier, int score)
        {
            Unwatch();

            settings = hudSettings ?? throw new ArgumentNullException(nameof(hudSettings));
            combo = shownCombo ?? throw new ArgumentNullException(nameof(shownCombo));
            scoreCaption.text = settings.ScoreCaption;
            scoreCaption.color = settings.ScoreColor;
            scoreValue.color = settings.ScoreColor;
            comboCaption.text = settings.ComboCaption;
            comboCaption.color = settings.ComboColor;
            comboValue.color = settings.ComboColor;
            comboBar.color = settings.ComboColor;
            boostValue.color = settings.BoostColor;
            boostTimer.color = settings.BoostColor;

            // Every multiplier's text, made once.
            multiplierTexts = new string[Mathf.Max(1, maxMultiplier) + 1];
            for (var i = 0; i < multiplierTexts.Length; i++)
            {
                multiplierTexts[i] = MultiplierPrefix + i;
            }

            onMultiplierChanged ??= OnMultiplierChanged;
            combo.MultiplierChanged += onMultiplierChanged;
            onScoreChanged ??= WriteScore;
            scoreCount.ValueChanged -= onScoreChanged;
            scoreCount.ValueChanged += onScoreChanged;
            ShowScore(score);
            ShowMultiplier(combo.Multiplier, kick: false);
            ShowBoostState();
        }

        /// <summary>Shows <paramref name="scoreBoost"/> beside the Score while it runs.</summary>
        public void ShowBoost(ScoreBoost scoreBoost)
        {
            boost = scoreBoost ?? throw new ArgumentNullException(nameof(scoreBoost));
            if (settings != null)
            {
                boostValue.SetText(settings.BoostFormat, boost.Factor);
            }

            ShowBoostState();
        }

        /// <summary>Writes the Score at once. Call it when the Score changes, not every frame.</summary>
        public void ShowScore(int score)
        {
            isCountWaiting = false;
            scoreCount.Set(score);
            WriteScore(score);
        }

        /// <summary>
        /// Counts the Score up to <paramref name="score"/> over <paramref name="overSeconds"/>, after
        /// <paramref name="delaySeconds"/>: a Station's accuracy bonus landing once the card has counted it. A kill
        /// meanwhile writes its Score at once, as ever.
        /// </summary>
        public void CountScoreTo(int score, float delaySeconds, float overSeconds)
        {
            countTarget = score;
            countSeconds = Mathf.Max(0f, overSeconds);
            countDelaySecondsLeft = Mathf.Max(0f, delaySeconds);
            isCountWaiting = true;
            if (countDelaySecondsLeft <= 0f)
            {
                StartCount();
            }
        }

        private void StartCount()
        {
            isCountWaiting = false;
            scoreCount.CountTo(countTarget, countSeconds);
        }

        private void WriteScore(int score)
        {
            scoreValue.SetText(ScoreFormat, score);
        }

        private void OnMultiplierChanged(int multiplier)
        {
            ShowMultiplier(multiplier, kick: multiplier > 1);
        }

        private void ShowMultiplier(int multiplier, bool kick)
        {
            var multiplies = multiplier > 1;
            if (comboRoot.activeSelf != multiplies)
            {
                comboRoot.SetActive(multiplies);
            }

            if (!multiplies)
            {
                return;
            }

            comboValue.text = multiplierTexts[Mathf.Min(multiplier, multiplierTexts.Length - 1)];
            kickSecondsLeft = kick ? settings.ComboKickSeconds : 0f;
            shownFill = -1f;
        }

        // Shown only while a boost runs; its seconds are written when the whole number changes.
        private void ShowBoostState()
        {
            var running = boost != null && boost.IsRunning;
            if (boostRoot.gameObject.activeSelf != running)
            {
                boostRoot.gameObject.SetActive(running);
                shownBoostSeconds = -1;
                pulseSeconds = 0f;
                boostRoot.localScale = Vector3.one;
            }

            if (!running)
            {
                return;
            }

            var whole = Mathf.CeilToInt(boost.SecondsLeft);
            if (whole != shownBoostSeconds)
            {
                shownBoostSeconds = whole;
                boostTimer.SetText(settings.BoostTimerFormat, whole);
            }

            var deltaSeconds = Time.deltaTime;
            if (boost.IsEnding(settings.BoostWarningSeconds))
            {
                pulseSeconds += deltaSeconds;
                var pulse = Mathf.Abs(Mathf.Sin(pulseSeconds * settings.BoostPulsesPerSecond * Mathf.PI));
                var scale = Mathf.Lerp(1f, settings.BoostPulseScale, pulse);
                boostRoot.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void Update()
        {
            if (settings == null)
            {
                return;
            }

            var deltaSeconds = Time.deltaTime;
            if (isCountWaiting)
            {
                countDelaySecondsLeft -= deltaSeconds;
                if (countDelaySecondsLeft <= 0f)
                {
                    StartCount();
                }
            }

            scoreCount.Tick(deltaSeconds);
            ShowBoostState();

            if (combo == null || !comboRoot.activeSelf)
            {
                return;
            }

            var fill = combo.TimeLeftFraction;
            if (!Mathf.Approximately(fill, shownFill))
            {
                shownFill = fill;
                comboBar.fillAmount = fill;
            }

            if (kickSecondsLeft > 0f || comboValue.rectTransform.localScale.x != 1f)
            {
                kickSecondsLeft = Mathf.Max(0f, kickSecondsLeft - deltaSeconds);
                var kick = settings.ComboKickSeconds > 0f ? kickSecondsLeft / settings.ComboKickSeconds : 0f;
                var scale = Mathf.Lerp(1f, settings.ComboKickScale, kick);
                comboValue.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void OnDestroy()
        {
            Unwatch();
            if (onScoreChanged != null)
            {
                scoreCount.ValueChanged -= onScoreChanged;
            }
        }

        private void Unwatch()
        {
            if (combo != null)
            {
                combo.MultiplierChanged -= onMultiplierChanged;
                combo = null;
            }
        }
    }
}
