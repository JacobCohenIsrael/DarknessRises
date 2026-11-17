using System;

namespace DarknessRises.Game.Enemies.Tells
{
    /// <summary>
    /// How strongly an Enemy's eyes glint at a distance from the character: fully within the range less its fade,
    /// fading out across the fade, and not at all beyond the range. It depends on distance only, never on light.
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
    }
}
