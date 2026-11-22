using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The switch beat shared by every Primary weapon: the gun in the hands dips out of line, the new one comes up in its
    /// place, and a click plays as it does. See <see cref="WeaponSwitchBeat"/> for what can fire when.
    /// </summary>
    [Serializable]
    public sealed class WeaponSwitchSettings
    {
        [SerializeField, Min(0f), Tooltip("Seconds the gun in the hands takes to go down. Nothing fires meanwhile, so keep it short.")]
        private float lowerSeconds = 0.1f;

        [SerializeField, Min(0f), Tooltip("Seconds the newly equipped gun takes to come up. It can already fire as it rises.")]
        private float raiseSeconds = 0.15f;

        [SerializeField, Range(0f, 90f), Tooltip("Degrees the gun tips down at the bottom of the beat, about the weapon mount.")]
        private float lowerDegrees = 50f;

        [SerializeField, Min(0f), Tooltip("Metres the gun drops at the bottom of the beat, on top of the tip.")]
        private float lowerMetres = 0.12f;

        [Header("Click")]
        [SerializeField, Tooltip("The clip the click is cut from. It plays once the new gun starts up.")]
        private AssetReferenceT<AudioClip> clip = new(string.Empty);

        [SerializeField, Min(0f), Tooltip("Where in the clip the click starts, in seconds.")]
        private float clipStartSeconds;

        [SerializeField, Min(0.01f), Tooltip("Seconds of the clip that play from the start point.")]
        private float clipSeconds = 0.15f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume of the click.")]
        private float volume = 0.8f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Above 1 plays the click higher and quicker.")]
        private float pitch = 1f;

        public float LowerSeconds => lowerSeconds;
        public float RaiseSeconds => raiseSeconds;
        public float LowerDegrees => lowerDegrees;
        public float LowerMetres => lowerMetres;
        public AssetReferenceT<AudioClip> Clip => clip;
        public float ClipStartSeconds => clipStartSeconds;
        public float ClipSeconds => clipSeconds;
        public float Volume => volume;
        public float Pitch => pitch;
    }
}
