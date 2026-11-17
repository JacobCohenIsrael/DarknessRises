using DarknessRises.Game.Audio;
using DarknessRises.Game.Cameras;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Death;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Health;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Levels;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Player;
using DarknessRises.Game.Randomness;
using DarknessRises.Game.Rooms;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Runs;
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

        [SerializeField, Tooltip("Debug/PrimaryWeapon1-3: equip the Nth Primary. Only read in the Editor and debug builds.")]
        private InputActionReference[] switchPrimaryWeapon;

        private LightningScheduler lightningScheduler;
#if UNITY_EDITOR || DEBUG
        private LightningDebugToggle lightningDebugToggle;
        private PrimaryWeaponDebugSwitch primaryWeaponDebugSwitch;
#endif
        [SerializeField] private RouteWavesView routeWaves;
        [SerializeField] private CharacterSkillsView characterSkills;
        [SerializeField] private SkillHudView flareHud;
        [SerializeField] private SkillHudView slowMoHud;

        // How fast time runs for Enemies. Slow-mo slows it; the global time scale is never touched.
        private readonly EnemyTime enemyTime = new();
        private EnemyAudioPitch enemyAudioPitch;

        [SerializeField, Tooltip("The Level, in a Level scene. Without one (the Test Room), the Route's Waves run in the one Room.")]
        private LevelView level;

        [Header("Health and death")]
        [SerializeField] private CharacterHealthView characterHealth;
        [SerializeField] private HitPointsHudView hitPointsHud;
        [SerializeField] private DeathFlow deathFlow;

        private RunRecord runRecord;
        private bool countsLevelKills;

        private void Awake()
        {
            Apply();
            routeWalker.Walk(
                configuration.Player,
                configuration.FixedAngleCamera.LookAhead,
                character,
                cameraTarget);
            if (level != null)
            {
                level.Run(configuration.Level, routeWalker.Progress, character, enemyTime);
            }
            else
            {
                routeWaves.Run(configuration.Route, routeWalker.Progress, character, enemyTime);
            }

            primaryWeapon.Arm(new[] { configuration.Pistol, configuration.MachineGun, configuration.Flamethrower });
            ammoHud.Show(primaryWeapon.Weapon);
            characterAnimation.Show(configuration.Player.Animation, routeWalker.Progress, primaryWeapon.Weapon);
            ShowHealthAndDeath();
            lightningScheduler = new LightningScheduler(configuration.Lightning.Timing, new SystemRandom());
            var skills = characterSkills.Use(configuration.Flare, configuration.SlowMo, enemyTime);
            flareHud.Show(skills.Flare.Skill);
            slowMoHud.Show(skills.SlowMo.Skill);
        }

        private void OnEnable()
        {
            lightningScheduler.Rumbled += lightning.PlayRumble;
            lightningScheduler.Flashed += lightning.Flash;
            if (level != null)
            {
                level.RoomEntered += EnterRoom;
            }

#if UNITY_EDITOR || DEBUG
            lightningDebugToggle = new LightningDebugToggle(toggleLightning, lightningScheduler, lightning);
            primaryWeaponDebugSwitch = new PrimaryWeaponDebugSwitch(switchPrimaryWeapon, primaryWeapon);
#endif
            primaryWeapon.Switched += OnPrimaryWeaponSwitched;
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

            // By the subscription made, not by Unity's null check: on scene unload the source may already be destroyed.
            if (countsLevelKills)
            {
                level.EnemyDied -= OnEnemyDied;
            }
            else if (runRecord != null)
            {
                routeWaves.EnemyDied -= OnEnemyDied;
            }
        }

        private void ShowHealthAndDeath()
        {
            var health = characterHealth.Show(configuration.Player);
            hitPointsHud.Show(health);
            runRecord = new RunRecord();

            // Kills come from whoever runs the fights: the Level, or the Test Room's Waves.
            countsLevelKills = level != null;
            if (countsLevelKills)
            {
                level.EnemyDied += OnEnemyDied;
            }
            else
            {
                routeWaves.EnemyDied += OnEnemyDied;
            }

            deathFlow.Watch(health, runRecord, characterAnimation, routeWalker, primaryWeapon);
        }

        private void OnEnemyDied(Enemy _)
        {
            runRecord.CountKill();
        }

        private void OnDisable()
        {
            lightningScheduler.Rumbled -= lightning.PlayRumble;
            lightningScheduler.Flashed -= lightning.Flash;
            if (level != null)
            {
                level.RoomEntered -= EnterRoom;
            }

#if UNITY_EDITOR || DEBUG
            lightningDebugToggle.Dispose();
            lightningDebugToggle = null;
            primaryWeaponDebugSwitch.Dispose();
            primaryWeaponDebugSwitch = null;
#endif
            primaryWeapon.Switched -= OnPrimaryWeaponSwitched;
        }

        private void Update()
        {
            lightningScheduler.Tick(Time.deltaTime);
        }

        private void OnPrimaryWeaponSwitched(PrimaryWeapon equipped)
        {
            ammoHud.Show(equipped);
            characterAnimation.ShowWeapon(equipped);
        }

        // The character walked into another Room of the Level: Lightning comes through its windows, if it has any.
        private void EnterRoom(Room entered)
        {
            // Cut a flash short first, so the last Room's windows go dark too.
            lightning.StopFlash();
            room = entered;
            lightning.Show(configuration.Lightning, room);
            lightningScheduler.EnterRoom(room.LetsLightningIn);
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
