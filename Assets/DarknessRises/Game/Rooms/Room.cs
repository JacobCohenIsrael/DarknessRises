using System.Collections.Generic;
using DarknessRises.Game.Waves;
using UnityEngine;

namespace DarknessRises.Game.Rooms
{
    /// <summary>
    /// One Room in the scene. It declares whether it lets Lightning in (Rooms with windows do),
    /// which way Lightning comes through its windows, the windows themselves, and where Enemies enter it.
    /// </summary>
    public sealed class Room : MonoBehaviour
    {
        [SerializeField, Tooltip("Rooms with windows let Lightning in. Rooms without windows get none.")]
        private bool letsLightningIn;

        [SerializeField, Tooltip("Lightning comes in along this transform's forward. Only its yaw counts; " +
                                 "the Lightning definition sets how low it comes in.")]
        private Transform lightningDirection;

        [SerializeField, Tooltip("What is seen through each window. These light up with every flash.")]
        private Renderer[] windows = new Renderer[0];

        [SerializeField, Tooltip("Where Enemies enter this Room.")]
        private RoomSpawnPoints spawnPoints;

        public bool LetsLightningIn => letsLightningIn;

        public RoomSpawnPoints SpawnPoints => spawnPoints;

        /// <summary>World yaw in degrees of the direction Lightning travels through the windows.</summary>
        public float LightningYawDegrees => (lightningDirection != null ? lightningDirection : transform).eulerAngles.y;

        public IReadOnlyList<Renderer> Windows => windows;
    }
}
