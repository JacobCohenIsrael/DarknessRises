using UnityEngine;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// Authored Lightning: when it comes, how long the Rumble warns before it, and how the flash looks and sounds.
    /// Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Lightning Definition", fileName = "LightningDefinition", order = 30)]
    public sealed class LightningDefinition : ScriptableObject
    {
        [Header("Timing")]
        [SerializeField, Min(0.1f), Tooltip("Shortest time in seconds from one ambient flash to the next.")]
        private float minIntervalSeconds = 4f;

        [SerializeField, Min(0.1f), Tooltip("Longest time in seconds from one ambient flash to the next.")]
        private float maxIntervalSeconds = 6.5f;

        [SerializeField, Min(0f), Tooltip("Seconds the Rumble starts before each flash.")]
        private float rumbleLeadSeconds = 1.2f;

        [SerializeField] private LightningFlashSettings flash = new();

        [SerializeField] private RumbleSettings rumble = new();

        public LightningTiming Timing => new(minIntervalSeconds, maxIntervalSeconds, rumbleLeadSeconds);
        public LightningFlashSettings Flash => flash;
        public RumbleSettings Rumble => rumble;

        private void OnValidate()
        {
            maxIntervalSeconds = Mathf.Max(maxIntervalSeconds, minIntervalSeconds);
        }
    }
}
