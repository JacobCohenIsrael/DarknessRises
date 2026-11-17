using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace DarknessRises.Game.Content
{
    /// <summary>Loads content through Addressables. Scenes load additively and unload when released.</summary>
    public sealed class AddressablesContentSource : IContentSource
    {
        public IContentHandle LoadAsset<T>(object key) where T : class
        {
            return new AssetHandle<T>(Addressables.LoadAssetAsync<T>(key));
        }

        public IContentHandle LoadScene(object key)
        {
            return new SceneHandle(Addressables.LoadSceneAsync(key, LoadSceneMode.Additive));
        }

        private sealed class AssetHandle<T> : IContentHandle
        {
            private AsyncOperationHandle<T> handle;
            private bool released;

            public AssetHandle(AsyncOperationHandle<T> operation)
            {
                handle = operation;
                Loaded = LoadAsync();
            }

            public UniTask<object> Loaded { get; }

            public UniTask ReleaseAsync()
            {
                if (released)
                {
                    return UniTask.CompletedTask;
                }

                released = true;
                if (handle.IsDone)
                {
                    handle.Release();
                }
                else
                {
                    // Let a running load finish first, then let it go.
                    handle.Completed += operation => operation.Release();
                }

                return UniTask.CompletedTask;
            }

            private async UniTask<object> LoadAsync()
            {
                // Throws the operation's exception if it failed.
                return await handle.ToUniTask();
            }
        }

        private sealed class SceneHandle : IContentHandle
        {
            private readonly AsyncOperationHandle<SceneInstance> handle;
            private bool released;

            public SceneHandle(AsyncOperationHandle<SceneInstance> operation)
            {
                handle = operation;
                Loaded = LoadAsync();
            }

            public UniTask<object> Loaded { get; }

            public async UniTask ReleaseAsync()
            {
                if (released || !handle.IsValid())
                {
                    return;
                }

                released = true;
                if (!handle.IsDone)
                {
                    await UniTask.WaitUntil(handle, operation => operation.IsDone);
                }

                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result.Scene.isLoaded)
                {
                    // Unloads the scene and releases its load handle along with it.
                    await Addressables.UnloadSceneAsync(handle).ToUniTask();
                }
                else
                {
                    handle.Release();
                }
            }

            private async UniTask<object> LoadAsync()
            {
                var instance = await handle.ToUniTask();
                return instance.Scene;
            }
        }
    }
}
