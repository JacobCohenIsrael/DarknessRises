using System;
using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// How a banner shows: it pops in (fading in while it shrinks from larger than life to its size), stays, and fades
    /// out once it's told to hide, after an optional linger. Plain C#: time arrives as an explicit delta (ADR 0003).
    /// </summary>
    public sealed class BannerPop
    {
        private readonly float popSeconds;
        private readonly float popScale;
        private readonly float fadeOutSeconds;
        private bool isShown;
        private bool isHiding;
        private float lingerSecondsLeft;
        private float secondsShown;

        public BannerPop(float popSeconds, float popScale, float fadeOutSeconds)
        {
            if (popSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(popSeconds), popSeconds, "A pop can't take less than no time.");
            }

            if (popScale < 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(popScale), popScale, "A banner pops in from larger than its size.");
            }

            if (fadeOutSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fadeOutSeconds), fadeOutSeconds, "A fade can't take less than no time.");
            }

            this.popSeconds = popSeconds;
            this.popScale = popScale;
            this.fadeOutSeconds = fadeOutSeconds;
            Scale = 1f;
        }

        /// <summary>How much the banner shows, from 0 (gone) to 1.</summary>
        public float Alpha { get; private set; }

        /// <summary>The banner's size, as a multiple of its own: above 1 while it pops in.</summary>
        public float Scale { get; private set; }

        public bool IsVisible => Alpha > 0f;

        /// <summary>Pops the banner in from the start, whatever it was doing.</summary>
        public void Show()
        {
            isShown = true;
            isHiding = false;
            secondsShown = 0f;
            Alpha = popSeconds > 0f ? 0f : 1f;
            Scale = popSeconds > 0f ? popScale : 1f;
        }

        /// <summary>Fades the banner out, after it has stayed on for <paramref name="lingerSeconds"/> more.</summary>
        public void Hide(float lingerSeconds = 0f)
        {
            if (!isShown)
            {
                return;
            }

            isHiding = true;
            lingerSecondsLeft = Mathf.Max(lingerSeconds, 0f);
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (!isShown && Alpha <= 0f)
            {
                return;
            }

            // Once the linger runs out partway through a step, only the rest of the step fades.
            var fadeSeconds = deltaSeconds;
            if (isShown && isHiding)
            {
                lingerSecondsLeft -= deltaSeconds;
                if (lingerSecondsLeft <= 0f)
                {
                    isShown = false;
                    isHiding = false;
                    fadeSeconds = -lingerSecondsLeft;
                }
            }

            if (isShown)
            {
                secondsShown += deltaSeconds;
                var popped = popSeconds > 0f ? Mathf.Clamp01(secondsShown / popSeconds) : 1f;

                // Fast at first, settling into place.
                var settled = 1f - (1f - popped) * (1f - popped);
                Scale = Mathf.Lerp(popScale, 1f, settled);
                Alpha = popped;
                return;
            }

            Alpha = fadeOutSeconds > 0f ? Mathf.Max(0f, Alpha - fadeSeconds / fadeOutSeconds) : 0f;
        }
    }
}
