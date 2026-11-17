using System;
using UnityEngine;

namespace DarknessRises.Game.Health
{
    /// <summary>How the game shows the character being hit: a camera shake, a hurt sound, and blinking while invulnerable.</summary>
    [Serializable]
    public sealed class HitReactionSettings
    {
        [SerializeField, Min(0f), Tooltip("Strength of the Cinemachine impulse a landed hit sends to the camera.")]
        private float shakeForce = 0.5f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the hit's camera shake lasts.")]
        private float shakeSeconds = 0.3f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the character spends shown, then hidden, as it blinks while invulnerable.")]
        private float blinkSeconds = 0.08f;

        [SerializeField, Tooltip("Played when a hit lands and the character survives it.")]
        private AudioClip hurtClip;

        [SerializeField, Tooltip("Played when the character dies.")]
        private AudioClip deathClip;

        [SerializeField, Range(0f, 1f)]
        private float volume = 0.9f;

        public float ShakeForce => shakeForce;
        public float ShakeSeconds => shakeSeconds;
        public float BlinkSeconds => blinkSeconds;
        public AudioClip HurtClip => hurtClip;
        public AudioClip DeathClip => deathClip;
        public float Volume => volume;
    }
}
