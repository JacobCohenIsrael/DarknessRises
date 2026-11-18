using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// Authored tuning for one Weapon. A new Weapon is a new definition asset. Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Weapon Definition", fileName = "WeaponDefinition", order = 30)]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField, Min(0.01f), Tooltip("Shots per second while the trigger is held.")]
        private float shotsPerSecond = 4f;

        [SerializeField, Min(1), Tooltip("Shots in a full magazine.")]
        private int magazineSize = 12;

        [SerializeField, Min(0f), Tooltip("Seconds a reload takes. A reload always refills the whole magazine.")]
        private float reloadSeconds = 1.2f;

        [SerializeField, Min(0.1f), Tooltip("Metres per second a Shot travels.")]
        private float shotSpeed = 30f;

        [SerializeField, Min(0), Tooltip("Damage one Shot deals to the Enemy it hits.")]
        private int damage = 1;

        [SerializeField, Min(0.1f), Tooltip("Metres a Shot travels before it fades out.")]
        private float range = 20f;

        [SerializeField, Min(0f), Tooltip("Degrees of random spread around the aim, edge to edge. 0 fires dead straight.")]
        private float spreadDegrees;

        [SerializeField, Tooltip("How far each hit pushes a regular Enemy along the Shot's direction. Bosses ignore it.")]
        private KnockbackSettings knockback = new();

        [SerializeField, Tooltip("How each Shot looks. It always glows enough to be seen in Darkness.")]
        private ShotSettings shot = new();

        [SerializeField, Tooltip("Whether Shots light their surroundings as they fly, as the Flamethrower's do.")]
        private ShotLightSettings shotLight = new();

        [SerializeField, Tooltip("The short, shadowless light at the Weapon each time it fires.")]
        private MuzzleFlashSettings muzzleFlash = new();

        [SerializeField, Tooltip("The small spark and light where a Shot hits something.")]
        private HitSparkSettings hitSpark = new();

        public float ShotsPerSecond => shotsPerSecond;
        public int MagazineSize => magazineSize;
        public float ReloadSeconds => reloadSeconds;
        public float ShotSpeed => shotSpeed;
        public int Damage => damage;
        public float Range => range;
        public float SpreadDegrees => spreadDegrees;
        public KnockbackSettings Knockback => knockback;
        public ShotSettings Shot => shot;
        public ShotLightSettings ShotLight => shotLight;
        public MuzzleFlashSettings MuzzleFlash => muzzleFlash;
        public HitSparkSettings HitSpark => hitSpark;

        /// <summary>A copy of the numbers the Primary weapon rule needs.</summary>
        public PrimaryWeaponStats PrimaryStats => new(shotsPerSecond, magazineSize, reloadSeconds);
    }
}
