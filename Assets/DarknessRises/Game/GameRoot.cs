using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Ads;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Cameras;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Continues;
using DarknessRises.Game.Content;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Death;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Gestures;
using DarknessRises.Game.Grenades;
using DarknessRises.Game.Health;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Levels;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Pausing;
using DarknessRises.Game.Player;
using DarknessRises.Game.Randomness;
using DarknessRises.Game.Rooms;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Skills;
using DarknessRises.Game.Visibility;
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

        [Header("Touch")]
        [SerializeField, Tooltip("Turns touches into Gestures that press the Gameplay actions.")]
        private GestureInput gestureInput;

        [SerializeField, Tooltip("Bends Shots toward a visible Enemy near the finger, on touch only.")]
        private AimAssistView aimAssist;

        [SerializeField, Tooltip("Debug/ToggleLightning. Only read in the Editor and debug builds.")]
        private InputActionReference toggleLightning;

        [SerializeField, Tooltip("Debug/PrimaryWeapon1-3: equip the Nth Primary. Only read in the Editor and debug builds.")]
        private InputActionReference[] switchPrimaryWeapon;

        [SerializeField, Tooltip("Debug/JumpToRampLevel: jump to the Ramp definition's debug Level. Only read in the Editor and debug builds.")]
        private InputActionReference jumpToRampLevel;

        [SerializeField, Tooltip("Debug/SkipToBoss: put the character straight on the Boss's Station. Only read in the Editor and debug builds.")]
        private InputActionReference skipToBoss;

        private LightningScheduler lightningScheduler;
#if UNITY_EDITOR || DEBUG
        private LightningDebugToggle lightningDebugToggle;
        private PrimaryWeaponDebugSwitch primaryWeaponDebugSwitch;
        private RampLevelDebugJump rampLevelDebugJump;
        private BossSkipDebug bossSkipDebug;
