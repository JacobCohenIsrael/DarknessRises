using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// How a Primary weapon's reload is shown and heard: a radial meter by the character that fills over the reload,
    /// and a sound of the whole reload sequence, lined up so it ends as the reload finishes. Shared by every Primary
    /// weapon, since each reloads the same way and only the reload time differs.
    /// </summary>
    [Serializable]
    public sealed class ReloadFeedbackSettings
    {
        [Header("Meter")]
        [SerializeField, Min(1f), Tooltip("Width and height of the meter, in HUD units (1080 tall).")]
        private float meterSize = 96f;

        [SerializeField, Tooltip("Where the meter sits relative to the character's feet, in world metres.")]
        private Vector3 worldOffset = new(0f, 2.4f, 0f);

        [SerializeField, Tooltip("The sweep that fills the disc clockwise from the top as the reload runs.")]
        private Color fillColor = new(1f, 0.78f, 0.32f, 0.95f);

        [SerializeField, Tooltip("The faint disc behind the sweep, showing how much is left. Light, so it reads in the dark. Clear for none.")]
        private Color backgroundColor = new(1f, 1f, 1f, 0.2f);

        [Header("Sound")]
        [SerializeField, Tooltip("One clip of a whole reload sequence. It's lined up so it ends as the reload finishes: " +
                                 "a shorter reload starts it partway in, a longer one starts it after a wait.")]
        private AssetReferenceT<AudioClip> clip = new(string.Empty);

        [SerializeField, Range(0f, 1f), Tooltip("Volume of the reload sound.")]
        private float volume = 0.8f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Above 1 plays the clip faster and higher, so it fills less of the reload. " +
                                                  "Slow-mo leaves it alone: the character, and so its reloads, keep full speed.")]
        private float pitch = 1f;

        [SerializeField, Min(0f), Tooltip("Seconds the sound takes to fade out when a reload is cut short: the weapon is " +
                                          "switched, the character dies or the Level ends. Short, but never a dead cut.")]
        private float interruptFadeSeconds = 0.12f;

        public float MeterSize => meterSize;
        public Vector3 WorldOffset => worldOffset;
        public Color FillColor => fillColor;
        public Color BackgroundColor => backgroundColor;
        public AssetReferenceT<AudioClip> Clip => clip;
        public float Volume => volume;
        public float Pitch => pitch;
        public float InterruptFadeSeconds => interruptFadeSeconds;
    }
}
