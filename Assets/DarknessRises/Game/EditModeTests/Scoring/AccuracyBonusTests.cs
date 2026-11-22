using DarknessRises.Game.Scoring;
using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Scoring
{
    /// <summary>
    /// The accuracy bonus: each Station's Score is multiplied by the best tier its fight's accuracy reaches, and a fight
    /// with too few Shots to say anything gets x1.
    /// </summary>
    public sealed class AccuracyBonusTests
    {
        // x1.2 from 70 %, x1.5 from 85 %, x2 at 100 %; at least 4 Shots. Listed out of order on purpose.
        private static readonly AccuracyTier[] Tiers =
        {
            new(0.85f, 1.5f),
            new(1f, 2f),
            new(0.7f, 1.2f),
        };

        private static AccuracyBonus NewBonus() => new(Tiers, 4);

        private static readonly GradeRequirement[] Grades = { new(Grade.S, 0.8f, 0f, -1) };

        [TestCase(5, 5, 1f)]
        [TestCase(7, 3, 1.2f)]
        [TestCase(17, 3, 1.5f)]
        [TestCase(19, 1, 1.5f)]
        [TestCase(20, 0, 2f)]
        public void The_best_tier_the_accuracy_reaches_multiplies_the_Score(int hits, int misses, float expected)
        {
            Assert.That(NewBonus().MultiplierFor(hits, misses), Is.EqualTo(expected));
        }

        [Test]
        public void A_fight_without_Shots_gets_no_bonus()
        {
            // Won by Grenades, or with Shots that only killed Critters: neither hits nor misses.
            Assert.That(NewBonus().MultiplierFor(0, 0), Is.EqualTo(1f));
        }

        [Test]
        public void Too_few_Shots_to_say_anything_get_no_bonus()
        {
            Assert.That(NewBonus().MultiplierFor(3, 0), Is.EqualTo(1f), "Three Shots, all hits: one lucky volley.");
            Assert.That(NewBonus().MultiplierFor(4, 0), Is.EqualTo(2f));
        }

        [Test]
        public void The_bonus_rounds_the_multiplied_Score_to_whole_points()
        {
            Assert.That(AccuracyBonus.BonusOf(100, 1.5f), Is.EqualTo(50));
            Assert.That(AccuracyBonus.BonusOf(35, 1.25f), Is.EqualTo(9), "43.75 rounds to 44.");
            Assert.That(AccuracyBonus.BonusOf(0, 2f), Is.Zero);
            Assert.That(AccuracyBonus.BonusOf(80, 1f), Is.Zero);
        }

        [Test]
        public void The_Station_result_carries_the_multiplier_and_its_bonus()
        {
            var keeper = new ScoreKeeper(new Combo(100, 1, 0f), Grades, Grade.C, NewBonus(), null);
            keeper.BeginStation();
            for (var i = 0; i < 9; i++)
            {
                keeper.ShotHit();
            }

            keeper.ShotMissed();
            keeper.EnemyKilled(30);
            keeper.EnemyKilled(30);

            var result = keeper.EndStation(10f);

            Assert.That(result.Points, Is.EqualTo(60));
            Assert.That(result.AccuracyMultiplier, Is.EqualTo(1.5f), "90 %.");
            Assert.That(result.Bonus, Is.EqualTo(30));
            Assert.That(result.Total, Is.EqualTo(90));
            Assert.That(result.HasBonus, Is.True);
        }

        [Test]
        public void Shots_that_only_killed_Critters_leave_the_bonus_at_x1()
        {
            var keeper = new ScoreKeeper(new Combo(100, 1, 0f), Grades, Grade.C, NewBonus(), null);
            keeper.BeginStation();
            for (var i = 0; i < 6; i++)
            {
                keeper.ShotLanded(ShotOutcome.Neutral);
            }

            keeper.EnemyKilled(40);

            var result = keeper.EndStation(5f);

            Assert.That(result.AccuracyMultiplier, Is.EqualTo(1f));
            Assert.That(result.Bonus, Is.Zero);
            Assert.That(result.Total, Is.EqualTo(40));
        }

        [Test]
        public void A_fight_that_earned_nothing_stamps_no_bonus()
        {
            var keeper = new ScoreKeeper(new Combo(100, 1, 0f), Grades, Grade.C, NewBonus(), null);
            keeper.BeginStation();
            for (var i = 0; i < 5; i++)
            {
                keeper.ShotHit();
            }

            var result = keeper.EndStation(5f);

            Assert.That(result.HasBonus, Is.False);
            Assert.That(result.AccuracyMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void The_old_keeper_has_no_bonus()
        {
            var keeper = new ScoreKeeper(new Combo(100, 1, 0f), Grades, Grade.C);
            keeper.BeginStation();
            for (var i = 0; i < 10; i++)
            {
                keeper.ShotHit();
            }

            keeper.EnemyKilled(50);

            Assert.That(keeper.EndStation(5f).Total, Is.EqualTo(50));
        }

        [Test]
        public void The_authored_tiers_make_a_bonus()
        {
            var bonus = new ScoreSettings().ToAccuracyBonus();

            Assert.That(bonus.MultiplierFor(4, 6), Is.EqualTo(1f), "40 %: under the B line.");
            Assert.That(bonus.MultiplierFor(9, 1), Is.EqualTo(1.75f), "90 %.");
            Assert.That(bonus.MultiplierFor(10, 0), Is.EqualTo(2f), "Perfect.");
        }
    }
}
