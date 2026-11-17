using UnityEngine;

namespace DarknessRises.Game.Grenades
{
    /// <summary>
    /// Authored tuning for the Grenade, the Secondary weapon: its charges, the throw, and the explosion.
    /// Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Grenade Definition", fileName = "Grenade", order = 31)]
    public sealed class GrenadeDefinition : ScriptableObject
    {
        [Header("Charges")]
        [SerializeField, Min(1), Tooltip("Grenades the character carries. It starts with all of them.")]
        private int charges = 3;

        [SerializeField, Min(0.1f), Tooltip("Seconds for one missing charge to refill. Charges refill one at a time.")]
        private float refillSeconds = 20f;

        [Header("Throw")]
        [SerializeField, Min(0.1f), Tooltip("Metres per second the Grenade travels across the floor toward the aim point.")]
        private float throwSpeed = 12f;

        [SerializeField, Min(0.5f), Tooltip("The farthest the Grenade can be thrown, in metres across the floor. Aim points beyond are pulled in.")]
        private float maxThrowRange = 14f;

        [SerializeField, Min(0.1f), Tooltip("Metres per second squared pulling the Grenade down. Higher makes the arc taller.")]
        private float gravity = 20f;

        [SerializeField, Min(0.05f), Tooltip("Seconds the shortest throw takes, so a Grenade thrown at the character's feet still arcs.")]
        private float minFlightSeconds = 0.35f;

        [SerializeField, Tooltip("How the Grenade looks in flight. It glows enough to be followed in Darkness.")]
        private GrenadeBodySettings body = new();

        [Header("Explosion")]
        [SerializeField, Min(0.1f), Tooltip("Metres from the explosion within which Enemies are damaged.")]
        private float blastRadius = 3.5f;

        [SerializeField, Min(0), Tooltip("Damage dealt to every Enemy in the blast radius.")]
        private int damage = 4;

        [SerializeField, Tooltip("The explosion's burst, light, sound and camera shake.")]
        private GrenadeExplosionSettings explosion = new();

        public float ThrowSpeed => throwSpeed;
        public float MaxThrowRange => maxThrowRange;
        public float Gravity => gravity;
        public float MinFlightSeconds => minFlightSeconds;
        public GrenadeBodySettings Body => body;
        public float BlastRadius => blastRadius;
        public int Damage => damage;
        public GrenadeExplosionSettings Explosion => explosion;

        /// <summary>A copy of the numbers the charges rule needs.</summary>
        public GrenadeChargeStats ChargeStats => new(charges, refillSeconds);

        /// <summary>The longest a Grenade can be in the air.</summary>
        public float MaxFlightSeconds => Mathf.Max(minFlightSeconds, maxThrowRange / throwSpeed);
    }
}
