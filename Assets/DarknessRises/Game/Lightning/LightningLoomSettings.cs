using System;
using UnityEngine;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// The looming strike: now and then Lightning comes in low from behind a near Enemy that no light shows yet, straight
    /// toward the character, so the Enemy's shadow stretches toward and over the player and up the walls behind, bigger
    /// and closer than the Enemy is. Only ever a real Enemy's shadow: when none qualifies, the strike is an ordinary one.
    /// </summary>
    [Serializable]
    public sealed class LightningLoomSettings
    {
        [SerializeField, Range(0f, 1f), Tooltip("Chance, from 0 to 1, that a strike tries to loom. It looms only when an Enemy qualifies; otherwise it's an ordinary strike.")]
        private float chance = 0.35f;

        [Header("Which Enemies qualify")]
        [SerializeField, Min(0f), Tooltip("Nearest metres from the character an Enemy may stand and qualify. Closer than this, its shadow would hardly stretch.")]
        private float minMetres = 2.5f;

        [SerializeField, Min(0f), Tooltip("Farthest metres from the character an Enemy may stand and qualify.")]
        private float maxMetres = 11f;

        [SerializeField, Range(0f, 180f), Tooltip("Degrees either side of the character's facing that count as in front. 180 lets an Enemy behind the character qualify too.")]
        private float aheadDegrees = 80f;

        [SerializeField, Range(0f, 180f), Tooltip("Most degrees the looming strike may turn from the Room's Lightning direction. 180 lets it come from any side.")]
        private float maxTurnFromRoomDegrees = 180f;

        [Header("Angle")]
        [SerializeField, Range(1f, 60f), Tooltip("Lowest degrees above the horizon a looming strike comes in at: the longest shadow it throws.")]
        private float minElevationDegrees = 7f;

        [SerializeField, Range(1f, 60f), Tooltip("Highest degrees above the horizon a looming strike comes in at.")]
        private float maxElevationDegrees = 16f;

        [SerializeField, Min(0f), Tooltip("Metres past the character the top of the Enemy's shadow reaches, within the elevation range above. " +
                                          "More throws it lower and longer, up the walls behind the character.")]
        private float reachPastCharacterMetres = 4f;

        public LightningLooming Looming => new(
            chance,
            minElevationDegrees,
            maxElevationDegrees,
            minMetres,
            maxMetres,
            aheadDegrees,
            maxTurnFromRoomDegrees,
            reachPastCharacterMetres);

        /// <summary>Keeps the ranges the right way round while they're edited.</summary>
        public void Validate()
        {
            maxMetres = Mathf.Max(maxMetres, minMetres);
            maxElevationDegrees = Mathf.Max(maxElevationDegrees, minElevationDegrees);
        }
    }
}
