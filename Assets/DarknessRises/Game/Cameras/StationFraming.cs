using System;
using DarknessRises.Game.Routes;
using UnityEngine;

namespace DarknessRises.Game.Cameras
{
    /// <summary>
    /// The Ride camera's framing of a Station: when it holds, and what it looks at. It's derived from where the
    /// character stands and where the Room's Enemies enter, so every Station gets a framing of its own without
    /// anyone authoring one.
    /// </summary>
    public static class StationFraming
    {
        /// <summary>
        /// Whether the camera holds the Station framing during <paramref name="beat"/>: from the moment the character
        /// plants until the Go cue, when it turns to lead the walk again.
        /// </summary>
        public static bool IsHeldIn(StationBeat beat)
        {
            return beat == StationBeat.Ready || beat == StationBeat.Fight || beat == StationBeat.Clear || beat == StationBeat.Quiet;
        }

        /// <summary>
        /// The point on the floor the camera looks at: from <paramref name="station"/> a share of the way to
        /// <paramref name="fightCentre"/> (the middle of where the Enemies come from), but never further than
        /// <paramref name="maxMetres"/>, so the character stays well inside the view.
        /// </summary>
        public static Vector3 Focus(Vector3 station, Vector3 fightCentre, float share, float maxMetres)
        {
            if (share < 0f || share > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(share), share, "A share is between 0 and 1.");
            }

            if (maxMetres < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxMetres), maxMetres, "A distance can't be negative.");
            }

            var towards = fightCentre - station;
            towards.y = 0f;
            return station + Vector3.ClampMagnitude(towards * share, maxMetres);
        }
    }
}
