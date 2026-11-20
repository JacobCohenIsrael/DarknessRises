using DarknessRises.Game.Hud;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Boot
{
    /// <summary>
    /// What the boot scene loads: the Level scene the game starts in, the Shop scene shown between Runs, and the Test
    /// Room sandbox. All are Addressables scenes, so the boot scene (the only scene in the build) holds nothing but these references and what
    /// lives as long as the app, such as the build version label. Never changed at runtime.
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

        public AssetReference LevelScene => levelScene;
        public AssetReference ShopScene => shopScene;
        public AssetReference TestRoomScene => testRoomScene;
        public VersionLabelSettings VersionLabel => versionLabel;
    }
}
