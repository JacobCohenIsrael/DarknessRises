using DarknessRises.Game.Enemies;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Enemies
{
    public sealed class EnemyBehaviourTests
    {
        private const float WindUp = 0.8f;
        private const int Damage = 1;
        private const float Cooldown = 1.5f;
        private const float Stagger = 0.6f;

        private Enemy enemy;
        private EnemyBehaviour behaviour;
        private int windUps;
        private int landedDamage;
        private int landings;
        private int misses;
        private int staggers;

        [SetUp]
        public void SetUp()
        {
            enemy = new Enemy();
            enemy.Spawn(4);
            behaviour = new EnemyBehaviour(enemy, WindUp, Damage, Cooldown, Stagger);
            behaviour.Begin();

            windUps = landedDamage = landings = misses = staggers = 0;
            behaviour.WindUpStarted += () => windUps++;
            behaviour.AttackLanded += damage =>
            {
                landings++;
                landedDamage += damage;
            };
            behaviour.AttackMissed += () => misses++;
            behaviour.Staggered += () => staggers++;
        }

        [Test]
        public void Chases_until_the_character_is_in_range()
        {
            behaviour.Tick(1f, characterInRange: false);

            Assert.That(behaviour.State, Is.EqualTo(EnemyBehaviourState.Chasing));
            Assert.That(behaviour.IsChasing, Is.True);
            Assert.That(windUps, Is.Zero);
        }

        [Test]
        public void Winds_up_in_range_and_holds_still()
        {
            behaviour.Tick(0.1f, characterInRange: true);

            Assert.That(behaviour.State, Is.EqualTo(EnemyBehaviourState.WindingUp));
            Assert.That(behaviour.IsChasing, Is.False);
            Assert.That(windUps, Is.EqualTo(1));
        }

        [Test]
        public void Attack_lands_when_the_Wind_up_ends_in_range()
        {
            behaviour.Tick(0.1f, true);
            behaviour.Tick(WindUp - 0.1f, true);
            Assert.That(landings, Is.Zero, "Still winding up.");
            Assert.That(behaviour.StateProgress, Is.GreaterThan(0.8f));

            behaviour.Tick(0.2f, true);

            Assert.That(landings, Is.EqualTo(1));
            Assert.That(landedDamage, Is.EqualTo(Damage));
            Assert.That(behaviour.State, Is.EqualTo(EnemyBehaviourState.Recovering));
        }

        [Test]
        public void Attack_misses_when_the_character_got_out_of_range()
        {
            behaviour.Tick(0.1f, true);
            behaviour.Tick(WindUp, false);

            Assert.That(landings, Is.Zero);
            Assert.That(misses, Is.EqualTo(1));
            Assert.That(behaviour.State, Is.EqualTo(EnemyBehaviourState.Recovering));
        }

        [Test]
        public void Recovers_before_the_next_Wind_up()
        {
            behaviour.Tick(0.1f, true);
            behaviour.Tick(WindUp, true);

            behaviour.Tick(Cooldown - 0.1f, true);
            Assert.That(behaviour.State, Is.EqualTo(EnemyBehaviourState.Recovering));

            behaviour.Tick(0.2f, true);
            Assert.That(behaviour.State, Is.EqualTo(EnemyBehaviourState.Chasing));

            behaviour.Tick(0.1f, true);
            Assert.That(windUps, Is.EqualTo(2));
        }

        [Test]
        public void A_hit_during_the_Wind_up_Staggers_and_cancels_the_attack()
        {
            behaviour.Tick(0.1f, true);

            enemy.TakeHit(1);

            Assert.That(behaviour.State, Is.EqualTo(EnemyBehaviourState.Staggered));
            Assert.That(staggers, Is.EqualTo(1));

            behaviour.Tick(WindUp, true);
            Assert.That(landings, Is.Zero, "The attack was cancelled.");
            Assert.That(misses, Is.Zero);
        }

        [Test]
        public void Chases_again_after_the_Stagger()
        {
            behaviour.Tick(0.1f, true);
            enemy.TakeHit(1);

            behaviour.Tick(Stagger + 0.01f, true);

            Assert.That(behaviour.State, Is.EqualTo(EnemyBehaviourState.Chasing));
            Assert.That(landings, Is.Zero);
        }

        [Test]
        public void A_hit_outside_the_Wind_up_does_not_Stagger()
        {
            enemy.TakeHit(1);
            Assert.That(behaviour.State, Is.EqualTo(EnemyBehaviourState.Chasing));

            behaviour.Tick(0.1f, true);
            behaviour.Tick(WindUp, true);
            enemy.TakeHit(1);

            Assert.That(behaviour.State, Is.EqualTo(EnemyBehaviourState.Recovering));
            Assert.That(staggers, Is.Zero);
        }

        [Test]
        public void Dying_during_the_Wind_up_cancels_the_attack()
        {
            behaviour.Tick(0.1f, true);

            enemy.TakeHit(10);
            behaviour.Tick(WindUp, true);

            Assert.That(behaviour.State, Is.EqualTo(EnemyBehaviourState.Dead));
            Assert.That(landings, Is.Zero);
            Assert.That(staggers, Is.Zero);
        }

        [Test]
        public void Begin_starts_a_respawned_Enemy_over()
        {
            behaviour.Tick(0.1f, true);
            enemy.TakeHit(10);

            enemy.Spawn(4);
            behaviour.Begin();

            Assert.That(behaviour.State, Is.EqualTo(EnemyBehaviourState.Chasing));
        }
    }
}
