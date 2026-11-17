using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Boot
{
    /// <summary>
    /// What the boot scene loads: the Level scene the game starts in, and the Test Room sandbox. Both are
    /// Addressables scenes, so the boot scene (the only scene in the build) holds nothing but these references.
    /// Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Boot Definition", fileName = "BootDefinition", order = 0)]
    public sealed class BootDefinition : ScriptableObject
    {
        /// <summary>Command-line switch that boots into the Test Room instead, in the Editor and debug builds.</summary>
        public const string TestRoomArgument = "-testRoom";

        [SerializeField, Tooltip("The Level scene the game boots into, loaded additively through Addressables.")]
        private AssetReference levelScene = new(string.Empty);

        [SerializeField, Tooltip("The Test Room sandbox scene. Booted into with -testRoom, in the Editor and debug builds only.")]
        private AssetReference testRoomScene = new(string.Empty);

        public AssetReference LevelScene => levelScene;
        public AssetReference TestRoomScene => testRoomScene;
    }
}
