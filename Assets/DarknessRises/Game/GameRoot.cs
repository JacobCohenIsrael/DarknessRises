using DarknessRises.Game.Cameras;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Player;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Waves;
using DarknessRises.Game.Weapons;
using UnityEngine;

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
        [SerializeField] private RouteWavesView routeWaves;

        private void Awake()
        {
            Apply();
            routeWalker.Walk(
                configuration.Player,
                configuration.FixedAngleCamera.LookAhead,
                character,
                cameraTarget);
            routeWaves.Run(configuration.Route, routeWalker.Progress, character);
            ammoHud.Show(primaryWeapon.Equip(configuration.Pistol));
        }

        /// <summary>Pushes the authored configuration into the scene's views.</summary>
        public void Apply()
        {
            aura.Show(configuration.Player.Aura);
            fixedAngleCamera.Frame(configuration.FixedAngleCamera, cameraTarget);
        }
    }
}
