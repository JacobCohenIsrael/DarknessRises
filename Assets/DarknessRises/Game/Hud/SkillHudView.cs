using DarknessRises.Game.Controls;
using DarknessRises.Game.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// One Skill button on the HUD, drawn from the Skill's authored <see cref="SkillButtonLook"/>: its icon on a dark
    /// disc with its name under it, a ring of the effect running out while active, a dark sweep with the seconds left
    /// while it recharges, and a short pulse when it's ready again. The key badge shows only on keyboard and mouse.
    /// <see cref="SkillButtonFace"/> decides what shows; this only touches the UI while something changes.
    /// </summary>
    public sealed class SkillHudView : MonoBehaviour
    {
        private const string SecondsFormat = "{0}";

        [SerializeField, Tooltip("Tells whether the player is on touch, which hides the key badge.")]
        private PlayerControls controls;

        [SerializeField, Tooltip("Scaled for the ready pulse.")]
        private RectTransform face;

        [SerializeField, Tooltip("The dark disc behind the icon.")]
        private Image disc;

        [SerializeField, Tooltip("The thin rim around the disc, in the Skill's colour.")]
        private Image rim;

        [SerializeField, Tooltip("The Skill's icon, tinted with its colour.")]
        private Image icon;

        [SerializeField, Tooltip("Radial ring of the effect's remaining time, shown while active.")]
        private Image activeRing;

        [SerializeField, Tooltip("Radial dark sweep over the disc, the share of the cooldown left.")]
        private Image cooldownSweep;

        [SerializeField, Tooltip("Whole seconds of cooldown left.")]
        private TMP_Text cooldownSeconds;

        [SerializeField, Tooltip("The Skill's name under the button.")]
        private TMP_Text nameLabel;

        [SerializeField, Tooltip("The corner badge with the keyboard key, shown only on keyboard and mouse.")]
        private GameObject keyBadge;

        [SerializeField, Tooltip("The key in the badge.")]
        private TMP_Text keyLabel;

        private SkillButtonFace buttonFace;
        private Color readyColor;
        private Color rechargingColor;
        private SkillPhase shownPhase;
        private int shownSeconds = -1;
        private float shownScale = -1f;
        private bool showsBadge;

        public void Show(Skill skill, SkillButtonLook look, SkillHudSettings settings)
        {
            buttonFace = new SkillButtonFace(skill, settings.ReadyPulseSeconds, settings.ReadyPulseScale);
            readyColor = look.Color;
            rechargingColor = look.Color;
            rechargingColor.a *= settings.RechargingAlpha;

            if (look.Icon != null)
            {
                icon.sprite = look.Icon;
            }

            disc.color = settings.DiscColor;
            activeRing.color = look.Color;
            cooldownSweep.color = settings.SweepColor;
            nameLabel.SetText(look.DisplayName);
            keyLabel.SetText(look.KeyBadge);
            showsBadge = !string.IsNullOrEmpty(look.KeyBadge);
            keyBadge.SetActive(showsBadge && !controls.IsTouchActive);
            ShowPhase(skill.Phase);
        }

        private void LateUpdate()
        {
            if (buttonFace == null)
            {
                return;
            }

            // Skills tick in Update with frame time, so this sees this frame's phase.
            buttonFace.Tick(Time.deltaTime);
            if (buttonFace.Phase != shownPhase)
            {
                ShowPhase(buttonFace.Phase);
            }

            switch (shownPhase)
            {
                case SkillPhase.Active:
                    activeRing.fillAmount = buttonFace.ActiveFill;
                    break;
                case SkillPhase.CoolingDown:
                    cooldownSweep.fillAmount = buttonFace.SweepFill;
                    ShowSeconds(buttonFace.SecondsLeft);
                    break;
            }

            var scale = buttonFace.Scale;
            if (!Mathf.Approximately(scale, shownScale))
            {
                shownScale = scale;
                face.localScale = new Vector3(scale, scale, 1f);
            }

            var badge = showsBadge && !controls.IsTouchActive;
            if (keyBadge.activeSelf != badge)
            {
                keyBadge.SetActive(badge);
            }
        }

        private void ShowPhase(SkillPhase phase)
        {
            shownPhase = phase;
            var tint = phase == SkillPhase.CoolingDown ? rechargingColor : readyColor;
            icon.color = tint;
            rim.color = tint;
            activeRing.gameObject.SetActive(phase == SkillPhase.Active);
            cooldownSweep.gameObject.SetActive(phase == SkillPhase.CoolingDown);
            cooldownSeconds.gameObject.SetActive(phase == SkillPhase.CoolingDown);
            activeRing.fillAmount = 1f;
            cooldownSweep.fillAmount = 1f;
            shownSeconds = -1;
        }

        private void ShowSeconds(int seconds)
        {
            if (seconds == shownSeconds)
            {
                return;
            }

            shownSeconds = seconds;

            // TMP's numeric SetText formats without allocating; {0} prints no decimals.
            cooldownSeconds.SetText(SecondsFormat, seconds);
        }
    }
}
