using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// Authored Lure: how long the noisemaker draws Enemies, the cooldown after, how far it reaches and is thrown, how
    /// it flies, how its rattle sounds, and its small light: a dim beacon showing where it lies, not a second Flare.
    /// Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Lure Definition", fileName = "LureDefinition", order = 62)]
    public sealed class LureDefinition : ScriptableObject
    {
        [Header("Timing")]
        [SerializeField, Min(0.1f), Tooltip("Seconds the Lure rattles on the floor, drawing Enemies, unless one breaks it first.")]
        private float durationSeconds = 5f;

        [SerializeField, Min(0f), Tooltip("Real-time seconds from the end of the Lure (time up or broken) until it can be thrown again. Adrenaline doesn't slow it.")]
        private float cooldownSeconds = 18f;

        [Header("Reach")]
        [SerializeField, Min(0f), Tooltip("Metres across the floor from where the Lure lands within which Enemies turn and go for it. Bosses ignore it.")]
        private float radius = 6f;

        [SerializeField, Min(0f), Tooltip("Metres from the character a sideways Two-finger flick throws the Lure, that way.")]
        private float flickThrowMetres = 6f;

        [SerializeField, Min(0f), Tooltip("Most metres from the character a Lure thrown at the pointer (F) lands. A farther pointer throws it this far its way.")]
        private float maxThrowMetres = 9f;

        [SerializeField, Min(0f), Tooltip("Metres a throw may be moved to land on the walkable floor, so Enemies can reach it. A throw with no floor this close isn't made.")]
        private float floorSnapMetres = 3f;

        [Header("Flight")]
        [SerializeField, Min(0f), Tooltip("Seconds from the throw until the Lure lands. It draws nothing in the air.")]
        private float flightSeconds = 0.45f;

        [SerializeField, Min(0f), Tooltip("Metres per second squared pulling the Lure down, which sets how high its arc goes.")]
        private float gravity = 25f;

        [SerializeField, Min(0f), Tooltip("Metres above the character's feet it leaves the hand.")]
        private float releaseHeight = 1.2f;

        [Header("Rattle (Sound category, 3D)")]
        [SerializeField, Tooltip("The rattle, looped where the Lure lies while it draws Enemies. An approved placeholder.")]
        private AssetReferenceT<AudioClip> rattleClip = new(string.Empty);

        [SerializeField, Range(0f, 1f), Tooltip("The rattle's volume, before the Sound slider.")]
        private float rattleVolume = 0.8f;

        [SerializeField, Min(1f), Tooltip("Metres from the Lure beyond which the rattle can't be heard.")]
        private float rattleHearingRange = 22f;

        [Header("Light (shadowless point light on the can)")]
        [SerializeField, Tooltip("The light's colour and the can's glow: the Lure's light blue, as on its HUD button.")]
        private Color lightColor = new(0.55f, 0.85f, 1f);

        [SerializeField, Min(0f), Tooltip("URP point light intensity at full strength. Dim: a beacon, not a lamp (the Aura is 2, the Flare 14).")]
        private float lightIntensity = 1.2f;

        [SerializeField, Min(0f), Tooltip("Metres the light reaches. Enemies within it count as visible, for Aim assist too, so keep it short: Enemies striking the Lure stand in it, the rest of the Room stays dark.")]
        private float lightRange = 1.8f;

        [SerializeField, Min(0f), Tooltip("Metres above the floor the light sits, on the can.")]
        private float lightHeight = 0.35f;

        [SerializeField, Min(0f), Tooltip("Seconds per slow pulse of the light and the glow while the Lure is out. 0 for a steady light.")]
        private float pulseSeconds = 0.9f;

        [SerializeField, Range(0f, 1f), Tooltip("How far each pulse dims the light and the glow, as a share of full strength.")]
        private float pulseDepth = 0.35f;

        [SerializeField, Min(0f), Tooltip("Seconds the light and the glow take to die out once the Lure ends (time up or broken). The can stays until they're out.")]
        private float fadeOutSeconds = 0.6f;

        [SerializeField, Min(0f), Tooltip("How strongly the can glows in the light's colour at full strength (HDR emission), so it reads even at the edge of its light.")]
        private float glowEmission = 0.6f;

        [Header("HUD")]
        [SerializeField, Tooltip("The Lure's button on the HUD: its icon, colour, name and key badge.")]
        private SkillButtonLook button = new(new Color(0.55f, 0.85f, 1f, 0.95f), "LURE", "F");

        /// <summary>How long it draws Enemies once landed, and its cooldown, before Upgrades.</summary>
        public SkillTiming Timing => new(durationSeconds, cooldownSeconds);
        public float Radius => radius;
        public float FlickThrowMetres => flickThrowMetres;
        public float MaxThrowMetres => maxThrowMetres;
        public float FloorSnapMetres => floorSnapMetres;
        public float FlightSeconds => flightSeconds;
        public float Gravity => gravity;
        public float ReleaseHeight => releaseHeight;
        public AssetReferenceT<AudioClip> RattleClip => rattleClip;
        public float RattleVolume => rattleVolume;
        public float RattleHearingRange => rattleHearingRange;
        public Color LightColor => lightColor;
        public float LightIntensity => lightIntensity;
        public float LightRange => lightRange;
        public float LightHeight => lightHeight;
        public float PulseSeconds => pulseSeconds;
        public float PulseDepth => pulseDepth;
        public float FadeOutSeconds => fadeOutSeconds;
        public float GlowEmission => glowEmission;
        public SkillButtonLook Button => button;
    }
}
