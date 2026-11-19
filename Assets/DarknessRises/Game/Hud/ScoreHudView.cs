using System;
using DarknessRises.Game.Scoring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The Run's Score and the Combo on the HUD. The Score's digits are written only when the Score changes, with TMP's
    /// number formatting, so nothing is allocated; the Combo's multiplier texts are made once. The Combo shows only
    /// while it multiplies, with a bar that runs down to its timeout. None of it is a raycast target.
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

        private ScoreHudSettings settings;
        private Combo combo;
        private Action<int> onMultiplierChanged;
        private string[] multiplierTexts;
        private float kickSecondsLeft;
        private float shownFill = -1f;

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

            // Every multiplier's text, made once.
            multiplierTexts = new string[Mathf.Max(1, maxMultiplier) + 1];
            for (var i = 0; i < multiplierTexts.Length; i++)
            {
                multiplierTexts[i] = MultiplierPrefix + i;
            }

            onMultiplierChanged ??= OnMultiplierChanged;
            combo.MultiplierChanged += onMultiplierChanged;
            ShowScore(score);
            ShowMultiplier(combo.Multiplier, kick: false);
        }

        /// <summary>Writes the Score. Call it when the Score changes, not every frame.</summary>
        public void ShowScore(int score)
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

        private void Update()
        {
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
                kickSecondsLeft = Mathf.Max(0f, kickSecondsLeft - Time.deltaTime);
                var kick = settings.ComboKickSeconds > 0f ? kickSecondsLeft / settings.ComboKickSeconds : 0f;
                var scale = Mathf.Lerp(1f, settings.ComboKickScale, kick);
                comboValue.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void OnDestroy()
        {
            Unwatch();
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
