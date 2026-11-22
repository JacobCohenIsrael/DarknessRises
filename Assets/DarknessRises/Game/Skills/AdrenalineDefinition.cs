using UnityEngine;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// Authored Adrenaline: the character's reflexes speed up, so Enemies seem to slow. How long Enemies and their sounds are slowed, the cooldown after, and how slow they get.
    /// Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Adrenaline Definition", fileName = "AdrenalineDefinition", order = 61)]
    public sealed class AdrenalineDefinition : ScriptableObject
    {
        [Header("Timing")]
        [SerializeField, Min(0.1f), Tooltip("Seconds Enemies stay slowed.")]
        private float durationSeconds = 4f;

        [SerializeField, Min(0f), Tooltip("Real-time seconds from the end of Adrenaline until it can be used again. Adrenaline doesn't slow it.")]
        private float cooldownSeconds = 15f;

        [Header("Strength")]
        [SerializeField, Range(0.05f, 1f), Tooltip("How fast time runs for Enemies while slowed: 1 is normal, 0.35 is about a third.")]
        private float enemyTimeScale = 0.35f;

        [SerializeField, Min(0f), Tooltip("Seconds Enemies take to slow down at the start and to speed back up at the end.")]
        private float easeSeconds = 0.25f;

        [Header("Heartbeat")]
        [SerializeField, Range(0f, 1f), Tooltip("Volume of the faint heartbeat (the HUD's fast heartbeat loop) while Adrenaline runs, before the Sound slider. 0 is silent.")]
        private float heartbeatVolume = 0.3f;

        [Header("HUD")]
        [SerializeField, Tooltip("Adrenaline's button on the HUD: its icon, colour, name and key badge.")]
        private SkillButtonLook button = new(new Color(1f, 0.36f, 0.42f, 0.95f), "ADRENALINE", "E");

        public SkillTiming Timing => new(durationSeconds, cooldownSeconds);
        public float EnemyTimeScale => enemyTimeScale;
        public float EaseSeconds => easeSeconds;
        public float HeartbeatVolume => heartbeatVolume;
        public SkillButtonLook Button => button;
    }
}
