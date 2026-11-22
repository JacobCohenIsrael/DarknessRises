using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Content;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Saving;
using DarknessRises.Game.Shop;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace DarknessRises.Game.Boot
{
    /// <summary>
    /// The boot scene's job: own the <see cref="RunFlow"/> and play its Levels, endlessly (ADR 0005), with the Shop
    /// between Runs. Each time the Run starts a Level (the first of a new Run, the next in the chain, or a debug jump)
    /// the Level scene before it unloads (its <see cref="GameRoot"/> releases the Level's content with it) and the Level
    /// scene loads again, additively through Addressables, to play that Level with the Ramp at its index. Until more
    /// Levels exist, every Level of the chain is the same Level scene.
    /// When the player leaves a Run's end screen, the Level unloads and the Shop scene loads the same way; PLAY there
    /// unloads it and starts a new Run. Only one of the two is ever loaded. A first install goes straight into Level 1;
    /// once a Run has been finished, the app opens in the Shop (<see cref="BootDestinations"/>).
    /// While a scene loads, and until it says it's ready (a Level's content has loaded and its start beat begins; the
    /// Shop is open), the boot's <see cref="LoadingScreenView"/> covers the screen, so a scene is never seen half-made.
    /// Before a destination's scene loads, everything that destination can need (its <see cref="ContentLabels"/> label
    /// and its scene) is downloaded to the device in one measured download, so the loading screen's bar can show the
    /// whole wait in bytes from the start; the loads that follow come from the device. A destination already downloaded
    /// (this session, or on an earlier visit with the same content) isn't downloaded again.
    /// It also owns the player's save, which outlives every Run: it reads it from the device at start, banks each Run's
    /// Credits in it, and writes it when the app goes to the background or quits.
    /// And it owns the sounds that carry across scenes (ADR 0011): the storm plays on the boot's own
    /// <see cref="StormPlayer"/>, which each Level drives while it plays, so it rolls on through a Level change instead of
    /// dying with the old scene, and fades out for the Shop. Its volumes follow the scene that is showing, and its own
    /// ears (<see cref="BootEars"/>) hear it while no scene's are there. Every other sound belongs to its scene and fades
    /// with it before the boot unloads it (<see cref="SceneAudioVolumes.FadeOut"/>), so nothing stops dead in a scene change.
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        // What follows the Level that is playing.
        private enum Next
        {
            // Another Level of the Run: the next in the chain, a jump, or the first of a new Run after Restart.
            Level,

            // The player left the Run's end screen: the Shop, then a new Run.
            Shop,
        }

        [SerializeField] private BootDefinition definition;

        [SerializeField, Tooltip("Keeps the screen black while no Level is showing, and behind the Shop. Off while a Level is, so it costs nothing then.")]
        private Camera loadingCamera;

        [SerializeField, Tooltip("The build version in a corner of the screen, shown for as long as the app runs.")]
        private VersionLabelView versionLabel;

        [SerializeField, Tooltip("Plays the storm bed for as long as the app runs, so it carries on from one Level to the next.")]
        private StormPlayer storm;

        [SerializeField, Tooltip("The boot scene's volumes: they follow the volumes of the Level or Shop that is showing.")]
        private SceneAudioVolumes audioVolumes;

        [SerializeField, Tooltip("Hears the storm while no Level or Shop is loaded to hear it: through every unload and load.")]
        private BootEars ears;

        [SerializeField, Tooltip("Covers the screen while a Level or the Shop loads, until that scene is ready to be seen.")]
        private LoadingScreenView loadingScreen;

        // Owns the boot's lifetime: the app's.
        private CancellationTokenSource lifetime;

        // The scene that is showing (a Level, or the Shop), released when it's left.
        private ContentScope sceneScope;

        // Completed when the Level that is playing is over, with what comes next.
        private UniTaskCompletionSource<Next> levelLeft;

        private readonly IContentSource source = new AddressablesContentSource();
        private readonly AddressablesContentDownloads downloads = new();

        // The destinations (by scene key) whose content is on the device this session: no need to download them again.
        private readonly HashSet<object> downloaded = new();

        // Banks each Run's Credits in the save, however the Run ends.
        private RunBanking banking;

        /// <summary>Raised each time a Level scene has loaded, is the active scene and has been handed the Run.</summary>
        public event Action<GameRoot> LevelLoaded;

        /// <summary>Raised each time a Level scene has unloaded and its content has been released.</summary>
        public event Action LevelUnloaded;

        /// <summary>Raised each time the Shop scene has loaded and is the active scene, before it opens.</summary>
        public event Action<ShopRoot> ShopLoaded;

        /// <summary>Raised each time the Shop scene has unloaded and its content has been released.</summary>
        public event Action ShopUnloaded;

        /// <summary>The Run being played. It lives as long as the app; Restart starts a new Run in it.</summary>
        public RunFlow Run { get; } = new();

        /// <summary>The storm player that outlives every Level.</summary>
        public StormPlayer Storm => storm;

        /// <summary>The boot's AudioListener, on while no other scene's is.</summary>
        public BootEars Ears => ears;

        /// <summary>The loading screen, up while no scene is ready to be seen.</summary>
        public LoadingScreenView LoadingScreen => loadingScreen;

        /// <summary>The player's save: the Credits in the bank and what they bought. Read from the device at start.</summary>
        public PlayerSave Save { get; private set; }

        private void Start()
        {
            versionLabel.Show(definition.VersionLabel, VersionLabel.Text(Application.version, Debug.isDebugBuild));
            loadingScreen.Use(definition.LoadingScreen);

            // Before any sound plays: on the Web, sounds that pile up are held under full scale instead of clipping.
            WebAudioLimiter.Install(definition.WebAudioLimiter);
            loadingScreen.Show();
            lifetime = new CancellationTokenSource();
            Save = PlayerSave.Load(SaveStorages.OfThisPlayer());
            banking = new RunBanking(Run, Save);
            Run.LevelStarted += OnLevelStarted;
            Run.Left += OnRunLeft;
            Run.PhaseChanged += OnRunPhaseChanged;
            RunAsync(lifetime.Token).Forget();
        }

        // On a phone the app may never come back from the background, so what the Run has earned is banked on the way out.
        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                banking?.BankNow();
            }
        }

        private void OnApplicationQuit()
        {
            banking?.BankNow();
        }

        private void OnDestroy()
        {
            banking?.Dispose();
            Run.LevelStarted -= OnLevelStarted;
            Run.Left -= OnRunLeft;
            Run.PhaseChanged -= OnRunPhaseChanged;
            lifetime?.Cancel();
            lifetime?.Dispose();
            sceneScope?.Dispose();
        }

        private async UniTaskVoid RunAsync(CancellationToken cancellationToken)
        {
            var levelKey = ChooseScene().RuntimeKey;
            if (BootDestinations.For(Save) == BootDestination.Shop)
            {
                await ShowShopAsync(cancellationToken);
            }

            Run.StartNewRun();
            while (true)
            {
                var next = await PlayLevelAsync(levelKey, cancellationToken);
                if (next == Next.Shop)
                {
                    await ShowShopAsync(cancellationToken);
                    Run.StartNewRun();
                }
            }
        }

        // Loads the Level scene and plays the Run's current Level in it until the Run moves on, then unloads it.
        private async UniTask<Next> PlayLevelAsync(object levelKey, CancellationToken cancellationToken)
        {
            levelLeft = new UniTaskCompletionSource<Next>();
            var scene = await LoadSceneAsync(levelKey, ContentLabels.Level, cancellationToken);
            loadingCamera.enabled = false;
            var gameRoot = Find<GameRoot>(scene);
            storm.Hush(false);
            gameRoot.Play(Run, Save, storm);

            // The scene is in, but its content (Enemies, sounds) may still be downloading: on a first visit that takes
            // seconds. The Level stays behind the loading screen until it has started.
            if (gameRoot.IsStarted)
            {
                loadingScreen.Hide();
            }
            else
            {
                gameRoot.Started += loadingScreen.Hide;
            }

            audioVolumes.Follow(gameRoot.AudioVolumes);
            LevelLoaded?.Invoke(gameRoot);

            var next = await levelLeft.Task.AttachExternalCancellation(cancellationToken);
            levelLeft = null;

            // On the way to the Shop the storm starts fading out now, through the change, rather than once the Shop shows.
            if (next == Next.Shop)
            {
                storm.Hush(true);
            }

            // Out of whatever callback moved the Run on (often the Level's own) before its scene goes. The storm plays on
            // at the Level's last volumes through the change, heard by the boot's ears while the Level's are gone.
            await UniTask.Yield(cancellationToken);
            audioVolumes.Unfollow();
            await UnloadSceneAsync(scene, gameRoot.AudioVolumes, cancellationToken);
            LevelUnloaded?.Invoke();
            return next;
        }

        // Loads the Shop scene, with nothing of a Level left loaded behind it, and shows it until the player presses PLAY.
        private async UniTask ShowShopAsync(CancellationToken cancellationToken)
        {
            // There is no storm in the Shop: it fades out, and fades back in when a Level with Lightning is played.
            storm.Hush(true);
            var scene = await LoadSceneAsync(definition.ShopScene.RuntimeKey, ContentLabels.Shop, cancellationToken);
            var shop = Find<ShopRoot>(scene);
            audioVolumes.Follow(shop.AudioVolumes);
            ShopLoaded?.Invoke(shop);

            // The Shop stays behind the loading screen until its content has loaded and its screen shows the save.
            shop.Opened += loadingScreen.Hide;
            await shop.RunAsync(Save, cancellationToken);
            audioVolumes.Unfollow();
            await UnloadSceneAsync(scene, shop.AudioVolumes, cancellationToken);
            ShopUnloaded?.Invoke();
        }

        // Downloads what the destination can need, then loads its scene. The label names the rest of what it can need.
        private async UniTask<Scene> LoadSceneAsync(object key, string label, CancellationToken cancellationToken)
        {
            loadingScreen.Show();
            loadingCamera.enabled = true;
            if (!downloaded.Contains(key))
            {
                var keys = new[] { key, label };
                if (await downloads.DownloadAsync(keys, label, loadingScreen, loadingScreen.ProgressPollSeconds, cancellationToken))
                {
                    downloaded.Add(key);
                }
            }

            sceneScope = new ContentScope(source);
            var scene = await sceneScope.LoadSceneAsync(key, cancellationToken);
            SceneManager.SetActiveScene(scene);
            return scene;
        }

        // The scene's own sounds fade to silence first (a Level that ended already has, with its end beat), so nothing in it
        // is still playing to stop dead as it goes (issue #121). The sounds the boot carries across scenes fade on their own.
        private async UniTask UnloadSceneAsync(Scene scene, SceneAudioVolumes sceneVolumes, CancellationToken cancellationToken)
        {
            if (sceneVolumes != null)
            {
                sceneVolumes.FadeOut(definition.SceneSoundFadeOutSeconds);
                await UniTask.WaitUntil(() => sceneVolumes == null || sceneVolumes.IsSilent, cancellationToken: cancellationToken);
            }

            loadingScreen.Show();
            loadingCamera.enabled = true;

            // The scene's ears go with it; the boot's hear on, so a sound that carries across scenes never cuts.
            ears.TakeFrom(scene);
            AuditUnload(scene);
            await sceneScope.EndAsync();
            sceneScope = null;

            // Whatever the scene made at runtime and nothing holds any more goes too.
            await Resources.UnloadUnusedAssets().ToUniTask(cancellationToken: cancellationToken);
        }

        // The Editor and development builds name every source of the scene that would still stop dead as it unloads.
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEBUG")]
        private void AuditUnload(Scene scene)
        {
            var readings = new List<SceneUnloadAudit.Reading>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
                {
                    readings.Add(new SceneUnloadAudit.Reading(
                        $"{scene.name}/{source.name}{(source.clip != null ? $" ({source.clip.name})" : string.Empty)}",
                        source.isActiveAndEnabled && source.isPlaying,
                        source.volume));
                }
            }

            var cuts = new List<SceneUnloadAudit.Reading>();
            SceneUnloadAudit.FindCuts(readings, definition.UnloadLoudnessWarning, cuts);
            foreach (var cut in cuts)
            {
                Debug.LogWarning($"[Audio] {cut.Name} is still playing at volume {cut.Volume:0.000} as its scene unloads: it stops dead.", this);
            }
        }

        private void OnLevelStarted(int _)
        {
            // Before a Level is playing (the first of a Run) there is nothing to leave.
            levelLeft?.TrySetResult(Next.Level);
        }

        // The Level is over (the calm before the far door): the storm fades out through the Level change, rather than
        // rolling on loud over the calm, the black screen and the load. The next Level lets it fade back in.
        private void OnRunPhaseChanged(RunPhase phase)
        {
            if (phase == RunPhase.LevelEnd)
            {
                storm.Hush(true);
            }
        }

        private void OnRunLeft()
        {
            levelLeft?.TrySetResult(Next.Shop);
        }

        private AssetReference ChooseScene()
        {
#if UNITY_EDITOR || DEBUG
            if (Array.IndexOf(Environment.GetCommandLineArgs(), BootDefinition.TestRoomArgument) >= 0)
            {
                return definition.TestRoomScene;
            }
#endif
            return definition.LevelScene;
        }

        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            throw new InvalidOperationException($"The scene '{scene.name}' has no {typeof(T).Name}.");
        }
    }
}
