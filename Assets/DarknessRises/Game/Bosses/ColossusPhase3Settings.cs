using System;
using UnityEngine;

namespace DarknessRises.Game.Bosses
{
    /// <summary>
    /// The Colossus's phase 3, its last hit points: the rhythm breaks. Lightning flashes twice in quick succession,
    /// each flash showing one foot's shadow, and the two stomps land one after the other. Damage windows and rests shorten.
    /// </summary>
    [Serializable]
    public sealed class ColossusPhase3Settings
    {
        [SerializeField, Min(0f), Tooltip("Seconds from the first flash to the second, which shows the second foot's shadow.")]
        private float doubleFlashGapSeconds = 0.3f;

        [SerializeField, Min(0.01f), Tooltip("Seconds from the first foot landing to the second.")]
        private float chainSeconds = 0.7f;

        [SerializeField, Min(0f), Tooltip("Seconds the last foot stays planted: a shorter damage window.")]
        private float plantedSeconds = 2.5f;

        [SerializeField, Min(0f), Tooltip("Seconds between the foot lifting and the next growl.")]
        private float restSeconds = 0.8f;

        public ColossusPhase3Settings()
        {
        }

        public ColossusPhase3Settings(float doubleFlashGapSeconds, float chainSeconds, float plantedSeconds, float restSeconds)
        {
            this.doubleFlashGapSeconds = doubleFlashGapSeconds;
            this.chainSeconds = chainSeconds;
            this.plantedSeconds = plantedSeconds;
            this.restSeconds = restSeconds;
        }

        public float DoubleFlashGapSeconds => doubleFlashGapSeconds;
        public float ChainSeconds => chainSeconds;
        public float PlantedSeconds => plantedSeconds;
        public float RestSeconds => restSeconds;
    }
}
