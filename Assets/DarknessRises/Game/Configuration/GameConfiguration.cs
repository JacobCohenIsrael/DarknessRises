using DarknessRises.Game.Ambience;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Cameras;
using DarknessRises.Game.Continues;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Credits;
using DarknessRises.Game.Critters;
using DarknessRises.Game.Enemies.Tells;
using DarknessRises.Game.Gestures;
using DarknessRises.Game.Grenades;
using DarknessRises.Game.Health;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Levels;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Phantoms;
using DarknessRises.Game.Player;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Scoring;
using DarknessRises.Game.Shop;
using DarknessRises.Game.Skills;
using DarknessRises.Game.Weapons;
using UnityEngine;
using UnityEngine.Serialization;

namespace DarknessRises.Game.Configuration
{
    /// <summary>
    /// The root configuration asset. Every authored definition the game reads is reachable from here,
    /// so tuning a number never needs a code change (ADR 0003). Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Game Configuration", fileName = "GameConfiguration", order = 0)]
    public sealed class GameConfiguration : ScriptableObject
    {
        [SerializeField] private PlayerDefinition player;
        [SerializeField, FormerlySerializedAs("fixedAngleCamera"), Tooltip("The Ride camera's framings: walking and at a Station.")]
        private CameraDefinition rideCamera;
        [SerializeField] private RouteDefinition route;
        [SerializeField] private WeaponDefinition pistol;
        [SerializeField] private WeaponDefinition machineGun;
        [SerializeField] private WeaponDefinition flamethrower;
        [SerializeField] private LightningDefinition lightning;
        [SerializeField] private FlareDefinition flare;
        [SerializeField] private SlowMoDefinition slowMo;
        [SerializeField, Tooltip("The Level the game boots into.")] private LevelDefinition level;
        [SerializeField] private GrenadeDefinition grenade;
        [SerializeField, Tooltip("How each Level after the first gets harder.")] private RampDefinition ramp;
        [SerializeField, Tooltip("Coming back after death, once per Run, for watching an ad.")] private ContinueDefinition @continue;
        [SerializeField, Tooltip("Touch Gestures and Aim assist on touch.")] private GestureDefinition gestures;

        [SerializeField, Tooltip(
            "Aim assist with the mouse (WebGL and the Editor): a slight pull toward a visible Enemy near the pointer, "
            + "growing in later Levels. Touch's is on the Gesture definition.")]
        private AimAssistSettings mouseAimAssist = new(
            radius: 1.2f, strength: 0.15f, strengthPerLevel: 0.03f, maxStrength: 0.4f, fullPullFraction: 0.4f);

        [SerializeField, Tooltip("How the Primary weapons' ammo reserves refill during a Run. Each Weapon's own reserve is on its definition.")]
        private AmmoSettings ammo = new();

        [SerializeField, Tooltip("The reload meter and sounds, shared by every Primary weapon.")]
        private ReloadFeedbackSettings reloadFeedback = new();

        [SerializeField, Tooltip("The voices on the character that every Primary weapon's Gunshots share.")]
        private GunshotVoiceSettings gunshotVoices = new();

        [SerializeField, Tooltip("The health bar across the top of the HUD during a Boss fight, shared by every Boss.")]
        private BossHealthBarSettings bossHealthBar = new();

        [SerializeField, Tooltip("The beats of every stop at a Station (ready, fight, clear, quiet, go) and how the walk eases into and out of it.")]
        private StationRhythmSettings stationRhythm = new();

        [SerializeField, Tooltip("The Ready and Clear banners and the Go cue shown to the Station rhythm, with their sounds.")]
        private StationBannerSettings stationBanners = new();

        [SerializeField, Tooltip("The Weapon button beside the ammo count: the equipped Primary weapon and its ammo reserve.")]
        private WeaponButtonSettings weaponButton = new();

        [SerializeField, Tooltip("The reticle where the player aims, which kicks with every Shot.")]
        private CrosshairSettings crosshair = new();

        [SerializeField, Tooltip("The first-run hint over the first Enemy: tap to shoot.")]
        private ShootHintSettings shootHint = new();

