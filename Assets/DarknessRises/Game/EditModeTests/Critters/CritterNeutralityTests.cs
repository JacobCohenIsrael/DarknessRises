using DarknessRises.Game.Scoring;
using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Critters
{
    /// <summary>A Shot that kills a Critter counts as neither a hit nor a miss: no Score, no Combo, no accuracy, no Grade.</summary>
    public sealed class CritterNeutralityTests
    {
        private static readonly GradeRequirement[] Grades =
        {
            new(Grade.S, 0.8f, 5f, 0),
            new(Grade.A, 0.6f, 8f, 1),
            new(Grade.B, 0.4f, 0f, -1),
        };

        // Every 3 hits in a row the multiplier steps up, to 4 at most; 2 s without a hit ends the streak.
        private static ScoreKeeper NewKeeper() => new(new Combo(3, 4, 2f), Grades, Grade.C);

        [Test]
        public void A_Shot_at_a_Critter_keeps_the_Combo_as_it_was()
        {
            var keeper = NewKeeper();
            for (var i = 0; i < 4; i++)
            {
                keeper.ShotLanded(ShotOutcome.HurtEnemy);
            }

            keeper.ShotLanded(ShotOutcome.Neutral);
            keeper.ShotLanded(ShotOutcome.Neutral);

            Assert.That(keeper.Combo.Streak, Is.EqualTo(4));
            Assert.That(keeper.Combo.Multiplier, Is.EqualTo(2));
        }

        [Test]
        public void A_Shot_at_a_Critter_neither_breaks_nor_builds_the_streak()
        {
            var keeper = NewKeeper();
            keeper.ShotLanded(ShotOutcome.HurtEnemy);
            keeper.ShotLanded(ShotOutcome.HurtEnemy);
            keeper.ShotLanded(ShotOutcome.Neutral);
            keeper.ShotLanded(ShotOutcome.HurtEnemy);

            Assert.That(keeper.Combo.Streak, Is.EqualTo(3));

            keeper.ShotLanded(ShotOutcome.Missed);
            Assert.That(keeper.Combo.Streak, Is.Zero);
        }

        [Test]
        public void A_Shot_at_a_Critter_leaves_the_Station_s_accuracy_and_Grade_alone()
        {
            var plain = NewKeeper();
            var withCritters = NewKeeper();
            plain.BeginStation();
            withCritters.BeginStation();
            foreach (var keeper in new[] { plain, withCritters })
            {
                for (var i = 0; i < 4; i++)
                {
                    keeper.ShotLanded(ShotOutcome.HurtEnemy);
                }

                keeper.ShotLanded(ShotOutcome.Missed);
                keeper.EnemyKilled(100);
            }

            for (var i = 0; i < 10; i++)
            {
                withCritters.ShotLanded(ShotOutcome.Neutral);
            }

            var expected = plain.EndStation(4f);
            var result = withCritters.EndStation(4f);

            Assert.That(result.Hits, Is.EqualTo(expected.Hits));
            Assert.That(result.Misses, Is.EqualTo(expected.Misses));
            Assert.That(result.Accuracy, Is.EqualTo(expected.Accuracy));
            Assert.That(result.Kills, Is.EqualTo(expected.Kills));
            Assert.That(result.Points, Is.EqualTo(expected.Points));
            Assert.That(result.Grade, Is.EqualTo(expected.Grade));
        }

        [Test]
        public void A_Shot_at_a_Critter_scores_nothing()
        {
            var keeper = NewKeeper();
            var scored = 0;
            keeper.Scored += points => scored += points;

            keeper.ShotLanded(ShotOutcome.Neutral);

            Assert.That(scored, Is.Zero);
        }

        [Test]
        public void Hits_and_misses_still_count_through_the_same_door()
        {
            var keeper = NewKeeper();
            keeper.BeginStation();

            keeper.ShotLanded(ShotOutcome.HurtEnemy);
            keeper.ShotLanded(ShotOutcome.Missed);
            var result = keeper.EndStation(1f);

            Assert.That(result.Hits, Is.EqualTo(1));
            Assert.That(result.Misses, Is.EqualTo(1));
        }
    }
}
