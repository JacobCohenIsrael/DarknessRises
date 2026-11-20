using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Content;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Randomness;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// Plays the storm bed: the thunder loop and its Rumble, on one looping AudioSource (Ambient), shaped by a
    /// <see cref="StormBed"/>. The boot scene has one that outlives every Level, so the storm rolls on through a Level
    /// change instead of cutting dead with the old Level's scene; each Level's <see cref="LightningView"/> drives it while
    /// that Level plays. A Level played on its own drives its own. It loads its clip into a <see cref="ContentScope"/> it
    /// owns, so the clip stays loaded when a Level releases its content (ADR 0007), and releases it when it goes.
    /// </summary>
    /// <remarks>
    /// Runs on scaled time, like the Level: the pause freezes the bed (with <c>AudioListener.pause</c>) and it carries on
    /// afterwards. The loop is started once and never stopped: it plays on silent while faded out, so fading back in
    /// carries on the storm instead of restarting it.
    /// </remarks>
    public sealed class StormPlayer : MonoBehaviour
    {
        [SerializeField, Tooltip("Plays the storm bed: the thunder loop, looping, in the Ambient category.")]
        private AudioSource source;

        private ContentScope content;
        private StormBed bed;
        private AudioClip clip;
        private object clipKey;
        private bool isStarted;
        private bool isHushed;

        /// <summary>The source the storm plays on.</summary>
        public AudioSource Source => source;

        /// <summary>Whether the loop has started playing.</summary>
        public bool IsStarted => isStarted;

        /// <summary>The bed's volume and Rumble, once something has played the storm.</summary>
        public StormBed Bed => bed;

        /// <summary>
        /// The pitch the bed is scaled by: Enemy time, so Slow-mo slows the storm the way it slows Enemy sounds.
        /// <see cref="EnemyTime.NormalScale"/> when no Level drives it.
        /// </summary>
        public float PitchScale { get; set; } = EnemyTime.NormalScale;

        /// <summary>
        /// Plays the storm in <paramref name="stormClip"/>, shaped by <paramref name="envelope"/>. The first call makes the
        /// bed; a later one with the same clip (the next Level) carries on with it as it is, so the storm never restarts.
        /// </summary>
        public void Play(AssetReferenceT<AudioClip> stormClip, StormEnvelope envelope)
        {
            if (bed == null)
            {
                bed = new StormBed(envelope);
                bed.Hush(isHushed);
            }
            if (!ContentReferences.IsSet(stormClip))
            {
                return;
            }

            var key = stormClip.RuntimeKey;
            if (Equals(key, clipKey))
            {
                return;
            }

            clipKey = key;
            LoadAsync(key, destroyCancellationToken).Forget();
        }

        /// <summary>Whether the storm reaches the character. The bed fades in or out from where it is; it never starts or stops dead.</summary>
        public void SetAudible(bool audible)
        {
            bed?.SetAudible(audible);
        }

        /// <summary>
        /// Hushes the storm whatever the Level says, or lets the Level drive it again (<see cref="StormBed.Hush"/>). The
        /// boot hushes it as the player leaves for the Shop, so it fades out through the change instead of after it, and
        /// lets it go when the next Level plays.
        /// </summary>
        public void Hush(bool hush)
        {
            isHushed = hush;
            bed?.Hush(hush);
        }

        /// <summary>The Rumble: the bed swells, with a small pitch nudge, then settles back.</summary>
        public void Rumble(IRandom random)
        {
            bed?.Rumble(random);
        }

        private async UniTaskVoid LoadAsync(object key, CancellationToken cancellationToken)
        {
            content ??= new ContentScope(new AddressablesContentSource());
            var loaded = await content.LoadAssetAsync<AudioClip>(key, cancellationToken);
            if (loaded == null || !Equals(key, clipKey))
            {
                return;
            }

            // Decoded before it plays (WebGL decodes audio late); Update starts it once it's ready.
            loaded.LoadAudioData();
            clip = loaded;
            isStarted = false;
        }

        private void Update()
        {
            if (bed == null)
            {
                return;
            }

            bed.Tick(Time.deltaTime);
            source.SetVolume(bed.Volume);
            source.pitch = EnemyVoicePitch.For(bed.Pitch, PitchScale);

            if (!isStarted && clip != null && clip.loadState == AudioDataLoadState.Loaded)
            {
                isStarted = true;
                source.clip = clip;
                source.loop = true;
                source.Play();
            }
        }

        private void OnDestroy()
        {
            // The scene's objects that used this content are gone or going with it.
            content?.Dispose();
        }
    }
}
