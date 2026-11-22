using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Health
{
    /// <summary>
    /// How the HUD shows the character being hurt: a red flash at the screen's edges on every landed hit, and on the
    /// last few hit points a looping heartbeat (slow, then fast on the very last) with the edge pulsing in time.
    /// Never changed at runtime.
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
        [SerializeField, Min(0), Tooltip("At or under this many hit points (and short of full) the slow heartbeat loops and the edge pulses. Zero for never.")]
        private int lowHitPoints = 2;

        [SerializeField, Min(0), Tooltip("At or under this many hit points the fast heartbeat takes over. Zero for never.")]
        private int criticalHitPoints = 1;

        [SerializeField, Min(0f), Tooltip("Seconds the slow and fast heartbeats take to crossfade into each other. Starting and stopping are instant.")]
        private float crossfadeSeconds = 0.6f;

        [SerializeField, Min(0.01f), Tooltip("Seconds a beat's swell of the edge takes to ease back.")]
        private float pulseDecaySeconds = 0.3f;

        [SerializeField, Range(0f, 1f), Tooltip("How strongly the edge shows between beats while low.")]
        private float pulseRestStrength = 0.18f;

        [SerializeField, Range(0f, 1f), Tooltip("How strongly the edge shows at a strong beat while low.")]
        private float pulsePeakStrength = 0.55f;

        [SerializeField, Range(0f, 1f), Tooltip("How strongly the soft beat after each strong one swells the edge, next to the strong one.")]
        private float softBeatStrength = 0.6f;

        [Header("Heartbeat loops (Sound volume)")]
        [SerializeField, Tooltip("The slow heartbeat, looped on the last few hit points. CC0, see Audio/Heartbeat/README.md.")]
        private HeartbeatLoopSettings slow = new(66.667f, 0f, 0.31f, 0.75f);

        [SerializeField, Tooltip("The fast heartbeat, looped on the very last hit point.")]
        private HeartbeatLoopSettings fast = new(106.667f, 0f, 0.19f, 0.85f);

        public Color EdgeColor => edgeColor;
        public HeartbeatLoopSettings Slow => slow;
        public HeartbeatLoopSettings Fast => fast;

        public HurtFeedbackTiming ToTiming() => new(
            flashStrengthPerHitPoint,
            flashMaxStrength,
            flashSeconds,
            lowHitPoints,
            criticalHitPoints,
            crossfadeSeconds,
            pulseDecaySeconds,
            pulseRestStrength,
            pulsePeakStrength,
            softBeatStrength,
            slow.ToRhythm(),
            fast.ToRhythm());
    }

    /// <summary>One looping heartbeat clip: the clip, how loud it plays, and how its beats are timed, so the edge pulses with it.</summary>
    [Serializable]
    public sealed class HeartbeatLoopSettings
    {
        [SerializeField, Tooltip("A seamless loop of whole heartbeats.")]
        private AssetReferenceT<AudioClip> clip = new(string.Empty);

        [SerializeField, Range(0f, 1f), Tooltip("Volume of the loop at full, before the Sound slider.")]
        private float volume = 0.8f;

        [SerializeField, Min(1f), Tooltip("The clip's tempo: strong beats per minute. The loop's length divided into its beats.")]
        private float beatsPerMinute = 70f;

        [SerializeField, Min(0f), Tooltip("Seconds into the clip of its first strong beat.")]
        private float firstBeatSeconds;

        [SerializeField, Min(0f), Tooltip("Seconds from each strong beat to the soft beat after it. Less than one beat.")]
        private float softBeatDelaySeconds = 0.3f;

        public HeartbeatLoopSettings()
        {
        }

        public HeartbeatLoopSettings(float beatsPerMinute, float firstBeatSeconds, float softBeatDelaySeconds, float loopVolume)
        {
            this.beatsPerMinute = beatsPerMinute;
            this.firstBeatSeconds = firstBeatSeconds;
            this.softBeatDelaySeconds = softBeatDelaySeconds;
            volume = loopVolume;
        }

        public AssetReferenceT<AudioClip> Clip => clip;
        public float Volume => volume;

        public HeartbeatRhythm ToRhythm() => new(beatsPerMinute, firstBeatSeconds, softBeatDelaySeconds);
    }
}
