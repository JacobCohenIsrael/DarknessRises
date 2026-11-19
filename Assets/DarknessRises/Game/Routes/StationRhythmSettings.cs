using System;
using UnityEngine;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// Authored timing of the Station rhythm (ready, fight, clear, quiet, go) and of the walk easing into and out of a
    /// Station. Together they make every stop loud and deliberate, so the game reads as a ride that drives itself.
    /// </summary>
    [Serializable]
    public sealed class StationRhythmSettings
    {
        [Header("Beats")]
        [SerializeField, Min(0f), Tooltip(
            "Seconds from the character stopping at a Station until its Waves start: the Ready banner shows and its sting plays. " +
            "Each Wave's own delay counts from the end of this beat.")]
        private float readySeconds = 1.6f;

        [SerializeField, Min(0f), Tooltip("Seconds the Clear banner shows, from the death of the Station's last Wave.")]
        private float clearSeconds = 1.3f;

        [SerializeField, Min(0f), Tooltip("Seconds of quiet after the Clear banner, with nothing on screen, before the Go cue.")]
        private float quietSeconds = 0.8f;

        [SerializeField, Min(0f), Tooltip("Seconds the Go cue points along the Route before the character sets off.")]
        private float goSeconds = 0.7f;

        [Header("Walk")]
        [SerializeField, Min(0f), Tooltip("Metres before a Station over which the character slows down, so it plants instead of stopping dead.")]
        private float brakingMetres = 1.4f;

        [SerializeField, Min(0f), Tooltip("Seconds after leaving a Station over which the character picks its walking pace back up.")]
        private float setOffSeconds = 0.6f;

        [SerializeField, Range(0.05f, 1f), Tooltip(
            "The share of the walking speed the character arrives at a Station with, and sets off from it at. 1 switches the easing off.")]
        private float slowestSpeedFraction = 0.35f;

        public StationRhythmTiming ToTiming() => new(readySeconds, clearSeconds, quietSeconds, goSeconds);

        public WalkEasing ToWalkEasing() => new(brakingMetres, setOffSeconds, slowestSpeedFraction);
    }
}
