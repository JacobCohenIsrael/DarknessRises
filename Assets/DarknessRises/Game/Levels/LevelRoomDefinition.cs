using System;
using System.Collections.Generic;
using DarknessRises.Game.Waves;
using UnityEngine;

namespace DarknessRises.Game.Levels
{
    /// <summary>
    /// The authored fights of one Room in a Level: its Stations' Waves and its Stragglers.
    /// The Room itself (walls, windows and whether it lets Lightning in, Obstacles, Route and Spawn points)
    /// is built in the Level scene; its <c>Room</c> component is matched to this entry by order.
    /// </summary>
    [Serializable]
    public sealed class LevelRoomDefinition
    {
        [SerializeField, Tooltip("Only for reading the list.")]
        private string name = "Room";

        [SerializeField, Tooltip(
            "The Waves at each of the Room's Stations, in the order the Route reaches them. " +
            "A Station without an entry here is cleared as soon as the character reaches it.")]
        private StationWavesDefinition[] stations = Array.Empty<StationWavesDefinition>();

        [SerializeField, Tooltip("Lone Enemies that come at the character while it walks between Stations in this Room.")]
        private StragglerSettings stragglers = new();

        public string Name => name;
        public IReadOnlyList<StationWavesDefinition> Stations => stations;
        public StragglerSettings Stragglers => stragglers;
    }
}
