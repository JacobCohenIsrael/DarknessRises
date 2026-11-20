using System;
using System.Collections.Generic;
using DarknessRises.Game.Content;
using DarknessRises.Game.Randomness;
using DarknessRises.Game.Weapons;
using UnityEngine;

namespace DarknessRises.Game.Enemies.Tells
{
    /// <summary>
    /// Plays the footsteps of the living Enemies: each one's <see cref="FootstepCadence"/> runs on Enemy time from how
    /// fast it walks, and only the nearest few walking within hearing range are heard (<see cref="FootstepVoicePicker"/>),
    /// each step on one of a small, fixed set of positional voices placed where the foot came down. Made by the scene's
    /// composition root, which hands it every Enemy as it spawns; dead and pooled Enemies drop out by themselves.
    /// </summary>
    /// <remarks>
    /// The voices are allotted like Gunshots (<see cref="GunshotVoices"/>): a free one, or the oldest when all are busy.
    /// Steps play at the type's random pitch slowed by Enemy time (<see cref="EnemyVoicePitch"/>), and the pause freezes
    /// them with game time and <c>AudioListener.pause</c>. Nothing here allocates per frame or per step.
    /// </remarks>
    [DefaultExecutionOrder(AfterEnemies)]
    public sealed class EnemyFootstepsView : MonoBehaviour
    {
        // Enemies move in their own Update at the default order; steps follow what they did this frame.
        private const int AfterEnemies = 50;
        private const int InitialCapacity = 32;

        private readonly List<EnemyView> enemies = new(InitialCapacity);
        private readonly Dictionary<EnemyDefinition, AudioClip[]> clipsByType = new();
        private readonly Dictionary<EnemyDefinition, AudioClip[]> dragClipsByType = new();
        private FootstepCandidate[] candidates = new FootstepCandidate[InitialCapacity];
        private bool[] heard = new bool[InitialCapacity];
        private bool[] stepped = new bool[InitialCapacity];
        private FootstepVoicePicker picker;
        private GunshotVoices voices;
        private AudioSource[] voiceSources;
        private float[] voiceVolumes;
        private float endFadeSeconds;
        private Transform listener;
        private EnemyTime time;
        private ILoadedContent content;
        private IRandom random;

        /// <summary>Raised for every step that is heard, with the Enemy that took it and the voice that plays it.</summary>
        public event Action<EnemyView, AudioSource> Stepped;

        /// <summary>The footstep voices, in the order they're allotted.</summary>
        public IReadOnlyList<AudioSource> VoiceSources => voiceSources;

        /// <summary>
        /// Makes the footsteps under <paramref name="parent"/>: their voices, heard from <paramref name="character"/>.
        /// Each Enemy type's clips come from <paramref name="loadedContent"/>, already loaded with the Level.
        /// </summary>
        public static EnemyFootstepsView Create(
            Transform parent,
            FootstepVoiceSettings settings,
            Transform character,
            EnemyTime enemyTime,
            ILoadedContent loadedContent)
        {
            var view = new GameObject("Enemy Footsteps").AddComponent<EnemyFootstepsView>();
            view.transform.SetParent(parent, false);
            view.listener = character;
            view.time = enemyTime;
            view.content = loadedContent;
            view.random = new SystemRandom();
            view.picker = new FootstepVoicePicker(settings.MaxHeardEnemies);
            view.voices = new GunshotVoices(settings.VoiceCount);
            view.voiceSources = new AudioSource[settings.VoiceCount];
            view.voiceVolumes = new float[settings.VoiceCount];
            view.endFadeSeconds = settings.EndFadeSeconds;
            for (var i = 0; i < view.voiceSources.Length; i++)
            {
                view.voiceSources[i] = view.CreateVoice(i);
            }

            return view;
        }

