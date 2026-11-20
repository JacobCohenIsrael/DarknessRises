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
using DarknessRises.Game.Credits;
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
using DarknessRises.Game.Saving;
using DarknessRises.Game.Scoring;
using DarknessRises.Game.Shop;
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
        [SerializeField, Tooltip("The walking and Station cameras, and the point the walking camera follows.")]
        private RideCameraView rideCamera;

        [SerializeField] private RouteWalker routeWalker;
        [SerializeField] private PrimaryWeaponView primaryWeapon;
        [SerializeField] private AmmoHudView ammoHud;

        [SerializeField, Tooltip("The radial meter by the character while the Primary weapon reloads.")]
        private ReloadMeterView reloadMeter;

        [SerializeField, Tooltip("The sound of a reload, ending as the reload finishes.")]
        private ReloadSoundView reloadSound;

        [SerializeField, Tooltip("The sound of the Primary weapons firing: Gunshots and Firing loops.")]
        private GunshotSoundView gunshotSound;

        [SerializeField, Tooltip("The Boss's name and hit points across the top of the HUD during a Boss fight.")]
        private BossHealthBarView bossHealthBar;

        [SerializeField, Tooltip("The Ready and Clear banners and the Go cue of the Station rhythm.")]
        private StationBannerView stationBanner;

        [SerializeField, Tooltip("The reticle where the player aims.")]
        private CrosshairView crosshair;

        [SerializeField, Tooltip("The first-run hint over the first Enemy.")]
        private ShootHintView shootHint;

        [Header("Score")]
        [SerializeField, Tooltip("The Run's Score and the Combo.")]
        private ScoreHudView scoreHud;

        [SerializeField, Tooltip("The points that pop up over a kill and the hit marker where a Shot hurts an Enemy.")]
        private ScorePopupsView scorePopups;

        [SerializeField, Tooltip("The result card under the Clear banner.")]
        private StationResultView stationResult;

        [SerializeField] private CharacterAnimationView characterAnimation;
        [SerializeField] private Room room;
        [SerializeField] private LightningView lightning;
        [SerializeField] private GrenadeThrowerView grenadeThrower;
        [SerializeField] private GrenadeHudView grenadeHud;

        [SerializeField, Tooltip("The touch Grenade button: tap to arm, tap the Room to throw. Hidden on keyboard and mouse.")]
        private GrenadeButtonView grenadeButton;

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
        private DebugMenuView debugMenu;

        // Debug builds carry every Primary, so the debug switch can try them all.
        private const bool CarriesEveryPrimary = true;
#else
        private const bool CarriesEveryPrimary = false;
