using System;
using System.Collections.Generic;
using DarknessRises.Game.Content;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The sound of the Primary weapons firing, in 2D on the character: a Gunshot per Shot on a small round-robin of
    /// voices (<see cref="GunshotVoices"/>), or a <see cref="FiringLoop"/> for a Weapon that fires continuously. Its
    /// AudioSources are its own, so a Gunshot never cuts the reload sound or a Grenade's bang.
    /// </summary>
    /// <remarks>
    /// Shots are heard as the Weapon fires them, from inside its tick; the voices advance afterwards, on real time, since
    /// Slow-mo never slows the character. The pause freezes them with game time (and <c>AudioListener.pause</c>). A clip
    /// whose audio data hasn't loaded is skipped rather than measured or seeked (WebGL decodes audio late); the Level's
    /// content loads every clip's audio data before play starts.
    /// </remarks>
    [DefaultExecutionOrder(AfterPrimaryWeapon)]
    public sealed class GunshotSoundView : MonoBehaviour
    {
        // The Primary weapon view ticks the Weapons at 100; this follows what they did.
        private const int AfterPrimaryWeapon = 110;

        private GunshotVoiceSettings settings;
        private PrimaryWeaponLoadout loadout;
        private GunshotVoices voices;
        private AudioSource[] voiceSources;
        private float[] voiceVolumes;
        private bool[] isVoiceActive;
        private WeaponSounds[] weapons;
        private Action[] onFired;

        /// <summary>Which voice each Gunshot plays on. Null before <see cref="Use"/>.</summary>
        public GunshotVoices Voices => voices;

        /// <summary>The Gunshot voices, in the order <see cref="Voices"/> numbers them.</summary>
        public IReadOnlyList<AudioSource> VoiceSources => voiceSources;

        /// <summary>The Firing loop of the Primary at <paramref name="index"/> in the loadout, or null if it has a Gunshot per Shot.</summary>
        public FiringLoop FiringLoopOf(int index)
        {
            return weapons[index].Loop;
        }

        /// <summary>The two voices of the Firing loop of the Primary at <paramref name="index"/>, or null.</summary>
        public IReadOnlyList<AudioSource> FiringLoopSourcesOf(int index)
        {
            return weapons[index].LoopSources;
        }

        /// <summary>
        /// Plays the Gunshots or Firing loop of each Primary in <paramref name="primaryLoadout"/>, made from
        /// <paramref name="definitions"/> in the same order. Builds its voices once.
        /// </summary>
        /// <param name="content">Where the clips are, already loaded with the Level's content. Never released here.</param>
        public void Use(
            GunshotVoiceSettings voiceSettings,
            IReadOnlyList<WeaponDefinition> definitions,
            PrimaryWeaponLoadout primaryLoadout,
            ILoadedContent content)
        {
            if (loadout != null)
            {
                throw new InvalidOperationException("The Gunshot voices are already in use.");
            }

            settings = voiceSettings;
            loadout = primaryLoadout;
            voices = new GunshotVoices(settings.VoiceCount);
            voiceSources = new AudioSource[voices.Count];
            voiceVolumes = new float[voices.Count];
            isVoiceActive = new bool[voices.Count];
            for (var i = 0; i < voiceSources.Length; i++)
            {
                voiceSources[i] = CreateSource($"Gunshot Voice {i + 1}");
            }

            weapons = new WeaponSounds[definitions.Count];
            onFired = new Action[definitions.Count];
            for (var i = 0; i < definitions.Count; i++)
            {
                weapons[i] = CreateWeaponSounds(definitions[i], content);
                var index = i;
                onFired[i] = () => OnFired(index);
                loadout[i].Fired += onFired[i];
            }
        }

        private void OnDestroy()
        {
            if (loadout == null)
            {
                return;
            }

            for (var i = 0; i < onFired.Length; i++)
            {
                loadout[i].Fired -= onFired[i];
            }

            loadout = null;
        }

        private void Update()
        {
            if (loadout == null)
            {
                return;
            }

            var deltaSeconds = Time.deltaTime;
            voices.Tick(deltaSeconds);
            for (var i = 0; i < voiceSources.Length; i++)
            {
                if (!isVoiceActive[i])
                {
                    continue;
                }

                if (voices.IsPlaying(i))
                {
                    voiceSources[i].volume = voiceVolumes[i] * voices.Gain(i, settings.EndFadeSeconds);
                }
                else
                {
                    voiceSources[i].Stop();
                    isVoiceActive[i] = false;
                }
            }

            var equipped = loadout.Equipped;
            for (var i = 0; i < weapons.Length; i++)
            {
                var loop = weapons[i].Loop;
                if (loop == null)
                {
                    continue;
                }

                // Firing stops with the trigger, a reload or a switch; a Weapon put away (on death) stops sending Shots.
                if (i != loadout.EquippedIndex || !equipped.IsTriggerHeld || equipped.IsReloading)
                {
                    loop.Release();
                }

                loop.Tick(deltaSeconds);
                var sources = weapons[i].LoopSources;
                for (var voice = 0; voice < sources.Length; voice++)
                {
                    if (loop.IsPlaying(voice))
                    {
                        sources[voice].volume = weapons[i].LoopVolume * loop.Volume(voice);
                    }
                }
            }
        }

        private void OnFired(int index)
        {
            var sounds = weapons[index];
            if (sounds.Loop != null)
            {
                sounds.Loop.Shot();
                return;
            }

            if (sounds.Clips.Length == 0)
            {
                return;
            }

            var pick = Random.Range(0, sounds.Clips.Length);
            var clip = sounds.Clips[pick];
            if (clip == null || clip.loadState != AudioDataLoadState.Loaded)
            {
                return;
            }

            sounds.Gunshots[pick].Trim(clip.length, out var startSeconds, out var lengthSeconds);
            var pitch = Random.Range(sounds.Settings.MinPitch, sounds.Settings.MaxPitch);
            var voice = voices.Start(lengthSeconds / pitch);

            var source = voiceSources[voice];
            var volume = sounds.Settings.Volume;
            source.Stop();
            source.clip = clip;
            source.pitch = pitch;
            source.volume = volume;
            source.Play();
            source.time = startSeconds;
            voiceVolumes[voice] = volume;
            isVoiceActive[voice] = true;
        }

        private WeaponSounds CreateWeaponSounds(WeaponDefinition definition, ILoadedContent content)
        {
            var sound = definition.Sound;
            var sounds = new WeaponSounds
            {
                Settings = sound,
                Gunshots = new GunshotClip[sound.Gunshots.Count],
                Clips = new AudioClip[sound.Gunshots.Count],
            };

            for (var i = 0; i < sounds.Gunshots.Length; i++)
            {
                sounds.Gunshots[i] = sound.Gunshots[i];
                sounds.Clips[i] = content.Clip(sound.Gunshots[i].Clip);
            }

            var loopClip = content.Clip(sound.FiringLoop.Clip);
            if (loopClip == null)
            {
                return sounds;
            }

            if (loopClip.loadState != AudioDataLoadState.Loaded)
            {
                Debug.LogWarning($"[Gunshots] {definition.name}'s Firing loop {loopClip.name} hasn't loaded; it fires silently.", loopClip);
                return sounds;
            }

            sounds.LoopVolume = sound.FiringLoop.Volume;
            sounds.Loop = new FiringLoop(sound.FiringLoop.Timing(loopClip.length));
            sounds.LoopSources = new AudioSource[FiringLoop.VoiceCount];
            for (var i = 0; i < sounds.LoopSources.Length; i++)
            {
                var source = CreateSource($"{definition.name} Firing Loop {i + 1}");
                source.clip = loopClip;
                sounds.LoopSources[i] = source;
            }

            var loopSources = sounds.LoopSources;
            var loop = sounds.Loop;
            var loopVolume = sounds.LoopVolume;
            loop.VoiceStarted += (voice, clipSeconds) =>
            {
                var source = loopSources[voice];
                source.Stop();
                source.volume = loopVolume * loop.Volume(voice);
                source.Play();
                source.time = clipSeconds;
            };
            loop.VoiceStopped += voice => loopSources[voice].Stop();
            return sounds;
        }

        private AudioSource CreateSource(string sourceName)
        {
            var sourceObject = new GameObject(sourceName, typeof(AudioSource));
            sourceObject.transform.SetParent(transform, false);
            var source = sourceObject.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.priority = settings.Priority;
            return source;
        }

        private sealed class WeaponSounds
        {
            public WeaponSoundSettings Settings;
            public GunshotClip[] Gunshots;
            public AudioClip[] Clips;
            public FiringLoop Loop;
            public AudioSource[] LoopSources;
            public float LoopVolume;
        }
    }
}
