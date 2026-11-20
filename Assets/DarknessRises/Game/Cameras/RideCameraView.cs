using System;
using DarknessRises.Game.Rooms;
using DarknessRises.Game.Routes;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace DarknessRises.Game.Cameras
{
    /// <summary>
    /// The Ride camera: two Cinemachine cameras and the point the first one follows. The walking camera stays low and
    /// close behind the character, aims ahead of it along the Route and swings toward where the Route heads. The
    /// Station camera is posed once, when the character plants, higher and wider and turned toward where the Room's
    /// Enemies come from; it is live from the Ready beat until the Go cue. Cinemachine owns the camera and blends
    /// between the two; this only hands it the framing the rules give (<see cref="RouteLookAhead"/>,
    /// <see cref="RouteHeading"/>, <see cref="RideSwing"/>, <see cref="StationFraming"/>).
    /// </summary>
    /// <remarks>
    /// Runs after the Route walker, so it frames where the character is this frame, and in Update, so Cinemachine's
    /// own update (later in the frame) sees it.
    /// </remarks>
    [DefaultExecutionOrder(AfterTheWalk)]
    public sealed class RideCameraView : MonoBehaviour
    {
        private const int AfterTheWalk = 300;

        // The walking camera is always live; the Station camera outranks it while it's held.
        private const int HeldPriority = 1;
        private const int ReleasedPriority = -1;

        [SerializeField, Tooltip("Blends between the cameras. On the Main Camera.")]
        private CinemachineBrain brain;

        [SerializeField, Tooltip("Follows the point ahead of the character while it walks.")]
        private CinemachineCamera walkCamera;

        [SerializeField] private CinemachineFollow walkFollow;

        [SerializeField, Tooltip("Stands still over a Station while the character holds it.")]
        private CinemachineCamera stationCamera;

        [SerializeField, Tooltip("The point the walking camera follows, a little ahead of the character along the Route.")]
        private Transform followTarget;

        private CameraDefinition definition;
        private RouteProgress progress;
        private StationRhythm rhythm;
        private RouteLookAhead lookAhead;
        private RideSwing swing;
        private Room room;
        private CinemachineBlenderSettings blends;
        private Action<StationBeat> onBeatChanged;
        private Action onRejoined;

        /// <summary>Degrees the walking camera is swung off north right now. 0 before the ride starts.</summary>
        public float SwingDegrees => swing != null ? swing.Degrees : 0f;

        /// <summary>Whether the Station camera is the live one.</summary>
        public bool HoldsStation => stationCamera.Priority.Value == HeldPriority;

        /// <summary>Gives both cameras the authored framing, with the walking camera looking north at its follow point.</summary>
        public void Frame(CameraDefinition cameraDefinition)
        {
            definition = cameraDefinition ? cameraDefinition : throw new ArgumentNullException(nameof(cameraDefinition));

            walkCamera.Follow = followTarget;
            SetFieldOfView(walkCamera, definition.Walking.FieldOfView);
            SetFieldOfView(stationCamera, definition.Station.FieldOfView);
            walkFollow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
            walkFollow.TrackerSettings.PositionDamping = definition.FollowDamping;
            stationCamera.Priority = ReleasedPriority;

            PoseWalkCamera(0f);
            walkCamera.transform.position = followTarget.position + walkFollow.FollowOffset;
        }

        /// <summary>
        /// Starts following the character's walk along <paramref name="routeProgress"/>, holding each Station to the
        /// beats of <paramref name="stationRhythm"/>. Station framings look toward the Spawn points of
        /// <paramref name="startRoom"/> until <see cref="EnterRoom"/> names another Room.
        /// </summary>
        public void Ride(RouteProgress routeProgress, StationRhythm stationRhythm, Room startRoom)
        {
            if (definition == null)
            {
                throw new InvalidOperationException("Frame the camera before it rides.");
            }

            StopRiding();

            progress = routeProgress ?? throw new ArgumentNullException(nameof(routeProgress));
            rhythm = stationRhythm ?? throw new ArgumentNullException(nameof(stationRhythm));
            room = startRoom;

            var settings = definition.LookAhead;
            lookAhead = new RouteLookAhead(settings.WalkingDistance, settings.StoppedDistance, settings.ChangeSpeed);
            swing = new RideSwing(definition.SwingEaseSeconds);
            swing.Snap(WalkSwingTarget());
            lookAhead.Tick(0f, progress, IsLeading());

            // The first frame starts in place rather than easing in from wherever the scene left the camera.
            followTarget.position = lookAhead.Target;
            PoseWalkCamera(swing.Degrees);
            walkCamera.ForceCameraPosition(followTarget.position + walkFollow.FollowOffset, walkCamera.transform.rotation);

            UseBlends();
            onBeatChanged ??= OnBeatChanged;
            rhythm.BeatChanged += onBeatChanged;
            onRejoined ??= OnRejoined;
            progress.Rejoined += onRejoined;
        }

        /// <summary>The character walked into <paramref name="entered"/>: Station framings look toward its Spawn points from now on.</summary>
        public void EnterRoom(Room entered)
        {
            room = entered;
        }

        private void Update()
        {
            if (progress == null)
            {
                return;
            }

            var deltaSeconds = Time.deltaTime;
            lookAhead.Tick(deltaSeconds, progress, IsLeading());
            followTarget.position = lookAhead.Target;
            swing.Tick(deltaSeconds, WalkSwingTarget());
            PoseWalkCamera(swing.Degrees);
        }

        private void OnDestroy()
        {
            StopRiding();
        }

        private void StopRiding()
        {
            if (rhythm != null)
            {
                rhythm.BeatChanged -= onBeatChanged;
                rhythm = null;
            }

            if (progress != null)
            {
                progress.Rejoined -= onRejoined;
            }

            if (blends != null)
            {
                if (brain != null && brain.CustomBlends == blends)
                {
                    brain.CustomBlends = null;
                }

                Destroy(blends);
                blends = null;
            }

            progress = null;
        }

        // The camera aims ahead while the character walks, and already on the Go cue, so it leads the set-off.
        private bool IsLeading()
        {
            return !progress.IsFinished && (!progress.IsAtStation || rhythm.Beat == StationBeat.Go);
        }

        private float WalkSwingTarget()
        {
            var heading = RouteHeading.Ahead(progress, definition.HeadingMetres);
            return RideSwing.TowardsDegrees(heading, definition.Walking.SwingDegrees);
        }

        private void PoseWalkCamera(float swingDegrees)
        {
            var walking = definition.Walking;
            walkFollow.FollowOffset = FixedAngleFraming.FollowOffset(walking.PitchDegrees, swingDegrees, walking.Distance);
            walkCamera.transform.rotation = FixedAngleFraming.Rotation(walking.PitchDegrees, swingDegrees);
        }

        // The character walks back onto the Route from wherever a Boss fight left it. The walking camera, not live during
        // the fight, is posed over it at once, so Cinemachine's blend from the Boss Room camera lands on the character
        // rather than on the Route where it left it.
        private void OnRejoined()
        {
            lookAhead.Tick(0f, progress, IsLeading());
            followTarget.position = lookAhead.Target;
            swing.Snap(WalkSwingTarget());
            PoseWalkCamera(swing.Degrees);
            walkCamera.ForceCameraPosition(followTarget.position + walkFollow.FollowOffset, walkCamera.transform.rotation);
        }

        private void OnBeatChanged(StationBeat beat)
        {
            var held = StationFraming.IsHeldIn(beat);
            if (held == HoldsStation)
            {
                return;
            }

            if (held)
            {
                PoseStationCamera();
            }

            stationCamera.Priority = held ? HeldPriority : ReleasedPriority;
        }

        // Posed once per stop: nothing moves the character at a Station, and a still camera keeps aiming steady.
        private void PoseStationCamera()
        {
            var settings = definition.Station;
            var stationPoint = progress.Position;
            var fightCentre = FightCentre(stationPoint);
            var focus = StationFraming.Focus(stationPoint, fightCentre, definition.StationFocusShare, definition.StationFocusMaxMetres);
            var swingDegrees = RideSwing.TowardsDegrees(fightCentre - stationPoint, settings.SwingDegrees);
            stationCamera.ForceCameraPosition(
                focus + FixedAngleFraming.FollowOffset(settings.PitchDegrees, swingDegrees, settings.Distance),
                FixedAngleFraming.Rotation(settings.PitchDegrees, swingDegrees));
        }

        // The middle of where the Room's Enemies enter; the Station itself in a Room with no Spawn points.
        private Vector3 FightCentre(Vector3 stationPoint)
        {
            var spawnPoints = room != null ? room.SpawnPoints : null;
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                return stationPoint;
            }

            var sum = Vector3.zero;
            for (var i = 0; i < spawnPoints.Count; i++)
            {
                sum += spawnPoints.PositionOf(i);
            }

            return sum / spawnPoints.Count;
        }

        // Cinemachine blends between the two cameras over the authored times; every other blend keeps the brain's default.
        private void UseBlends()
        {
            blends = ScriptableObject.CreateInstance<CinemachineBlenderSettings>();
            blends.CustomBlends = new[]
            {
                new CinemachineBlenderSettings.CustomBlend
                {
                    From = walkCamera.Name,
                    To = stationCamera.Name,
                    Blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, definition.StationBlendSeconds),
                },
                new CinemachineBlenderSettings.CustomBlend
                {
                    From = stationCamera.Name,
                    To = walkCamera.Name,
                    Blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, definition.WalkBlendSeconds),
                },
            };
            brain.CustomBlends = blends;
        }

        private static void SetFieldOfView(CinemachineCamera camera, float fieldOfView)
        {
            var lens = camera.Lens;
            lens.FieldOfView = fieldOfView;
            camera.Lens = lens;
        }
    }
}
