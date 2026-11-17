using System;

namespace DarknessRises.Game.Enemies
{
    /// <summary>What an Enemy is doing.</summary>
    public enum EnemyBehaviourState
    {
        /// <summary>Closing in on the character. The only state it moves in.</summary>
        Chasing,

        /// <summary>Giving its visible and audible Wind-up. A hit now Staggers it.</summary>
        WindingUp,

        /// <summary>Just attacked; it can't start another Wind-up yet.</summary>
        Recovering,

        /// <summary>Reeling from a hit during its Wind-up. The attack was cancelled.</summary>
        Staggered,

        Dead,
    }

    /// <summary>
    /// An Enemy's behaviour: it chases the character, and once in range gives a Wind-up and then attacks. The attack
    /// lands only if the character is still in range when the Wind-up ends. A hit during the Wind-up Staggers it and
    /// cancels the attack. Plain C# (ADR 0003); it reads hits and death from its <see cref="Enemy"/>, and time comes
    /// in as an explicit delta. Pooled with its Enemy, so <see cref="Begin"/> starts it over.
    /// </summary>
    public sealed class EnemyBehaviour
    {
        private readonly Enemy enemy;
        private readonly float windUpSeconds;
        private readonly int damage;
        private readonly float cooldownSeconds;
        private readonly float staggerSeconds;
        private float stateSeconds;
        private float stateDuration;

        public EnemyBehaviour(Enemy enemy, float windUpSeconds, int damage, float cooldownSeconds, float staggerSeconds)
        {
            this.enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            this.windUpSeconds = NotNegative(windUpSeconds, nameof(windUpSeconds));
            this.damage = damage >= 0 ? damage : throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage can't be negative.");
            this.cooldownSeconds = NotNegative(cooldownSeconds, nameof(cooldownSeconds));
            this.staggerSeconds = NotNegative(staggerSeconds, nameof(staggerSeconds));

            enemy.Hurt += OnHurt;
            enemy.Died += OnDied;
            State = enemy.IsAlive ? EnemyBehaviourState.Chasing : EnemyBehaviourState.Dead;
        }

        /// <summary>Raised when the Wind-up starts.</summary>
        public event Action WindUpStarted;

        /// <summary>Raised when the Wind-up ends with the character in range, with the hit points the attack takes.</summary>
        public event Action<int> AttackLanded;

        /// <summary>Raised when the Wind-up ends with the character out of range.</summary>
        public event Action AttackMissed;

        /// <summary>Raised when a hit during the Wind-up cancels the attack.</summary>
        public event Action Staggered;

        public EnemyBehaviourState State { get; private set; }

        /// <summary>How far through the current timed state (Wind-up, recovery or Stagger) it is, from 0 to 1. 0 otherwise.</summary>
        public float StateProgress => stateDuration > 0f ? Math.Min(1f, stateSeconds / stateDuration) : 0f;

        /// <summary>Whether it should be closing in on the character. It holds still while winding up, recovering or reeling.</summary>
        public bool IsChasing => State == EnemyBehaviourState.Chasing;

        /// <summary>Starts the behaviour over for a freshly spawned Enemy.</summary>
        public void Begin()
        {
            Enter(enemy.IsAlive ? EnemyBehaviourState.Chasing : EnemyBehaviourState.Dead, 0f);
        }

        /// <summary>Moves time on. <paramref name="characterInRange"/> says whether the character is within attack range now.</summary>
        public void Tick(float deltaSeconds, bool characterInRange)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            switch (State)
            {
                case EnemyBehaviourState.Chasing:
                    if (characterInRange)
                    {
                        Enter(EnemyBehaviourState.WindingUp, windUpSeconds);
                        WindUpStarted?.Invoke();
                    }

                    return;

                case EnemyBehaviourState.WindingUp:
                    if (!RunOut(deltaSeconds))
                    {
                        return;
                    }

                    Enter(EnemyBehaviourState.Recovering, cooldownSeconds);
                    if (characterInRange)
                    {
                        AttackLanded?.Invoke(damage);
                    }
                    else
                    {
                        AttackMissed?.Invoke();
                    }

                    return;

                case EnemyBehaviourState.Recovering:
                case EnemyBehaviourState.Staggered:
                    if (RunOut(deltaSeconds))
                    {
                        Enter(EnemyBehaviourState.Chasing, 0f);
                    }

                    return;
            }
        }

        private bool RunOut(float deltaSeconds)
        {
            stateSeconds += deltaSeconds;
            return stateSeconds >= stateDuration;
        }

        private void Enter(EnemyBehaviourState state, float duration)
        {
            State = state;
            stateSeconds = 0f;
            stateDuration = duration;
        }

        private void OnHurt(Enemy _)
        {
            if (State != EnemyBehaviourState.WindingUp)
            {
                return;
            }

            Enter(EnemyBehaviourState.Staggered, staggerSeconds);
            Staggered?.Invoke();
        }

        private void OnDied(Enemy _)
        {
            Enter(EnemyBehaviourState.Dead, 0f);
        }

        private static float NotNegative(float seconds, string name)
        {
            return seconds >= 0f ? seconds : throw new ArgumentOutOfRangeException(name, seconds, "Seconds can't be negative.");
        }
    }
}
