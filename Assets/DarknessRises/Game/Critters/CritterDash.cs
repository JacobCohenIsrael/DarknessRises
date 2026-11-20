using System;
using DarknessRises.Game.Randomness;
using UnityEngine;

namespace DarknessRises.Game.Critters
{
    /// <summary>
    /// One Critter's dash: a straight scurry across the floor from one dark spot to another, passing in front of the
    /// character at the edge of its light, fast, and gone in a second or two. Plain C# (ADR 0003).
    /// </summary>
    public readonly struct CritterDash
    {
        public CritterDash(Vector3 from, Vector3 to, float speed)
        {
            if (speed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), speed, "A Critter must move.");
            }

            From = from;
            To = to;
            Speed = speed;
        }

        public Vector3 From { get; }
        public Vector3 To { get; }

        /// <summary>Metres per second.</summary>
        public float Speed { get; }

        public float Length => Vector3.Distance(From, To);

        public float Seconds => Length / Speed;

        /// <summary>Where the Critter is <paramref name="seconds"/> into the dash. It holds at the end.</summary>
        public Vector3 At(float seconds)
        {
            var total = Seconds;
            return total <= 0f ? To : Vector3.Lerp(From, To, Mathf.Clamp01(seconds / total));
        }

        /// <summary>Whether the dash is over <paramref name="seconds"/> in.</summary>
        public bool IsOver(float seconds)
        {
            return seconds >= Seconds;
        }

        /// <summary>
        /// A dash across the character's view: the line from the character to the dash's middle is turned at most
        /// <paramref name="settings"/>' ahead angle off <paramref name="forward"/>, the middle is between its pass
        /// distances from the character, and the dash runs square to that line, one way or the other, so the Critter
        /// crosses in front rather than coming at the character. Heights are the character's: the caller puts the ends
        /// on the floor.
        /// </summary>
        public static CritterDash Plan(Vector3 character, Vector3 forward, CritterDashShape settings, IRandom random)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            var turn = random.Range(-settings.AheadDegrees, settings.AheadDegrees);
            var toMiddle = Quaternion.AngleAxis(turn, Vector3.up) * forward;
            var middle = character + toMiddle * random.Range(settings.MinPassMetres, settings.MaxPassMetres);
            var across = Vector3.Cross(Vector3.up, toMiddle);
            if (random.Range(0f, 1f) < 0.5f)
            {
                across = -across;
            }

            var half = random.Range(settings.MinLengthMetres, settings.MaxLengthMetres) * 0.5f;
            return new CritterDash(middle - across * half, middle + across * half, random.Range(settings.MinSpeed, settings.MaxSpeed));
        }
    }

    /// <summary>The shape of the dashes Critters make, from <see cref="CritterDashSettings"/>.</summary>
    public readonly struct CritterDashShape
    {
        public CritterDashShape(
            float aheadDegrees,
            float minPassMetres,
            float maxPassMetres,
            float minLengthMetres,
            float maxLengthMetres,
            float minSpeed,
            float maxSpeed)
        {
            if (aheadDegrees < 0f || aheadDegrees > 180f)
            {
                throw new ArgumentOutOfRangeException(nameof(aheadDegrees), aheadDegrees, "From 0 to 180 degrees.");
            }

            Ordered(minPassMetres, maxPassMetres, nameof(maxPassMetres));
            Ordered(minLengthMetres, maxLengthMetres, nameof(maxLengthMetres));
            Ordered(minSpeed, maxSpeed, nameof(maxSpeed));
            if (minSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minSpeed), minSpeed, "A Critter must move.");
            }

            AheadDegrees = aheadDegrees;
            MinPassMetres = minPassMetres;
            MaxPassMetres = maxPassMetres;
            MinLengthMetres = minLengthMetres;
            MaxLengthMetres = maxLengthMetres;
            MinSpeed = minSpeed;
            MaxSpeed = maxSpeed;
        }

        public float AheadDegrees { get; }
        public float MinPassMetres { get; }
        public float MaxPassMetres { get; }
        public float MinLengthMetres { get; }
        public float MaxLengthMetres { get; }
        public float MinSpeed { get; }
        public float MaxSpeed { get; }

        private static void Ordered(float min, float max, string name)
        {
            if (min < 0f || max < min)
            {
                throw new ArgumentOutOfRangeException(name, max, "A range runs from a non-negative least to a most at least as big.");
            }
        }
    }
}
