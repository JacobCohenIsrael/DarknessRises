using System;
using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// How every Skill button on the HUD behaves, whichever Skill it shows: the cooldown sweep, the dimmed look while
    /// recharging, and the pulse when the Skill is ready again. Each Skill's own icon, colour, name and key are on its
    /// definition (<see cref="Skills.SkillButtonLook"/>).
    /// </summary>
    [Serializable]
    public sealed class SkillHudSettings
    {
        [SerializeField, Tooltip("The radial sweep over the button while it recharges: the share of the cooldown left.")]
        private Color sweepColor = new(0f, 0f, 0f, 0.62f);

        [SerializeField, Range(0f, 1f), Tooltip("How visible the icon and its rim stay while the Skill recharges: 1 is fully lit.")]
        private float rechargingAlpha = 0.35f;

        [SerializeField, Tooltip("The button's dark disc behind the icon.")]
        private Color discColor = new(0.06f, 0.06f, 0.08f, 0.78f);

        [SerializeField, Min(0f), Tooltip("Seconds the button swells and settles back when the Skill is ready again. Zero for no pulse.")]
        private float readyPulseSeconds = 0.35f;

        [SerializeField, Min(1f), Tooltip("How many times its size the button swells to at the peak of the ready pulse.")]
        private float readyPulseScale = 1.18f;

        public Color SweepColor => sweepColor;
        public float RechargingAlpha => rechargingAlpha;
        public Color DiscColor => discColor;
        public float ReadyPulseSeconds => readyPulseSeconds;
        public float ReadyPulseScale => readyPulseScale;
    }
}
