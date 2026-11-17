using System;
using UnityEngine;

namespace DarknessRises.Game.Enemies
{
    /// <summary>
    /// How an Enemy type attacks: where it stops, how long its Wind-up and Stagger last, what an attack costs the
    /// character, and how its body leans to show each of them.
    /// </summary>
    [Serializable]
    public sealed class EnemyAttackSettings
    {
        [SerializeField, Min(0f), Tooltip("Metres from the character at which the Enemy stops closing in and attacks. The attack lands only if the character is still this close when the Wind-up ends.")]
        private float range = 1.3f;

        [SerializeField, Min(0f), Tooltip("Seconds of visible and audible Wind-up before the attack lands.")]
        private float windUpSeconds = 0.8f;

        [SerializeField, Min(0), Tooltip("Character hit points one attack takes.")]
        private int damage = 1;

        [SerializeField, Min(0f), Tooltip("Seconds after an attack before the next Wind-up can start. The Enemy holds still meanwhile.")]
        private float cooldownSeconds = 1.5f;

        [SerializeField, Min(0f), Tooltip("Seconds the Enemy reels when hit during its Wind-up.")]
        private float staggerSeconds = 0.6f;

        [SerializeField, Range(0f, 60f), Tooltip("Degrees the Enemy rears back over its Wind-up.")]
        private float windUpLeanDegrees = 22f;

        [SerializeField, Range(0f, 60f), Tooltip("Degrees the Enemy lunges forward as it strikes, straightening up over the cooldown.")]
        private float attackLungeDegrees = 28f;

        [SerializeField, Range(0f, 60f), Tooltip("Degrees a Stagger knocks the Enemy back, straightening up over the Stagger.")]
        private float staggerLeanDegrees = 35f;

        public float Range => range;
        public float WindUpSeconds => windUpSeconds;
        public int Damage => damage;
        public float CooldownSeconds => cooldownSeconds;
        public float StaggerSeconds => staggerSeconds;
        public float WindUpLeanDegrees => windUpLeanDegrees;
        public float AttackLungeDegrees => attackLungeDegrees;
        public float StaggerLeanDegrees => staggerLeanDegrees;
    }
}
