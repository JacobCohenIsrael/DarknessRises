using UnityEngine;

namespace DarknessRises.Game.Cameras
{
    /// <summary>
    /// Authored framing for the Ride camera: low and close behind the walking character, leading it along the Route
    /// and swinging toward where it heads, then settling higher and wider over a Station for the fight.
    /// Boss Rooms have their own framing, on the Boss's definition.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Camera Definition", fileName = "CameraDefinition", order = 20)]
    public sealed class CameraDefinition : ScriptableObject
    {
        [Header("Walking")]
        [SerializeField, Tooltip("The framing while the character walks the Route: low, close and swung toward where the Route heads.")]
        private RideFramingSettings walking = new(46f, 12.5f, 42f, 38f);

        [SerializeField, Tooltip("How far ahead along the Route the camera aims, so it frames more ahead than behind.")]
        private LookAheadSettings lookAhead = new();

        [SerializeField, Min(0f), Tooltip(
            "Metres ahead along the Route the camera reads the Route's heading from. Further starts the swing earlier before a corner " +
            "and steadies it through a zigzag.")]
        private float headingMetres = 8f;

        [SerializeField, Min(0f), Tooltip("Seconds the swing takes to close about two thirds of the way to a new heading. 0 snaps.")]
        private float swingEaseSeconds = 0.9f;

        [SerializeField, Tooltip("Seconds the camera takes to catch up with the point it follows on each axis.")]
        private Vector3 followDamping = new(0.5f, 0.5f, 0.5f);

        [Header("Station")]
        [SerializeField, Tooltip(
            "The framing while the character holds a Station: higher and wider, so the whole fight reads. " +
            "Its swing turns it toward the middle of the Room's Spawn points.")]
        private RideFramingSettings station = new(58f, 15.5f, 40f, 14f);

        [SerializeField, Range(0f, 1f), Tooltip(
            "How far from the character toward the middle of the Room's Spawn points the Station framing looks. " +
            "0 centres the character; more shows more of where the Enemies come from.")]
        private float stationFocusShare = 0.25f;

        [SerializeField, Min(0f), Tooltip("The Station framing never looks further than this from the character, in metres.")]
        private float stationFocusMaxMetres = 2.5f;

        [Header("Blends")]
        [SerializeField, Min(0f), Tooltip("Seconds the camera takes to settle into the Station framing when the character plants.")]
        private float stationBlendSeconds = 1.1f;

        [SerializeField, Min(0f), Tooltip("Seconds the camera takes to swing back behind the character on the Go cue.")]
        private float walkBlendSeconds = 1.3f;

        public RideFramingSettings Walking => walking;
        public LookAheadSettings LookAhead => lookAhead;
        public float HeadingMetres => headingMetres;
        public float SwingEaseSeconds => swingEaseSeconds;
        public Vector3 FollowDamping => followDamping;
        public RideFramingSettings Station => station;
        public float StationFocusShare => stationFocusShare;
        public float StationFocusMaxMetres => stationFocusMaxMetres;
        public float StationBlendSeconds => stationBlendSeconds;
        public float WalkBlendSeconds => walkBlendSeconds;
    }
}
