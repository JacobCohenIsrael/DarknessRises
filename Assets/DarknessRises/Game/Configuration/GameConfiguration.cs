using DarknessRises.Game.Audio;
using DarknessRises.Game.Cameras;
using DarknessRises.Game.Continues;
using DarknessRises.Game.Grenades;
using DarknessRises.Game.Levels;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Player;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Skills;
using DarknessRises.Game.Weapons;
using UnityEngine;

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
        [SerializeField] private CameraDefinition fixedAngleCamera;
        [SerializeField] private RouteDefinition route;
        [SerializeField] private WeaponDefinition pistol;
        [SerializeField] private WeaponDefinition machineGun;
        [SerializeField] private WeaponDefinition flamethrower;
        [SerializeField] private LightningDefinition lightning;
        [SerializeField] private FlareDefinition flare;
        [SerializeField] private SlowMoDefinition slowMo;
        [SerializeField] private AudioDefinition audio;
        [SerializeField, Tooltip("The Level the game boots into.")] private LevelDefinition level;
        [SerializeField] private GrenadeDefinition grenade;
        [SerializeField, Tooltip("How each Level after the first gets harder.")] private RampDefinition ramp;
        [SerializeField, Tooltip("Coming back after death, once per Run, for watching an ad.")] private ContinueDefinition @continue;

        public PlayerDefinition Player => player;
        public CameraDefinition FixedAngleCamera => fixedAngleCamera;
        public RouteDefinition Route => route;
        public WeaponDefinition Pistol => pistol;
        public WeaponDefinition MachineGun => machineGun;
        public WeaponDefinition Flamethrower => flamethrower;
        public LightningDefinition Lightning => lightning;
        public FlareDefinition Flare => flare;
        public SlowMoDefinition SlowMo => slowMo;
        public AudioDefinition Audio => audio;
        public LevelDefinition Level => level;
        public GrenadeDefinition Grenade => grenade;
        public RampDefinition Ramp => ramp;
        public ContinueDefinition Continue => @continue;
    }
}