        [SerializeField, Tooltip("How every Skill button on the HUD behaves: the cooldown sweep and the ready pulse. Each Skill's own look is on its definition.")]
        private SkillHudSettings skillHud = new();

        [SerializeField, Tooltip("The red at the screen's edges when the character is hurt: a flash on each hit, and a pulse with a heartbeat on the last hit points.")]
        private HurtFeedbackSettings hurtFeedback = new();

        [SerializeField, Tooltip("The Score's rules: the Combo, and what each Grade of a Station's fight requires.")]
        private ScoreSettings score = new();

        [SerializeField, Tooltip("How the Score shows: the running Score and Combo, the points over a kill, the hit marker and the result card.")]
        private ScoreHudSettings scoreHud = new();

        [SerializeField, Tooltip("Credits: what a cleared Level pays, the end-of-Run ad, and how the end screen shows them. What a kill pays is on each Enemy type and Boss.")]
        private CreditsDefinition credits;

        [SerializeField, Tooltip("The Shop, open only between Runs: the Primary weapons it sells with their prices, and how its screen reads and looks.")]
        private ShopDefinition shop;

        [SerializeField, Tooltip("The volume sliders for Music, Sound and Ambient: their defaults, how loudness follows them, the sample sound and their panel's wording.")]
        private AudioVolumeSettings audioVolumes = new();
        [SerializeField, Tooltip("The voices every Enemy's footsteps share, and how many Enemies are heard stepping at once. Each Enemy type's own steps are on its definition.")]
        private FootstepVoiceSettings footstepVoices = new();

        [SerializeField, Tooltip("Critters: small, harmless animals that dart through the dark to make the player jump. Not Enemies. Leave empty for none.")]
        private CritterDefinition critters;

        [SerializeField, Tooltip("Phantom eyes: eyes that look out of the dark but aren't really there, far out, when no Enemy is near. Leave empty for none.")]
        private PhantomEyesDefinition phantomEyes;

        [SerializeField, Tooltip("The ambient bed: now and then a quiet sound from the dark up near the ceiling, such as bats. Leave empty for none.")]
        private AmbienceDefinition ambience;

        public PlayerDefinition Player => player;
        public CameraDefinition RideCamera => rideCamera;
        public RouteDefinition Route => route;
        public WeaponDefinition Pistol => pistol;
        public WeaponDefinition MachineGun => machineGun;
        public WeaponDefinition Flamethrower => flamethrower;
        public LightningDefinition Lightning => lightning;
        public FlareDefinition Flare => flare;
        public SlowMoDefinition SlowMo => slowMo;
        public LevelDefinition Level => level;
        public GrenadeDefinition Grenade => grenade;
        public RampDefinition Ramp => ramp;
        public ContinueDefinition Continue => @continue;
        public GestureDefinition Gestures => gestures;
        public AimAssistSettings MouseAimAssist => mouseAimAssist;
        public AmmoSettings Ammo => ammo;
        public ReloadFeedbackSettings ReloadFeedback => reloadFeedback;
        public GunshotVoiceSettings GunshotVoices => gunshotVoices;
        public BossHealthBarSettings BossHealthBar => bossHealthBar;
        public StationRhythmSettings StationRhythm => stationRhythm;
        public StationBannerSettings StationBanners => stationBanners;
        public WeaponButtonSettings WeaponButton => weaponButton;
        public CrosshairSettings Crosshair => crosshair;
        public ShootHintSettings ShootHint => shootHint;
        public ScoreSettings Score => score;
        public ScoreHudSettings ScoreHud => scoreHud;
        public CreditsDefinition Credits => credits;
        public ShopDefinition Shop => shop;
        public AudioVolumeSettings AudioVolumes => audioVolumes;
        public FootstepVoiceSettings FootstepVoices => footstepVoices;
        public CritterDefinition Critters => critters;
        public PhantomEyesDefinition PhantomEyes => phantomEyes;
        public AmbienceDefinition Ambience => ambience;
        public SkillHudSettings SkillHud => skillHud;
        public HurtFeedbackSettings HurtFeedback => hurtFeedback;
    }
}
