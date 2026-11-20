using System;
using UnityEngine;

namespace DarknessRises.Game.Enemies.Tells
{
    /// <summary>
    /// How an Enemy type's eyes open after it spawns: shut for a moment, a slow crack to a peek, one blink, then fully
    /// open. It multiplies the distance glint, so an Enemy that spawns close isn't given away at full strength at once.
    /// Runs on Enemy time (<see cref="EyeOpening"/>).
    /// </summary>
    [Serializable]
    public sealed class EyeOpeningSettings
    {
        [SerializeField, Min(0f), Tooltip("Seconds of Enemy time from spawning to fully open eyes. 0 opens them at once. About a second reads as waking up.")]
        private float seconds = 1.2f;

        [SerializeField, Range(0f, 1f), Tooltip("Share of the opening the eyes stay shut, right after the Enemy comes in.")]
        private float shutShare = 0.2f;

        [SerializeField, Range(0f, 1f), Tooltip("Share of the opening over which the eyes crack open, slowly at first, to the peek.")]
        private float peekShare = 0.3f;

        [SerializeField, Range(0f, 1f), Tooltip("How far the eyes crack open before they blink, from 0 (shut) to 1 (fully open).")]
        private float peekStrength = 0.35f;

        [SerializeField, Range(0f, 1f), Tooltip("Share of the opening taken by one blink, shut and back to the peek. What's left after the shut, peek and blink opens them fully. The shares can't add up to more than 1.")]
        private float blinkShare = 0.15f;

        public float Seconds => seconds;
        public float ShutShare => shutShare;
        public float PeekShare => peekShare;
        public float PeekStrength => peekStrength;
        public float BlinkShare => blinkShare;

        /// <summary>A fresh opening for one Enemy, shut until it spawns.</summary>
        public EyeOpening Create()
        {
            return new EyeOpening(seconds, shutShare, peekShare, peekStrength, blinkShare);
        }
    }
}
