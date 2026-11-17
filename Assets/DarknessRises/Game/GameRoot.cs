using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Cameras;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Content;
using DarknessRises.Game.Death;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Grenades;
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
using UnityEngine.SceneManagement;
#if UNITY_EDITOR || DEBUG
using DarknessRises.Game.Debugging;
#endif

namespace DarknessRises.Game
{
    /// <summary>
    /// The scene's composition root: holds the root configuration asset and the scene references,
    /// and hands each view the definition it shows. It first loads the scene's content (Enemy prefabs, Shots, effects
    /// and sounds) through Addressables into a <see cref="ContentScope"/> it owns, and releases it when the scene goes.
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
        [SerializeField] private GrenadeThrowerView grenadeThrower;
        [SerializeField] private GrenadeHudView grenadeHud;

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

        // Owns the scene's lifetime: the content load, and everything loaded for it.
        private CancellationTokenSource lifetime;
        private ContentScope content;
        private bool isStarted;
        private bool isSubscribed;

        /// <summary>Raised once the Level is over and the character has walked out the far door.</summary>
        public event Action LevelFinished;

        /// <summary>
        /// Raised when the player asks for a new Run after dying. Whoever loaded this scene starts it; with no one
        /// listening (the scene was played on its own in the Editor), the game goes back through the boot scene.
        /// </summary>
        public event Action NewRunRequested;

        /// <summary>Whether the scene's content has loaded and its gameplay has started.</summary>
        public bool IsStarted => isStarted;

        private WeaponDefinition[] PrimaryWeapons => new[] { configuration.Pistol, configuration.MachineGun, configuration.Flamethrower };

        private void Awake()
        {
            Apply();
            lifetime = new CancellationTokenSource();
            content = new ContentScope(new AddressablesContentSource());
            StartAsync(lifetime.Token).Forget();
        }

        // Loads the scene's content, then starts the gameplay. Views stay idle until they're handed what they show.
        private async UniTaskVoid StartAsync(CancellationToken cancellationToken)
        {
            var primaryWeapons = PrimaryWeapons;
            await GameContent.For(configuration, primaryWeapons, level != null).LoadAsync(content, cancellationToken);

            // At least a frame after Awake: a mixer parameter set before the mixer's first update can be overwritten by its snapshot.
            await UniTask.NextFrame(cancellationToken);

            // Runtime objects (pools, effects) are created in the active scene, so they go when this scene does.
            if (gameObject.scene.isLoaded && SceneManager.GetActiveScene() != gameObject.scene)
            {
                SceneManager.SetActiveScene(gameObject.scene);
            }

            Begin(primaryWeapons);
        }

        private void Begin(WeaponDefinition[] primaryWeapons)
        {
            routeWalker.Walk(
                configuration.Player,
                configuration.FixedAngleCamera.LookAhead,
                character,
                cameraTarget);
            if (level != null)
            {
                level.Run(configuration.Level, routeWalker.Progress, character, enemyTime, content);
            }
            else
            {
                routeWaves.Run(configuration.Route, routeWalker.Progress, character, enemyTime, content);
            }

            primaryWeapon.Arm(primaryWeapons, content);
            ammoHud.Show(primaryWeapon.Weapon);
            characterAnimation.Show(configuration.Player.Animation, routeWalker.Progress, primaryWeapon.Weapon);
            grenadeHud.Show(grenadeThrower.Equip(configuration.Grenade, characterAnimation, content));
            ShowHealthAndDeath();
            lightning.UseRumble(content.Clip(configuration.Lightning.Rumble.Clip));
            lightningScheduler = new LightningScheduler(configuration.Lightning.Timing, new SystemRandom());
            var skills = characterSkills.Use(configuration.Flare, configuration.SlowMo, enemyTime);
            flareHud.Show(skills.Flare.Skill);
            slowMoHud.Show(skills.SlowMo.Skill);
            isStarted = true;

            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            lightningScheduler.EnterRoom(room.LetsLightningIn);
            enemyAudioPitch = new EnemyAudioPitch(configuration.Audio, enemyTime);
        }

        private void OnEnable()
        {
            if (isStarted)
            {
                Subscribe();
            }
        }

        private void Subscribe()
        {
            if (isSubscribed)
            {
                return;
            }

            isSubscribed = true;
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

        private void OnDestroy()
        {
            lifetime.Cancel();
            lifetime.Dispose();
            enemyAudioPitch?.Dispose();

            // By the subscription made, not by Unity's null check: on scene unload the source may already be destroyed.
            if (countsLevelKills)
            {
                level.EnemyDied -= OnEnemyDied;
                level.Finished -= OnLevelFinished;
            }
            else if (runRecord != null)
            {
                routeWaves.EnemyDied -= OnEnemyDied;
            }

            if (runRecord != null)
            {
                deathFlow.NewRunRequested -= OnNewRunRequested;
            }

            // Last: the scene's objects made from this content are gone or going with it.
            content.Dispose();
        }

        private void ShowHealthAndDeath()
        {
            var health = characterHealth.Show(configuration.Player, content);
            hitPointsHud.Show(health);
            runRecord = new RunRecord();

            // Kills come from whoever runs the fights: the Level, or the Test Room's Waves.
            countsLevelKills = level != null;
            if (countsLevelKills)
            {
                level.EnemyDied += OnEnemyDied;
                level.Finished += OnLevelFinished;
            }
            else
            {
                routeWaves.EnemyDied += OnEnemyDied;
            }

            deathFlow.Watch(health, runRecord, characterAnimation, routeWalker, primaryWeapon);
            deathFlow.NewRunRequested += OnNewRunRequested;
        }

        private void OnEnemyDied(Enemy _)
        {
            runRecord.CountKill();
        }

        private void OnLevelFinished()
        {
            LevelFinished?.Invoke();
        }

        private void OnNewRunRequested()
        {
            if (NewRunRequested != null)
            {
                NewRunRequested.Invoke();
                return;
            }

            // Played on its own: the boot scene (the only one in Build Settings) loads the game afresh.
            SceneManager.LoadScene(0);
        }

        private void OnDisable()
        {
            if (!isSubscribed)
            {
                return;
            }

            isSubscribed = false;
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
            lightningScheduler?.Tick(Time.deltaTime);
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
