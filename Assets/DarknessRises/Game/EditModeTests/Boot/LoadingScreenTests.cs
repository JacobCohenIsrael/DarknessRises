using System;
using DarknessRises.Game.Boot;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Boot
{
    /// <summary>What the loading screen shows over time: a plain cover first, a label with growing dots if the wait goes on.</summary>
    public sealed class LoadingScreenTests
    {
        // Numbers that are exact in floats: the label after 1 s, a dot every half second, three dots.
        private const float LabelDelay = 1f;
        private const float DotSeconds = 0.5f;
        private const int MaxDots = 3;

        private LoadingScreen screen;

        [SetUp]
        public void SetUp()
        {
            screen = new LoadingScreen(LabelDelay, DotSeconds, MaxDots);
        }

        [Test]
        public void Nothing_is_covered_until_something_loads()
        {
            screen.Tick(10f);

            Assert.That(screen.IsShown, Is.False);
            Assert.That(screen.IsLabelShown, Is.False);
            Assert.That(screen.Dots, Is.Zero);
        }

        [Test]
        public void A_load_covers_the_screen_at_once_without_the_label()
        {
            screen.Show();

            Assert.That(screen.IsShown, Is.True);
            Assert.That(screen.IsLabelShown, Is.False, "A quick change between Levels stays plain black.");
        }

        [Test]
        public void A_short_load_never_shows_the_label()
        {
            screen.Show();
            screen.Tick(0.75f);
            Assert.That(screen.IsLabelShown, Is.False);

            screen.Hide();

            Assert.That(screen.IsShown, Is.False);
            Assert.That(screen.IsLabelShown, Is.False);
        }

        [Test]
        public void A_load_that_goes_on_brings_the_label_up()
        {
            screen.Show();

            screen.Tick(LabelDelay);

            Assert.That(screen.IsLabelShown, Is.True);
            Assert.That(screen.Dots, Is.Zero, "The label comes up bare.");
        }

        [Test]
        public void The_dots_grow_one_at_a_time_then_start_over()
        {
            screen.Show();
            screen.Tick(LabelDelay);

            var seen = new int[9];
            for (var beat = 0; beat < seen.Length; beat++)
            {
                seen[beat] = screen.Dots;
                screen.Tick(DotSeconds);
            }

            Assert.That(seen, Is.EqualTo(new[] { 0, 1, 2, 3, 0, 1, 2, 3, 0 }));
        }

        [Test]
        public void Ready_uncovers_the_screen_however_long_the_wait_was()
        {
            screen.Show();
            screen.Tick(30f);

            screen.Hide();

            Assert.That(screen.IsShown, Is.False);
            Assert.That(screen.IsLabelShown, Is.False);
            Assert.That(screen.Dots, Is.Zero);
        }

        [Test]
        public void One_wait_made_of_several_loads_counts_as_one()
        {
            // A scene unloads, the next one loads, then its content: each asks for the screen, the wait carries on.
            screen.Show();
            screen.Tick(0.75f);

            screen.Show();
            screen.Tick(0.25f);

            Assert.That(screen.IsLabelShown, Is.True);
        }

        [Test]
        public void The_next_wait_starts_from_plain_black_again()
        {
            screen.Show();
            screen.Tick(5f);
            screen.Hide();

            screen.Show();

            Assert.That(screen.IsShown, Is.True);
            Assert.That(screen.IsLabelShown, Is.False);
        }

        [Test]
        public void Time_that_passes_while_nothing_loads_doesnt_count()
        {
            screen.Tick(5f);

            screen.Show();

            Assert.That(screen.IsLabelShown, Is.False);
        }

        [Test]
        public void With_no_delay_the_label_is_there_from_the_start()
        {
            var eager = new LoadingScreen(0f, DotSeconds, MaxDots);

            eager.Show();

            Assert.That(eager.IsLabelShown, Is.True);
        }

        [Test]
        public void With_no_dots_the_label_stays_bare()
        {
            var bare = new LoadingScreen(LabelDelay, DotSeconds, 0);
            bare.Show();

            bare.Tick(7.5f);

            Assert.That(bare.IsLabelShown, Is.True);
            Assert.That(bare.Dots, Is.Zero);
        }

        [Test]
        public void Its_times_and_dots_are_never_negative()
        {
            Assert.That(() => new LoadingScreen(-1f, DotSeconds, MaxDots), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new LoadingScreen(LabelDelay, 0f, MaxDots), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new LoadingScreen(LabelDelay, DotSeconds, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new LoadingScreen(float.NaN, DotSeconds, MaxDots), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => screen.Tick(-0.1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
