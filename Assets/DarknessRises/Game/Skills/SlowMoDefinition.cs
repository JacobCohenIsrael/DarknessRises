using UnityEngine;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// Authored Slow-mo: how long Enemies and their sounds are slowed, the cooldown after, and how slow they get.
    /// Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Slow-mo Definition", fileName = "SlowMoDefinition", order = 61)]
    public sealed class SlowMoDefinition : ScriptableObject
    {
        [Header("Timing")]
        [SerializeField, Min(0.1f), Tooltip("Seconds Enemies stay slowed.")]
        private float durationSeconds = 4f;

        [SerializeField, Min(0f), Tooltip("Real-time seconds from the end of Slow-mo until it can be used again. Slow-mo doesn't slow it.")]
        private float cooldownSeconds = 15f;

        [Header("Strength")]
        [SerializeField, Range(0.05f, 1f), Tooltip("How fast time runs for Enemies while slowed: 1 is normal, 0.35 is about a third.")]
        private float enemyTimeScale = 0.35f;

        [SerializeField, Min(0f), Tooltip("Seconds Enemies take to slow down at the start and to speed back up at the end.")]
        private float easeSeconds = 0.25f;

        public SkillTiming Timing => new(durationSeconds, cooldownSeconds);
        public float EnemyTimeScale => enemyTimeScale;
        public float EaseSeconds => easeSeconds;
    }
}
