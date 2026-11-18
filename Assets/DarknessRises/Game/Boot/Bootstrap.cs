using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Content;
using DarknessRises.Game.Runs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace DarknessRises.Game.Boot
{
    /// <summary>
    /// The boot scene's job: own the <see cref="RunFlow"/> and play its Levels, endlessly (ADR 0005). Each time the Run
    /// starts a Level (the first of a new Run, the next in the chain, or a debug jump) the Level scene before it unloads
    /// (its <see cref="GameRoot"/> releases the Level's content with it) and the Level scene loads again, additively
    /// through Addressables, to play that Level with the Ramp at its index. Until more Levels exist, every Level of the
    /// chain is the same Level scene.
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField] private BootDefinition definition;

        [SerializeField, Tooltip("Keeps the screen black while no Level is showing. Off while a Level is, so it costs nothing then.")]
        private Camera loadingCamera;

        // Owns the boot's lifetime: the app's.
        private CancellationTokenSource lifetime;

        // The running Level's scene, released when the Level ends.
        private ContentScope levelScope;

        // Completed when the Run starts another Level, so the current one goes.
        private UniTaskCompletionSource levelChanged;

        private readonly IContentSource source = new AddressablesContentSource();

        /// <summary>Raised each time a Level scene has loaded, is the active scene and has been handed the Run.</summary>
        public event Action<GameRoot> LevelLoaded;

        /// <summary>Raised each time a Level scene has unloaded and its content has been released.</summary>
        public event Action LevelUnloaded;

        /// <summary>The Run being played. It lives as long as the app; Restart starts a new Run in it.</summary>
        public RunFlow Run { get; } = new();

        private void Start()
        {
            lifetime = new CancellationTokenSource();
            Run.LevelStarted += OnLevelStarted;
            RunAsync(lifetime.Token).Forget();
        }

        private void OnDestroy()
        {
            Run.LevelStarted -= OnLevelStarted;
            lifetime?.Cancel();
            lifetime?.Dispose();
            levelScope?.Dispose();
        }

        private async UniTaskVoid RunAsync(CancellationToken cancellationToken)
        {
            var sceneKey = ChooseScene().RuntimeKey;
            Run.StartNewRun();
            while (true)
            {
                levelChanged = new UniTaskCompletionSource();
                loadingCamera.enabled = true;
                levelScope = new ContentScope(source);
                var scene = await levelScope.LoadSceneAsync(sceneKey, cancellationToken);
                SceneManager.SetActiveScene(scene);
                loadingCamera.enabled = false;
                var gameRoot = FindGameRoot(scene);
                gameRoot.Play(Run);
                LevelLoaded?.Invoke(gameRoot);

                await levelChanged.Task.AttachExternalCancellation(cancellationToken);

                // Out of whatever callback started the next Level (often the Level's own) before its scene goes.
                await UniTask.Yield(cancellationToken);
                loadingCamera.enabled = true;
                await levelScope.EndAsync();
                levelScope = null;

                // Whatever the Level made at runtime and nothing holds any more goes too.
                await Resources.UnloadUnusedAssets().ToUniTask(cancellationToken: cancellationToken);
                LevelUnloaded?.Invoke();
            }
        }

        private void OnLevelStarted(int _)
        {
            // Before the first Level loads there is nothing to leave.
            levelChanged?.TrySetResult();
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

        private static GameRoot FindGameRoot(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var gameRoot = root.GetComponentInChildren<GameRoot>(true);
                if (gameRoot != null)
                {
                    return gameRoot;
                }
            }

            throw new InvalidOperationException($"The scene '{scene.name}' has no Game Root.");
        }
    }
}
