using UnityEngine;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// Puts the <see cref="AudioSource"/> beside it in an <see cref="AudioCategory"/>, so it follows that category's
    /// volume slider. Every AudioSource in the game has one: builders add it with the category, and a check fails on any
    /// built source without one (ADR 0010).
    /// <para>
    /// The source's volume is the view's own volume (its fade, its authored loudness) times the category's gain, so views
    /// set it through <see cref="Volume"/> (or <see cref="AudioSourceVolume.SetVolume"/>), never <c>AudioSource.volume</c>.
    /// The gain comes from the <see cref="SceneAudioVolumes"/> of the source's scene, found once, and is applied again
    /// whenever its slider moves, so a sound already playing changes with it.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public sealed class AudioCategorySource : MonoBehaviour
    {
        [SerializeField, Tooltip("Which volume slider this source follows. Sound: what the player and the threats make. " +
            "Ambient: the world around (the storm bed, Room ambience). Music: music.")]
        private AudioCategory category;

        private AudioSource source;
        private float volume;
        private bool hasVolume;
        private SceneAudioVolumes volumes;
        private bool hasLookedForVolumes;
        private bool isRegistered;

        /// <summary>The slider this source follows.</summary>
        public AudioCategory Category => category;

        /// <summary>The source it categorizes.</summary>
        public AudioSource Source
        {
            get
            {
                if (source == null)
                {
                    source = GetComponent<AudioSource>();
                }

                return source;
            }
        }

        /// <summary>
        /// The view's own volume for the source, before the category's gain: what <c>AudioSource.volume</c> would be with
        /// the slider at full. Until a view sets it, it's the volume the source was built with.
        /// </summary>
        public float Volume
        {
            get
            {
                KeepBuiltVolume();
                return volume;
            }
            set
            {
                volume = value;
                hasVolume = true;
                Apply();
            }
        }

        /// <summary>The category's gain the source plays at now: 1 with no volumes in its scene.</summary>
        public float Gain
        {
            get
            {
                var sceneVolumes = Volumes();
                return sceneVolumes != null ? sceneVolumes.Gain(category) : 1f;
            }
        }

        /// <summary>
        /// Gives <paramref name="sourceObject"/>'s AudioSource (added if missing) the category <paramref name="sourceCategory"/>.
        /// For sources a view makes at runtime; builders set the category on the sources they build.
        /// </summary>
        public static AudioCategorySource Add(GameObject sourceObject, AudioCategory sourceCategory)
        {
            if (!sourceObject.TryGetComponent<AudioSource>(out _))
            {
                sourceObject.AddComponent<AudioSource>();
            }

            var categorized = sourceObject.AddComponent<AudioCategorySource>();
            categorized.category = sourceCategory;
            categorized.Apply();
            return categorized;
        }

        private void Awake()
        {
            KeepBuiltVolume();
        }

        private void OnEnable()
        {
            var sceneVolumes = Volumes();
            if (sceneVolumes != null && !isRegistered)
            {
                sceneVolumes.Add(this);
                isRegistered = true;
            }

            Apply();
        }

        private void OnDisable()
        {
            if (!isRegistered)
            {
                return;
            }

            isRegistered = false;

            // On scene unload the volumes may already be destroyed.
            if (volumes != null)
            {
                volumes.Remove(this);
            }
        }

        /// <summary>Plays the source at its own volume times its category's gain now.</summary>
        internal void Apply()
        {
            KeepBuiltVolume();
            Source.volume = volume * Gain;
        }

        private void KeepBuiltVolume()
        {
            if (hasVolume)
            {
                return;
            }

            hasVolume = true;
            volume = Source.volume;
        }

        private SceneAudioVolumes Volumes()
        {
            if (!hasLookedForVolumes)
            {
                hasLookedForVolumes = true;
                volumes = SceneAudioVolumes.Of(gameObject.scene);
            }

            return volumes;
        }
    }
}