        /// <summary>An Enemy has spawned: its steps count from now until it dies.</summary>
        public void Track(EnemyView enemy)
        {
            if (enemies.Contains(enemy))
            {
                return;
            }

            if (!clipsByType.ContainsKey(enemy.Definition))
            {
                var footsteps = enemy.Definition.Audio.Footsteps;
                clipsByType.Add(enemy.Definition, content.Clips(footsteps.Clips));
                dragClipsByType.Add(enemy.Definition, content.Clips(footsteps.DragClips));
            }

            enemies.Add(enemy);
            if (enemies.Count > candidates.Length)
            {
                // Rare: more Enemies alive than ever before in this scene.
                Array.Resize(ref candidates, enemies.Count * 2);
                Array.Resize(ref heard, candidates.Length);
                Array.Resize(ref stepped, candidates.Length);
            }
        }

        private void Update()
        {
            voices.Tick(Time.deltaTime);
            FadeVoices();
            var deltaSeconds = time.Scaled(Time.deltaTime);
            var here = listener.position;
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i] == null || !enemies[i].Enemy.IsAlive || !enemies[i].isActiveAndEnabled)
                {
                    enemies.RemoveAt(i);
                }
            }

            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                var cadence = enemy.Footsteps;
                stepped[i] = cadence.Tick(deltaSeconds, enemy.WalkingSpeed);
                var offset = enemy.transform.position - here;
                offset.y = 0f;
                candidates[i] = new FootstepCandidate(offset.sqrMagnitude, cadence.IsWalking, enemy.Definition.Audio.HearingRange);
            }

            picker.Pick(new ReadOnlySpan<FootstepCandidate>(candidates, 0, enemies.Count), heard);
            for (var i = 0; i < enemies.Count; i++)
            {
                if (stepped[i] && heard[i])
                {
                    Step(enemies[i], enemies[i].Footsteps.LastStepDragged);
                }
            }
        }

        // Each voice plays at its step's volume until its last moment, then fades out (GunshotVoices.Gain), so a long
        // clip cut short at the step's most seconds ends softly rather than dead.
        private void FadeVoices()
        {
            if (Time.deltaTime <= 0f)
            {
                return;
            }

            for (var i = 0; i < voiceSources.Length; i++)
            {
                var source = voiceSources[i];
                if (voices.IsPlaying(i))
                {
                    source.volume = voiceVolumes[i] * voices.Gain(i, endFadeSeconds);
                }
                else if (source.isPlaying)
                {
                    source.Stop();
                }
            }
        }

        private void Step(EnemyView enemy, bool dragged)
        {
            var footsteps = enemy.Definition.Audio.Footsteps;
            var drags = dragClipsByType[enemy.Definition];
            var useDrag = dragged && drags.Length > 0;
            var clips = useDrag ? drags : clipsByType[enemy.Definition];
            if (clips.Length == 0)
            {
                return;
            }

            var clip = clips[Math.Min((int)random.Range(0f, clips.Length), clips.Length - 1)];
            if (clip == null)
            {
                return;
            }

            var pitch = EnemyVoicePitch.For(random.Range(footsteps.MinPitch, footsteps.MaxPitch), time.Scale);
            if (pitch <= 0f)
            {
                return;
            }

            // A clip's length is only known once its audio data has loaded (WebGL decodes audio late); a long one is cut short.
            var clipSeconds = clip.loadState == AudioDataLoadState.Loaded ? clip.length / pitch : footsteps.MaxStepSeconds;
            var voice = voices.Start(Mathf.Min(clipSeconds, footsteps.MaxStepSeconds));
            var source = voiceSources[voice];
            voiceVolumes[voice] = useDrag ? footsteps.DragVolume : footsteps.Volume;
            source.transform.position = enemy.transform.position;
            source.maxDistance = enemy.Definition.Audio.HearingRange;
            source.volume = voiceVolumes[voice];
            source.pitch = pitch;
            source.clip = clip;
            source.Play();
            Stepped?.Invoke(enemy, source);
        }

        // The one place footstep voices are made: route them to the sound effects' mixer group here.
        private AudioSource CreateVoice(int index)
        {
            var voice = new GameObject($"Footstep Voice {index + 1}").AddComponent<AudioSource>();
            voice.transform.SetParent(transform, false);
            voice.playOnAwake = false;
            voice.loop = false;
            voice.spatialBlend = 1f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.dopplerLevel = 0f;
            return voice;
        }
    }
}
