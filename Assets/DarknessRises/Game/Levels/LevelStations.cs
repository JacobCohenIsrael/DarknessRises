using System;
using System.Collections.Generic;
using DarknessRises.Game.Routes;

namespace DarknessRises.Game.Levels
{
    /// <summary>Which Room each of a Level's Stations stands in, and which of that Room's Stations it is.</summary>
    public static class LevelStations
    {
        /// <summary>
        /// For each Station, in Route order: the Room it stands in, and its order among that Room's Stations, from 0.
        /// </summary>
        public static (int room, int indexInRoom)[] AssignToRooms(IReadOnlyList<Station> stations, RoomSequence rooms)
        {
            if (stations == null)
            {
                throw new ArgumentNullException(nameof(stations));
            }

            if (rooms == null)
            {
                throw new ArgumentNullException(nameof(rooms));
            }

            var assigned = new (int room, int indexInRoom)[stations.Count];
            var countInRoom = new int[rooms.RoomCount];
            for (var i = 0; i < stations.Count; i++)
            {
                var room = rooms.RoomAt(stations[i].Distance);
                assigned[i] = (room, countInRoom[room]);
                countInRoom[room]++;
            }

            return assigned;
        }
    }
}
