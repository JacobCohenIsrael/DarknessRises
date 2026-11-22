using DarknessRises.Game.Audio;
using DarknessRises.Game.Content;
using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The sound of the equipped Primary weapon's reload, from a 2D source on the character: one clip of a whole reload
    /// sequence, lined up by <see cref="ReloadSoundAlignment"/> so it ends as the reload finishes. A reload cut short
    /// (the weapon switched, the character dead, the Level over) fades the sound out instead of cutting it dead.
    /// </summary>
    /// <remarks>
    /// The sound follows the reload's own clock, read from the Weapon after it has been ticked this frame. A reload
    /// that stops advancing while game time runs (the weapon was put away, as on death) counts as cut short; the pause
    /// stops game time, so it only freezes the sound (with <c>AudioListener.pause</c>). Adrenaline leaves the character's
    /// reloads at full speed, and so this sound at its own pitch. The clip plays only once its audio data has loaded.
    /// </remarks>
    [DefaultExecutionOrder(AfterPrimaryWeapon)]
    public sealed class ReloadSoundView : MonoBehaviour
    {
        // The Primary weapon view ticks the reload at 100; this reads where it got to.
        private const int AfterPrimaryWeapon = 110;

        [SerializeField, Tooltip("2D, not played on awake. Plays the reload sequence of the equipped Primary weapon.")]
        private AudioSource source;

        private readonly SoundFade fade = new();
        private ReloadFeedbackSettings settings;
        private AudioClip clip;
        private PrimaryWeapon weapon;

        // The current reload's sound hasn't started yet, or is playing; each ends when the reload is cut short.
        private bool isWaiting;
        private bool isPlaying;
        private float lastReloadElapsedSeconds;

        /// <summary>Where the current or last reload sound started, in clip seconds, or -1 before the first.</summary>
        public float LastClipStartSeconds { get; private set; } = -1f;

        /// <summary>Whether the reload sound is fading out after its reload was cut short.</summary>
        public bool IsFadingOut => fade.IsFading;

        /// <param name="content">Where the clip is, already loaded with the Level's content. Never released here.</param>
        public void Use(ReloadFeedbackSettings feedback, ILoadedContent content)
        {
            settings = feedback;
            clip = content.Clip(settings.Clip);
        }

        /// <summary>
        /// Plays the reloads of <paramref name="primaryWeapon"/>, the equipped Primary, from now on. The sound of the
        /// Primary put away fades out; a reload the new one is in the middle of carries on in silence.
        /// </summary>
        public void Show(PrimaryWeapon primaryWeapon)
        {
            if (weapon != null && weapon != primaryWeapon)
            {
                Interrupt();
            }

            Unbind();

            weapon = primaryWeapon;
            weapon.ReloadStarted += OnReloadStarted;
        }

        /// <summary>Cuts the current reload's sound short: it fades out over the authored time.</summary>
        public void Interrupt()
        {
            isWaiting = false;
            isPlaying = false;
            if (settings != null)
            {
                fade.FadeOut(settings.InterruptFadeSeconds);
            }
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

            weapon.ReloadStarted -= OnReloadStarted;
            weapon = null;
        }

        // Raised from inside the Weapon's tick: the reload's clock starts at 0 now.
        private void OnReloadStarted()
        {
            isWaiting = true;
            isPlaying = false;
            lastReloadElapsedSeconds = -1f;
            TryStart(weapon.ReloadElapsedSeconds);
        }

        private void Update()
        {
            var deltaSeconds = Time.deltaTime;

            if ((isWaiting || isPlaying) && weapon != null)
            {
                if (!weapon.IsReloading)
                {
                    // Finished: the clip plays out its last moment, if a frame's rounding left one.
                    isWaiting = false;
                    isPlaying = false;
                }
                else
                {
                    var elapsed = weapon.ReloadElapsedSeconds;
                    if (deltaSeconds > 0f && elapsed <= lastReloadElapsedSeconds)
                    {
                        // Game time ran but the reload didn't: the weapon was put away.
                        Interrupt();
                    }
                    else
                    {
                        lastReloadElapsedSeconds = elapsed;
                        if (isWaiting)
                        {
                            TryStart(elapsed);
                        }
                    }
                }
            }

            if (!fade.IsFading)
            {
                return;
            }

            fade.Tick(deltaSeconds);
            if (fade.IsAudible)
            {
                source.SetVolume(fade.Volume);
            }
            else
            {
                source.Stop();
            }
        }

        private void TryStart(float reloadElapsedSeconds)
        {
            // Measuring or seeking a clip that hasn't loaded fails (WebGL decodes audio late), so it waits for it.
            if (clip == null || clip.loadState != AudioDataLoadState.Loaded)
            {
                return;
            }

            var alignment = ReloadSoundAlignment.For(clip.length, weapon.ReloadSeconds, settings.Pitch);
            if (!alignment.TryClipTimeAt(reloadElapsedSeconds, out var clipTimeSeconds))
            {
                // Before the wait is over; or past the end, when the clip loaded too late for this reload.
                isWaiting = reloadElapsedSeconds < alignment.DelaySeconds;
                return;
            }

            isWaiting = false;
            isPlaying = true;
            fade.Start(settings.Volume);
            source.clip = clip;
            source.pitch = settings.Pitch;
            source.SetVolume(settings.Volume);
            source.Play();
            source.time = clipTimeSeconds;
            LastClipStartSeconds = clipTimeSeconds;
        }
    }
}
