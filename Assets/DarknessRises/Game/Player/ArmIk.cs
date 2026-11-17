using System;
using UnityEngine;

namespace DarknessRises.Game.Player
{
    /// <summary>
    /// One arm's bones and the grip its hand reaches for. Solved after the Animator and the upper body's twist,
    /// so the hand ends up on the grip whatever the walk and twist did to the shoulder.
    /// </summary>
    [Serializable]
    public struct ArmIk
    {
        private const float MinLength = 1e-4f;

        [SerializeField] private Transform upperArm;
        [SerializeField] private Transform forearm;
        [SerializeField] private Transform hand;

        [SerializeField, Tooltip("A finger bone. The hand turns so the line from the wrist to it runs along the grip's forward.")]
        private Transform fingers;

        [SerializeField, Tooltip("Where the hand goes, facing along the Weapon.")]
        private Transform grip;

        [SerializeField, Tooltip("Which way the elbow points, in the character's own space (x right, y up, z forward).")]
        private Vector3 elbowHint;

        public ArmIk(Transform upperArm, Transform forearm, Transform hand, Transform fingers, Transform grip, Vector3 elbowHint)
        {
            this.upperArm = upperArm;
            this.forearm = forearm;
            this.hand = hand;
            this.fingers = fingers;
            this.grip = grip;
            this.elbowHint = elbowHint;
        }

        public bool IsSet => upperArm != null && forearm != null && hand != null && grip != null;

        /// <summary>
        /// Moves the hand toward the grip by <paramref name="weight"/> (0 leaves the animated pose, 1 puts it on the grip),
        /// bending the elbow toward the hint in <paramref name="body"/>'s space.
        /// </summary>
        public void Solve(Transform body, float weight)
        {
            if (weight <= 0f || !IsSet)
            {
                return;
            }

            var shoulder = upperArm.position;
            var elbow = forearm.position;
            var wrist = hand.position;
            var target = Vector3.Lerp(wrist, grip.position, weight);
            var pole = elbow + body.TransformDirection(elbowHint);

            var upperLength = Vector3.Distance(shoulder, elbow);
            var lowerLength = Vector3.Distance(elbow, wrist);
            var reach = Mathf.Clamp(Vector3.Distance(shoulder, target), MinLength, upperLength + lowerLength - MinLength);

            // Bend the elbow to the angle that makes the arm exactly as long as the reach.
            var elbowAngle = Vector3.Angle(shoulder - elbow, wrist - elbow);
            var wantedAngle = Mathf.Acos(Mathf.Clamp(
                (upperLength * upperLength + lowerLength * lowerLength - reach * reach) / (2f * upperLength * lowerLength),
                -1f,
                1f)) * Mathf.Rad2Deg;
            var bendAxis = Vector3.Cross(shoulder - elbow, wrist - elbow);
            if (bendAxis.sqrMagnitude < MinLength * MinLength)
            {
                bendAxis = Vector3.Cross(shoulder - elbow, pole - elbow);
            }

            if (bendAxis.sqrMagnitude >= MinLength * MinLength)
            {
                forearm.rotation = Quaternion.AngleAxis(wantedAngle - elbowAngle, bendAxis.normalized) * forearm.rotation;
            }

            // Swing the whole arm so the hand reaches the target.
            upperArm.rotation = Quaternion.FromToRotation(hand.position - shoulder, target - shoulder) * upperArm.rotation;

            // Roll the arm about the shoulder–hand line so the elbow points toward the hint.
            var line = target - shoulder;
            var elbowDirection = Vector3.ProjectOnPlane(forearm.position - shoulder, line);
            var poleDirection = Vector3.ProjectOnPlane(pole - shoulder, line);
            if (elbowDirection.sqrMagnitude > MinLength && poleDirection.sqrMagnitude > MinLength)
            {
                upperArm.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(elbowDirection, poleDirection, line), line.normalized)
                    * upperArm.rotation;
            }

            if (fingers != null)
            {
                var along = Quaternion.FromToRotation(fingers.position - hand.position, grip.forward) * hand.rotation;
                hand.rotation = Quaternion.Slerp(hand.rotation, along, weight);
            }
        }
    }
}
