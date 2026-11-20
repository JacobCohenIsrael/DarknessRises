using System;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Content;
using DarknessRises.Game.Critters;
using DarknessRises.Game.Randomness;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Runs;
using UnityEngine;

namespace DarknessRises.Game.Ambience
{
    /// <summary>
    /// Plays the ambient bed: when its <see cref="AmbientBedSchedule"/> says so, a slice of a long recording
    /// (<see cref="AmbientSlice"/>) from a point out in the dark up near the ceiling (<see cref="AmbientBedPlace"/>),
    /// fading in and out, on one positional voice. A Boss fight or death fades it out early. Made by the scene's
    /// composition root once the content has loaded; the voice is its own and never moves while a bed plays, so the
    /// character walks past the sound.
    /// </summary>
    public sealed class AmbientBedView : MonoBehaviour
    {
        private readonly SoundFade cut = new();
        private AmbienceDefinition definition;
        private AmbientBedSchedule schedule;
        private AudioSource voice;
        private AudioClip[] clips;
        private Transform character;
        private RunFlow run;
        private StationRhythm rhythm;
        private IRandom random;
        private AmbientSlice slice;
        private float secondsIn;
        private bool isPlaying;

        /// <summary>Raised when a bed starts, with where it comes from and the slice it plays.</summary>
        public event Action<Vector3, AmbientSlice> Started;

        /// <summary>The bed's voice, for checking its state.</summary>
        public AudioSource Voice => voice;

        public bool IsPlaying => isPlaying;

        /// <summary>
        /// Makes the ambient bed under <paramref name="parent"/>, around <paramref name="characterTransform"/>, to the
        /// moments of <paramref name="runFlow"/> and <paramref name="stationRhythm"/>. Its clips come from
        /// <paramref name="content"/>, already loaded.
        /// </summary>
        public static AmbientBedView Create(
            Transform parent,
            AmbienceDefinition ambienceDefinition,
            Transform characterTransform,
            RunFlow runFlow,
            StationRhythm stationRhythm,
            ILoadedContent content)
        {
            var view = new GameObject("Ambient Bed").AddComponent<AmbientBedView>();
            view.transform.SetParent(parent, false);
            view.definition = ambienceDefinition;
            view.character = characterTransform;
            view.run = runFlow;
            view.rhythm = stationRhythm;
            view.random = new SystemRandom();
            view.schedule = ambienceDefinition.ToSchedule(view.random);
            view.clips = content.Clips(ambienceDefinition.Clips);
            view.voice = view.CreateVoice();
            return view;
        }

        /// <summary>Starts a bed now, whatever the schedule says.</summary>
        public bool PlayNow()
        {
            if (clips.Length == 0)
            {
                return false;
            }

            var clip = clips[Mathf.Min((int)random.Range(0f, clips.Length), clips.Length - 1)];
            if (clip == null)
            {
                return false;
            }

            slice = AmbientSlice.Plan(clip.length, definition.MinSliceSeconds, definition.MaxSliceSeconds, definition.FadeSeconds, random);
            var place = AmbientBedPlace.Pick(character.position, definition.MinMetres, definition.MaxMetres, definition.HeightMetres, random);
            voice.transform.position = place;
            voice.clip = clip;
            voice.pitch = random.Range(definition.MinPitch, definition.MaxPitch);
            voice.time = slice.StartSeconds;
            voice.SetVolume(0f);
            voice.Play();
            secondsIn = 0f;
            isPlaying = true;
            cut.Stop();
            Started?.Invoke(place, slice);
            return true;
        }

        private void Update()
        {
            var deltaSeconds = Time.deltaTime;
            var moment = CritterMoments.Of(run.Phase, rhythm.Beat);
            if (!isPlaying)
            {
                if (schedule.Tick(deltaSeconds, moment, false))
                {
                    PlayNow();
                }

                return;
            }

            // Clip seconds: the slice is measured in the clip, which plays at the voice's pitch.
            secondsIn += deltaSeconds * voice.pitch;
            if (!AmbientBedSchedule.Allows(moment) && !cut.IsFading)
            {
                cut.Start(voice.volume);
                cut.FadeOut(definition.CutFadeSeconds);
            }

            cut.Tick(deltaSeconds);
            voice.SetVolume(cut.IsFading ? cut.Volume : definition.Volume * slice.Volume(secondsIn));
            if (slice.IsOver(secondsIn) || (cut.IsFading && !cut.IsAudible))
            {
                voice.Stop();
                cut.Stop();
                isPlaying = false;
            }
        }

        // The one place the bed's voice is made: it's Ambient, so the Ambient slider sets it (ADR 0010).
        private AudioSource CreateVoice()
        {
            var sourceObject = new GameObject("Ambient Bed Voice");
            sourceObject.transform.SetParent(transform, false);
            var source = AudioCategorySource.Add(sourceObject, AudioCategory.Ambient).Source;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.maxDistance = definition.HearingRange;
            source.dopplerLevel = 0f;
            return source;
        }
    }
}
