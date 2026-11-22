using System;
using UnityEngine;

namespace DarknessRises.Game.Skills
{
    /// <summary>Why a Lure stopped drawing Enemies.</summary>
    public enum LureEnd
    {
        /// <summary>Its time ran out.</summary>
        TimeUp,

        /// <summary>An Enemy reached it and broke it.</summary>
        Broken,
    }

    /// <summary>
    /// The Lure Skill: the character throws a rattling noisemaker to a point in the Room. Once it lands, every Enemy that
    /// comes within its radius goes for it instead of the character, until its time runs out or an Enemy breaks it; then
    /// they all go back to the character. Plain C# (ADR 0003), ticked with real time like the other Skills.
    /// </summary>
    /// <remarks>
    /// The Skill is active from the throw: the flight, then the time on the floor (the Run's Lure time, Upgrades applied).
    /// An Enemy once lured stays lured while the Lure lasts, even if a hit knocks it back out of the radius. Bosses never
    /// read the Lure. Whether an Enemy already attacking the character finishes first is the Enemy's own rule
    /// (<see cref="Enemies.EnemyQuarry"/>).
    /// </remarks>
    public sealed class Lure
    {
        private readonly float radius;
        private readonly float flightSeconds;
        private float secondsSinceThrow;

        /// <param name="lureTiming">How long it draws Enemies once it has landed, and its cooldown.</param>
        /// <param name="radius">Metres across the floor from where it lands within which Enemies are drawn to it.</param>
        /// <param name="flightSeconds">Seconds from the throw until it lands.</param>
        public Lure(SkillTiming lureTiming, float radius, float flightSeconds)
        {
            if (radius < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "A radius can't be negative.");
            }

            if (flightSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(flightSeconds), flightSeconds, "A flight can't take negative time.");
            }

            this.radius = radius;
            this.flightSeconds = flightSeconds;
            Skill = new Skill(new SkillTiming(flightSeconds + lureTiming.DurationSeconds, lureTiming.CooldownSeconds));
            Skill.Ended += OnSkillEnded;
        }

        /// <summary>Raised when it comes down at <see cref="Point"/> and starts drawing Enemies.</summary>
        public event Action<Lure> Landed;

        /// <summary>Raised when it stops drawing Enemies, with why. Lured Enemies go back to the character.</summary>
        public event Action<Lure, LureEnd> Ended;

        public Skill Skill { get; }

        /// <summary>Where the last Lure was thrown to.</summary>
        public Vector3 Point { get; private set; }

        /// <summary>Metres across the floor from <see cref="Point"/> within which Enemies are drawn.</summary>
        public float Radius => radius;

        /// <summary>Seconds from the throw until it lands.</summary>
        public float FlightSeconds => flightSeconds;

        /// <summary>Whether it's in the air, thrown but not landed yet.</summary>
        public bool IsFlying => Skill.IsActive && secondsSinceThrow < flightSeconds;

        /// <summary>Whether it's on the floor, rattling and drawing Enemies.</summary>
        public bool IsOut => Skill.IsActive && secondsSinceThrow >= flightSeconds;

        /// <summary>Why the last Lure stopped, once one has.</summary>
        public LureEnd LastEnd { get; private set; }

        /// <summary>Throws it to <paramref name="point"/> if the Skill is ready. Returns whether it did.</summary>
        public bool TryThrow(Vector3 point)
        {
            if (!Skill.IsReady)
            {
                return false;
            }

            Point = point;
            secondsSinceThrow = 0f;
            Skill.TryActivate();
            if (flightSeconds <= 0f)
            {
                Landed?.Invoke(this);
            }

            return true;
        }

        /// <summary>
        /// An Enemy reached it and struck it: it stops drawing Enemies now and its cooldown starts. Does nothing unless it's
        /// out on the floor.
        /// </summary>
        public void Break()
        {
            if (!IsOut)
            {
                return;
            }

            LastEnd = LureEnd.Broken;
            Skill.End();
        }

        /// <summary>
        /// Whether an Enemy standing at <paramref name="position"/> goes for the Lure: only while it's out, and only once
        /// the Enemy has come within its radius (<paramref name="alreadyLured"/> keeps one lured while the Lure lasts).
        /// </summary>
        public bool Draws(Vector3 position, bool alreadyLured)
        {
            if (!IsOut)
            {
                return false;
            }

            if (alreadyLured)
            {
                return true;
            }

            var offset = position - Point;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }

        public void Tick(float realDeltaSeconds)
        {
            var wasFlying = IsFlying;
            if (Skill.IsActive)
            {
                secondsSinceThrow += realDeltaSeconds;
            }

            // Landing is raised before the Skill ticks, so a Lure whose whole time passes in one frame still lands first.
            if (wasFlying && secondsSinceThrow >= flightSeconds)
            {
                Landed?.Invoke(this);
            }

            if (Skill.IsActive)
            {
                LastEnd = LureEnd.TimeUp;
            }

            Skill.Tick(realDeltaSeconds);
        }

        private void OnSkillEnded(Skill _)
        {
            Ended?.Invoke(this, LastEnd);
        }
    }
}