#endif
        [SerializeField] private RouteWavesView routeWaves;
        [SerializeField] private CharacterSkillsView characterSkills;
        [SerializeField] private SkillHudView flareHud;
        [SerializeField] private SkillHudView slowMoHud;

        // How fast time runs for Enemies. Slow-mo slows it; the global time scale is never touched.
        private readonly EnemyTime enemyTime = new();

        // The lights that can reveal an Enemy, for Aim assist's idea of a visible Enemy.
        private readonly SceneLights lights = new();
        private EnemyAudioPitch enemyAudioPitch;

        [SerializeField, Tooltip("The Level, in a Level scene. Without one (the Test Room), the Route's Waves run in the one Room.")]
        private LevelView level;

        [Header("Health and death")]
        [SerializeField] private CharacterHealthView characterHealth;
        [SerializeField] private HitPointsHudView hitPointsHud;
        [SerializeField] private DeathFlow deathFlow;

        [SerializeField, Tooltip("The pause button and menu. Pausing freezes the whole Level; see PauseFlow.")]
        private PauseFlow pauseFlow;

        [Header("Continue")]
        [SerializeField, Tooltip("The placeholder ad watched for a Continue. A real ad network replaces it later.")]
        private FakeAdView continueAd;

        [SerializeField, Tooltip("The burst of light around the character that pushes Enemies back on a Continue.")]
        private ContinueBurstView continueBurst;

        // The Run this scene plays a Level of: handed in by the boot, or the scene's own when it's played on its own.
        private RunFlow run;
        private bool ownsRun;
        private bool reportsToRun;

        // Whether the scene runs a Level (or the Test Room's Waves). Kept from Awake: on unload the Level may be destroyed first.
        private bool runsLevel;

        // The character's Flare, which shows a Boss's next stomp early.
        private Skill flareSkill;

        // The weather's Rumble, and the Boss's growl that stands in for it during its fight.
        private AudioClip rumbleClip;
        private AudioClip bossGrowlClip;

        // Owns the scene's lifetime: the content load, and everything loaded for it.
        private CancellationTokenSource lifetime;
        private ContentScope content;
        private bool isStarted;
        private bool isSubscribed;

        /// <summary>Whether the scene's content has loaded and its gameplay has started.</summary>
        public bool IsStarted => isStarted;

        /// <summary>The Run this scene plays a Level of, once it has one.</summary>
        public RunFlow Run => run;

        // The boot scene is the only scene in Build Settings. While it's loaded, it loaded this scene and hands it the Run.
        private const int BootSceneBuildIndex = 0;

        private static bool IsBooted => SceneManager.GetSceneByBuildIndex(BootSceneBuildIndex).isLoaded;

        private WeaponDefinition[] PrimaryWeapons => new[] { configuration.Pistol, configuration.MachineGun, configuration.Flamethrower };

        private void Awake()
        {
            runsLevel = level != null;
            Apply();
            lifetime = new CancellationTokenSource();
            content = new ContentScope(new AddressablesContentSource());
            StartAsync(lifetime.Token).Forget();
        }

        /// <summary>
        /// Plays the current Level of <paramref name="runFlow"/> in this scene, with the Ramp at its Level index.
        /// Called by the boot right after it loads the scene.
        /// </summary>
        public void Play(RunFlow runFlow)
        {
            if (run != null)
            {
                throw new InvalidOperationException("This scene already plays a Level of a Run.");
            }

            run = runFlow ?? throw new ArgumentNullException(nameof(runFlow));
        }

        // Waits for the Run, loads the content of its current Level, then starts the gameplay.
        // Views stay idle until they're handed what they show.
        private async UniTaskVoid StartAsync(CancellationToken cancellationToken)
        {
            if (run == null && !IsBooted)
            {
                // Played on its own in the Editor: a Run of its own, from Level 1.
                PlayOwnRun();
            }

            await UniTask.WaitUntil(this, root => root.run != null, cancellationToken: cancellationToken);

            var ramp = configuration.Ramp.ToRamp();
            var rampStep = ramp.StepAt(run.LevelIndex);
            var primaryWeapons = PrimaryWeapons;
            await GameContent.For(configuration, primaryWeapons, level != null, rampStep).LoadAsync(content, cancellationToken);

            // At least a frame after Awake: a mixer parameter set before the mixer's first update can be overwritten by its snapshot.
            await UniTask.NextFrame(cancellationToken);

            // Runtime objects (pools, effects) are created in the active scene, so they go when this scene does.
            if (gameObject.scene.isLoaded && SceneManager.GetActiveScene() != gameObject.scene)
            {
                SceneManager.SetActiveScene(gameObject.scene);
            }

            Begin(primaryWeapons, ramp);
        }

        private void PlayOwnRun()
        {
            run = new RunFlow();
            ownsRun = true;
            run.StartNewRun();
            run.LevelStarted += OnOwnRunLevelStarted;
        }

        private void Begin(WeaponDefinition[] primaryWeapons, Ramp<EnemyDefinition> ramp)
        {
            routeWalker.Walk(
                configuration.Player,
                configuration.FixedAngleCamera.LookAhead,
                character,
                cameraTarget);
            if (level != null)
            {
                level.Run(configuration.Level, run.LevelIndex, ramp, routeWalker.Progress, character, enemyTime, content);
            }
            else
            {
                routeWaves.Run(configuration.Route, routeWalker.Progress, character, enemyTime, content);
            }

            lights.Add(aura.Light);
            lights.Add(lightning.KeyLight);
            lights.Add(characterSkills.FlareLight);
            primaryWeapon.Arm(primaryWeapons, content, lights);
            ammoHud.Show(primaryWeapon.Weapon);
            characterAnimation.Show(configuration.Player.Animation, routeWalker.Progress, primaryWeapon.Weapon);
            grenadeHud.Show(grenadeThrower.Equip(configuration.Grenade, characterAnimation, content, lights));
            ShowHealthAndDeath();
            pauseFlow.Watch(new GamePause(run), run);
            rumbleClip = content.Clip(configuration.Lightning.Rumble.Clip);
            var boss = level != null ? configuration.Level.Boss : null;
            bossGrowlClip = boss != null ? content.Clip(boss.GrowlClip) : null;
            lightning.UseRumble(rumbleClip);
            lightningScheduler = new LightningScheduler(configuration.Lightning.Timing, new SystemRandom());
            var skills = characterSkills.Use(configuration.Flare, configuration.SlowMo, enemyTime);
            flareSkill = skills.Flare.Skill;
            flareHud.Show(skills.Flare.Skill);
            slowMoHud.Show(skills.SlowMo.Skill);
            if (gestureInput != null)
            {
                gestureInput.Use(configuration.Gestures);
            }

            if (aimAssist != null)
            {
                aimAssist.Use(configuration.Gestures.AimAssist, lights);
            }

            isStarted = true;

            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            lightningScheduler.EnterRoom(room.LetsLightningIn);
            enemyAudioPitch = new EnemyAudioPitch(configuration.Audio, enemyTime);

            // The Test Room has no beats: it starts walking at once.
            if (level == null)
            {
                run.FinishStartBeat();
            }
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
            if (jumpToRampLevel != null)
            {
                rampLevelDebugJump = new RampLevelDebugJump(jumpToRampLevel, run, configuration.Ramp.DebugJumpLevel);
            }

            if (skipToBoss != null && level != null)
            {
                bossSkipDebug = new BossSkipDebug(skipToBoss, level);
            }
#endif
            primaryWeapon.Switched += OnPrimaryWeaponSwitched;
        }

        private void OnDestroy()
        {
            try
            {
                lifetime.Cancel();
                lifetime.Dispose();
                enemyAudioPitch?.Dispose();

                // By the subscription made, not by Unity's null check: on scene unload the source may already be destroyed.
                if (reportsToRun)
                {
                    ReportToRun(false);
                }

                // The Run outlives this scene when the boot owns it.
                if (ownsRun)
                {
                    run.LevelStarted -= OnOwnRunLevelStarted;
                }
            }
            finally
            {
                // Last, and always, so the Level's memory is released: the scene's objects made from this content
                // are gone or going with it.
                content.Dispose();
            }
        }

        private void ShowHealthAndDeath()
        {
            var health = characterHealth.Show(configuration.Player, content);
            hitPointsHud.Show(health);
            ReportToRun(true);
            var continueDefinition = configuration.Continue;
            continueAd.Use(continueDefinition.FakeAdSeconds);
            continueBurst.Prepare(continueDefinition);
            deathFlow.Watch(
                health, run, characterAnimation, routeWalker, primaryWeapon, continueDefinition, continueAd, continueBurst);
        }

        // What happens in the Level moves the Run on. Kills come from whoever runs the fights: the Level, or the Test Room's Waves.
        private void ReportToRun(bool report)
        {
            reportsToRun = report;
            if (runsLevel)
            {
                if (report)
                {
                    level.EnemyDied += OnEnemyDied;
                    level.StartBeatFinished += OnStartBeatFinished;
                    level.FightStarted += OnFightStarted;
                    level.FightCleared += OnFightCleared;
                    level.BossFightStarted += OnBossFightStarted;
                    level.BossFightEnded += OnBossFightEnded;
                    level.EndBeatStarted += OnEndBeatStarted;
                    level.Finished += OnLevelFinished;
                }
                else
                {
                    level.EnemyDied -= OnEnemyDied;
                    level.StartBeatFinished -= OnStartBeatFinished;
                    level.FightStarted -= OnFightStarted;
                    level.FightCleared -= OnFightCleared;
                    level.BossFightStarted -= OnBossFightStarted;
                    level.BossFightEnded -= OnBossFightEnded;
                    level.EndBeatStarted -= OnEndBeatStarted;
                    level.Finished -= OnLevelFinished;
                }
            }
            else if (report)
            {
                routeWaves.EnemyDied += OnEnemyDied;
            }
            else
            {
                routeWaves.EnemyDied -= OnEnemyDied;
            }
        }

        private void OnEnemyDied(Enemy _) => run.CountKill();

        private void OnStartBeatFinished() => run.FinishStartBeat();

        private void OnFightStarted() => run.BeginStationFight();

        private void OnFightCleared() => run.ClearStationFight();

        // In the Boss fight the Boss decides when Lightning flashes, and its growl is the Rumble.
        private void OnBossFightStarted()
        {
            run.BeginBossFight();
            lightningScheduler.EnterBossDrivenMode();
            level.Boss.FlashRequested += lightningScheduler.RequestFlash;
            level.Boss.WatchFlare(flareSkill);
            lightning.UseRumble(bossGrowlClip != null ? bossGrowlClip : rumbleClip);
        }

        private void OnBossFightEnded()
        {
            StopBossLightning();
        }

        // Back to the weather, if the Boss had Lightning.
        private void StopBossLightning()
        {
            if (lightningScheduler == null || lightningScheduler.Mode != LightningMode.BossDriven)
            {
                return;
            }

            if (level != null && level.Boss != null)
            {
                level.Boss.FlashRequested -= lightningScheduler.RequestFlash;
            }

            lightningScheduler.EnterAmbientMode();
            lightning.UseRumble(rumbleClip);
        }

        private void OnEndBeatStarted() => run.BeginLevelEnd();

        // The character walked out the far door: the Run moves on to the next Level, which the boot loads.
        private void OnLevelFinished() => run.FinishLevel();

        // Played on its own, nobody loads the next Level or a new Run: the boot scene (the only one in Build Settings)
        // starts the game afresh instead.
        private void OnOwnRunLevelStarted(int _)
        {
            SceneManager.LoadScene(BootSceneBuildIndex);
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
            rampLevelDebugJump?.Dispose();
            rampLevelDebugJump = null;
            bossSkipDebug?.Dispose();
            bossSkipDebug = null;
#endif
            primaryWeapon.Switched -= OnPrimaryWeaponSwitched;
        }

        private void Update()
        {
            if (lightningScheduler == null)
            {
                return;
            }

            // The Boss runs on Enemy time, so a flash it asks for is timed on it too: Slow-mo stretches both alike.
            var deltaSeconds = Time.deltaTime;
            lightningScheduler.Tick(lightningScheduler.Mode == LightningMode.BossDriven ? enemyTime.Scaled(deltaSeconds) : deltaSeconds);
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
