using DarknessRises.Game.Health;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Health
{
    public sealed class HeartbeatBeatsTests
    {
        // 60 beats a minute: a strong beat every second, the first 0.2 s in.
        private static HeartbeatBeats Beats() => new(new HeartbeatRhythm(60f, 0.2f, 0.3f));

        [Test]
        public void A_beat_is_found_each_time_the_clip_passes_a_strong_beat()
        {
            var beats = Beats();
            var found = 0;
            for (var frame = 1; frame <= 300; frame++)
            {
                if (beats.Advance(frame / 100f, 0.01f))
                {
                    found++;
                }
            }

            // Strong beats at 0.2, 1.2 and 2.2 s of three seconds.
            Assert.That(found, Is.EqualTo(3));
        }

        [Test]
        public void The_beat_follows_the_clip_through_its_loop()
        {
            var beats = Beats();
            Assert.That(beats.Advance(0.9f, 0.01f), Is.False);
            Assert.That(beats.Advance(1.1f, 0.2f), Is.False);
            Assert.That(beats.Advance(1.25f, 0.15f), Is.True, "passed 1.2 s");

            // The clip looped back to its start: no beat until it passes the first one again.
            Assert.That(beats.Advance(0.1f, 0.05f), Is.False);
            Assert.That(beats.Advance(0.21f, 0.11f), Is.True);
        }

        [Test]
        public void A_frozen_clip_beats_no_more()
        {
            var beats = Beats();
            beats.Advance(0.5f, 0.01f);

            for (var i = 0; i < 10; i++)
            {
                Assert.That(beats.Advance(0.5f, 0f), Is.False);
            }
        }

        [Test]
        public void Starting_on_a_beat_counts_it_once()
        {
            var beats = Beats();

            Assert.That(beats.Advance(0.2f, 0.016f), Is.True);
            Assert.That(beats.Advance(0.216f, 0.016f), Is.False);

            beats.Reset();
            Assert.That(beats.Advance(0.6f, 0.016f), Is.False, "a heartbeat picked up mid-beat waits for the next one");
        }
    }
}
