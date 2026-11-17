using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// A Route authored in the scene: the waypoints the character walks through, in order.
    /// A waypoint with a <see cref="StationMarker"/> is a Station.
    /// </summary>
    public sealed class RouteLayout : MonoBehaviour
    {
        private const float GizmoWaypointRadius = 0.15f;
        private const float GizmoStationRadius = 0.6f;

        [SerializeField, Tooltip("The points the character walks through, in order. Add a Station Marker to stop at one.")]
        private Transform[] waypoints = Array.Empty<Transform>();

        public RoutePath CreatePath()
        {
            var points = new Vector3[waypoints.Length];
            for (var i = 0; i < waypoints.Length; i++)
            {
                points[i] = waypoints[i].position;
            }

            return new RoutePath(points);
        }

        /// <summary>The Route's Stations in the order the character reaches them, measured along <paramref name="path"/>.</summary>
        public Station[] CreateStations(RoutePath path)
        {
            var stations = new List<Station>();
            for (var i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i].TryGetComponent<StationMarker>(out _))
                {
                    stations.Add(new Station(stations.Count, path.DistanceAtPoint(i)));
                }
            }

            return stations.ToArray();
        }

        private void OnDrawGizmos()
        {
            for (var i = 0; i < waypoints.Length; i++)
            {
                var waypoint = waypoints[i];
                if (waypoint == null)
                {
                    continue;
                }

                var isStation = waypoint.TryGetComponent<StationMarker>(out _);
                Gizmos.color = isStation ? Color.yellow : Color.cyan;
                Gizmos.DrawWireSphere(waypoint.position, isStation ? GizmoStationRadius : GizmoWaypointRadius);

                if (i + 1 < waypoints.Length && waypoints[i + 1] != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(waypoint.position, waypoints[i + 1].position);
                }
            }
        }
    }
}
