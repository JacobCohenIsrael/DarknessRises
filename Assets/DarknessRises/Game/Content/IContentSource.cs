using Cysharp.Threading.Tasks;

namespace DarknessRises.Game.Content
{
    /// <summary>
    /// Where content comes from: starts loading assets and scenes by key. In the game that's Addressables
    /// (<see cref="AddressablesContentSource"/>); a <see cref="ContentScope"/> decides what stays loaded and for how long.
    /// </summary>
    public interface IContentSource
    {
        /// <summary>Starts loading the asset at <paramref name="key"/> as a <typeparamref name="T"/>.</summary>
        IContentHandle LoadAsset<T>(object key) where T : class;

        /// <summary>Starts loading the scene at <paramref name="key"/>, added to the scenes already loaded.</summary>
        IContentHandle LoadScene(object key);
    }

    /// <summary>One load from an <see cref="IContentSource"/>. Whoever started it releases it, exactly once.</summary>
    public interface IContentHandle
    {
        /// <summary>
        /// Completes with what was loaded (a scene as a <see cref="UnityEngine.SceneManagement.Scene"/>), or throws if
        /// loading failed. Awaited once.
        /// </summary>
        UniTask<object> Loaded { get; }

        /// <summary>
        /// Lets go of what was loaded; a scene is unloaded. Safe while the load is still running: it's let go as soon as
        /// it finishes. Also releases a failed load.
        /// </summary>
        UniTask ReleaseAsync();
    }
}
