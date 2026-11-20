using DarknessRises.Game.Randomness;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Phantoms
{
    /// <summary>
    /// Authored tuning for Phantom eyes: pairs of eyes that look out of the dark but aren't really there. They come only
    /// when no real Enemy is near and no fight is on, only far out where real eyes don't glint, and vanish as soon as
    /// light reaches their spot or the player aims at them, so real eyes stay a Tell the player can trust. Never changed
    /// at runtime. They blink while they hold, sometimes open again nearby, and a voice (or a voice alone) may come from
    /// their spot.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Phantom Eyes Definition", fileName = "PhantomEyesDefinition", order = 46)]
    public sealed class PhantomEyesDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("The eyes: a prefab with two emissive eye renderers and a voice for the whisper.")]
        private AssetReferenceGameObject prefab = new(string.Empty);

        [Header("When")]
        [SerializeField, Min(0f), Tooltip("Fewest seconds of safe time (walking, or the quiet after a clear, with no Enemy near) between two events: a pair of eyes or a lone voice. Time in a fight or with an Enemy near doesn't count.")]
        private float minGapSeconds = 2f;

        [SerializeField, Min(0f), Tooltip("Most seconds of safe time between two events. With the fewest and the share of lone voices, this sets how often eyes come: a 2-4 s gap with a quarter of them voices is a pair about every 4 s of safe walking.")]
        private float maxGapSeconds = 4f;

        [SerializeField, Range(0f, 1f), Tooltip("Share of events that are a lone voice from a dark spot, with no eyes.")]
        private float voiceOnlyShare = 0.25f;

        [SerializeField, Min(0f), Tooltip("Seconds of safe time from the Level's start before the first event.")]
        private float firstDelaySeconds = 4f;

        [SerializeField, Min(0f), Tooltip("Seconds of safe time before trying again when an event found no dark spot (the Room lit, or no open floor in view). Short, so a bright moment doesn't cost a whole gap.")]
        private float retrySeconds = 1f;

        [SerializeField, Min(0f), Tooltip("Seconds after a flash that lit the whole Room (Lightning) fades in which due eyes open. They wait for that moment, so they get the whole dark stretch until the next strike: the flash showed nothing, then they're there.")]
        private float afterFlashSeconds = 0.2f;

        [SerializeField, Min(0f), Tooltip("Most seconds due eyes wait for a flash to fade before they open anyway, for a calm stretch with no Lightning.")]
        private float longestFlashWaitSeconds = 5f;

        [SerializeField, Min(1), Tooltip("Most pairs of eyes out at once, each at its own spot. A pair about to open again nearby counts.")]
        private int maxPairs = 4;

        [SerializeField, Min(0f), Tooltip("Fewest metres between two pairs, or a pair and a lone voice, so each looks out from its own spot.")]
        private float pairSeparationMetres = 3f;

        [SerializeField, Min(0f), Tooltip("Metres from the character within which a living Enemy keeps Phantom eyes away in safe time, and sends a safe-time pair away at once. Beyond it, Enemies may be about but not close.")]
        private float enemyClearanceMetres = 9f;

        [SerializeField, Min(0f), Tooltip("Metres from the eyes' own spot within which a living Enemy keeps them away, and sends a showing pair away at once, in safe time and in a fight. A phantom never sits next to a real threat.")]
        private float spotClearanceMetres = 6f;

        [Header("In a Wave fight")]
        [SerializeField, Min(0f), Tooltip("Fewest seconds of Wave fight between two pairs of eyes in a fight. Rare: the fight's real eyes come first. Never in the Boss fight.")]
        private float fightMinGapSeconds = 8f;

        [SerializeField, Min(0f), Tooltip("Most seconds of Wave fight between two pairs of eyes in a fight.")]
        private float fightMaxGapSeconds = 15f;

        [SerializeField, Min(0), Tooltip("Most pairs out at once during a Wave fight. 0: none in fights.")]
        private int fightMaxPairs = 1;

        [SerializeField, Min(0f), Tooltip("Fewest metres from the character eyes open at in a fight: the far edge of the dark, well beyond the keep-away distance and where real eyes glint.")]
        private float fightMinMetres = 10f;

        [SerializeField, Min(0f), Tooltip("Most metres from the character eyes open at in a fight.")]
        private float fightMaxMetres = 12f;

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
        private int placementAttempts = 32;

        [SerializeField, Min(0.01f), Tooltip("Metres a spot may be moved to find the floor (NavMesh) under it.")]
        private float floorSearchMetres = 1.5f;

        [Header("How long")]
        [SerializeField, Min(0f), Tooltip("Seconds the eyes take to open.")]
        private float openSeconds = 0.6f;

        [SerializeField, Min(0f), Tooltip("Fewest seconds a sighting holds, its opening included. Long enough for a blink or two.")]
        private float minHoldSeconds = 2.5f;

        [SerializeField, Min(0f), Tooltip("Most seconds a sighting holds. A few seconds at most.")]
        private float maxHoldSeconds = 4.5f;

        [SerializeField, Min(0f), Tooltip("Seconds the eyes take to blink out when aimed at or done holding. Light makes them vanish at once.")]
        private float blinkOutSeconds = 0.08f;

        [Header("Blinking")]
        [SerializeField, Min(0f), Tooltip("Seconds a blink takes while the eyes hold: a quick close and reopen. 0: they never blink.")]
        private float blinkSeconds = 0.16f;

        [SerializeField, Min(0f), Tooltip("Fewest seconds of open eyes before a blink.")]
        private float minBlinkGapSeconds = 0.3f;

        [SerializeField, Min(0f), Tooltip("Most seconds of open eyes before a blink. Uneven gaps read as alive; short, since Lightning (every few seconds) sends most pairs away within a few seconds.")]
        private float maxBlinkGapSeconds = 1.2f;

        [Header("Opening again nearby")]
        [SerializeField, Range(0f, 1f), Tooltip("Chance that a pair that held its time, or that light sent away (Lightning shows nothing, then they are back), opens again a moment later at another dark spot nearby. One sent away by aim, by the character coming near, by an Enemy or by a fight never does.")]
        private float hopChance = 0.65f;

        [SerializeField, Min(0), Tooltip("Most times one pair may open again nearby.")]
        private int maxHops = 2;

        [SerializeField, Min(0f), Tooltip("Fewest seconds of dark before the pair opens again, counted once no light reaches its old spot. At least the voice's fade, so its sound doesn't jump.")]
        private float minHopGapSeconds = 0.5f;

        [SerializeField, Min(0f), Tooltip("Most seconds of dark before the pair opens again.")]
        private float maxHopGapSeconds = 1.2f;

        [SerializeField, Min(0f), Tooltip("Fewest metres from the old spot to the new one: far enough to be another spot.")]
        private float minHopMetres = 2f;

        [SerializeField, Min(0f), Tooltip("Most metres from the old spot to the new one: near enough to be maybe the same thing.")]
        private float maxHopMetres = 5f;

        [Header("Aim")]
        [SerializeField, Range(0f, 0.5f), Tooltip("How near the Crosshair (or a tap) must come to the eyes to send them away, as a share of the screen's height.")]
        private float aimRadiusShare = 0.08f;

        [Header("Look and sound")]
        [SerializeField, ColorUsage(false, true), Tooltip("Emission colour of the eyes at full strength. HDR. Match the Enemies' glint, or they'd be easy to tell apart.")]
        private Color color = new(3f, 0.7f, 0.15f);

        [SerializeField, Tooltip("The voices from the eyes' spot (a whisper, a breath, a hush), one picked at random but never the same twice in a row. Ambient. Leave empty for silent eyes.")]
        private AssetReferenceT<AudioClip>[] whisperClips = new AssetReferenceT<AudioClip>[0];

        [SerializeField, Range(0f, 1f), Tooltip("Chance that a pair of eyes comes with a voice from its spot, each time it opens.")]
        private float voiceWithEyesChance = 0.7f;

        [SerializeField, Min(0f), Tooltip("Fewest seconds between two voices starting, with eyes or alone, so they don't pile up. Eyes that open sooner open silent; a lone voice due sooner waits.")]
        private float minVoiceGapSeconds = 4f;

        [SerializeField, Min(0f), Tooltip("Fewest seconds a lone voice plays before it fades.")]
        private float minVoiceOnlySeconds = 2.5f;

        [SerializeField, Min(0f), Tooltip("Most seconds a lone voice plays before it fades.")]
        private float maxVoiceOnlySeconds = 4.5f;

        [SerializeField, Range(0f, 180f), Tooltip("Degrees either side of the character's facing a lone voice may come from. Wide: a voice from behind needs no screen.")]
        private float voiceOnlyDegrees = 180f;

        [SerializeField, Min(0f), Tooltip("Latest second into the whisper clip it may start from, picked at random, so it isn't the same every time.")]
        private float whisperLatestStartSeconds = 8f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Lowest pitch of the whisper.")]
        private float whisperMinPitch = 0.95f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Highest pitch of the whisper.")]
        private float whisperMaxPitch = 1.05f;

        [SerializeField, Range(0f, 1f), Tooltip("Volume of the voice at its spot. Quiet but clear: it fades with distance (linear to the hearing range), so 0.7 at 10 m out is heard at about 0.43.")]
        private float whisperVolume = 0.7f;

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
        public int MaxPairs => maxPairs;
        public int FightMaxPairs => fightMaxPairs;
        public float SpotClearanceMetres => spotClearanceMetres;
        public float PairSeparationMetres => pairSeparationMetres;
        public float VoiceWithEyesChance => voiceWithEyesChance;
        public float MinVoiceOnlySeconds => minVoiceOnlySeconds;
        public float MaxVoiceOnlySeconds => maxVoiceOnlySeconds;
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
            return new PhantomEyesSchedule(
                minGapSeconds,
                maxGapSeconds,
                voiceOnlyShare,
                firstDelaySeconds,
                retrySeconds,
                enemyClearanceMetres,
                maxPairs,
                random);
        }

        public PhantomEyesSighting ToSighting(IRandom random)
        {
            return new PhantomEyesSighting(
                openSeconds,
                blinkOutSeconds,
                enemyClearanceMetres,
                keepAwayMetres,
                new PhantomBlinking(blinkSeconds, minBlinkGapSeconds, maxBlinkGapSeconds),
                random,
                spotClearanceMetres);
        }

        public PhantomEyesSpot ToSpot()
        {
            return new PhantomEyesSpot(aheadDegrees, minMetres, maxMetres);
        }

        /// <summary>Where a lone voice may come from: as far out as eyes, from any side within its angle.</summary>
        public PhantomEyesSpot ToVoiceSpot()
        {
            return new PhantomEyesSpot(voiceOnlyDegrees, minMetres, maxMetres);
        }

        public PhantomFightSchedule ToFightSchedule(IRandom random)
        {
            return new PhantomFightSchedule(fightMinGapSeconds, fightMaxGapSeconds, retrySeconds, fightMaxPairs, random);
        }

        /// <summary>Where eyes may open in a fight: the far edge of the dark, within the same angle as in safe time.</summary>
        public PhantomEyesSpot ToFightSpot()
        {
            return new PhantomEyesSpot(aheadDegrees, fightMinMetres, fightMaxMetres);
        }

        public PhantomAfterFlash ToAfterFlash()
        {
            return new PhantomAfterFlash(afterFlashSeconds, longestFlashWaitSeconds);
        }

        public PhantomVoiceGap ToVoiceGap()
        {
            return new PhantomVoiceGap(minVoiceGapSeconds);
        }

        public PhantomRelocation ToRelocation()
        {
            return new PhantomRelocation(hopChance, maxHops, minHopGapSeconds, maxHopGapSeconds, minHopMetres, maxHopMetres);
        }
    }
}
