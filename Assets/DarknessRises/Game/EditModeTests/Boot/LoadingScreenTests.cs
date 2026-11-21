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
            Assert.That(() => screen.BeginDownload(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        // The download bar.
        private const long Megabytes10 = 10L * 1024 * 1024;

        [Test]
        public void A_download_shows_no_bar_before_the_label_comes_up()
        {
            screen.Show();
            screen.BeginDownload(Megabytes10);
            screen.ReportDownloaded(Megabytes10 / 4);

            Assert.That(screen.IsBarShown, Is.False, "The first second stays plain black.");
            Assert.That(screen.Progress, Is.EqualTo(0.25f));
        }

        [Test]
        public void A_download_that_goes_on_brings_the_bar_up_with_the_label_and_no_dots()
        {
            screen.Show();
            screen.BeginDownload(Megabytes10);

            screen.Tick(LabelDelay + DotSeconds * 2);

            Assert.That(screen.IsLabelShown, Is.True);
            Assert.That(screen.IsBarShown, Is.True);
            Assert.That(screen.Dots, Is.Zero, "The bar shows the wait is moving; no looping dots stand in for it.");
            Assert.That(screen.Progress, Is.Zero, "Nothing is in yet, however long it has been.");
        }

        [Test]
        public void The_bar_fills_with_the_bytes_that_are_in_and_only_with_them()
        {
            screen.Show();
            screen.BeginDownload(Megabytes10);
            screen.Tick(LabelDelay);

            screen.ReportDownloaded(Megabytes10 / 2);
            screen.Tick(5f);

            Assert.That(screen.Progress, Is.EqualTo(0.5f), "Time alone never fills it.");
            Assert.That(screen.DownloadedBytes, Is.EqualTo(Megabytes10 / 2));
            Assert.That(screen.TotalBytes, Is.EqualTo(Megabytes10));

            screen.EndDownload();

            Assert.That(screen.Progress, Is.EqualTo(1f));
            Assert.That(screen.IsBarShown, Is.True, "A full bar stays up until the scene behind it is ready.");
        }

        [Test]
        public void The_bar_never_goes_backwards()
        {
            screen.Show();
            screen.BeginDownload(Megabytes10);
            screen.Tick(LabelDelay);
            screen.ReportDownloaded(Megabytes10 / 2);

            screen.ReportDownloaded(Megabytes10 / 4);

            Assert.That(screen.Progress, Is.EqualTo(0.5f));
            Assert.That(screen.DownloadedBytes, Is.EqualTo(Megabytes10 / 2));
        }

        [Test]
        public void It_never_counts_more_than_the_download_is()
        {
            screen.Show();
            screen.BeginDownload(Megabytes10);

            screen.ReportDownloaded(Megabytes10 * 2);

            Assert.That(screen.DownloadedBytes, Is.EqualTo(Megabytes10));
            Assert.That(screen.Progress, Is.EqualTo(1f));
        }

        [Test]
        public void More_bytes_joining_the_wait_grow_the_total_and_the_bar_holds_until_they_catch_up()
        {
            screen.Show();
            screen.BeginDownload(Megabytes10 / 2);
            screen.Tick(LabelDelay);
            screen.EndDownload();
            Assert.That(screen.Progress, Is.EqualTo(1f));

            screen.BeginDownload(Megabytes10 / 2);

            Assert.That(screen.TotalBytes, Is.EqualTo(Megabytes10));
            Assert.That(screen.Progress, Is.EqualTo(1f), "Never backwards, even as the total grows.");

            screen.ReportDownloaded(Megabytes10 / 4);
            Assert.That(screen.DownloadedBytes, Is.EqualTo(Megabytes10 * 3 / 4));
            Assert.That(screen.Progress, Is.EqualTo(1f));

            screen.EndDownload();
            Assert.That(screen.DownloadedBytes, Is.EqualTo(Megabytes10));
        }

        [Test]
        public void Two_downloads_in_one_wait_add_up()
        {
            screen.Show();
            screen.BeginDownload(Megabytes10 / 2);
            screen.BeginDownload(Megabytes10 / 2);
            screen.Tick(LabelDelay);

            screen.ReportDownloaded(Megabytes10 / 4);

            Assert.That(screen.TotalBytes, Is.EqualTo(Megabytes10));
            Assert.That(screen.DownloadedBytes, Is.EqualTo(Megabytes10 / 4));
            Assert.That(screen.Progress, Is.EqualTo(0.25f));
        }

        [Test]
        public void Nothing_to_download_shows_no_bar_just_the_label_with_dots()
        {
            screen.Show();
            screen.BeginDownload(0);
            screen.Tick(LabelDelay + DotSeconds);

            Assert.That(screen.IsLabelShown, Is.True);
            Assert.That(screen.IsBarShown, Is.False);
            Assert.That(screen.Dots, Is.EqualTo(1));
        }

        [Test]
        public void A_download_over_before_the_label_never_flashes_a_bar()
        {
            // Content already on the device: every byte is in at once, then the scene takes a while to start.
            screen.Show();
            screen.BeginDownload(Megabytes10);
            screen.EndDownload();

            screen.Tick(LabelDelay * 3);

            Assert.That(screen.IsLabelShown, Is.True);
            Assert.That(screen.IsBarShown, Is.False);
        }

        [Test]
        public void A_size_that_cant_be_known_shows_the_label_without_a_bar()
        {
            screen.Show();
            screen.BeginDownload(Megabytes10);
            screen.Tick(LabelDelay);
            Assert.That(screen.IsBarShown, Is.True);

            screen.DownloadUnknown();
            screen.Tick(DotSeconds);

            Assert.That(screen.IsLabelShown, Is.True);
            Assert.That(screen.IsBarShown, Is.False, "Better no bar than one that isn't true.");
            Assert.That(screen.Dots, Is.EqualTo(1));
        }

        [Test]
        public void An_unknown_size_lasts_for_the_wait()
        {
            screen.Show();
            screen.DownloadUnknown();
            screen.BeginDownload(Megabytes10);

            screen.Tick(LabelDelay);

            Assert.That(screen.IsBarShown, Is.False);
        }

        [Test]
        public void Ready_takes_the_bar_away_and_the_next_wait_starts_empty()
        {
            screen.Show();
            screen.BeginDownload(Megabytes10);
            screen.Tick(LabelDelay);
            screen.ReportDownloaded(Megabytes10 / 2);

            screen.Hide();

            Assert.That(screen.IsBarShown, Is.False);

            screen.Show();
            screen.Tick(LabelDelay);

            Assert.That(screen.IsBarShown, Is.False);
            Assert.That(screen.Progress, Is.Zero);
            Assert.That(screen.TotalBytes, Is.Zero);
            Assert.That(screen.DownloadedBytes, Is.Zero);
        }

        [Test]
        public void Asking_for_the_screen_again_keeps_the_wait_s_download()
        {
            screen.Show();
            screen.BeginDownload(Megabytes10);
            screen.ReportDownloaded(Megabytes10 / 2);

            screen.Show();

            Assert.That(screen.DownloadedBytes, Is.EqualTo(Megabytes10 / 2));
        }

        [Test]
        public void Downloads_while_nothing_is_covered_are_not_counted()
        {
            screen.BeginDownload(Megabytes10);
            screen.ReportDownloaded(Megabytes10);

            screen.Show();
            screen.Tick(LabelDelay);

            Assert.That(screen.TotalBytes, Is.Zero);
            Assert.That(screen.IsBarShown, Is.False);
        }
    }
}
