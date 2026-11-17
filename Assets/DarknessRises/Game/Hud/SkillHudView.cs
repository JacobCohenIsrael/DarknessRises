using DarknessRises.Game.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// Shows one Skill on the HUD: its key while ready, a ring of the effect running out while active, and a
    /// dark sweep with the seconds left while it cools down. Only touches the UI while something changes.
    /// </summary>
    public sealed class SkillHudView : MonoBehaviour
    {
        private const string SecondsFormat = "{0}";

        [SerializeField, Tooltip("The Skill's disc, bright when ready.")]
        private Image disc;

        [SerializeField, Tooltip("Radial ring of the effect's remaining time, shown while active.")]
        private Image activeRing;

        [SerializeField, Tooltip("Radial dark sweep over the disc, the share of the cooldown left.")]
        private Image cooldownSweep;

        [SerializeField, Tooltip("The key that triggers the Skill, shown while ready.")]
        private TMP_Text keyLabel;

        [SerializeField, Tooltip("Whole seconds of cooldown left.")]
        private TMP_Text cooldownSeconds;

        [SerializeField] private Color readyColor = new(1f, 1f, 1f, 0.9f);
        [SerializeField] private Color unavailableColor = new(1f, 1f, 1f, 0.35f);

        private Skill skill;
        private SkillPhase shownPhase;
        private int shownSeconds = -1;

        public void Show(Skill shownSkill)
        {
            skill = shownSkill;
            ShowPhase(skill.Phase);
        }

        private void LateUpdate()
        {
            if (skill == null)
            {
                return;
            }

            if (skill.Phase != shownPhase)
            {
                ShowPhase(skill.Phase);
            }

            switch (shownPhase)
            {
                case SkillPhase.Active:
                    activeRing.fillAmount = skill.ActiveFraction;
                    break;
                case SkillPhase.CoolingDown:
                    cooldownSweep.fillAmount = skill.CooldownFraction;
                    ShowSeconds(Mathf.CeilToInt(skill.CooldownSecondsLeft));
                    break;
            }
        }

        private void ShowPhase(SkillPhase phase)
        {
            shownPhase = phase;
            disc.color = phase == SkillPhase.CoolingDown ? unavailableColor : readyColor;
            keyLabel.gameObject.SetActive(phase != SkillPhase.CoolingDown);
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
            cooldownSeconds.SetText(SecondsFormat, (float)seconds);
        }
    }
}
