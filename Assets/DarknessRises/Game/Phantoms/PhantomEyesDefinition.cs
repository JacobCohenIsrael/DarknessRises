using DarknessRises.Game.Randomness;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Phantoms
{
    /// <summary>
    /// Authored tuning for Phantom eyes: pairs of eyes that look out of the dark but aren't really there. They come only
    /// when no real Enemy is near and no fight is on, only far out where real eyes don't glint, and vanish as soon as
    /// light reaches their spot or the player aims at them, so real eyes stay a Tell the player can trust. Never changed
    /// at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Phantom Eyes Definition", fileName = "PhantomEyesDefinition", order = 46)]
    public sealed class PhantomEyesDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("The eyes: a prefab with two emissive eye renderers and a voice for the whisper.")]
        private AssetReferenceGameObject prefab = new(string.Empty);

        [Header("When")]
        [SerializeField, Min(0f), Tooltip("Chance per second of a sighting while one may come (walking, or the quiet after a clear, with no Enemy near). 0.02 is about one a minute of such time, cooldown aside.")]
        private float chancePerSecond = 0.02f;

        [SerializeField, Min(0f), Tooltip("Seconds after a sighting appears before another may. Keep them rare.")]
        private float cooldownSeconds = 45f;

        [SerializeField, Min(0f), Tooltip("Seconds from the Level's start before the first sighting may come.")]
        private float firstDelaySeconds = 20f;

        [SerializeField, Min(0f), Tooltip("Metres from the character within which a living Enemy keeps Phantom eyes away, and sends a showing pair away at once.")]
        private float enemyClearanceMetres = 14f;

        [Header("Where")]
        [SerializeField, Range(0f, 180f), Tooltip("Degrees either side of the character's facing the eyes may be. Wide: the Ride camera sees little far ahead, so far-out eyes are mostly off to the sides. Spots out of view are skipped anyway.")]
        private float aheadDegrees = 120f;

        [SerializeField, Min(0f), Tooltip("Fewest metres from the character they appear at: beyond the Aura and beyond the range real eyes glint at (7 m) and Critters' (7.5 m), at the edge of the dark.")]
        private float minMetres = 8.5f;

        [SerializeField, Min(0f), Tooltip("Most metres from the character they appear at.")]
        private float maxMetres = 12f;

        [SerializeField, Min(0f), Tooltip("If the character walks nearer to them than this, they blink out, so eyes that near are always real. Keep it at or beyond the range real eyes glint at.")]
        private float keepAwayMetres = 7.5f;

        [SerializeField, Min(0f), Tooltip("Metres of the character's walk ahead a spot must stay beyond the keep-away distance along, so the walk doesn't cut a sighting short. About the walking speed times the longest hold.")]
        private float walkMarginMetres = 5f;

        [SerializeField, Range(0f, 0.5f), Tooltip("The eyes must be drawn at least this share of the screen inside its edges, so the player can see them.")]
        private float screenMarginShare = 0.05f;

        [SerializeField, Min(1), Tooltip("Spots tried each time a sighting is due, before giving up until the next: a spot must be on the floor, in the dark, in view, with nothing between it and the character.")]
        private int placementAttempts = 16;

        [SerializeField, Min(0.01f), Tooltip("Metres a spot may be moved to find the floor (NavMesh) under it.")]
        private float floorSearchMetres = 1.5f;

        [Header("How long")]
        [SerializeField, Min(0f), Tooltip("Seconds the eyes take to open.")]
        private float openSeconds = 0.6f;

        [SerializeField, Min(0f), Tooltip("Fewest seconds a sighting holds, its opening included.")]
        private float minHoldSeconds = 2f;

        [SerializeField, Min(0f), Tooltip("Most seconds a sighting holds. A few seconds at most.")]
        private float maxHoldSeconds = 4f;

        [SerializeField, Min(0f), Tooltip("Seconds the eyes take to blink out when aimed at or done holding. Light makes them vanish at once.")]
        private float blinkOutSeconds = 0.08f;

        [SerializeField, Range(0f, 0.5f), Tooltip("How near the Crosshair (or a tap) must come to the eyes to send them away, as a share of the screen's height.")]
        private float aimRadiusShare = 0.08f;

        [Header("Look and sound")]
        [SerializeField, ColorUsage(false, true), Tooltip("Emission colour of the eyes at full strength. HDR. Match the Enemies' glint, or they'd be easy to tell apart.")]
        private Color color = new(3f, 0.7f, 0.15f);

        [SerializeField, Tooltip("A faint whisper or breath from the eyes' spot, one picked at random. Leave empty for silent eyes.")]
        private AssetReferenceT<AudioClip>[] whisperClips = new AssetReferenceT<AudioClip>[0];

        [SerializeField, Min(0f), Tooltip("Latest second into the whisper clip it may start from, picked at random, so it isn't the same every time.")]
        private float whisperLatestStartSeconds = 8f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Lowest pitch of the whisper.")]
        private float whisperMinPitch = 0.95f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Highest pitch of the whisper.")]
        private float whisperMaxPitch = 1.05f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume of the whisper at the eyes. Faint.")]
        private float whisperVolume = 0.25f;

        [SerializeField, Min(0f), Tooltip("Seconds the whisper fades out over when the eyes go.")]
        private float whisperFadeSeconds = 0.4f;

        [SerializeField, Min(0.1f), Tooltip("Metres beyond which the whisper can't be heard.")]
        private float hearingRange = 25f;

        public AssetReferenceGameObject Prefab => prefab;
        public float EnemyClearanceMetres => enemyClearanceMetres;
        public float WalkMarginMetres => walkMarginMetres;
        public float KeepAwayMetres => keepAwayMetres;
        public float ScreenMarginShare => screenMarginShare;
        public int PlacementAttempts => placementAttempts;
        public float FloorSearchMetres => floorSearchMetres;
        public float MinHoldSeconds => minHoldSeconds;
        public float MaxHoldSeconds => maxHoldSeconds;
        public float AimRadiusShare => aimRadiusShare;
        public Color Color => color;
        public AssetReferenceT<AudioClip>[] WhisperClips => whisperClips;
        public float WhisperLatestStartSeconds => whisperLatestStartSeconds;
        public float WhisperMinPitch => whisperMinPitch;
        public float WhisperMaxPitch => whisperMaxPitch;
        public float WhisperVolume => whisperVolume;
        public float WhisperFadeSeconds => whisperFadeSeconds;
        public float HearingRange => hearingRange;

        public PhantomEyesSchedule ToSchedule(IRandom random)
        {
            return new PhantomEyesSchedule(chancePerSecond, cooldownSeconds, firstDelaySeconds, enemyClearanceMetres, random);
        }

        public PhantomEyesSighting ToSighting()
        {
            return new PhantomEyesSighting(openSeconds, blinkOutSeconds, enemyClearanceMetres, keepAwayMetres);
        }

        public PhantomEyesSpot ToSpot()
        {
            return new PhantomEyesSpot(aheadDegrees, minMetres, maxMetres);
        }
    }
}
