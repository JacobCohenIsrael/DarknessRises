using System;
using UnityEngine;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// How bright the Lure's small light and the glow on its can are: lit from the throw, pulsing slowly while it's out,
    /// then fading out over an authored moment when the Lure ends, with no pop. Plain C# (ADR 0003), ticked with frame
    /// time by the Lure's view.
    /// </summary>
    public sealed class LureGlow
    {
        private readonly float fadeOutSeconds;
        private readonly float pulseSeconds;
        private readonly float pulseDepth;
        private float level;
        private float litSeconds;
        private bool lit;

        /// <param name="fadeOutSeconds">Seconds it takes to go dark once the Lure ends.</param>
        /// <param name="pulseSeconds">Seconds per slow pulse while it's lit; 0 for a steady light.</param>
        /// <param name="pulseDepth">How far a pulse dims it, as a share of full brightness (0 to 1).</param>
        public LureGlow(float fadeOutSeconds, float pulseSeconds, float pulseDepth)
        {
            if (fadeOutSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fadeOutSeconds), fadeOutSeconds, "A fade can't take negative time.");
            }

            if (pulseSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(pulseSeconds), pulseSeconds, "A pulse can't take negative time.");
            }

            if (pulseDepth < 0f || pulseDepth > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(pulseDepth), pulseDepth, "A pulse dims between none and all of the light.");
            }

            this.fadeOutSeconds = fadeOutSeconds;
            this.pulseSeconds = pulseSeconds;
            this.pulseDepth = pulseDepth;
        }

        /// <summary>From 0 (dark) to 1 (full strength), the pulse and the fade applied.</summary>
        public float Brightness
        {
            get
            {
                if (level <= 0f)
                {
                    return 0f;
                }

                if (pulseSeconds <= 0f || pulseDepth <= 0f)
                {
                    return level;
                }

                // Starts at full strength, dips by the depth halfway through each pulse.
                var phase = litSeconds / pulseSeconds * (2f * Mathf.PI);
                var dip = pulseDepth * (0.5f - 0.5f * Mathf.Cos(phase));
                return level * (1f - dip);
            }
        }

        /// <summary>Whether it gives any light: lit, or still fading out.</summary>
        public bool IsShining => level > 0f;

        /// <summary>Whether it's fading out after the Lure ended.</summary>
        public bool IsFading => !lit && level > 0f;

        /// <summary>The Lure was thrown: full strength at once, the pulse starting over.</summary>
        public void Light()
        {
            lit = true;
            level = 1f;
            litSeconds = 0f;
        }

        /// <summary>The Lure ended: it fades out from where it is now.</summary>
        public void FadeOut()
        {
            lit = false;
            if (fadeOutSeconds <= 0f)
            {
                level = 0f;
            }
        }

        public void Tick(float deltaSeconds)
        {
            if (level <= 0f)
            {
                return;
            }

            litSeconds += deltaSeconds;
            if (pulseSeconds > 0f && litSeconds >= pulseSeconds)
            {
                litSeconds %= pulseSeconds;
            }

            if (!lit)
            {
                level = fadeOutSeconds > 0f ? Mathf.Max(0f, level - deltaSeconds / fadeOutSeconds) : 0f;
            }
        }
    }
}
