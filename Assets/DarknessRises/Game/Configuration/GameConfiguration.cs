using DarknessRises.Game.Cameras;
using DarknessRises.Game.Continues;
using DarknessRises.Game.Gestures;
using DarknessRises.Game.Grenades;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Levels;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Player;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Runs;
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
        [SerializeField, Tooltip("Touch Gestures and Aim assist.")] private GestureDefinition gestures;

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

        [SerializeField, Tooltip("The reticle where the player aims, which kicks with every Shot.")]
        private CrosshairSettings crosshair = new();

        [SerializeField, Tooltip("The first-run hint over the first Enemy: tap to shoot.")]
        private ShootHintSettings shootHint = new();

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
        public ReloadFeedbackSettings ReloadFeedback => reloadFeedback;
        public GunshotVoiceSettings GunshotVoices => gunshotVoices;
        public BossHealthBarSettings BossHealthBar => bossHealthBar;
        public StationRhythmSettings StationRhythm => stationRhythm;
        public StationBannerSettings StationBanners => stationBanners;
        public CrosshairSettings Crosshair => crosshair;
        public ShootHintSettings ShootHint => shootHint;
    }
}
