using DarknessRises.Game.Cameras;
using DarknessRises.Game.Player;
using UnityEngine;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// Walks the character along the scene's Route each frame and moves the camera's follow target ahead of it.
    /// The rules live in <see cref="RouteProgress"/> and <see cref="RouteLookAhead"/>; this only feeds them frame time
    /// and shows where they put things. Whoever owns the fights at Stations clears them through <see cref="Progress"/>.
    /// </summary>
    public sealed class RouteWalker : MonoBehaviour
    {
        [SerializeField] private RouteLayout route;

        private Transform character;
        private Transform cameraTarget;
        private RouteLookAhead lookAhead;

        /// <summary>The character's progress along the Route, once walking has begun. Owners of Station fights listen here.</summary>
        public RouteProgress Progress { get; private set; }

        public void Walk(
            PlayerDefinition player,
            LookAheadSettings lookAheadSettings,
            Transform walkingCharacter,
            Transform followTarget)
        {
            character = walkingCharacter;
            cameraTarget = followTarget;

            var path = route.CreatePath();
            Progress = new RouteProgress(path, route.CreateStations(path), player.WalkingSpeed);
            lookAhead = new RouteLookAhead(
                lookAheadSettings.WalkingDistance,
                lookAheadSettings.StoppedDistance,
                lookAheadSettings.ChangeSpeed);

            lookAhead.Tick(0f, Progress);
            Show();
        }

        private void Update()
        {
            if (Progress == null)
            {
                return;
            }

            var deltaSeconds = Time.deltaTime;
            Progress.Tick(deltaSeconds);
            lookAhead.Tick(deltaSeconds, Progress);
            Show();
        }

        private void Show()
        {
            character.position = Progress.Position;
            cameraTarget.position = lookAhead.Target;
        }

        private void Reset()
        {
            route = GetComponent<RouteLayout>();
        }
    }
}
