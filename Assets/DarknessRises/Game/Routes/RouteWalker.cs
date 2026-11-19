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
    /// <remarks>
    /// The character's transform has one owner at a time. The walker places it until a Boss fight takes it with
    /// <see cref="HandOverPlacement"/>; from then on the fight places the character (Relocate) and the walker only moves
    /// the camera's follow target, until <see cref="TakeBackPlacement"/> once the character is back on the Boss Station.
    /// </remarks>
    public sealed class RouteWalker : MonoBehaviour
    {
        [SerializeField] private RouteLayout route;

        private Transform character;
        private Transform cameraTarget;
        private RouteLookAhead lookAhead;
        private bool placesCharacter;

        /// <summary>The character's progress along the Route, once walking has begun. Owners of Station fights listen here.</summary>
        public RouteProgress Progress { get; private set; }

        /// <summary>Whether the walker places the character; false while a Boss fight has taken placement over.</summary>
        public bool PlacesCharacter => placesCharacter;

        /// <summary>Stops placing the character: whoever calls this owns its position until <see cref="TakeBackPlacement"/>.</summary>
        public void HandOverPlacement()
        {
            placesCharacter = false;
        }

        /// <summary>Places the character on the Route again, from this frame on.</summary>
        public void TakeBackPlacement()
        {
            placesCharacter = true;
            if (Progress != null)
            {
                Show();
            }
        }

        /// <summary>
        /// Starts walking <paramref name="walkingCharacter"/> along the Route at the player's walking speed, easing into
        /// and out of each Station as <paramref name="easing"/> says, with the camera's follow target ahead of it.
        /// </summary>
        public void Walk(
            PlayerDefinition player,
            LookAheadSettings lookAheadSettings,
            Transform walkingCharacter,
            Transform followTarget,
            WalkEasing easing = default)
        {
            character = walkingCharacter;
            cameraTarget = followTarget;
            placesCharacter = true;

            var path = route.CreatePath();
            Progress = new RouteProgress(path, route.CreateStations(path), player.WalkingSpeed, easing);
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
            if (placesCharacter)
            {
                character.position = Progress.Position;
            }

            cameraTarget.position = lookAhead.Target;
        }

        private void Reset()
        {
            route = GetComponent<RouteLayout>();
        }
    }
}
