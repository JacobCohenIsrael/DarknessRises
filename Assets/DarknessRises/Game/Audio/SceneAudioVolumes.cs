using System.Collections.Generic;
using DarknessRises.Game.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// A scene's volume sliders at work (ADR 0010): it holds the scene's <see cref="AudioVolumes"/>, read from the device
    /// when the scene first needs them, and applies each category's gain to the scene's <see cref="AudioCategorySource"/>s
    /// whenever its slider moves. It sits on a root object of every scene that has sound (a Level, the Test Room, the
    /// Shop), where each source finds it once by its scene: sources made at runtime, pooled Enemies among them, are
    /// created in their scene and find it the same way, with nothing handed down to them.
    /// It also lets one category play on while the listener is paused, so the Ambient slider is heard from the pause menu.
    /// </summary>
    public sealed class SceneAudioVolumes : MonoBehaviour
    {
        // Enough for a Level's sources (Gunshot voices, banners, the storm, a few dozen pooled Enemies) before it grows.
        private const int ExpectedSources = 128;

        [SerializeField, Tooltip("The root configuration: the sliders' defaults and curve.")]
        private GameConfiguration configuration;

        // Every enabled source of the scene with a category.
        private readonly List<AudioCategorySource> sources = new(ExpectedSources);
        private readonly bool[] heardWhilePaused = new bool[AudioVolumes.CategoryCount];
        private AudioVolumes volumes;

        // Reused by every lookup, so finding a scene's volumes allocates nothing.
        private static readonly List<GameObject> Roots = new();

        /// <summary>The scene's volumes, read from the device the first time they're needed.</summary>
        public AudioVolumes Volumes
        {
            get
            {
                if (volumes == null)
                {
                    var settings = configuration.AudioVolumes;
                    volumes = new AudioVolumes(settings.Curve, settings.Defaults, new PlayerPrefsVolumeStorage());
                    volumes.Changed += OnChanged;
                }

                return volumes;
            }
        }

        /// <summary>How many sources follow the sliders now.</summary>
        public int SourceCount => sources.Count;

        /// <summary>The source at <paramref name="index"/> of those that follow the sliders now, for checks and logs.</summary>
        public AudioCategorySource SourceAt(int index) => sources[index];

        /// <summary>The volumes on a root object of <paramref name="scene"/>, or null if it has none.</summary>
        public static SceneAudioVolumes Of(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            scene.GetRootGameObjects(Roots);
            SceneAudioVolumes found = null;
            for (var i = 0; i < Roots.Count && found == null; i++)
            {
                Roots[i].TryGetComponent(out found);
            }

            Roots.Clear();
            return found;
        }

        /// <summary>The factor the sources of <paramref name="category"/> play their own volume at.</summary>
        public float Gain(AudioCategory category) => Volumes.Gain(category);

        /// <summary>
        /// Lets the sources of <paramref name="category"/> play on while <c>AudioListener.pause</c> holds every other
        /// sound, or stops letting them. The pause menu's sliders use it for Ambient while they're open.
        /// </summary>
        public void HearWhilePaused(AudioCategory category, bool hear)
        {
            var index = AudioVolumes.IndexOf(category);
            if (heardWhilePaused[index] == hear)
            {
                return;
            }

            heardWhilePaused[index] = hear;
            for (var i = 0; i < sources.Count; i++)
            {
                if (sources[i].Category == category)
                {
                    sources[i].Source.ignoreListenerPause = hear;
                }
            }
        }

        /// <summary>Whether the sources of <paramref name="category"/> play on while the listener is paused.</summary>
        public bool IsHeardWhilePaused(AudioCategory category) => heardWhilePaused[AudioVolumes.IndexOf(category)];

        internal void Add(AudioCategorySource source)
        {
            sources.Add(source);
            if (heardWhilePaused[AudioVolumes.IndexOf(source.Category)])
            {
                source.Source.ignoreListenerPause = true;
            }
        }

        internal void Remove(AudioCategorySource source)
        {
            sources.Remove(source);
        }

        private void OnChanged(AudioCategory category)
        {
            for (var i = 0; i < sources.Count; i++)
            {
                if (sources[i].Category == category)
                {
                    sources[i].Apply();
                }
            }
        }

        // On a phone the app may never come back from the background.
        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                volumes?.Save();
            }
        }

        private void OnDestroy()
        {
            if (volumes == null)
            {
                return;
            }

            volumes.Changed -= OnChanged;
            volumes.Save();
        }
    }
}
