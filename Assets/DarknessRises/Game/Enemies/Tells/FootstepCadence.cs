using System;

namespace DarknessRises.Game.Enemies.Tells
{
    /// <summary>
    /// When a walking Enemy puts a foot down: once per stride of ground covered, so its steps come faster the faster it
    /// moves. A limp makes every other stride short, so steps come in uneven pairs (a step, then a dragged foot). It
    /// stops while the Enemy stands still (a Wind-up, an attack, a Stagger, death) and starts again with a quick first
    /// step when it walks off. Plain C# (ADR 0003); time and speed come in as Enemy time, so Slow-mo slows the steps and
    /// a paused game makes none. Pooled with its Enemy, so <see cref="Stop"/> starts it over.
    /// </summary>
    public sealed class FootstepCadence
    {
        private readonly float strideMetres;
        private readonly float limp;
        private readonly float firstStepStrides;
        private readonly float stopSpeed;
        private float metresToNextStep;
        private bool nextIsDragged;

        /// <param name="strideMetres">Metres covered per step at an even gait.</param>
        /// <param name="limp">From 0 (even) to below 1: how much shorter every other stride is, and the one before it longer.</param>
        /// <param name="firstStepStrides">Strides to the first step after setting off.</param>
        /// <param name="stopSpeed">Metres per second of Enemy time below which the Enemy counts as standing still.</param>
        public FootstepCadence(float strideMetres, float limp, float firstStepStrides, float stopSpeed)
        {
            if (strideMetres <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(strideMetres), strideMetres, "A stride must cover some ground.");
            }

            if (limp < 0f || limp >= 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(limp), limp, "A limp is from 0 (even) to below 1.");
            }

            if (firstStepStrides < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(firstStepStrides), firstStepStrides, "The first step can't come before setting off.");
            }

            if (stopSpeed < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(stopSpeed), stopSpeed, "A speed can't be negative.");
            }

            this.strideMetres = strideMetres;
            this.limp = limp;
            this.firstStepStrides = firstStepStrides;
            this.stopSpeed = stopSpeed;
        }

        /// <summary>Whether the Enemy is walking: its steps are counting.</summary>
        public bool IsWalking { get; private set; }

        /// <summary>Steps at an even gait per second of Enemy time at <paramref name="speed"/> metres per second.</summary>
        public float StepsPerSecond(float speed)
        {
            return speed < stopSpeed ? 0f : speed / strideMetres;
        }

        /// <summary>The Enemy stands still, or has just spawned: no steps until it walks again.</summary>
        public void Stop()
        {
            IsWalking = false;
        }

        /// <summary>
        /// Moves Enemy time on by <paramref name="deltaSeconds"/> at <paramref name="speed"/> metres per second.
        /// True when a foot goes down now. At most one step per call, so a long frame never makes a burst of steps.
        /// </summary>
        public bool Tick(float deltaSeconds, float speed)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (speed < stopSpeed)
            {
                IsWalking = false;
                return false;
            }

            if (!IsWalking)
            {
                IsWalking = true;
                nextIsDragged = false;
                metresToNextStep = strideMetres * firstStepStrides;
            }

            metresToNextStep -= speed * deltaSeconds;
            if (metresToNextStep > 0f)
            {
                return false;
            }

            // A short stride after the good foot, a long one after the dragged foot: same ground over each pair.
            metresToNextStep = strideMetres * (nextIsDragged ? 1f + limp : 1f - limp);
            nextIsDragged = !nextIsDragged;
            return true;
        }
    }
}
