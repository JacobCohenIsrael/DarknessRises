using System;
using UnityEngine;

namespace DarknessRises.Game.Enemies.Tells
{
    /// <summary>
    /// The budget every Enemy's footsteps share: a small, fixed set of positional voices, and how many Enemies are heard
    /// stepping at once (the nearest walking ones). Each Enemy type's own steps are on its <see cref="FootstepSettings"/>.
    /// </summary>
    [Serializable]
    public sealed class FootstepVoiceSettings
    {
        [SerializeField, Min(1), Tooltip("Positional voices the footsteps play on. When all are busy, a new step cuts the oldest short.")]
        private int voiceCount = 4;

        [SerializeField, Min(0), Tooltip("How many Enemies are heard stepping at once: the nearest ones that walk within hearing range. 0 silences every footstep.")]
        private int maxHeardEnemies = 3;

        public int VoiceCount => voiceCount;
        public int MaxHeardEnemies => maxHeardEnemies;
    }
}
