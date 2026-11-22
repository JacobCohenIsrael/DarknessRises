using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Content;
using DarknessRises.Game.Haptics;
using DarknessRises.Game.Saving;
using UnityEngine;
#if UNITY_EDITOR || DEBUG
using DarknessRises.Game.Debugging;
#endif

namespace DarknessRises.Game.Shop
{
    /// <summary>
    /// The Shop scene's composition root. The boot loads the scene through Addressables between Runs and hands it the
    /// player's save; the Shop loads its own content (its sounds) into a <see cref="ContentScope"/> it owns, shows the
    /// screen until the player presses PLAY, and releases that content when the scene goes. Nothing of a Level is
    /// loaded while it's open.
    /// </summary>
    public sealed class ShopRoot : MonoBehaviour
    {
        [SerializeField, Tooltip("The root configuration: the Shop's catalog, the definitions its Upgrades start from, and the Credits' colour.")]
        private GameConfiguration configuration;

        [SerializeField] private ShopView view;

        [SerializeField, Tooltip("The volume sliders, behind the SOUND button.")]
        private VolumePanelView volumePanel;

        [SerializeField, Tooltip("The scene's volumes, which every sound source in it follows.")]
        private SceneAudioVolumes audioVolumes;

        private ContentScope content;

        /// <summary>The Shop's rules over the save it was opened with, once it's open.</summary>
        public ShopKeeper Keeper { get; private set; }

        /// <summary>Everything the open Shop sells.</summary>
        public ShopCatalog Catalog { get; private set; }

        /// <summary>The scene's volumes, which the boot's own sounds follow while the Shop is showing.</summary>
        public SceneAudioVolumes AudioVolumes => audioVolumes;

        /// <summary>Whether the Shop's content has loaded and its screen is up.</summary>
        public bool IsOpen => Keeper != null;

        /// <summary>
        /// Raised once, when the Shop's content has loaded and its screen shows the save: from here it's fit to be seen.
        /// Until then the screen shows what it was built with.
        /// </summary>
        public event Action Opened;

        /// <summary>
        /// Opens the Shop over <paramref name="save"/> and waits until the player presses PLAY. What's bought is
        /// written to the save as it's bought.
        /// </summary>
        public UniTask RunAsync(PlayerSave save, CancellationToken cancellationToken) => RunAsync(save, null, cancellationToken);

        /// <summary>
        /// Opens the Shop over <paramref name="save"/> and waits until the player presses PLAY. Buying vibrates through
        /// <paramref name="haptics"/>, the game's; without them nothing vibrates.
        /// </summary>
        public async UniTask RunAsync(PlayerSave save, HapticsPlayer haptics, CancellationToken cancellationToken)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            if (content != null)
            {
                throw new InvalidOperationException("This Shop is already open.");
            }

            var shop = configuration.Shop;
            content = new ContentScope(new AddressablesContentSource());
            var list = new ContentList();
            list.Add(shop.Screen.BuyClip);
            var audioVolumes = configuration.AudioVolumes;
            list.Add(audioVolumes.SampleClip);
            await list.LoadAsync(content, cancellationToken);

            Catalog = shop.ToCatalog();
            Keeper = new ShopKeeper(save);
            view.Show(configuration, Catalog, Keeper, content.Clip(shop.Screen.BuyClip));
            view.UseHaptics(haptics);
            volumePanel.Use(audioVolumes, content.Clip(audioVolumes.SampleClip), haptics);
#if UNITY_EDITOR || DEBUG
            // Built from code, so the scene doesn't reference it and release builds have none of it.
            ShopDebugMenuView.Create(transform, new SaveDebugCommands(save, shop));
#endif
            Opened?.Invoke();
            await view.PlayAsync(cancellationToken);
        }

        private void OnDestroy()
        {
            // The scene's objects that used this content are gone or going with it.
            content?.Dispose();
        }
    }
}
