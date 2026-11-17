using DarknessRises.Game.Enemies.Tells;
using UnityEngine;

namespace DarknessRises.Game.Enemies
{
    /// <summary>
    /// Authored tuning for one Enemy type: how tough it is, how it moves, looks, sounds and attacks, and its Tells.
    /// A new Enemy type is a new definition asset. Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Enemy Definition", fileName = "EnemyDefinition", order = 40)]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField, Min(1), Tooltip("Hit points of a freshly spawned Enemy. One Shot takes the Weapon's damage.")]
        private int hitPoints = 4;

        [SerializeField, Min(0f), Tooltip("Metres per second it moves. Compare with the character's walking speed.")]
        private float speed = 1.4f;

        [SerializeField, Min(0f), Tooltip("Metres per second squared it speeds up and slows down at.")]
        private float acceleration = 8f;

        [SerializeField, Min(0f), Tooltip("Degrees per second it turns at.")]
        private float turnSpeed = 240f;

        [SerializeField, Min(0.02f), Tooltip("Seconds between path updates toward the character. Lower follows more tightly and costs more.")]
        private float pathRefreshSeconds = 0.25f;

        [SerializeField, Tooltip("What it looks like: the pooled Enemy prefab, with its NavMesh agent, hitbox and renderers.")]
        private EnemyView prefab;

        [SerializeField] private EnemyAudioSettings audio = new();

        [SerializeField] private EnemyDeathSettings death = new();

        [SerializeField] private EnemyAttackSettings attack = new();

        [SerializeField, Tooltip("What gives it away in Darkness without lighting it: glinting eyes and positional sounds.")]
        private EnemyTellSettings tells = new();

        public int HitPoints => hitPoints;
        public float Speed => speed;
        public float Acceleration => acceleration;
        public float TurnSpeed => turnSpeed;
        public float PathRefreshSeconds => pathRefreshSeconds;
        public EnemyView Prefab => prefab;
        public EnemyAudioSettings Audio => audio;
        public EnemyDeathSettings Death => death;
        public EnemyAttackSettings Attack => attack;
        public EnemyTellSettings Tells => tells;
    }
}
