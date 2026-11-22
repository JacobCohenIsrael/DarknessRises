using System;
using DarknessRises.Game.Skills;
using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>A Skill and how its button looks: one entry of the <see cref="SkillBarView"/>.</summary>
    public readonly struct SkillButton
    {
        public SkillButton(Skill skill, SkillButtonLook look)
        {
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            Look = look ?? throw new ArgumentNullException(nameof(look));
        }

        public Skill Skill { get; }
        public SkillButtonLook Look { get; }
    }

    /// <summary>
    /// The row of Skill buttons at the bottom left of the HUD, one per Skill the character has, in order. The first
    /// button is built into the scene; the others are copies of it, made when the Skills are shown, so a new Skill
    /// needs no new HUD objects, only its authored <see cref="SkillButtonLook"/>.
    /// </summary>
    public sealed class SkillBarView : MonoBehaviour
    {
        [SerializeField, Tooltip("The first button. The others are copies of it, laid out by the row's layout group.")]
        private SkillHudView template;

        private SkillHudView[] buttons = Array.Empty<SkillHudView>();

        /// <summary>The buttons shown, in the order their Skills were given.</summary>
        public int Count => buttons.Length;

        public SkillHudView this[int index] => buttons[index];

        /// <summary>Shows one button per entry of <paramref name="skills"/>. Once per Level, so it may allocate.</summary>
        public void Show(SkillHudSettings settings, params SkillButton[] skills)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            MakeButtons(skills.Length);
            for (var i = 0; i < skills.Length; i++)
            {
                buttons[i].Show(skills[i].Skill, skills[i].Look, settings);
            }
        }

        private void MakeButtons(int count)
        {
            if (buttons.Length == count)
            {
                return;
            }

            for (var i = 1; i < buttons.Length; i++)
            {
                Destroy(buttons[i].gameObject);
            }

            buttons = new SkillHudView[count];
            template.gameObject.SetActive(count > 0);
            if (count == 0)
            {
                return;
            }

            buttons[0] = template;
            for (var i = 1; i < count; i++)
            {
                buttons[i] = Instantiate(template, template.transform.parent);
                buttons[i].name = $"{template.name} {i + 1}";
            }
        }
    }
}
