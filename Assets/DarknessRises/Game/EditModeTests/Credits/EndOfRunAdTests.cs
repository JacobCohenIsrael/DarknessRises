using System;
using DarknessRises.Game.Ads;
using DarknessRises.Game.Credits;
using DarknessRises.Game.Runs;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Credits
{
    public sealed class EndOfRunAdTests
    {
        private const float Double = 2f;
        private const int Minimum = 20;

        private static RunCredits RunThatEarned(int credits)
        {
            var runCredits = new RunCredits();
            runCredits.Earn(credits);
            return runCredits;
        }

        [Test]
        public void A_Run_that_earned_enough_is_offered_the_ad()
        {
            var ad = new EndOfRunAd(RunThatEarned(150), Double, Minimum);

            Assert.That(ad.IsOffered, Is.True);
            Assert.That(ad.Bonus, Is.EqualTo(150));
        }

        [Test]
        public void Watching_it_multiplies_the_Credits_the_Run_earned()
        {
            var credits = RunThatEarned(150);
            var ad = new EndOfRunAd(credits, Double, Minimum);

            var bonus = ad.Finish(AdOutcome.Watched);

            Assert.That(bonus, Is.EqualTo(150));
            Assert.That(credits.Earned, Is.EqualTo(150));
            Assert.That(credits.AdBonus, Is.EqualTo(150));
            Assert.That(credits.Total, Is.EqualTo(300));
        }

        [Test]
        public void It_pays_once_per_Run_end()
        {
            var credits = RunThatEarned(150);
            var ad = new EndOfRunAd(credits, Double, Minimum);
            ad.Finish(AdOutcome.Watched);

            Assert.That(ad.IsOffered, Is.False);
            Assert.That(ad.Finish(AdOutcome.Watched), Is.Zero);
            Assert.That(credits.Total, Is.EqualTo(300));
        }

        [Test]
        public void An_offer_made_again_for_the_same_Run_doesnt_pay_twice()
        {
            var credits = RunThatEarned(150);
            new EndOfRunAd(credits, Double, Minimum).Finish(AdOutcome.Watched);

            var again = new EndOfRunAd(credits, Double, Minimum);

            Assert.That(again.IsOffered, Is.False);
            Assert.That(again.Finish(AdOutcome.Watched), Is.Zero);
            Assert.That(credits.Total, Is.EqualTo(300));
        }

        [Test]
        public void An_ad_that_wasnt_watched_through_pays_nothing_and_the_offer_stays()
        {
            var credits = RunThatEarned(150);
            var ad = new EndOfRunAd(credits, Double, Minimum);

            Assert.That(ad.Finish(AdOutcome.NotWatched), Is.Zero);

            Assert.That(credits.Total, Is.EqualTo(150));
            Assert.That(credits.HasAdBonus, Is.False);
            Assert.That(ad.IsOffered, Is.True);
            Assert.That(ad.Finish(AdOutcome.Watched), Is.EqualTo(150));
        }

        [Test]
        public void A_Run_that_earned_nothing_isnt_offered_the_ad()
        {
            var credits = new RunCredits();
            var ad = new EndOfRunAd(credits, Double, minimumCredits: 0);

            Assert.That(ad.IsOffered, Is.False);
            Assert.That(ad.Finish(AdOutcome.Watched), Is.Zero);
            Assert.That(credits.HasAdBonus, Is.False);
        }

        [Test]
        public void A_Run_that_earned_less_than_the_minimum_isnt_offered_the_ad()
        {
            Assert.That(new EndOfRunAd(RunThatEarned(19), Double, Minimum).IsOffered, Is.False);
            Assert.That(new EndOfRunAd(RunThatEarned(20), Double, Minimum).IsOffered, Is.True);
        }

        [Test]
        public void Only_what_the_Run_earned_is_multiplied_and_the_bonus_is_whole_Credits()
        {
            Assert.That(new EndOfRunAd(RunThatEarned(101), 1.5f, Minimum).Bonus, Is.EqualTo(51));
            Assert.That(new EndOfRunAd(RunThatEarned(100), 3f, Minimum).Bonus, Is.EqualTo(200));
        }

        [Test]
        public void A_factor_that_adds_nothing_makes_no_offer()
        {
            var ad = new EndOfRunAd(RunThatEarned(150), EndOfRunAd.MinFactor, Minimum);

            Assert.That(ad.Bonus, Is.Zero);
            Assert.That(ad.IsOffered, Is.False);
        }

        [Test]
        public void The_ad_never_takes_Credits_away()
        {
            var credits = RunThatEarned(150);

            Assert.That(() => new EndOfRunAd(credits, 0.5f, Minimum), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new EndOfRunAd(credits, float.NaN, Minimum), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new EndOfRunAd(credits, Double, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new EndOfRunAd(null, Double, Minimum), Throws.InstanceOf<ArgumentNullException>());
        }
    }
}