#endif
        [SerializeField] private RouteWavesView routeWaves;
        [SerializeField] private CharacterSkillsView characterSkills;
        [SerializeField] private SkillHudView flareHud;
        [SerializeField] private SkillHudView slowMoHud;

        // How fast time runs for Enemies. Slow-mo slows it; the global time scale is never touched.
        private readonly EnemyTime enemyTime = new();

        // The lights that can reveal an Enemy, for Aim assist's idea of a visible Enemy.
        private readonly SceneLights lights = new();

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

        [Header("Credits")]
        [SerializeField, Tooltip("The end screen, which shows the Credits a Run earned and offers the end-of-Run ad.")]
        private EndScreenView endScreen;

        [SerializeField, Tooltip("The placeholder ad watched to multiply a Run's Credits. A real ad network replaces it later.")]
        private FakeAdView endOfRunAd;

        // The Run this scene plays a Level of: handed in by the boot, or the scene's own when it's played on its own.
        private RunFlow run;
        private bool ownsRun;

        // The player's save, where Credits are banked: handed in by the boot with the Run, or a throwaway one kept in
        // memory when the scene is played on its own.
        private PlayerSave save;
        private RunBanking ownBanking;

        // What a cleared Level pays on top of its kills.
        private LevelClearBonus levelClearBonus;
        private bool reportsToRun;

        // Whether the scene runs a Level (or the Test Room's Waves). Kept from Awake: on unload the Level may be destroyed first.
        private bool runsLevel;

        // The character's Flare, which shows a Boss's next stomp early.
        private Skill flareSkill;

        // The beats of each stop at a Station. Whoever runs the fights drives it; the banner and the character show it.
        private StationRhythm stationRhythm;

        // Keeps the Level's Score: the Combo, what each kill earns and each Station's tally. The Run keeps the total.
        private ScoreKeeper scoreKeeper;
        private CharacterHealth scoredHealth;


        // Owns the scene's lifetime: the content load, and everything loaded for it.
        private CancellationTokenSource lifetime;
        private ContentScope content;
        private bool isStarted;
        private bool isSubscribed;

        /// <summary>Whether the scene's content has loaded and its gameplay has started.</summary>
        public bool IsStarted => isStarted;

        /// <summary>The Run this scene plays a Level of, once it has one.</summary>
        public RunFlow Run => run;

        /// <summary>The beats of each stop at a Station, once the gameplay has started.</summary>
        public StationRhythm StationRhythm => stationRhythm;

        // The boot scene is the only scene in Build Settings. While it's loaded, it loaded this scene and hands it the Run.
        private const int BootSceneBuildIndex = 0;

        private static bool IsBooted => SceneManager.GetSceneByBuildIndex(BootSceneBuildIndex).isLoaded;

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
        /// The Run's Credits are banked in <paramref name="playerSave"/>, by whoever owns both.
        /// Called by the boot right after it loads the scene.
        /// </summary>
        public void Play(RunFlow runFlow, PlayerSave playerSave)
        {
            if (run != null)
            {
                throw new InvalidOperationException("This scene already plays a Level of a Run.");
            }

            save = playerSave ?? throw new ArgumentNullException(nameof(playerSave));
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
            // One Primary per Run: the one chosen in the Shop. Only what the character carries is loaded.
            var chosenPrimary = configuration.Shop.PrimaryWeapon(new ShopKeeper(save));
            var primaryWeapons = CarriedPrimaries.For(
                configuration.Shop.ToPrimaryWeapons(), chosenPrimary, CarriesEveryPrimary, out var equippedPrimary);
            await GameContent.For(configuration, primaryWeapons, level != null, rampStep).LoadAsync(content, cancellationToken);

            // Runtime objects (pools, effects) are created in the active scene, so they go when this scene does.
            if (gameObject.scene.isLoaded && SceneManager.GetActiveScene() != gameObject.scene)
            {
                SceneManager.SetActiveScene(gameObject.scene);
            }

            Begin(primaryWeapons, equippedPrimary, ramp);
        }

        // Nothing is written to the device: the real save belongs to the boot.
        private void PlayOwnRun()
        {
            save = PlayerSave.Load(new MemorySaveStorage());
            run = new RunFlow();
            ownsRun = true;
            ownBanking = new RunBanking(run, save);
            run.StartNewRun();
            run.LevelStarted += OnOwnRunLevelStarted;
            run.Left += OnOwnRunLeft;
        }

        private void Begin(WeaponDefinition[] primaryWeapons, int equippedPrimary, Ramp<EnemyDefinition> ramp)
        {
            routeWalker.Walk(configuration.Player, character, configuration.StationRhythm.ToWalkEasing());
            stationRhythm = new StationRhythm(configuration.StationRhythm.ToTiming());
            rideCamera.Ride(routeWalker.Progress, stationRhythm, room);
            if (stationBanner != null)
            {
                stationBanner.Watch(stationRhythm, configuration.StationBanners, routeWalker.Progress, content);
            }

            if (level != null)
            {
                level.Run(configuration.Level, run.LevelIndex, ramp, routeWalker.Progress, character, enemyTime, content, stationRhythm);
            }
            else
            {
                routeWaves.Run(configuration.Route, routeWalker.Progress, character, enemyTime, content, stationRhythm);
            }

            lights.Add(aura.Light);
            lights.Add(lightning.KeyLight);
            lights.Add(characterSkills.FlareLight);
            var loadout = primaryWeapon.Arm(primaryWeapons, content, lights);
            loadout.Switch(equippedPrimary);
            gunshotSound.Use(configuration.GunshotVoices, primaryWeapons, loadout, content);
            ammoHud.Show(primaryWeapon.Weapon);
            reloadMeter.Use(configuration.ReloadFeedback, character);
            reloadMeter.Show(primaryWeapon.Weapon);
            reloadSound.Use(configuration.ReloadFeedback, content);
            reloadSound.Show(primaryWeapon.Weapon);
            if (crosshair != null)
            {
                crosshair.Show(configuration.Crosshair, primaryWeapon.Weapon);
            }

            if (shootHint != null)
            {
                shootHint.Use(configuration.ShootHint);
            }

            characterAnimation.Show(configuration.Player.Animation, routeWalker.Progress, primaryWeapon.Weapon, stationRhythm);
            var grenadeCharges = grenadeThrower.Equip(configuration.Grenade, characterAnimation, content, lights);
            grenadeHud.Show(grenadeCharges);
            if (grenadeButton != null)
            {
                grenadeButton.Show(grenadeCharges, configuration.Gestures.GrenadeButton);
            }
            ShowHealthAndDeath();
            KeepScore();
            pauseFlow.Watch(new GamePause(run), run);
            // The storm that swells after every flash, and the Boss's growl that warns of its flashes.
            var lightningRandom = new SystemRandom();
            lightning.UseStorm(content.Clip(configuration.Lightning.Storm.Clip), lightningRandom, enemyTime);
            var boss = level != null ? configuration.Level.Boss : null;
            if (bossHealthBar != null && level != null && level.Boss != null)
            {
                bossHealthBar.Watch(level.Boss, configuration.BossHealthBar);
            }

            if (boss != null)
            {
                lightning.UseWarning(content.Clip(boss.GrowlClip), boss.GrowlClipStartSeconds);
            }

            lightningScheduler = new LightningScheduler(configuration.Lightning.Timing, lightningRandom);
            var skills = characterSkills.Use(configuration.Flare, configuration.SlowMo, enemyTime);
            flareSkill = skills.Flare.Skill;
            flareHud.Show(skills.Flare.Skill);
            slowMoHud.Show(skills.SlowMo.Skill);
            if (gestureInput != null)
            {
                gestureInput.Use(configuration.Gestures);
                if (grenadeButton != null)
                {
                    gestureInput.UseGrenadeButton(grenadeButton.Rules);
                }
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
            lightningScheduler.Warned += lightning.PlayWarning;
            lightningScheduler.Flashed += lightning.Flash;
            lightningScheduler.Rumbled += lightning.PlayRumble;
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

            // Built from code, so no scene references it and release builds have none of it.
            debugMenu = DebugMenuView.Create(
                transform,
                PrimaryWeaponLabels(),
                primaryWeaponDebugSwitch,
                () => characterHealth.GodMode,
                lightningDebugToggle,
                rampLevelDebugJump,
                bossSkipDebug,
                pauseFlow.Pause);
#endif
            primaryWeapon.Switched += OnPrimaryWeaponSwitched;
        }

        private void OnDestroy()
        {
            try
            {
                lifetime.Cancel();
                lifetime.Dispose();

                // By the subscription made, not by Unity's null check: on scene unload the source may already be destroyed.
                if (reportsToRun)
                {
                    ReportToRun(false);
                }

                StopKeepingScore();

                // The Run outlives this scene when the boot owns it.
                if (ownsRun)
                {
                    run.LevelStarted -= OnOwnRunLevelStarted;
                    run.Left -= OnOwnRunLeft;
                    ownBanking.Dispose();
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
            ShowCredits();
        }

        // Credits are earned in the Level and shown on the end screen, which also offers the end-of-Run ad.
        private void ShowCredits()
        {
            var credits = configuration.Credits;
            levelClearBonus = credits.LevelClearBonus.ToBonus();
            endOfRunAd.Use(credits.EndOfRunAd.FakeAdSeconds);
            endScreen.Use(credits, endOfRunAd, save, content.Clip(credits.EndScreen.BonusClip));
        }

        // Score comes from what the Level's fights report: every Shot's hit or miss drives the Combo, every kill earns
        // points, and each Station's fight is tallied from its first Wave to its clear, then graded on its result card.
        private void KeepScore()
        {
            scoreKeeper = configuration.Score.ToKeeper();
            if (scoreHud != null)
            {
                scoreHud.Show(configuration.ScoreHud, scoreKeeper.Combo, configuration.Score.ComboMaxMultiplier, run.Record.Score);
            }

            if (scorePopups != null)
            {
                scorePopups.Use(configuration.ScoreHud);
            }

            if (stationResult != null)
            {
                stationResult.Use(configuration.ScoreHud, configuration.StationBanners);
            }

            scoredHealth = characterHealth.Health;
            scoredHealth.Hurt += OnCharacterHurt;
            primaryWeapon.ShotLanded += OnShotLanded;
            stationRhythm.BeatChanged += OnStationBeatChanged;
        }

        private void StopKeepingScore()
        {
            if (scoreKeeper == null)
            {
                return;
            }

            scoredHealth.Hurt -= OnCharacterHurt;
            primaryWeapon.ShotLanded -= OnShotLanded;
            stationRhythm.BeatChanged -= OnStationBeatChanged;
            scoreKeeper = null;
        }

        private void OnShotLanded(bool hurtEnemy, Vector3 point)
        {
            if (!hurtEnemy)
            {
                scoreKeeper.ShotMissed();
                return;
            }

            scoreKeeper.ShotHit();
            if (scorePopups != null)
            {
                scorePopups.Mark(point);
            }
        }

        private void OnCharacterHurt() => scoreKeeper.CharacterHit();

        // The Station's tally runs from the start of its fight to its clear, when its result card shows.
        private void OnStationBeatChanged(StationBeat beat)
        {
            switch (beat)
            {
                case StationBeat.Fight:
                    scoreKeeper.BeginStation();
                    break;
                case StationBeat.Clear:
                    var result = scoreKeeper.EndStation(stationRhythm.FightSeconds);
                    if (stationResult != null)
                    {
                        stationResult.Show(result);
                    }

                    break;
                case StationBeat.Go:
                case StationBeat.Walking:
                    if (stationResult != null)
                    {
                        stationResult.Hide();
                    }

                    break;
            }
        }

        // A kill earns its Enemy's points times the Combo, shown floating up from where it died, and its Credits, which
        // the Combo doesn't touch.
        private void OnEnemyKilled(EnemyView enemy)
        {
            AwardScore(enemy.Definition.ScorePoints, enemy.transform.position);
            run.EarnCredits(enemy.Definition.Credits);
        }

        private void AwardScore(int basePoints, Vector3 position)
        {
            if (scoreKeeper == null)
            {
                return;
            }

            var multiplied = scoreKeeper.Combo.Multiplier > 1;
            var earned = scoreKeeper.EnemyKilled(basePoints);
            if (earned <= 0 || !run.AddScore(earned))
            {
                return;
            }

            if (scoreHud != null)
            {
                scoreHud.ShowScore(run.Record.Score);
            }

            if (scorePopups != null)
            {
                scorePopups.Pop(position, earned, multiplied);
            }
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
                    level.EnemySpawned += OnEnemySpawned;
                    level.EnemyKilled += OnEnemyKilled;
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
                    level.EnemySpawned -= OnEnemySpawned;
                    level.EnemyKilled -= OnEnemyKilled;
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
                routeWaves.EnemySpawned += OnEnemySpawned;
                routeWaves.EnemyKilled += OnEnemyKilled;
            }
            else
            {
                routeWaves.EnemyDied -= OnEnemyDied;
                routeWaves.EnemySpawned -= OnEnemySpawned;
                routeWaves.EnemyKilled -= OnEnemyKilled;
            }
        }

        private void OnEnemyDied(Enemy _) => run.CountKill();

        // The first Enemy of a Run gets the shoot hint over it, if the hint is due.
        private void OnEnemySpawned(EnemyView enemy)
        {
            if (shootHint != null)
            {
                shootHint.Offer(enemy, run.LevelIndex, primaryWeapon.Weapon);
            }
        }

        private void OnStartBeatFinished() => run.FinishStartBeat();

        private void OnFightStarted() => run.BeginStationFight();

        private void OnFightCleared() => run.ClearStationFight();

        // In the Boss fight the Boss decides when Lightning flashes, and its growl warns of each flash.
        private void OnBossFightStarted()
        {
            // The fight places the character (Relocate) until it's back on the Boss Station.
            routeWalker.HandOverPlacement();
            run.BeginBossFight();
            lightningScheduler.EnterBossDrivenMode();
            level.Boss.FlashRequested += lightningScheduler.RequestFlash;
            level.Boss.WatchFlare(flareSkill);
        }

        private void OnBossFightEnded()
        {
            routeWalker.TakeBackPlacement();
            StopBossLightning();

            // Beating the Boss is the Level's biggest kill, and it clears the Level, which pays its bonus.
            var boss = configuration.Level.Boss;
            AwardScore(boss.ScorePoints, character.position);
            run.EarnCredits(boss.Credits);
            run.EarnCredits(levelClearBonus.For(run.LevelIndex));
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
        }

        // The Level is over: a reload under way stops being heard, fading out rather than cut dead.
        private void OnEndBeatStarted()
        {
            reloadSound.Interrupt();
            run.BeginLevelEnd();
        }

        // The character walked out the far door: the Run moves on to the next Level, which the boot loads.
        private void OnLevelFinished() => run.FinishLevel();

        // Played on its own, nobody loads the next Level, a new Run or the Shop: the boot scene (the only one in Build
        // Settings) starts the game afresh instead.
        private void OnOwnRunLevelStarted(int _)
        {
            SceneManager.LoadScene(BootSceneBuildIndex);
        }

        private void OnOwnRunLeft()
        {
            SceneManager.LoadScene(BootSceneBuildIndex);
        }

#if UNITY_EDITOR || DEBUG
        // The touch debug menu's labels for the Primaries, in the order they're carried: the Shop's.
        private string[] PrimaryWeaponLabels()
        {
            var weapons = configuration.Shop.Weapons;
            var labels = new string[weapons.Count];
            for (var i = 0; i < labels.Length; i++)
            {
                labels[i] = weapons[i].DisplayName;
            }

            return labels;
        }
#endif

        private void OnDisable()
        {
            if (!isSubscribed)
            {
                return;
            }

            isSubscribed = false;
            lightningScheduler.Warned -= lightning.PlayWarning;
            lightningScheduler.Flashed -= lightning.Flash;
            lightningScheduler.Rumbled -= lightning.PlayRumble;
            if (level != null)
            {
                level.RoomEntered -= EnterRoom;
            }

#if UNITY_EDITOR || DEBUG
            if (debugMenu != null)
            {
                Destroy(debugMenu.gameObject);
                debugMenu = null;
            }

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
            scoreKeeper?.Tick(deltaSeconds);
            lightningScheduler.Tick(lightningScheduler.Mode == LightningMode.BossDriven ? enemyTime.Scaled(deltaSeconds) : deltaSeconds);
            lightning.SetStormAudible(lightningScheduler.IsStormRunning);
        }

        private void OnPrimaryWeaponSwitched(PrimaryWeapon equipped)
        {
            ammoHud.Show(equipped);
            if (crosshair != null)
            {
                crosshair.ShowWeapon(equipped);
            }

            reloadMeter.Show(equipped);
            reloadSound.Show(equipped);
            characterAnimation.ShowWeapon(equipped);
        }

        // The character walked into another Room of the Level: Lightning comes through its windows, if it has any.
        private void EnterRoom(Room entered)
        {
            // Cut a flash short first, so the last Room's windows go dark too.
            lightning.StopFlash();
            room = entered;
            rideCamera.EnterRoom(entered);
            lightning.Show(configuration.Lightning, room);
            lightningScheduler.EnterRoom(room.LetsLightningIn);
        }

        /// <summary>Pushes the authored configuration into the scene's views.</summary>
        public void Apply()
        {
            aura.Show(configuration.Player.Aura);
            rideCamera.Frame(configuration.RideCamera);
            lightning.Show(configuration.Lightning, room);
        }
    }
}
