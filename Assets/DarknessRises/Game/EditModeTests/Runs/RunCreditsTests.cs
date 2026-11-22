using System;
using System.Collections.Generic;
using DarknessRises.Game.Credits;
using DarknessRises.Game.Runs;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Runs
{
    public sealed class RunCreditsTests
    {
        private static RunCredits AtFifty() => new(new ScoreToCredits(50));

        [Test]
        public void A_new_Run_has_earned_nothing()
        {
            var credits = AtFifty();

            Assert.That(credits.Score, Is.Zero);
            Assert.That(credits.FromScore, Is.Zero);
            Assert.That(credits.Earned, Is.Zero);
            Assert.That(credits.Total, Is.Zero);
            Assert.That(credits.Unbanked, Is.Zero);
            Assert.That(credits.ScorePerCredit, Is.EqualTo(50));
        }

        [Test]
        public void The_Score_pays_Credits_and_the_clear_bonus_comes_on_top()
        {
            var credits = AtFifty();

            credits.AddScore(42300);
            credits.Earn(150);

            Assert.That(credits.FromScore, Is.EqualTo(846));
            Assert.That(credits.Earned, Is.EqualTo(150));
            Assert.That(credits.Total, Is.EqualTo(996));
        }

        [Test]
        public void Points_left_over_from_each_kill_add_up_to_Credits()
        {
            var credits = AtFifty();

            // 30 kills of 20 points each: none pays a Credit on its own, together they pay 12.
            for (var i = 0; i < 30; i++)
            {
                credits.AddScore(20);
            }

            Assert.That(credits.FromScore, Is.EqualTo(12));
        }

        [Test]
        public void Credits_and_Score_are_only_ever_earned()
        {
            var credits = AtFifty();

            Assert.That(() => credits.Earn(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => credits.AddScore(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Taking_the_unbanked_Credits_never_takes_one_twice()
        {
            var credits = AtFifty();
            credits.AddScore(1520);

            Assert.That(credits.TakeUnbanked(), Is.EqualTo(30));
            Assert.That(credits.TakeUnbanked(), Is.Zero);

            credits.AddScore(30);
            credits.Earn(100);

            Assert.That(credits.Unbanked, Is.EqualTo(101), "The 20 points left over and the 30 new ones make one Credit.");
            Assert.That(credits.TakeUnbanked(), Is.EqualTo(101));
            Assert.That(credits.Banked, Is.EqualTo(131));
            Assert.That(credits.Unbanked, Is.Zero);
        }

        [Test]
        public void Banking_after_every_kill_loses_nothing_to_rounding()
        {
            var often = AtFifty();
            var once = AtFifty();
            var bankedOften = 0;
            for (var i = 0; i < 200; i++)
            {
                var points = 100 * (1 + i % 5) + 37;
                often.AddScore(points);
                once.AddScore(points);
                bankedOften += often.TakeUnbanked();
            }

            Assert.That(bankedOften, Is.EqualTo(once.TakeUnbanked()));
            Assert.That(bankedOften, Is.EqualTo(once.Score / 50));
        }

        [Test]
        public void It_tells_when_the_total_changes()
        {
            var credits = AtFifty();
            var totals = new List<int>();
            credits.Changed += totals.Add;

            credits.AddScore(40);
            credits.AddScore(20);
            credits.Earn(0);
            credits.Earn(5);
            credits.AddScore(0);

            Assert.That(totals, Is.EqualTo(new[] { 1, 6 }));
        }

        [Test]
        public void The_rate_can_be_set_until_the_Run_scores()
        {
            var credits = new RunCredits();

            Assert.That(credits.ConvertAt(new ScoreToCredits(50)), Is.True);
            credits.AddScore(100);

            Assert.That(credits.ConvertAt(new ScoreToCredits(50)), Is.True);
            Assert.That(credits.ConvertAt(new ScoreToCredits(25)), Is.False);
            Assert.That(credits.FromScore, Is.EqualTo(2));
        }

        [Test]
        public void Without_a_rate_every_point_is_a_Credit()
        {
            var credits = new RunCredits();

            credits.AddScore(7);

            Assert.That(credits.Total, Is.EqualTo(7));
        }

        [Test]
        public void Adding_Score_allocates_nothing()
        {
            var credits = AtFifty();

            AllocationAssert.DoesNotAllocate(() => credits.AddScore(137));
        }
    }
}
