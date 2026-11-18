using System;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// How the Boss health bar moves: it slides in and out, and after each hit a lighter trail marks the hit points just
    /// lost, holds for a moment, then drains down to the fill. The fill itself is whatever share of hit points the Boss
    /// has left, read from the Boss each frame; this never keeps its own copy of them.
    /// </summary>
    public sealed class BossHealthBar
    {
        private readonly float trailHoldSeconds;
        private readonly float trailDrainPerSecond;
        private readonly float slideSeconds;

        private float holdSecondsLeft;
        private float slideTarget;

        /// <param name="trailHoldSeconds">Seconds the trail stays put after a hit before it drains.</param>
        /// <param name="trailDrainPerSecond">Share of the whole bar the trail drains per second.</param>
        /// <param name="slideSeconds">Seconds the bar takes to slide all the way in or out. 0 snaps.</param>
        public BossHealthBar(float trailHoldSeconds, float trailDrainPerSecond, float slideSeconds)
        {
            if (trailHoldSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(trailHoldSeconds), trailHoldSeconds, "Can't hold for negative time.");
            }

            if (trailDrainPerSecond <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(trailDrainPerSecond), trailDrainPerSecond, "The trail has to drain.");
            }

            if (slideSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(slideSeconds), slideSeconds, "Can't slide for negative time.");
            }

            this.trailHoldSeconds = trailHoldSeconds;
            this.trailDrainPerSecond = trailDrainPerSecond;
            this.slideSeconds = slideSeconds;
        }

        /// <summary>The trail's end, as a share of the whole bar. Never below the fill.</summary>
        public float Trail { get; private set; }

        /// <summary>How far the bar has slid in: 0 is out of sight, 1 all the way in.</summary>
        public float SlideIn { get; private set; }

        /// <summary>Whether any of the bar is on screen.</summary>
        public bool IsOnScreen => SlideIn > 0f;

        /// <summary>A fight starts with <paramref name="fill"/> of the hit points: the bar slides in, with no trail.</summary>
        public void Show(float fill)
        {
            Trail = Clamp01(fill);
            holdSecondsLeft = 0f;
            slideTarget = 1f;
        }

        /// <summary>The fight is over: the bar slides out.</summary>
        public void Hide()
        {
            slideTarget = 0f;
        }

        /// <summary>The Boss took a hit. The trail holds where it is, then drains.</summary>
        public void Hit()
        {
            holdSecondsLeft = trailHoldSeconds;
        }

        /// <summary>Moves the trail and the slide on by <paramref name="deltaSeconds"/>, with the bar filled to <paramref name="fill"/>.</summary>
        public void Tick(float deltaSeconds, float fill)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time only moves forward.");
            }

            fill = Clamp01(fill);
            if (Trail <= fill)
            {
                Trail = fill;
                holdSecondsLeft = 0f;
            }
            else
            {
                var drainSeconds = deltaSeconds;
                if (holdSecondsLeft > 0f)
                {
                    var held = Math.Min(holdSecondsLeft, drainSeconds);
                    holdSecondsLeft -= held;
                    drainSeconds -= held;
                }

                Trail = Math.Max(fill, Trail - trailDrainPerSecond * drainSeconds);
            }

            SlideIn = slideSeconds > 0f
                ? MoveTowards(SlideIn, slideTarget, deltaSeconds / slideSeconds)
                : slideTarget;
        }

        private static float MoveTowards(float current, float target, float step)
        {
            return current < target ? Math.Min(target, current + step) : Math.Max(target, current - step);
        }

        private static float Clamp01(float value)
        {
            return value < 0f ? 0f : value > 1f ? 1f : value;
        }
    }
}
