using DarknessRises.Game.Audio;
using DarknessRises.Game.Cameras;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Player;
using DarknessRises.Game.Randomness;
using DarknessRises.Game.Rooms;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Skills;
using DarknessRises.Game.Waves;
using DarknessRises.Game.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR || DEBUG
using DarknessRises.Game.Debugging;
#endif

namespace DarknessRises.Game
{
    /// <summary>
    /// The scene's composition root: holds the root configuration asset and the scene references,
    /// and hands each view the definition it shows.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        [SerializeField] private GameConfiguration configuration;
        [SerializeField] private Transform character;
        [SerializeField] private AuraView aura;
        [SerializeField] private FixedAngleCameraView fixedAngleCamera;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private RouteWalker routeWalker;
        [SerializeField] private PrimaryWeaponView primaryWeapon;
        [SerializeField] private AmmoHudView ammoHud;
        [SerializeField] private CharacterAnimationView characterAnimation;
        [SerializeField] private Room room;
        [SerializeField] private LightningView lightning;

        [SerializeField, Tooltip("Debug/ToggleLightning. Only read in the Editor and debug builds.")]
        private InputActionReference toggleLightning;

        private LightningScheduler lightningScheduler;
#if UNITY_EDITOR || DEBUG
        private LightningDebugToggle lightningDebugToggle;
#endif
        [SerializeField] private RouteWavesView routeWaves;
        [SerializeField] private CharacterSkillsView characterSkills;
        [SerializeField] private SkillHudView flareHud;
        [SerializeField] private SkillHudView slowMoHud;

        // How fast time runs for Enemies. Slow-mo slows it; the global time scale is never touched.
        private readonly EnemyTime enemyTime = new();
        private EnemyAudioPitch enemyAudioPitch;

        private void Awake()
        {
            Apply();
            routeWalker.Walk(
                configuration.Player,
                configuration.FixedAngleCamera.LookAhead,
                character,
                cameraTarget);
            routeWaves.Run(configuration.Route, routeWalker.Progress, character, enemyTime);
            ammoHud.Show(primaryWeapon.Equip(configuration.Pistol));
            characterAnimation.Show(configuration.Player.Animation, routeWalker.Progress, primaryWeapon.Weapon);
            lightningScheduler = new LightningScheduler(configuration.Lightning.Timing, new SystemRandom());
            var skills = characterSkills.Use(configuration.Flare, configuration.SlowMo, enemyTime);
            flareHud.Show(skills.Flare.Skill);
            slowMoHud.Show(skills.SlowMo.Skill);
        }

        private void OnEnable()
        {
            lightningScheduler.Rumbled += lightning.PlayRumble;
            lightningScheduler.Flashed += lightning.Flash;
#if UNITY_EDITOR || DEBUG
            lightningDebugToggle = new LightningDebugToggle(toggleLightning, lightningScheduler, lightning);
#endif
        }

        private void Start()
        {
            lightningScheduler.EnterRoom(room.LetsLightningIn);

            // In Start, not Awake: a mixer parameter set before the mixer's first update can be overwritten by its snapshot.
            enemyAudioPitch = new EnemyAudioPitch(configuration.Audio, enemyTime);
        }

        private void OnDestroy()
        {
            enemyAudioPitch?.Dispose();
        }

        private void OnDisable()
        {
            lightningScheduler.Rumbled -= lightning.PlayRumble;
            lightningScheduler.Flashed -= lightning.Flash;
#if UNITY_EDITOR || DEBUG
            lightningDebugToggle.Dispose();
            lightningDebugToggle = null;
#endif
        }

        private void Update()
        {
            lightningScheduler.Tick(Time.deltaTime);
        }

        /// <summary>Pushes the authored configuration into the scene's views.</summary>
        public void Apply()
        {
            aura.Show(configuration.Player.Aura);
            fixedAngleCamera.Frame(configuration.FixedAngleCamera, cameraTarget);
            lightning.Show(configuration.Lightning, room);
        }
    }
}
