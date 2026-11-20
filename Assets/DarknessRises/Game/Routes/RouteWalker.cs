using System;
using DarknessRises.Game.Player;
using UnityEngine;
using UnityEngine.AI;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// Walks the character along the scene's Route each frame. The rules live in <see cref="RouteProgress"/>; this
    /// only feeds it frame time and shows where it puts the character. Whoever owns the fights at Stations clears them
    /// through <see cref="Progress"/>, and the Ride camera follows it.
    /// </summary>
    /// <remarks>
    /// The character's transform has one owner at a time. The walker places it until a Boss fight takes it with
    /// <see cref="HandOverPlacement"/>; from then on the fight places the character (Relocate), until
    /// <see cref="TakeBackPlacement"/> once the fight is over. The walker then walks the character on from wherever the
    /// fight left it, back onto the Route (<see cref="RouteRejoin"/>), never snapping it there.
    /// </remarks>
    public sealed class RouteWalker : MonoBehaviour
    {
        [SerializeField] private RouteLayout route;

        private Transform character;
        private bool placesCharacter;
        private RouteRejoinSettings rejoin;
        private Func<Vector3, Vector3, bool> isWalkable;

        /// <summary>The character's progress along the Route, once walking has begun. Owners of Station fights listen here.</summary>
        public RouteProgress Progress { get; private set; }

        /// <summary>Whether the walker places the character; false while a Boss fight has taken placement over.</summary>
        public bool PlacesCharacter => placesCharacter;

        /// <summary>Stops placing the character: whoever calls this owns its position until <see cref="TakeBackPlacement"/>.</summary>
        public void HandOverPlacement()
        {
            placesCharacter = false;
        }

        /// <summary>
        /// Places the character again, from this frame on, starting where it stands: it walks from there back onto the
        /// Route and on, as <see cref="RouteRejoin"/> picks the way.
        /// </summary>
        public void TakeBackPlacement()
        {
            placesCharacter = true;
            if (Progress == null)
            {
                return;
            }

            if (!Progress.IsFinished)
            {
                var from = character.position;
                var joinAt = RouteRejoin.JoinDistance(
                    from, Progress.Path, Progress.Distance, Progress.NextStopDistance, rejoin.OnRouteMetres, rejoin.SampleMetres, isWalkable);
                Progress.Rejoin(from, joinAt);
            }

            Show();
        }

        /// <summary>
        /// Starts walking <paramref name="walkingCharacter"/> along the Route at the player's walking speed, easing into
        /// and out of each Station as <paramref name="easing"/> says.
        /// </summary>
        public void Walk(PlayerDefinition player, Transform walkingCharacter, WalkEasing easing = default)
        {
            character = walkingCharacter;
            placesCharacter = true;
            rejoin = player.Rejoin;
            isWalkable ??= IsWalkable;

            var path = route.CreatePath();
            Progress = new RouteProgress(path, route.CreateStations(path), player.WalkingSpeed, easing);
            Show();
        }

        private void Update()
        {
            if (Progress == null)
            {
                return;
            }

            Progress.Tick(Time.deltaTime);
            Show();
        }

        private void Show()
        {
            if (placesCharacter)
            {
                character.position = Progress.Position;
            }
        }

        // A straight walk is clear when the NavMesh has no edge (a wall, an Obstacle) between its ends. Where there's no
        // NavMesh under the character to check against, it is taken as clear: the fight only puts it on open floor.
        private bool IsWalkable(Vector3 from, Vector3 to)
        {
            var search = rejoin.NavMeshSearchMetres;
            if (!NavMesh.SamplePosition(from, out var start, search, NavMesh.AllAreas))
            {
                return true;
            }

            return NavMesh.SamplePosition(to, out var end, search, NavMesh.AllAreas)
                && !NavMesh.Raycast(start.position, end.position, out _, NavMesh.AllAreas);
        }

        private void Reset()
        {
            route = GetComponent<RouteLayout>();
        }
    }
}
