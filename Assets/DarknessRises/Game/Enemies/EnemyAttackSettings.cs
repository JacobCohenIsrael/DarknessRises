using System;
using UnityEngine;

namespace DarknessRises.Game.Enemies
{
    /// <summary>
    /// How an Enemy type attacks. Only <see cref="Range"/> is used so far (it's where the Enemy stops);
    /// the Wind-up, damage and Stagger tickets fill in the rest.
    /// </summary>
    [Serializable]
    public sealed class EnemyAttackSettings
    {
        [SerializeField, Min(0f), Tooltip("Metres from the character at which the Enemy stops closing in and attacks.")]
        private float range = 1.3f;

        [SerializeField, Min(0f), Tooltip("Seconds of visible and audible Wind-up before the attack lands.")]
        private float windUpSeconds = 0.8f;

        [SerializeField, Min(0), Tooltip("Character hit points one attack takes.")]
        private int damage = 1;

        [SerializeField, Min(0f), Tooltip("Seconds after an attack before the next Wind-up can start.")]
        private float cooldownSeconds = 1.5f;

        [SerializeField, Min(0f), Tooltip("Seconds the Enemy reels when hit during its Wind-up.")]
        private float staggerSeconds = 0.6f;

        public float Range => range;
        public float WindUpSeconds => windUpSeconds;
        public int Damage => damage;
        public float CooldownSeconds => cooldownSeconds;
        public float StaggerSeconds => staggerSeconds;
    }
}
