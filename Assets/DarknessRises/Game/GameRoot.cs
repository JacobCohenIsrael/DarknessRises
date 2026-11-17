using DarknessRises.Game.Cameras;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Levels;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Player;
using DarknessRises.Game.Randomness;
using DarknessRises.Game.Rooms;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Waves;
using DarknessRises.Game.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR || DEBUG
using DarknessRises.Game.Debugging;
#endif

namespace DarknessRises.Game
{
    /// <summary>
    /// The scene's composition root: holds the root configuration asset and the scene references,
    /// and hands each view the definition it shows.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        [SerializeField] private GameConfiguration configuration;
        [SerializeField] private Transform character;
        [SerializeField] private AuraView aura;
        [SerializeField] private FixedAngleCameraView fixedAngleCamera;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private RouteWalker routeWalker;
        [SerializeField] private PrimaryWeaponView primaryWeapon;
        [SerializeField] private AmmoHudView ammoHud;
        [SerializeField] private CharacterAnimationView characterAnimation;
        [SerializeField] private Room room;
        [SerializeField] private LightningView lightning;

        [SerializeField, Tooltip("Debug/ToggleLightning. Only read in the Editor and debug builds.")]
        private InputActionReference toggleLightning;

        private LightningScheduler lightningScheduler;
#if UNITY_EDITOR || DEBUG
        private LightningDebugToggle lightningDebugToggle;
#endif
        [SerializeField] private RouteWavesView routeWaves;

        [SerializeField, Tooltip("The Level, in a Level scene. Without one (the Test Room), the Route's Waves run in the one Room.")]
        private LevelView level;

        private void Awake()
        {
            Apply();
            routeWalker.Walk(
                configuration.Player,
                configuration.FixedAngleCamera.LookAhead,
                character,
                cameraTarget);
            if (level != null)
            {
                level.Run(configuration.Level, routeWalker.Progress, character);
            }
            else
            {
                routeWaves.Run(configuration.Route, routeWalker.Progress, character);
            }

            ammoHud.Show(primaryWeapon.Equip(configuration.Pistol));
            characterAnimation.Show(configuration.Player.Animation, routeWalker.Progress, primaryWeapon.Weapon);
            lightningScheduler = new LightningScheduler(configuration.Lightning.Timing, new SystemRandom());
        }

        private void OnEnable()
        {
            lightningScheduler.Rumbled += lightning.PlayRumble;
            lightningScheduler.Flashed += lightning.Flash;
            if (level != null)
            {
                level.RoomEntered += EnterRoom;
            }

#if UNITY_EDITOR || DEBUG
            lightningDebugToggle = new LightningDebugToggle(toggleLightning, lightningScheduler, lightning);
#endif
        }

        private void Start()
        {
            lightningScheduler.EnterRoom(room.LetsLightningIn);
        }

        private void OnDisable()
        {
            lightningScheduler.Rumbled -= lightning.PlayRumble;
            lightningScheduler.Flashed -= lightning.Flash;
            if (level != null)
            {
                level.RoomEntered -= EnterRoom;
            }

#if UNITY_EDITOR || DEBUG
            lightningDebugToggle.Dispose();
            lightningDebugToggle = null;
#endif
        }

        private void Update()
        {
            lightningScheduler.Tick(Time.deltaTime);
        }

        // The character walked into another Room of the Level: Lightning comes through its windows, if it has any.
        private void EnterRoom(Room entered)
        {
            // Cut a flash short first, so the last Room's windows go dark too.
            lightning.StopFlash();
            room = entered;
            lightning.Show(configuration.Lightning, room);
            lightningScheduler.EnterRoom(room.LetsLightningIn);
        }

        /// <summary>Pushes the authored configuration into the scene's views.</summary>
        public void Apply()
        {
            aura.Show(configuration.Player.Aura);
            fixedAngleCamera.Frame(configuration.FixedAngleCamera, cameraTarget);
            lightning.Show(configuration.Lightning, room);
        }
    }
}
