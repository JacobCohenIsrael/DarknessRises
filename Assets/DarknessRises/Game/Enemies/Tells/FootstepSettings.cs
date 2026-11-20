using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Enemies.Tells
{
    /// <summary>
    /// An Enemy type's footsteps: a Tell heard from where it walks, before it can be seen. Their cadence follows how fast
    /// it moves (<see cref="FootstepCadence"/>). Played positionally through its <see cref="EnemyAudioSettings.HearingRange"/>
    /// on the shared footstep voices (<see cref="FootstepVoiceSettings"/>).
    /// </summary>
    [Serializable]
    public sealed class FootstepSettings
    {
        [SerializeField, Tooltip("One of these is picked at random for each step. Leave empty for an Enemy type that walks silently.")]
        private AssetReferenceT<AudioClip>[] clips = new AssetReferenceT<AudioClip>[0];

        [SerializeField, Tooltip("Played for the dragged foot instead of a step, when the gait limps: a scrape or a drag. Leave empty to use the steps for every foot.")]
        private AssetReferenceT<AudioClip>[] dragClips = new AssetReferenceT<AudioClip>[0];

        [SerializeField, Range(0f, 1f), Tooltip("Volume of a dragged foot at the Enemy.")]
        private float dragVolume = 0.45f;

        [SerializeField, Min(0.05f), Tooltip("Longest a step or a drag plays, in seconds, before it fades out: long clips are cut short so steps don't pile up on the few voices.")]
        private float maxStepSeconds = 0.8f;

        [SerializeField, Min(0.05f), Tooltip("Metres covered per step. Steps per second = speed / stride, so a fast Enemy patters and a slow one plods.")]
        private float strideMetres = 0.75f;

        [SerializeField, Range(0f, 0.9f), Tooltip("How uneven the gait is, from 0 (even) up: every other stride is this much shorter, so steps come in step-drag pairs.")]
        private float limp = 0.4f;

        [SerializeField, Min(0f), Tooltip("Strides from setting off to the first step, so steps start soon after the Enemy walks again.")]
        private float firstStepStrides = 0.3f;

        [SerializeField, Min(0f), Tooltip("Metres per second (Enemy time) below which the Enemy counts as standing still and makes no steps.")]
        private float stopSpeed = 0.3f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Lowest pitch a step plays at. Each step picks its own, so no two sound alike.")]
        private float minPitch = 0.9f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Highest pitch a step plays at.")]
        private float maxPitch = 1.05f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume of a step at the Enemy.")]
        private float volume = 0.55f;

        public AssetReferenceT<AudioClip>[] Clips => clips;
        public AssetReferenceT<AudioClip>[] DragClips => dragClips;
        public float DragVolume => dragVolume;
        public float MaxStepSeconds => maxStepSeconds;
        public float StrideMetres => strideMetres;
        public float Limp => limp;
        public float FirstStepStrides => firstStepStrides;
        public float StopSpeed => stopSpeed;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;
        public float Volume => volume;

        /// <summary>A fresh cadence for one Enemy of this type, standing still.</summary>
        public FootstepCadence CreateCadence()
        {
            return new FootstepCadence(strideMetres, limp, firstStepStrides, stopSpeed);
        }
    }
}
