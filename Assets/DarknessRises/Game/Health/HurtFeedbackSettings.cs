using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Health
{
    /// <summary>
    /// How the HUD shows the character being hurt: a red flash at the screen's edges on every landed hit, and on the
    /// last few hit points a slow pulse there with a heartbeat sound. Never changed at runtime.
    /// </summary>
    [Serializable]
    public sealed class HurtFeedbackSettings
    {
        [Header("Edge")]
        [SerializeField, Tooltip("The colour at the screen's edges. Its alpha is the edge at full strength.")]
        private Color edgeColor = new(0.75f, 0.02f, 0.02f, 0.85f);

        [Header("Hit flash")]
        [SerializeField, Range(0f, 1f), Tooltip("How strong the flash is for each hit point a hit takes.")]
        private float flashStrengthPerHitPoint = 0.7f;

        [SerializeField, Range(0f, 1f), Tooltip("The strongest a flash gets, however big the hit.")]
        private float flashMaxStrength = 1f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the flash takes to fade out.")]
        private float flashSeconds = 0.45f;

        [Header("Low health")]
        [SerializeField, Min(0), Tooltip("At or under this many hit points (and short of full) the edge pulses with a heartbeat. Zero for never.")]
        private int lowHitPoints = 2;

        [SerializeField, Min(0.1f), Tooltip("Seconds from one heartbeat to the next.")]
        private float beatPeriodSeconds = 1.1f;

        [SerializeField, Min(0f), Tooltip("Seconds from the strong beat to the softer one after it. Less than the period.")]
        private float dubDelaySeconds = 0.28f;

        [SerializeField, Range(0f, 1f), Tooltip("How strongly the soft beat swells the edge, and how loud it plays, next to the strong one.")]
        private float dubStrength = 0.6f;

        [SerializeField, Min(0.01f), Tooltip("Seconds a beat's swell of the edge takes to ease back.")]
        private float pulseDecaySeconds = 0.45f;

        [SerializeField, Range(0f, 1f), Tooltip("How strongly the edge shows between beats while low.")]
        private float pulseRestStrength = 0.18f;

        [SerializeField, Range(0f, 1f), Tooltip("How strongly the edge shows at a strong beat while low.")]
        private float pulsePeakStrength = 0.55f;

        [Header("Heartbeat sound (Sound volume)")]
        [SerializeField, Tooltip("Played on each beat while low: the soft beat plays it quieter. Placeholder from the approved Free Pack.")]
        private AssetReferenceT<AudioClip> heartbeatClip = new(string.Empty);

        [SerializeField, Range(0f, 1f), Tooltip("Volume of the strong beat.")]
        private float heartbeatVolume = 0.8f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Below 1 plays the clip lower and slower, deeper like a heartbeat.")]
        private float heartbeatPitch = 0.6f;

        public Color EdgeColor => edgeColor;
        public AssetReferenceT<AudioClip> HeartbeatClip => heartbeatClip;
        public float HeartbeatVolume => heartbeatVolume;
        public float HeartbeatPitch => heartbeatPitch;
        public float DubStrength => dubStrength;

        public HurtFeedbackTiming ToTiming() => new(
            flashStrengthPerHitPoint,
            flashMaxStrength,
            flashSeconds,
            lowHitPoints,
            beatPeriodSeconds,
            dubDelaySeconds,
            dubStrength,
            pulseDecaySeconds,
            pulseRestStrength,
            pulsePeakStrength);
    }
}
