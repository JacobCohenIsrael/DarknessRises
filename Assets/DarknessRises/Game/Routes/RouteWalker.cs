using DarknessRises.Game.Cameras;
using DarknessRises.Game.Player;
using UnityEngine;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// Walks the character along the scene's Route each frame and moves the camera's follow target ahead of it.
    /// The rules live in <see cref="RouteProgress"/>, <see cref="TimedStationClearing"/> and <see cref="RouteLookAhead"/>;
    /// this only feeds them frame time and shows where they put things.
    /// </summary>
    public sealed class RouteWalker : MonoBehaviour
    {
        [SerializeField] private RouteLayout route;

        private Transform character;
        private Transform cameraTarget;
        private TimedStationClearing stationClearing;
        private RouteLookAhead lookAhead;

        /// <summary>The character's progress along the Route, once walking has begun. Owners of Station fights listen here.</summary>
        public RouteProgress Progress { get; private set; }

        public void Walk(
            PlayerDefinition player,
            RouteDefinition definition,
            LookAheadSettings lookAheadSettings,
            Transform walkingCharacter,
            Transform followTarget)
        {
            stationClearing?.Dispose();

            character = walkingCharacter;
            cameraTarget = followTarget;

            var path = route.CreatePath();
            Progress = new RouteProgress(path, route.CreateStations(path), player.WalkingSpeed);
            stationClearing = new TimedStationClearing(Progress, definition.PlaceholderStationHoldSeconds);
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
            stationClearing.Tick(deltaSeconds);
            Progress.Tick(deltaSeconds);
            lookAhead.Tick(deltaSeconds, Progress);
            Show();
        }

        private void OnDestroy()
        {
            stationClearing?.Dispose();
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
