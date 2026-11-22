using System;
using System.Collections.Generic;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Storage;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// A scene's volume sliders at work (ADR 0010): it holds the scene's <see cref="AudioVolumes"/>, read from the device
    /// when the scene first needs them, and applies each category's gain to the scene's <see cref="AudioCategorySource"/>s
    /// whenever its slider moves. It sits on a root object of every scene that has sound (a Level, the Test Room, the
    /// Shop, the boot), where each source finds it once by its scene: sources made at runtime, pooled Enemies among them,
    /// are created in their scene and find it the same way, with nothing handed down to them.
    /// It also lets one category play on while the listener is paused, so the Ambient slider is heard from the pause menu.
    /// <para>
    /// The boot scene's has no volumes of its own: the boot makes it <see cref="Follow"/> the volumes of the Level or Shop
    /// that is showing, so the sounds it carries across scenes (the storm) move with that scene's sliders and are heard
    /// from its pause menu. Between scenes it keeps the gains it last had (ADR 0011).
    /// </para>
    /// </summary>
    public sealed class SceneAudioVolumes : MonoBehaviour
    {
        // Enough for a Level's sources (Gunshot voices, banners, the storm, a few dozen pooled Enemies) before it grows.
        private const int ExpectedSources = 128;

        [SerializeField, Tooltip("The root configuration: the sliders' defaults and curve. Empty in the boot scene, whose volumes follow the scene that is showing.")]
        private GameConfiguration configuration;

        // Every enabled source of the scene with a category.
        private readonly List<AudioCategorySource> sources = new(ExpectedSources);
        private readonly bool[] heardWhilePaused = new bool[AudioVolumes.CategoryCount];

        // The gains while following nothing and owning no volumes: full, until a followed scene leaves its own here.
        private readonly float[] keptGains = { 1f, 1f, 1f };
        private AudioVolumes volumes;
        private SceneAudioVolumes leader;

        // The scene's own sounds on their way out, before it unloads. Never followed: each scene fades its own.
        private readonly SceneSoundFade fade = new();

        // Reused by every lookup, so finding a scene's volumes allocates nothing.
        private static readonly List<GameObject> Roots = new();
        private static readonly List<AudioCategorySource> Found = new();

        /// <summary>Raised when a category starts or stops playing on over a paused listener, with the category and whether it does.</summary>
        public event Action<AudioCategory, bool> HearingWhilePausedChanged;

        /// <summary>
        /// The volumes the sliders set: the followed scene's while following one, else the scene's own, read from the
        /// device the first time they're needed. Null in a scene with no configuration that follows nothing (the boot
        /// between scenes).
        /// </summary>
        public AudioVolumes Volumes
        {
            get
            {
                if (leader != null)
                {
                    return leader.Volumes;
                }

                if (volumes == null && configuration != null)
                {
                    var settings = configuration.AudioVolumes;
                    UseVolumes(new AudioVolumes(settings.Curve, settings.Defaults, new DeviceVolumeStorage(DeviceSettings.OfThisPlayer())));
                }

                return volumes;
            }
        }

        /// <summary>
        /// The factor every source of this scene plays at on top of its category's gain: 1 while the scene plays, down to
        /// 0 once it fades out before it unloads (<see cref="FadeOut"/>). The scenes that follow these don't take it.
        /// </summary>
        public float Fade => fade.Level;

        /// <summary>Whether the scene's sounds have faded all the way to silence.</summary>
        public bool IsSilent => fade.IsSilent;

        /// <summary>The volumes this one follows, or null.</summary>
        public SceneAudioVolumes Leader => leader;

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

        /// <summary>
        /// Makes <paramref name="ownVolumes"/> the scene's volumes, in place of the ones it would read from the device.
        /// For a scene whose volumes are made elsewhere, and for tests.
        /// </summary>
        public void UseVolumes(AudioVolumes ownVolumes)
        {
            if (volumes != null)
            {
                volumes.Changed -= OnChanged;
            }

            volumes = ownVolumes ?? throw new ArgumentNullException(nameof(ownVolumes));
            volumes.Changed += OnChanged;
            ApplyAll();
        }

        /// <summary>The factor the sources of <paramref name="category"/> play their own volume at.</summary>
        public float Gain(AudioCategory category)
        {
            var current = Volumes;
            return current != null ? current.Gain(category) : keptGains[AudioVolumes.IndexOf(category)];
        }

        /// <summary>
        /// Follows <paramref name="followed"/>, the volumes of the scene that is showing: the sources here play at its
        /// gains, change with its sliders, and play on over a paused listener when its do. Replaces any scene followed before.
        /// </summary>
        public void Follow(SceneAudioVolumes followed)
        {
            if (followed == null)
            {
                throw new ArgumentNullException(nameof(followed));
            }

            if (followed == this)
            {
                throw new ArgumentException("Volumes can't follow themselves.", nameof(followed));
            }

            Unfollow();
            leader = followed;
            leader.Volumes.Changed += OnChanged;
            leader.HearingWhilePausedChanged += HearWhilePaused;
            for (var i = 0; i < AudioVolumes.CategoryCount; i++)
            {
                var category = (AudioCategory)i;
                HearWhilePaused(category, leader.IsHeardWhilePaused(category));
            }

            ApplyAll();
        }

        /// <summary>
        /// Stops following, keeping the gains the followed scene had, so the sounds here play on as they were until the
        /// next scene is followed. Nothing here plays on over a paused listener any more.
        /// </summary>
        public void Unfollow()
        {
            if (leader == null)
            {
                return;
            }

            var followed = leader;
            leader = null;
            for (var i = 0; i < AudioVolumes.CategoryCount; i++)
            {
                keptGains[i] = followed.Gain((AudioCategory)i);
                HearWhilePaused((AudioCategory)i, false);
            }

            // Plain C# events: fine to leave even if the followed scene is already being destroyed.
            followed.Volumes.Changed -= OnChanged;
            followed.HearingWhilePausedChanged -= HearWhilePaused;

            ApplyAll();
        }

        /// <summary>
        /// Fades every sound of the scene to silence over <paramref name="seconds"/> of real time, whatever plays it, so
        /// nothing still playing when the scene unloads stops dead (issue #121). Real time, so it ends even while the game
        /// is paused. A shorter fade during a longer one takes over.
        /// </summary>
        public void FadeOut(float seconds)
        {
            fade.FadeOut(seconds);
            ApplyAll();
        }

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

            HearingWhilePausedChanged?.Invoke(category, hear);
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

        private void ApplyAll()
        {
            for (var i = 0; i < sources.Count; i++)
            {
                sources[i].Apply();
            }
        }

        private void Update()
        {
            if (fade.Tick(Time.unscaledDeltaTime))
            {
                ApplyAll();
            }
        }

        // The scene has loaded by now: every source built into it, which woke before it could find these, follows them.
        private void Start()
        {
            AttachSources();
        }

        /// <summary>
        /// Makes every categorized source of the scene, enabled or not, follow these volumes. Runs at Start; sources made
        /// later find them themselves.
        /// </summary>
        public void AttachSources()
        {
            gameObject.scene.GetRootGameObjects(Roots);
            foreach (var root in Roots)
            {
                root.GetComponentsInChildren(true, Found);
                foreach (var source in Found)
                {
                    source.Attach(this);
                }
            }

            Roots.Clear();
            Found.Clear();
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
            Unfollow();
            if (volumes == null)
            {
                return;
            }

            volumes.Changed -= OnChanged;
            volumes.Save();
        }
    }
}
