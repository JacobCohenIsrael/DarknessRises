using UnityEngine;
using UnityEngine.Serialization;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// Authored Lightning: when it comes, how long after each flash the Rumble follows, how the flash looks, when it
    /// throws an Enemy's shadow at the character (the looming strike), how the storm (its bed and its Rumbles) sounds,
    /// and how the Boss's warning plays. Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Lightning Definition", fileName = "LightningDefinition", order = 30)]
    public sealed class LightningDefinition : ScriptableObject
    {
        [Header("Timing")]
        [SerializeField, Min(0.1f), Tooltip("Shortest time in seconds from one ambient flash to the next.")]
        private float minIntervalSeconds = 1.5f;

        [SerializeField, Min(0.1f), Tooltip("Longest time in seconds from one ambient flash to the next.")]
        private float maxIntervalSeconds = 3.5f;

        [SerializeField, Min(0f), Tooltip("Shortest time in seconds from a flash to its Rumble (a close strike).")]
        private float minRumbleDelaySeconds = 0.15f;

        [SerializeField, Min(0f), Tooltip("Longest time in seconds from a flash to its Rumble (a distant strike).")]
        private float maxRumbleDelaySeconds = 1.2f;

        [SerializeField] private LightningFlashSettings flash = new();

        [SerializeField] private LightningLoomSettings looming = new();

        [SerializeField, FormerlySerializedAs("rumble")] private StormSettings storm = new();

        [SerializeField] private BossWarningSettings bossWarning = new();

        public LightningTiming Timing => new(minIntervalSeconds, maxIntervalSeconds, minRumbleDelaySeconds, maxRumbleDelaySeconds);
        public LightningFlashSettings Flash => flash;
        public LightningLoomSettings Looming => looming;
        public StormSettings Storm => storm;
        public BossWarningSettings BossWarning => bossWarning;

        private void OnValidate()
        {
            maxIntervalSeconds = Mathf.Max(maxIntervalSeconds, minIntervalSeconds);
            maxRumbleDelaySeconds = Mathf.Max(maxRumbleDelaySeconds, minRumbleDelaySeconds);
            flash.Validate();
            looming.Validate();
            storm.Validate();
        }
    }
}
