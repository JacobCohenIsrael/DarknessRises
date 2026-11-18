using System;
using UnityEngine;

namespace DarknessRises.Game.Bosses
{
    /// <summary>
    /// The Colossus's phase 2, below half health: feints (the flash shows two shadows, only one foot comes down, and a
    /// second, late flash reveals which) and Shamblers dropping from its back between stomps. Adds keep dropping in phase 3.
    /// </summary>
    [Serializable]
    public sealed class ColossusPhase2Settings
    {
        [SerializeField, Range(0f, 1f), Tooltip("The chance that a phase 2 stomp is a feint.")]
        private float feintChance = 0.6f;

        [SerializeField, Min(0f), Tooltip(
            "Seconds after the first flash that the late flash reveals which shadow is the real foot. " +
            "Kept inside the feint's Relocate window.")]
        private float revealSeconds = 0.9f;

        [SerializeField, Min(0f), Tooltip("Seconds of growl (the Rumble) before the late flash.")]
        private float revealGrowlSeconds = 0.3f;

        [SerializeField, Min(0f), Tooltip("Seconds a feint adds to both its Relocate window and its flash to impact, to read the late flash.")]
        private float extraWindowSeconds = 0.6f;

        [SerializeField, Min(0), Tooltip("Shamblers that drop from its back after each stomp lifts.")]
        private int addsPerDrop = 2;

        [SerializeField, Min(0), Tooltip("Most adds alive at once. A drop only tops them up to this.")]
        private int maxAddsAlive = 4;

        public ColossusPhase2Settings()
        {
        }

        public ColossusPhase2Settings(
            float feintChance,
            float revealSeconds,
            float revealGrowlSeconds,
            float extraWindowSeconds,
            int addsPerDrop,
            int maxAddsAlive)
        {
            this.feintChance = feintChance;
            this.revealSeconds = revealSeconds;
            this.revealGrowlSeconds = revealGrowlSeconds;
            this.extraWindowSeconds = extraWindowSeconds;
            this.addsPerDrop = addsPerDrop;
            this.maxAddsAlive = maxAddsAlive;
        }

        public float FeintChance => feintChance;
        public float RevealSeconds => revealSeconds;

        /// <summary>The growl before the late flash, never longer than the wait for it.</summary>
        public float RevealGrowlSeconds => Math.Min(revealGrowlSeconds, revealSeconds);

        public float ExtraWindowSeconds => extraWindowSeconds;
        public int AddsPerDrop => addsPerDrop;
        public int MaxAddsAlive => maxAddsAlive;
    }
}
