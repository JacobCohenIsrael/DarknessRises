using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Content;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace DarknessRises.Game.Boot
{
    /// <summary>
    /// The boot scene's one job: load the Level scene additively through Addressables, hold it while it plays, and
    /// unload it when it ends. A Level ends when it's finished or when the player asks for a new Run; either way the
    /// Level's scene unloads (its <see cref="GameRoot"/> releases the Level's content with it) and a fresh one loads.
    /// Until the Run flow chains Levels, the next Level is the same Level again.
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

        private readonly IContentSource source = new AddressablesContentSource();

        /// <summary>Raised each time a Level scene has loaded and is the active scene.</summary>
        public event Action<GameRoot> LevelLoaded;

        /// <summary>Raised each time a Level scene has unloaded and its content has been released.</summary>
        public event Action LevelUnloaded;

        private void Start()
        {
            lifetime = new CancellationTokenSource();
            RunAsync(lifetime.Token).Forget();
        }

        private void OnDestroy()
        {
            lifetime?.Cancel();
            lifetime?.Dispose();
            levelScope?.Dispose();
        }

        private async UniTaskVoid RunAsync(CancellationToken cancellationToken)
        {
            var sceneKey = ChooseScene().RuntimeKey;
            while (true)
            {
                loadingCamera.enabled = true;
                levelScope = new ContentScope(source);
                var scene = await levelScope.LoadSceneAsync(sceneKey, cancellationToken);
                SceneManager.SetActiveScene(scene);
                loadingCamera.enabled = false;
                var gameRoot = FindGameRoot(scene);
                LevelLoaded?.Invoke(gameRoot);

                await WaitForLevelEndAsync(gameRoot, cancellationToken);

                await levelScope.EndAsync();
                levelScope = null;

                // Whatever the Level made at runtime and nothing holds any more goes too.
                await Resources.UnloadUnusedAssets().ToUniTask(cancellationToken: cancellationToken);
                LevelUnloaded?.Invoke();
            }
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

        private static async UniTask WaitForLevelEndAsync(GameRoot gameRoot, CancellationToken cancellationToken)
        {
            var ended = new UniTaskCompletionSource();
            Action onEnded = () => ended.TrySetResult();
            gameRoot.LevelFinished += onEnded;
            gameRoot.NewRunRequested += onEnded;
            try
            {
                await ended.Task.AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                gameRoot.LevelFinished -= onEnded;
                gameRoot.NewRunRequested -= onEnded;
            }
        }
    }
}
