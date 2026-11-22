using DarknessRises.Game.Audio;
using DarknessRises.Game.Hud;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Boot
{
    /// <summary>
    /// What the boot scene loads: the Level scene the game starts in, the Shop scene shown between Runs, and the Test
    /// Room sandbox. All are Addressables scenes, so the boot scene (the only scene in the build) holds nothing but these references and what
    /// lives as long as the app, such as the build version label and the loading screen. Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Boot Definition", fileName = "BootDefinition", order = 0)]
    public sealed class BootDefinition : ScriptableObject
    {
        /// <summary>Command-line switch that boots into the Test Room instead, in the Editor and debug builds.</summary>
        public const string TestRoomArgument = "-testRoom";

        [SerializeField, Tooltip("The Level scene the game boots into, loaded additively through Addressables.")]
        private AssetReference levelScene = new(string.Empty);

        [SerializeField, Tooltip("The Shop scene, shown between Runs, loaded additively through Addressables and released when a Run starts.")]
        private AssetReference shopScene = new(string.Empty);

        [SerializeField, Tooltip("The Test Room sandbox scene. Booted into with -testRoom, in the Editor and debug builds only.")]
        private AssetReference testRoomScene = new(string.Empty);

        [SerializeField, Tooltip(
            "The build version in a corner of the screen, from boot through every Level. Here rather than in the Game Configuration, " +
            "which is Level content loaded through Addressables, so the label shows before any Level loads.")]
        private VersionLabelSettings versionLabel = new();

        [SerializeField, Tooltip("The loading screen: what covers the screen while a Level or the Shop loads, until it's ready to be seen.")]
        private LoadingScreenSettings loadingScreen = new();

        [SerializeField, Min(0f), Tooltip(
            "Seconds every sound still playing in a Level or the Shop takes to fade to silence before the boot unloads that scene " +
            "(on the way to the Shop, after Restart, after PLAY), so none stops dead with it. A Level that ended has already faded with its end beat.")]
        private float sceneSoundFadeOutSeconds = 0.6f;

        [SerializeField, Range(0f, 1f), Tooltip(
            "Development builds and the Editor: a source of a scene that is still playing louder than this (its effective volume) when the " +
            "scene unloads is logged as a warning, since it stops dead there.")]
        private float unloadLoudnessWarning = 0.01f;

        [SerializeField, Tooltip(
            "The Web player's safety limiter: holds the summed output under full scale where sounds pile up, since the browser has no " +
            "mixer between the game's sounds and its output and clips them hard.")]
        private WebAudioLimiterSettings webAudioLimiter = new();

        public AssetReference LevelScene => levelScene;
        public AssetReference ShopScene => shopScene;
        public AssetReference TestRoomScene => testRoomScene;
        public VersionLabelSettings VersionLabel => versionLabel;
        public LoadingScreenSettings LoadingScreen => loadingScreen;

        /// <summary>Seconds a scene's sounds take to fade to silence before the boot unloads it.</summary>
        public float SceneSoundFadeOutSeconds => sceneSoundFadeOutSeconds;

        /// <summary>The effective volume above which a source still playing when its scene unloads is reported (dev builds).</summary>
        public float UnloadLoudnessWarning => unloadLoudnessWarning;

        /// <summary>The Web player's safety limiter.</summary>
        public WebAudioLimiterSettings WebAudioLimiter => webAudioLimiter;
    }
}
