using DarknessRises.Game.Enemies;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Enemies
{
    public sealed class EnemyQuarryTests
    {
        private const float WindUp = 0.8f;
        private const float Cooldown = 1.5f;
        private const float Stagger = 0.6f;

        private EnemyQuarry quarry;

        [SetUp]
        public void SetUp()
        {
            quarry = new EnemyQuarry();
        }

        [Test]
        public void A_fresh_Enemy_goes_for_the_character()
        {
            Assert.That(quarry.IsLured, Is.False);
            Assert.That(quarry.IsDrawn, Is.False);
        }

        [Test]
        public void A_chasing_Enemy_drawn_by_a_Lure_turns_to_it_at_once()
        {
            quarry.Draw();

            Assert.That(quarry.Settle(EnemyBehaviourState.Chasing), Is.True);
            Assert.That(quarry.IsLured, Is.True);
            Assert.That(quarry.Settle(EnemyBehaviourState.Chasing), Is.False, "Nothing more changes.");
        }

        [TestCase(EnemyBehaviourState.WindingUp)]
        [TestCase(EnemyBehaviourState.Recovering)]
        [TestCase(EnemyBehaviourState.Staggered)]
        [TestCase(EnemyBehaviourState.Dead)]
        public void An_Enemy_busy_with_the_character_finishes_first(EnemyBehaviourState busy)
        {
            quarry.Draw();

            Assert.That(quarry.Settle(busy), Is.False);
            Assert.That(quarry.IsLured, Is.False);
        }

        [Test]
        public void Once_the_Lure_ends_it_comes_back_to_the_character_when_it_next_chases()
        {
            quarry.Draw();
            quarry.Settle(EnemyBehaviourState.Chasing);

            quarry.Release();
            Assert.That(quarry.Settle(EnemyBehaviourState.WindingUp), Is.False, "Mid-swing at the Lure, it finishes the swing.");
            Assert.That(quarry.IsLured, Is.True);

            Assert.That(quarry.Settle(EnemyBehaviourState.Chasing), Is.True);
            Assert.That(quarry.IsLured, Is.False);
        }

        [Test]
        public void A_respawned_Enemy_forgets_any_Lure()
        {
            quarry.Draw();
            quarry.Settle(EnemyBehaviourState.Chasing);

            quarry.Reset();

            Assert.That(quarry.IsLured, Is.False);
            Assert.That(quarry.IsDrawn, Is.False);
        }

        // The fairness rule with the real behaviour: a Wind-up at the character when the Lure lands still lands on it.
        [Test]
        public void A_lunge_at_the_character_in_progress_still_lands_when_a_Lure_draws_the_Enemy()
        {
            var enemy = new Enemy();
            enemy.Spawn(3);
            var behaviour = new EnemyBehaviour(enemy, WindUp, 1, Cooldown, Stagger);
            behaviour.Begin();
            var landed = 0;
            var missed = 0;
            behaviour.AttackLanded += _ => landed++;
            behaviour.AttackMissed += () => missed++;

            // In range of the character: the Wind-up starts.
            Step(behaviour, characterInRange: true, lureInRange: false, deltaSeconds: 0.1f);
            Assert.That(behaviour.State, Is.EqualTo(EnemyBehaviourState.WindingUp));

            // The Lure lands mid-Wind-up and draws it. It keeps its target until the swing is over.
            quarry.Draw();
            Step(behaviour, characterInRange: true, lureInRange: false, deltaSeconds: WindUp);
            Assert.That(landed, Is.EqualTo(1), "The attack on the character lands as it would have.");
            Assert.That(missed, Is.Zero);
            Assert.That(quarry.IsLured, Is.False);

            // Recovered and chasing again: now it goes for the Lure.
            Step(behaviour, characterInRange: true, lureInRange: false, deltaSeconds: Cooldown);
            Step(behaviour, characterInRange: true, lureInRange: false, deltaSeconds: 0.01f);
            Assert.That(quarry.IsLured, Is.True);
            Assert.That(behaviour.State, Is.EqualTo(EnemyBehaviourState.Chasing), "Away from the Lure, it walks to it instead of winding up at the character.");
        }

        // Like EnemyView.Update: settle what it goes for, then tick with that target's range.
        private void Step(EnemyBehaviour behaviour, bool characterInRange, bool lureInRange, float deltaSeconds)
        {
            quarry.Settle(behaviour.State);
            behaviour.Tick(deltaSeconds, quarry.IsLured ? lureInRange : characterInRange);
        }
    }
}
