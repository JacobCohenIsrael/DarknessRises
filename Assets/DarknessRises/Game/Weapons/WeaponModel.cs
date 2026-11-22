using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// A Primary weapon's model in the character's hands: where the hands hold it, where its Shots and Muzzle flash
    /// start, and the Flamethrower's pilot flame. Its root sits on the weapon mount's recoil pivot, so its own space is
    /// the mount's (z along the aim, y up) while the gun is at rest.
    /// </summary>
    public sealed class WeaponModel : MonoBehaviour
    {
        [SerializeField, Tooltip("Where the right hand holds the gun, facing along it.")]
        private Transform rightGrip;

        [SerializeField, Tooltip("Where the left hand holds the gun, facing along it.")]
        private Transform leftGrip;

        [SerializeField, Tooltip("Where Shots and the Muzzle flash start: the end of the barrel or the nozzle.")]
        private Transform muzzle;

        [SerializeField, Tooltip("A small, unlit flame that always glows faintly at the nozzle, in the dark too. Not a light. Optional.")]
        private Transform pilotFlame;

        [SerializeField, Range(0f, 1f), Tooltip("How much the pilot flame's size wavers, as a share of its size.")]
        private float pilotFlicker = 0.3f;

        [SerializeField, Min(0f), Tooltip("How quickly the pilot flame wavers.")]
        private float pilotFlickerSpeed = 9f;

        private Vector3 pilotScale = Vector3.one;

        public Transform RightGrip => rightGrip;
        public Transform LeftGrip => leftGrip;
        public Transform Muzzle => muzzle;
        public Transform PilotFlame => pilotFlame;

        /// <summary>Where the muzzle is in the model's own space: the mount's, while the gun is at rest.</summary>
        public Vector3 MuzzlePoint => muzzle != null ? transform.InverseTransformPoint(muzzle.position) : Vector3.zero;

        private void Awake()
        {
            if (pilotFlame != null)
            {
                pilotScale = pilotFlame.localScale;
            }
            else
            {
                // Nothing to waver.
                enabled = false;
            }
        }

        private void Update()
        {
            var noise = Mathf.PerlinNoise(Time.time * pilotFlickerSpeed, 0.5f);
            pilotFlame.localScale = pilotScale * (1f - pilotFlicker * noise);
        }
    }
}
