using DarknessRises.Game.Content;
using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The sounds of the equipped Primary weapon's reload, from a 2D source on the character: magazine out when a
    /// reload starts and magazine in when it finishes. A clip plays only once its audio data has loaded.
    /// </summary>
    public sealed class ReloadSoundView : MonoBehaviour
    {
        [SerializeField, Tooltip("2D, not played on awake. Each reload sound replaces the one before.")]
        private AudioSource source;

        private ReloadFeedbackSettings settings;
        private AudioClip magazineOutClip;
        private AudioClip magazineInClip;
        private PrimaryWeapon weapon;

        /// <param name="content">Where the clips are, already loaded with the Level's content. Never released here.</param>
        public void Use(ReloadFeedbackSettings feedback, ILoadedContent content)
        {
            settings = feedback;
            magazineOutClip = content.Clip(settings.MagazineOutClip);
            magazineInClip = content.Clip(settings.MagazineInClip);
        }

        /// <summary>Plays the reloads of <paramref name="primaryWeapon"/>, the equipped Primary, from now on.</summary>
        public void Show(PrimaryWeapon primaryWeapon)
        {
            Unbind();

            weapon = primaryWeapon;
            weapon.ReloadStarted += PlayMagazineOut;
            weapon.ReloadFinished += PlayMagazineIn;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (weapon == null)
            {
                return;
            }

            weapon.ReloadStarted -= PlayMagazineOut;
            weapon.ReloadFinished -= PlayMagazineIn;
            weapon = null;
        }

        private void PlayMagazineOut()
        {
            Play(magazineOutClip, settings.MagazineOutVolume, settings.MagazineOutPitch);
        }

        private void PlayMagazineIn()
        {
            Play(magazineInClip, settings.MagazineInVolume, settings.MagazineInPitch);
        }

        private void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || clip.loadState != AudioDataLoadState.Loaded)
            {
                return;
            }

            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
        }
    }
}
