using System;
using UnityEngine;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// How one Skill's button looks on the HUD: its icon, colour and name, and the key that triggers it, shown as a
    /// corner badge only on keyboard and mouse. Authored on the Skill's definition, so a new Skill gets its button
    /// from data alone.
    /// </summary>
    [Serializable]
    public sealed class SkillButtonLook
    {
        [SerializeField, Tooltip("A flat white icon, tinted with the colour. Drawn by the HUD builder when empty.")]
        private Sprite icon;

        [SerializeField, Tooltip("The icon's and the button's colour while the Skill is ready or active.")]
        private Color color = Color.white;

        [SerializeField, Tooltip("The Skill's name, shown under the button.")]
        private string displayName = "SKILL";

        [SerializeField, Tooltip("The keyboard key that triggers the Skill, shown in the badge on keyboard and mouse. Match it to the Skill's input binding.")]
        private string keyBadge = "";

        public SkillButtonLook()
        {
        }

        public SkillButtonLook(Color color, string displayName, string keyBadge)
        {
            this.color = color;
            this.displayName = displayName;
            this.keyBadge = keyBadge;
        }

        public Sprite Icon => icon;
        public Color Color => color;
        public string DisplayName => displayName;
        public string KeyBadge => keyBadge;
    }
}
