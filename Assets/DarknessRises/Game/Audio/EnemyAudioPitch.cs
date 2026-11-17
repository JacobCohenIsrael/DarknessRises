using System;
using DarknessRises.Game.Enemies;
using UnityEngine.Audio;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// Keeps the Enemies mixer group's pitch on the Enemy time scale, so slowed Enemies sound slowed:
    /// every Enemy sound plays through that group. Sets the mixer only when the scale changes.
    /// </summary>
    public sealed class EnemyAudioPitch : IDisposable
    {
        private readonly EnemyTime time;
        private readonly AudioMixer mixer;
        private readonly string pitchParameter;

        public EnemyAudioPitch(AudioDefinition definition, EnemyTime enemyTime)
        {
            time = enemyTime;
            mixer = definition.Mixer;
            pitchParameter = definition.EnemyPitchParameter;
            time.ScaleChanged += Follow;
            Follow(time.Scale);
        }

        public void Dispose()
        {
            time.ScaleChanged -= Follow;
            Follow(EnemyTime.NormalScale);
        }

        private void Follow(float scale)
        {
            mixer.SetFloat(pitchParameter, scale);
        }
    }
}
