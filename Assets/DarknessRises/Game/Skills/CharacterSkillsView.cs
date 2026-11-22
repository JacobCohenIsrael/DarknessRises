using DarknessRises.Game.Audio;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Enemies;
using UnityEngine;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// Runs the character's Skills in the scene: forwards the player's Flare and Adrenaline commands to the
    /// <see cref="CharacterSkills"/> rules, feeds them frame time, shows the Flare's light and plays a faint heartbeat
    /// while Adrenaline runs. The Lure's throw is the <see cref="LureView"/>'s.
    /// </summary>
    public sealed class CharacterSkillsView : MonoBehaviour
    {
        [SerializeField] private PlayerControls controls;
        [SerializeField] private FlareView flare;

        [SerializeField, Tooltip("A 2D looping source in the Sound category for the faint heartbeat while Adrenaline runs.")]
        private AudioSource adrenalineHeartbeat;

        [SerializeField, Tooltip("Throws the Lure.")]
        private LureView lure;

        private bool hasHeartbeat;

        public CharacterSkills Skills { get; private set; }

        /// <summary>The Flare's light on the character.</summary>
        public Light FlareLight => flare.Light;

        /// <summary>
        /// Gives the character fresh, ready Skills built from the definitions, lasting and cooling down by the Run's
        /// timings (Upgrades applied). Adrenaline slows <paramref name="enemyTime"/> and plays
        /// <paramref name="heartbeatClip"/> faintly (none without a clip); the Lure draws <paramref name="livingEnemies"/>
        /// and rattles with <paramref name="rattleClip"/>. Clips come already loaded.
        /// </summary>
        public CharacterSkills Use(
            FlareDefinition flareDefinition,
            AdrenalineDefinition adrenalineDefinition,
            LureDefinition lureDefinition,
            SkillTiming flareTiming,
            SkillTiming adrenalineTiming,
            SkillTiming lureTiming,
            EnemyTime enemyTime,
            LivingEnemies livingEnemies,
            AudioClip heartbeatClip,
            AudioClip rattleClip)
        {
            Skills = new CharacterSkills(
                flareDefinition, adrenalineDefinition, lureDefinition, flareTiming, adrenalineTiming, lureTiming, enemyTime);
            flare.Show(flareDefinition);
            lure.Use(lureDefinition, Skills.Lure, livingEnemies, rattleClip);

            hasHeartbeat = adrenalineHeartbeat != null && heartbeatClip != null;
            if (hasHeartbeat)
            {
                adrenalineHeartbeat.Stop();
                adrenalineHeartbeat.clip = heartbeatClip;
                adrenalineHeartbeat.loop = true;
                adrenalineHeartbeat.playOnAwake = false;
                adrenalineHeartbeat.spatialBlend = 0f;
                adrenalineHeartbeat.SetVolume(adrenalineDefinition.HeartbeatVolume);
            }

            return Skills;
        }

        private void Update()
        {
            if (Skills == null)
            {
                return;
            }

            // Global frame time: Adrenaline only scales Enemy time, so this stays real time for Skills and cooldowns.
            Skills.Tick(Time.deltaTime);

            // Triggered after the tick, so a Skill's first frame isn't cut short.
            if (controls.WasFlarePressedThisFrame)
            {
                Skills.Flare.Skill.TryActivate();
            }

            if (controls.WasAdrenalinePressedThisFrame)
            {
                Skills.Adrenaline.Skill.TryActivate();
            }

            flare.Render(Skills.Flare.Brightness);
            PlayHeartbeat(Skills.Adrenaline.Skill.IsActive);
        }

        // The heartbeat runs from the start of Adrenaline to its end, from the top of the loop each time.
        private void PlayHeartbeat(bool active)
        {
            if (!hasHeartbeat || active == adrenalineHeartbeat.isPlaying)
            {
                return;
            }

            if (active)
            {
                adrenalineHeartbeat.time = 0f;
                adrenalineHeartbeat.Play();
            }
            else
            {
                adrenalineHeartbeat.Stop();
            }
        }
    }
}
