using DarknessRises.Game.Controls;
using DarknessRises.Game.Enemies;
using UnityEngine;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// Runs the character's Skills in the scene: forwards the player's Flare and Slow-mo commands to the
    /// <see cref="CharacterSkills"/> rules, feeds them frame time and shows the Flare's light.
    /// </summary>
    public sealed class CharacterSkillsView : MonoBehaviour
    {
        [SerializeField] private PlayerControls controls;
        [SerializeField] private FlareView flare;

        public CharacterSkills Skills { get; private set; }

        /// <summary>The Flare's light on the character.</summary>
        public Light FlareLight => flare.Light;

        /// <summary>Gives the character fresh, ready Skills built from the definitions. Slow-mo slows <paramref name="enemyTime"/>.</summary>
        public CharacterSkills Use(FlareDefinition flareDefinition, SlowMoDefinition slowMoDefinition, EnemyTime enemyTime)
        {
            Skills = new CharacterSkills(flareDefinition, slowMoDefinition, enemyTime);
            flare.Show(flareDefinition);
            return Skills;
        }

        private void Update()
        {
            if (Skills == null)
            {
                return;
            }

            // Global frame time: Slow-mo only scales Enemy time, so this stays real time for Skills and cooldowns.
            Skills.Tick(Time.deltaTime);

            // Triggered after the tick, so a Skill's first frame isn't cut short.
            if (controls.WasFlarePressedThisFrame)
            {
                Skills.Flare.Skill.TryActivate();
            }

            if (controls.WasSlowMoPressedThisFrame)
            {
                Skills.SlowMo.Skill.TryActivate();
            }

            flare.Render(Skills.Flare.Brightness);
        }
    }
}
