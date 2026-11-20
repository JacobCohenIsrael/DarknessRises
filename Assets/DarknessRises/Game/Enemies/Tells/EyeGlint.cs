using System;

namespace DarknessRises.Game.Enemies.Tells
{
    /// <summary>
    /// How strongly an Enemy's eyes glint at a distance from the character: fully within the range less its fade,
    /// fading out across the fade, and not at all beyond the range. It depends on distance only, never on light,
    /// times how far the eyes have opened since the Enemy spawned (<see cref="EyeOpening"/>).
    /// </summary>
    public static class EyeGlint
    {
        /// <returns>0 (no glint) to 1 (full glint).</returns>
        public static float Strength(float distance, float range, float fadeDistance)
        {
            if (range < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(range), range, "The glint range can't be negative.");
            }

            if (fadeDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fadeDistance), fadeDistance, "The glint fade can't be negative.");
            }

            if (distance >= range)
            {
                return 0f;
            }

            var fade = Math.Min(fadeDistance, range);
            var fullUpTo = range - fade;
            if (distance <= fullUpTo)
            {
                return 1f;
            }

            return (range - distance) / fade;
        }

        /// <summary>The distance glint times <paramref name="openness"/>: how far the eyes have opened, from 0 to 1.</summary>
        /// <returns>0 (no glint) to 1 (full glint).</returns>
        public static float Strength(float distance, float range, float fadeDistance, float openness)
        {
            if (openness < 0f || openness > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(openness), openness, "Eyes open from 0 (shut) to 1 (open).");
            }

            return Strength(distance, range, fadeDistance) * openness;
        }
    }
}
