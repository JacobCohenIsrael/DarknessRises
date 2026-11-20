using System;
using DarknessRises.Game.Randomness;
using UnityEngine;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// A living Enemy as a looming strike sees it: where it stands on the floor, how tall it stands, and whether any light
    /// shows it right now.
    /// </summary>
    public readonly struct LoomCandidate
    {
        public LoomCandidate(Vector2 floor, float heightMetres, bool isSeen)
        {
            Floor = floor;
            HeightMetres = heightMetres;
            IsSeen = isSeen;
        }

        /// <summary>Where it stands, across the floor: x is world x, y is world z.</summary>
        public Vector2 Floor { get; }

        /// <summary>Metres from its feet to the top of its body: how far up its shadow starts.</summary>
        public float HeightMetres { get; }

        /// <summary>Whether a light shows it right now (Gunfire, the Aura, a fading flash...).</summary>
        public bool IsSeen { get; }
    }

    /// <summary>One strike: the angle it comes in at, and the Enemy whose shadow it throws at the character, if any.</summary>
    public readonly struct LightningStrike
    {
        public LightningStrike(LightningAngle angle, int loomingTarget)
        {
            Angle = angle;
            LoomingTarget = loomingTarget;
        }

        public LightningAngle Angle { get; }

        /// <summary>The index of the candidate whose shadow looms, or -1 for an ordinary strike.</summary>
        public int LoomingTarget { get; }

        public bool IsLooming => LoomingTarget >= 0;
    }

    /// <summary>
    /// The looming strike: now and then Lightning comes in low from behind a near Enemy that no light shows yet, straight
    /// toward the character, so its shadow stretches across the floor toward and over the player, bigger and closer than
    /// the Enemy is. The shadow is always a real Enemy's: when none qualifies, the strike is an ordinary one.
    /// </summary>
    /// <remarks>
    /// The light travels along the strike's yaw, so the yaw is the direction from the Enemy to the character. The
    /// elevation is the one whose shadow of the Enemy's top just reaches an authored distance past the character, kept
    /// within an authored low range.
    /// </remarks>
    public readonly struct LightningLooming
    {
        public LightningLooming(
            float chance,
            float minElevationDegrees,
            float maxElevationDegrees,
            float minMetres,
            float maxMetres,
            float aheadDegrees,
            float maxTurnFromRoomDegrees,
            float reachPastCharacterMetres)
        {
            if (chance < 0f || chance > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(chance), chance, "A chance is from 0 to 1.");
            }

            if (minElevationDegrees <= 0f || maxElevationDegrees >= 90f || maxElevationDegrees < minElevationDegrees)
            {
                throw new ArgumentOutOfRangeException(nameof(minElevationDegrees), "A looming strike comes in from above the horizon and below the zenith.");
            }

            if (minMetres < 0f || maxMetres < minMetres)
            {
                throw new ArgumentOutOfRangeException(nameof(maxMetres), maxMetres, "The distances must be a range from zero up.");
            }

            if (aheadDegrees < 0f || aheadDegrees > 180f)
            {
                throw new ArgumentOutOfRangeException(nameof(aheadDegrees), aheadDegrees, "Ahead is from 0 to 180 degrees either side.");
            }

            if (maxTurnFromRoomDegrees < 0f || maxTurnFromRoomDegrees > 180f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTurnFromRoomDegrees), maxTurnFromRoomDegrees, "A turn is from 0 to 180 degrees either way.");
            }

            if (reachPastCharacterMetres < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(reachPastCharacterMetres), reachPastCharacterMetres, "The reach can't be negative.");
            }

            Chance = chance;
            MinElevationDegrees = minElevationDegrees;
            MaxElevationDegrees = maxElevationDegrees;
            MinMetres = minMetres;
            MaxMetres = maxMetres;
            AheadDegrees = aheadDegrees;
            MaxTurnFromRoomDegrees = maxTurnFromRoomDegrees;
            ReachPastCharacterMetres = reachPastCharacterMetres;
        }

        /// <summary>The chance, from 0 to 1, that a strike tries to loom.</summary>
        public float Chance { get; }

        public float MinElevationDegrees { get; }
        public float MaxElevationDegrees { get; }

        /// <summary>The nearest an Enemy may stand to the character and qualify.</summary>
        public float MinMetres { get; }

        /// <summary>The farthest an Enemy may stand from the character and qualify.</summary>
        public float MaxMetres { get; }

        /// <summary>Degrees either side of the character's facing that count as in front.</summary>
        public float AheadDegrees { get; }

        /// <summary>Degrees the strike may turn from the Room's Lightning direction; 180 lets it come from anywhere.</summary>
        public float MaxTurnFromRoomDegrees { get; }

        /// <summary>Metres past the character the top of the Enemy's shadow reaches.</summary>
        public float ReachPastCharacterMetres { get; }

        /// <summary>
        /// Picks a strike: if it rolls to loom and an Enemy qualifies, the nearest one's looming angle; otherwise an
        /// ordinary angle from <paramref name="ordinary"/>.
        /// </summary>
        /// <param name="facing">The character's facing across the floor (x, z). Zero counts every direction as in front.</param>
        public LightningStrike Pick(
            IRandom random,
            in LightningAngles ordinary,
            float roomYawDegrees,
            ReadOnlySpan<LoomCandidate> candidates,
            Vector2 character,
            Vector2 facing)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (Lightning.Chance.Roll(random, Chance))
            {
                var target = PickTarget(candidates, character, facing, roomYawDegrees);
                if (target >= 0)
                {
                    return new LightningStrike(AngleFrom(candidates[target], character), target);
                }
            }

            return new LightningStrike(ordinary.Pick(random, roomYawDegrees), -1);
        }

        /// <summary>The nearest qualifying candidate, or -1 when none qualifies.</summary>
        public int PickTarget(ReadOnlySpan<LoomCandidate> candidates, Vector2 character, Vector2 facing, float roomYawDegrees)
        {
            var best = -1;
            var bestMetres = float.PositiveInfinity;
            for (var i = 0; i < candidates.Length; i++)
            {
                if (!Qualifies(candidates[i], character, facing, roomYawDegrees))
                {
                    continue;
                }

                var metres = Vector2.Distance(candidates[i].Floor, character);
                if (metres < bestMetres)
                {
                    best = i;
                    bestMetres = metres;
                }
            }

            return best;
        }

        /// <summary>
        /// Whether <paramref name="candidate"/> can throw a looming shadow: unseen, standing near, in front, and where
        /// light from behind it toward the character doesn't turn too far from the Room's Lightning direction.
        /// </summary>
        public bool Qualifies(in LoomCandidate candidate, Vector2 character, Vector2 facing, float roomYawDegrees)
        {
            if (candidate.IsSeen || candidate.HeightMetres <= 0f)
            {
                return false;
            }

            var offset = candidate.Floor - character;
            var metres = offset.magnitude;
            if (metres <= 0f || metres < MinMetres || metres > MaxMetres)
            {
                return false;
            }

            if (facing.sqrMagnitude > 0f && Vector2.Angle(facing, offset) > AheadDegrees)
            {
                return false;
            }

            var yaw = YawToward(candidate.Floor, character);
            return Mathf.Abs(Mathf.DeltaAngle(roomYawDegrees, yaw)) <= MaxTurnFromRoomDegrees;
        }

        /// <summary>The angle that throws <paramref name="candidate"/>'s shadow straight at and past the character.</summary>
        public LightningAngle AngleFrom(in LoomCandidate candidate, Vector2 character)
        {
            var reach = Vector2.Distance(candidate.Floor, character) + ReachPastCharacterMetres;
            var elevation = Mathf.Atan2(candidate.HeightMetres, reach) * Mathf.Rad2Deg;
            elevation = Mathf.Clamp(elevation, MinElevationDegrees, MaxElevationDegrees);
            return new LightningAngle(elevation, YawToward(candidate.Floor, character));
        }

        /// <summary>The yaw of light travelling across the floor from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static float YawToward(Vector2 from, Vector2 to)
        {
            var travel = to - from;
            return Mathf.Atan2(travel.x, travel.y) * Mathf.Rad2Deg;
        }
    }
}
