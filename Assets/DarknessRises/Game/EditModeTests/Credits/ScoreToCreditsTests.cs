using System;
using DarknessRises.Game.Credits;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Credits
{
    public sealed class ScoreToCreditsTests
    {
        [TestCase(0, 0)]
        [TestCase(49, 0)]
        [TestCase(50, 1)]
        [TestCase(99, 1)]
        [TestCase(42300, 846)]
        public void Every_whole_rate_of_Score_pays_one_Credit_rounded_down(int score, int credits)
        {
            Assert.That(new ScoreToCredits(50).CreditsFor(score), Is.EqualTo(credits));
        }

        [Test]
        public void A_Credit_costs_at_least_one_point()
        {
            Assert.That(() => new ScoreToCredits(0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new ScoreToCredits(-5), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void A_Score_is_never_below_0()
        {
            Assert.That(() => new ScoreToCredits(50).CreditsFor(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void A_default_made_rule_pays_one_Credit_a_point()
        {
            Assert.That(default(ScoreToCredits).ScorePerCredit, Is.EqualTo(1));
            Assert.That(default(ScoreToCredits).CreditsFor(7), Is.EqualTo(7));
        }

        [Test]
        public void Doubling_the_Score_doubles_the_Credits()
        {
            var rule = new ScoreToCredits(50);

            Assert.That(rule.CreditsFor(2 * 12000), Is.EqualTo(2 * rule.CreditsFor(12000)));
        }
    }
}
