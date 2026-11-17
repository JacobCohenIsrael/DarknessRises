using UnityEngine;
using UnityEngine.Audio;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// The game's authored audio routing: the mixer, and which of its exposed parameters follow game state.
    /// Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Audio Definition", fileName = "AudioDefinition", order = 70)]
    public sealed class AudioDefinition : ScriptableObject
    {
        [SerializeField] private AudioMixer mixer;

        [SerializeField, Tooltip("Exposed mixer parameter for the Enemies group's pitch (1 is normal). " +
                                 "It follows the Enemy time scale, so Slow-mo slows Enemy sounds.")]
        private string enemyPitchParameter = "EnemyPitch";

        public AudioMixer Mixer => mixer;
        public string EnemyPitchParameter => enemyPitchParameter;
    }
}
