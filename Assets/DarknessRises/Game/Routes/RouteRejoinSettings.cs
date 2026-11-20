using System;
using UnityEngine;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// How the character walks back onto its Route once a Boss fight that moved it ends (<see cref="RouteRejoin"/>).
    /// It walks there at the walking speed, setting off as it does from any Station (the Station rhythm's easing).
    /// </summary>
    [Serializable]
    public sealed class RouteRejoinSettings
    {
        [SerializeField, Min(0f), Tooltip(
            "Metres from where it left the Route within which the character counts as still on it, and just walks the Route on.")]
        private float onRouteMetres = 0.25f;

        [SerializeField, Min(0.05f), Tooltip(
            "Metres apart of the Route points tried as places to rejoin, from the next Station back. Smaller finds a " +
            "farther point past an Obstacle, at more NavMesh checks (once per fight, never per frame).")]
        private float sampleMetres = 0.5f;

        [SerializeField, Min(0.01f), Tooltip(
            "Metres around the character and each Route point searched for the NavMesh, which says whether the straight " +
            "walk between them is clear.")]
        private float navMeshSearchMetres = 1f;

        public RouteRejoinSettings()
        {
        }

        public RouteRejoinSettings(float onRouteMetres, float sampleMetres, float navMeshSearchMetres)
        {
            this.onRouteMetres = onRouteMetres;
            this.sampleMetres = sampleMetres;
            this.navMeshSearchMetres = navMeshSearchMetres;
        }

        public float OnRouteMetres => onRouteMetres;
        public float SampleMetres => sampleMetres;
        public float NavMeshSearchMetres => navMeshSearchMetres;
    }
}
