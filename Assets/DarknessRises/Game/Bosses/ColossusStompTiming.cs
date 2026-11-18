using System;
using UnityEngine;

namespace DarknessRises.Game.Bosses
{
    /// <summary>
    /// The rhythm of one Colossus stomp: its growl (the Rumble), the Lightning flash that shows the raised foot's
    /// shadow, the Relocate window, the impact, and how long the planted foot stays down as the damage window.
    /// All in Enemy time, so Slow-mo stretches them.
    /// </summary>
    [Serializable]
    public sealed class ColossusStompTiming
    {
        [SerializeField, Min(0f), Tooltip("Seconds from the start of the fight to the first growl.")]
        private float firstStompDelaySeconds = 2f;

        [SerializeField, Min(0f), Tooltip("Seconds of the growl (the Rumble) before Lightning flashes and shows where the foot will land.")]
        private float growlSeconds = 1.2f;

        [SerializeField, Min(0.01f), Tooltip("Seconds from the flash to the impact.")]
        private float flashToImpactSeconds = 1.9f;

        [SerializeField, Min(0.01f), Tooltip(
            "Seconds after the flash in which the player may Relocate. A dash still has to finish before the impact; " +
            "never longer than the flash to impact.")]
        private float relocateWindowSeconds = 1.5f;

        [SerializeField, Min(0f), Tooltip("Seconds the planted foot stays down with its weak spot lit: the damage window.")]
        private float plantedSeconds = 3.5f;

        [SerializeField, Min(0f), Tooltip("Seconds the foot takes to lift back out of the Room.")]
        private float liftSeconds = 0.6f;

        [SerializeField, Min(0f), Tooltip("Seconds between the foot lifting and the next growl.")]
        private float restSeconds = 1.5f;

        public ColossusStompTiming()
        {
        }

        public ColossusStompTiming(
            float firstStompDelaySeconds,
            float growlSeconds,
            float flashToImpactSeconds,
            float relocateWindowSeconds,
            float plantedSeconds,
            float liftSeconds,
            float restSeconds)
        {
            this.firstStompDelaySeconds = firstStompDelaySeconds;
            this.growlSeconds = growlSeconds;
            this.flashToImpactSeconds = flashToImpactSeconds;
            this.relocateWindowSeconds = relocateWindowSeconds;
            this.plantedSeconds = plantedSeconds;
            this.liftSeconds = liftSeconds;
            this.restSeconds = restSeconds;
        }

        public float FirstStompDelaySeconds => firstStompDelaySeconds;
        public float GrowlSeconds => growlSeconds;
        public float FlashToImpactSeconds => flashToImpactSeconds;

        /// <summary>The Relocate window, never longer than the time from flash to impact.</summary>
        public float RelocateWindowSeconds => Math.Min(relocateWindowSeconds, flashToImpactSeconds);

        public float PlantedSeconds => plantedSeconds;
        public float LiftSeconds => liftSeconds;
        public float RestSeconds => restSeconds;
    }
}
