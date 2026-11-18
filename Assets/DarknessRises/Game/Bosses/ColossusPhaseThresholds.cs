using System;
using UnityEngine;

namespace DarknessRises.Game.Bosses
{
    /// <summary>When the Colossus moves on to its next phase, by the share of its hit points left.</summary>
    [Serializable]
    public sealed class ColossusPhaseThresholds
    {
        [SerializeField, Range(0f, 1f), Tooltip("Phase 2 (feints and adds) starts once its hit points drop below this share.")]
        private float phase2Below = 0.5f;

        [SerializeField, Range(0f, 1f), Tooltip("Phase 3 (double flashes and chained stomps) starts below this share.")]
        private float phase3Below = 0.15f;

        public ColossusPhaseThresholds()
        {
        }

        public ColossusPhaseThresholds(float phase2Below, float phase3Below)
        {
            this.phase2Below = phase2Below;
            this.phase3Below = phase3Below;
        }

        public float Phase2Below => phase2Below;

        /// <summary>Never above <see cref="Phase2Below"/>.</summary>
        public float Phase3Below => Math.Min(phase3Below, phase2Below);

        /// <summary>The phase, from 1 to 3, with <paramref name="hitPoints"/> left of <paramref name="maxHitPoints"/>.</summary>
        public int PhaseAt(int hitPoints, int maxHitPoints)
        {
            var share = maxHitPoints > 0 ? (float)hitPoints / maxHitPoints : 0f;
            if (share < Phase3Below)
            {
                return 3;
            }

            return share < Phase2Below ? 2 : 1;
        }
    }
}
