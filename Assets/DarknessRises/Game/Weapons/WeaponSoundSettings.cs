using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// What a Primary weapon sounds like when it fires: a Gunshot per Shot, or one Firing loop for a Weapon that fires
    /// continuously. Played in 2D on the character, which Slow-mo never slows, so they keep their pitch.
    /// </summary>
    [Serializable]
    public sealed class WeaponSoundSettings
    {
        [SerializeField, Tooltip("The clips a Gunshot can be, one picked at random per Shot. Empty for none, or for a " +
                                 "Weapon with a Firing loop.")]
        private GunshotClip[] gunshots = Array.Empty<GunshotClip>();

        [SerializeField, Range(0.5f, 2f), Tooltip("Lowest pitch a Gunshot plays at, picked at random per Shot so no two " +
                                                  "sound quite alike. Positive only (WebGL).")]
        private float minPitch = 0.95f;

        [SerializeField, Range(0.5f, 2f), Tooltip("Highest pitch a Gunshot plays at.")]
        private float maxPitch = 1.05f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume of each Gunshot.")]
        private float volume = 0.7f;

        [SerializeField, Tooltip("One sound for as long as the Weapon fires, instead of a Gunshot per Shot. Used when its clip is set.")]
        private FiringLoopSettings firingLoop = new();

        public IReadOnlyList<GunshotClip> Gunshots => gunshots;
        public float MinPitch => minPitch;
        public float MaxPitch => Mathf.Max(minPitch, maxPitch);
        public float Volume => volume;
        public FiringLoopSettings FiringLoop => firingLoop;
    }
}
