using System;
using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The voices (AudioSources) on the character that every Primary weapon's Gunshots share. Kept small, so rapid fire
    /// stays within Unity's 32 real voices next to the reload, the Grenade, the character's and the Enemies' sounds.
    /// </summary>
    [Serializable]
    public sealed class GunshotVoiceSettings
    {
        [SerializeField, Range(1, 16), Tooltip("Gunshots that can sound at once. One more cuts the oldest short. The Machine " +
                                               "gun's 10 Shots a second, each about half a second long, need 5.")]
        private int voiceCount = 6;

        [SerializeField, Min(0f), Tooltip("Seconds each Gunshot fades out over at the end of its trimmed length, so a trim " +
                                          "never stops dead.")]
        private float endFadeSeconds = 0.04f;

        [SerializeField, Range(0, 256), Tooltip("AudioSource priority of the Gunshots and Firing loops (0 is the highest). " +
                                                "Slightly below the other sounds' default of 128, so when real voices run " +
                                                "out the reload, the Grenade and the character's hurt sounds win.")]
        private int priority = 140;

        public int VoiceCount => voiceCount;
        public float EndFadeSeconds => endFadeSeconds;
        public int Priority => priority;
    }
}
