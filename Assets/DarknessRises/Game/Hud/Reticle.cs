using System;
using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// How the crosshair shows: it's there while the player aims (always with a mouse; on touch from the first Shot,
    /// then for a moment after the last one), it kicks larger with every Shot and settles back, and it fades once
    /// nobody aims. Plain C#: time arrives as an explicit delta (ADR 0003).
    /// </summary>
    public sealed class Reticle
    {
        private readonly float kickScale;
        private readonly float kickSeconds;
        private readonly float lingerSeconds;
        private readonly float fadeSeconds;
        private float kickSecondsLeft;
        private float lingerSecondsLeft;

        /// <param name="kickScale">How many times its size the crosshair jumps to with a Shot.</param>
        /// <param name="kickSeconds">Seconds the jump takes to settle back.</param>
        /// <param name="lingerSeconds">Seconds the crosshair stays after the last Shot once nobody holds the aim.</param>
        /// <param name="fadeSeconds">Seconds it then takes to fade out.</param>
        public Reticle(float kickScale, float kickSeconds, float lingerSeconds, float fadeSeconds)
        {
            if (kickScale < 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(kickScale), kickScale, "A Shot kicks the crosshair larger, never smaller.");
            }

            if (kickSeconds < 0f || lingerSeconds < 0f || fadeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(kickSeconds), "A time can't be negative.");
            }

            this.kickScale = kickScale;
            this.kickSeconds = kickSeconds;
            this.lingerSeconds = lingerSeconds;
            this.fadeSeconds = fadeSeconds;
            Scale = 1f;
        }

        /// <summary>How much the crosshair shows, from 0 (gone) to 1.</summary>
        public float Alpha { get; private set; }

        /// <summary>The crosshair's size, as a multiple of its own: above 1 just after a Shot.</summary>
        public float Scale { get; private set; }

        public bool IsVisible => Alpha > 0f;

        /// <summary>A Shot was fired: the crosshair shows at once and kicks.</summary>
        public void Fire()
        {
            Alpha = 1f;
            kickSecondsLeft = kickSeconds;
            lingerSecondsLeft = lingerSeconds;
            Scale = kickSeconds > 0f ? kickScale : 1f;
        }

        /// <summary>Takes the crosshair away at once: the game is paused, or the character is dead.</summary>
        public void Hide()
        {
            Alpha = 0f;
            Scale = 1f;
            kickSecondsLeft = 0f;
            lingerSecondsLeft = 0f;
        }

        /// <param name="aiming">
        /// Whether the player's aim is on the screen right now: a mouse pointer always is, a finger while it holds Fire.
        /// </param>
        public void Tick(float deltaSeconds, bool aiming)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (kickSecondsLeft > 0f)
            {
                kickSecondsLeft = Mathf.Max(0f, kickSecondsLeft - deltaSeconds);
                Scale = Mathf.Lerp(1f, kickScale, kickSecondsLeft / kickSeconds);
            }

            if (aiming)
            {
                Alpha = 1f;
                lingerSecondsLeft = lingerSeconds;
                return;
            }

            if (Alpha <= 0f)
            {
                return;
            }

            // Once the linger runs out partway through a step, only the rest of the step fades.
            var fadeSecondsNow = deltaSeconds - lingerSecondsLeft;
            lingerSecondsLeft = Mathf.Max(0f, lingerSecondsLeft - deltaSeconds);
            if (fadeSecondsNow > 0f)
            {
                Alpha = fadeSeconds > 0f ? Mathf.Max(0f, Alpha - fadeSecondsNow / fadeSeconds) : 0f;
            }
        }
    }
}
