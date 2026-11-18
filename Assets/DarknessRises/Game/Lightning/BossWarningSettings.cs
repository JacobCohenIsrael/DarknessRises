using System;
using UnityEngine;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// How the Boss's warning before its flashes (the Colossus's growl) plays: at full volume until the flash, then
    /// fading out. The clip itself belongs to the Boss.
    /// </summary>
    [Serializable]
    public sealed class BossWarningSettings
    {
        [SerializeField, Range(0f, 1f), Tooltip("The warning's volume until the flash.")]
        private float volume = 0.9f;

        [SerializeField, Min(0f), Tooltip("Seconds the warning rolls on, fading out, after the flash.")]
        private float fadeOutSeconds = 3.5f;

        [SerializeField, Min(0f), Tooltip("Seconds a warning takes to fade out when it's cut short (leaving the Room, " +
                                          "Lightning switched off), so it never stops dead.")]
        private float cutFadeSeconds = 0.6f;

        public float Volume => volume;
        public float FadeOutSeconds => fadeOutSeconds;
        public float CutFadeSeconds => cutFadeSeconds;
    }
}
