using System;
using DarknessRises.Game.Randomness;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace DarknessRises.Game.Critters
{
    /// <summary>
    /// Authored tuning for Critters: small, harmless animals that dart through the dark to make the player jump. Not
    /// Enemies: they never hurt the character, and a Shot that kills one counts as neither a hit nor a miss. When they
    /// come, how they dash, how their eyes glint and how they sound. Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Critter Definition", fileName = "CritterDefinition", order = 45)]
    public sealed class CritterDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("What a Critter looks like: the pooled prefab, with its body, eyes, collider and voice.")]
        private AssetReferenceGameObject prefab = new(string.Empty);

        [SerializeField, Tooltip("When Critters come: how often in each moment of a Level, the cooldown and the cap.")]
        private CritterScheduleSettings schedule = new();

        [SerializeField, Tooltip("How a Critter dashes across the character's view, from one dark spot to another.")]
        private CritterDashSettings dash = new();

        [SerializeField, Tooltip("How a Critter's eyes glint: emissive, like an Enemy's, but only near the character.")]
        private CritterEyeSettings eyes = new();

        [SerializeField, Tooltip("How a Critter sounds: a scurry as it dashes, a squeak when a Shot kills it.")]
        private CritterSoundSettings sounds = new();

        public AssetReferenceGameObject Prefab => prefab;
        public CritterScheduleSettings Schedule => schedule;
        public CritterDashSettings Dash => dash;
        public CritterEyeSettings Eyes => eyes;
        public CritterSoundSettings Sounds => sounds;
    }

    [Serializable]
    public sealed class CritterScheduleSettings
    {
        [SerializeField, Min(0f), Tooltip("Chance per second of a Critter while the character walks between Stations. 0.05 is about one every 20 s, cooldown aside.")]
        private float walkingChancePerSecond = 0.05f;

        [SerializeField, Min(0f), Tooltip("Chance per second of a Critter during the Clear banner, the quiet after it and the Go cue (about 3 s in all). 0.25 makes one at about half the Stations.")]
        private float afterClearChancePerSecond = 0.25f;

        [SerializeField, Min(0f), Tooltip("Chance per second of a Critter during a Station's Ready banner and Waves. Keep it rare, so Critters never crowd a real fight. Boss fights never have one.")]
        private float waveFightChancePerSecond = 0.01f;

        [SerializeField, Min(0f), Tooltip("Seconds after a Critter darts before another may, whatever the moment.")]
        private float cooldownSeconds = 14f;

        [SerializeField, Min(0f), Tooltip("Seconds from the Level's start before the first Critter may come.")]
        private float firstDelaySeconds = 6f;

        [SerializeField, Min(0), Tooltip("Most Critters out at once. Also how many are pooled.")]
        private int maxAlive = 1;

        public int MaxAlive => maxAlive;

        public CritterSchedule ToSchedule(IRandom random)
        {
            return new CritterSchedule(
                new CritterRates(walkingChancePerSecond, afterClearChancePerSecond, waveFightChancePerSecond),
                cooldownSeconds,
                firstDelaySeconds,
                maxAlive,
                random);
        }
    }

    [Serializable]
    public sealed class CritterDashSettings
    {
        [SerializeField, Range(0f, 180f), Tooltip("Degrees either side of the character's facing the dash's middle may be: ahead, where the camera looks.")]
        private float aheadDegrees = 60f;

        [SerializeField, Min(0f), Tooltip("Fewest metres from the character the dash passes: beyond the Aura, at the edge of the light.")]
        private float minPassMetres = 4.5f;

        [SerializeField, Min(0f), Tooltip("Most metres from the character the dash passes. Within the eyes' glint range, so the eyes show.")]
        private float maxPassMetres = 7f;

        [SerializeField, Min(0f), Tooltip("Shortest dash, in metres.")]
        private float minLengthMetres = 5f;

        [SerializeField, Min(0f), Tooltip("Longest dash, in metres.")]
        private float maxLengthMetres = 8f;

        [SerializeField, Min(0.1f), Tooltip("Slowest a Critter runs, in metres per second. Length / speed is how long it's out: a second or two.")]
        private float minSpeed = 4.5f;

        [SerializeField, Min(0.1f), Tooltip("Fastest a Critter runs, in metres per second.")]
        private float maxSpeed = 6.5f;

        [SerializeField, Min(1), Tooltip("Dashes tried each time a Critter is due, before giving up until the next one: a dash must start and end in the dark, on the NavMesh, with no Obstacle between.")]
        private int placementAttempts = 6;

        [SerializeField, Min(0.01f), Tooltip("Metres a dash's end may be moved to find the NavMesh under it.")]
        private float floorSearchMetres = 1f;

        public int PlacementAttempts => placementAttempts;
        public float FloorSearchMetres => floorSearchMetres;

        public CritterDashShape ToShape()
        {
            return new CritterDashShape(aheadDegrees, minPassMetres, maxPassMetres, minLengthMetres, maxLengthMetres, minSpeed, maxSpeed);
        }
    }

    [Serializable]
    public sealed class CritterEyeSettings
    {
        [SerializeField, Min(0f), Tooltip("Metres from the character within which a Critter's eyes glint.")]
        private float range = 7.5f;

        [SerializeField, Min(0f), Tooltip("Metres inside the range over which the glint fades in.")]
        private float fadeDistance = 1.5f;

        [SerializeField, ColorUsage(false, true), Tooltip("Emission colour of a full glint. HDR. A little paler than an Enemy's.")]
        private Color color = new(2.2f, 1.4f, 0.35f);

        public float Range => range;
        public float FadeDistance => fadeDistance;
        public Color Color => color;
    }

    [Serializable]
    public sealed class CritterSoundSettings
    {
        [SerializeField, Tooltip("Played as a Critter dashes, one picked at random. Leave empty for a silent dash.")]
        private AssetReferenceT<AudioClip>[] scurryClips = new AssetReferenceT<AudioClip>[0];

        [SerializeField, Min(0f), FormerlySerializedAs("scurryStartSeconds"), Tooltip("Latest second into the scurry clip it may start from, picked at random each dash, so a long scratching or hissing clip isn't the same every time. Never so late that the clip runs out before the dash ends.")]
        private float scurryLatestStartSeconds = 6f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Lowest pitch of a scurry.")]
        private float scurryMinPitch = 0.95f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Highest pitch of a scurry.")]
        private float scurryMaxPitch = 1.15f;

        [SerializeField, Range(0f, 1f)]
        private float scurryVolume = 0.45f;

        [SerializeField, Min(0f), Tooltip("Seconds the scurry fades out over once the dash ends.")]
        private float scurryFadeSeconds = 0.2f;

        [SerializeField, Tooltip("Played when a Shot kills a Critter, one picked at random.")]
        private AssetReferenceT<AudioClip>[] squeakClips = new AssetReferenceT<AudioClip>[0];

        [SerializeField, Range(0.1f, 3f), Tooltip("Lowest pitch of a squeak.")]
        private float squeakMinPitch = 1f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Highest pitch of a squeak.")]
        private float squeakMaxPitch = 1.2f;

        [SerializeField, Range(0f, 1f)]
        private float squeakVolume = 0.6f;

        [SerializeField, Min(0.1f), Tooltip("Metres beyond which a Critter can't be heard.")]
        private float hearingRange = 18f;

        public AssetReferenceT<AudioClip>[] ScurryClips => scurryClips;
        public float ScurryLatestStartSeconds => scurryLatestStartSeconds;
        public float ScurryMinPitch => scurryMinPitch;
        public float ScurryMaxPitch => scurryMaxPitch;
        public float ScurryVolume => scurryVolume;
        public float ScurryFadeSeconds => scurryFadeSeconds;
        public AssetReferenceT<AudioClip>[] SqueakClips => squeakClips;
        public float SqueakMinPitch => squeakMinPitch;
        public float SqueakMaxPitch => squeakMaxPitch;
        public float SqueakVolume => squeakVolume;
        public float HearingRange => hearingRange;
    }
}
