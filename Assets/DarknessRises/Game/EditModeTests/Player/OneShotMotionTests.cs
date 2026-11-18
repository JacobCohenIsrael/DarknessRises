using System;
using System.Collections.Generic;
using DarknessRises.Game.Player;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Player
{
    public sealed class OneShotMotionTests
    {
        // A throw: the Grenade leaves the hand 0.4 s in, and the throw lasts 1 s.
        private const float ReleaseSeconds = 0.4f;
        private const float ThrowSeconds = 1f;

        [Test]
        public void The_Grenade_leaves_the_hand_at_the_authored_moment_and_only_once()
        {
            var motion = new OneShotMotion(ReleaseSeconds, ThrowSeconds);
            var released = 0;
            motion.Cued += () => released++;
            motion.TryPlay();

            motion.Tick(0.3f);
            Assert.That(released, Is.EqualTo(0));

            motion.Tick(0.1f);
            Assert.That(released, Is.EqualTo(1));

            motion.Tick(0.3f);
            Assert.That(released, Is.EqualTo(1));
        }

        [Test]
        public void The_motion_finishes_at_its_authored_length()
        {
            var motion = new OneShotMotion(ReleaseSeconds, ThrowSeconds);
            var finished = 0;
            motion.Finished += () => finished++;
            motion.TryPlay();

            motion.Tick(0.9f);
            Assert.That(motion.IsPlaying, Is.True);
            Assert.That(finished, Is.EqualTo(0));

            motion.Tick(0.1f);
            Assert.That(motion.IsPlaying, Is.False);
            Assert.That(finished, Is.EqualTo(1));

            motion.Tick(1f);
            Assert.That(finished, Is.EqualTo(1));
        }

        [Test]
        public void Cannot_start_again_until_it_has_finished()
        {
            var motion = new OneShotMotion(ReleaseSeconds, ThrowSeconds);

            Assert.That(motion.TryPlay(), Is.True);
            Assert.That(motion.TryPlay(), Is.False);

            motion.Tick(ThrowSeconds);

            Assert.That(motion.TryPlay(), Is.True);
        }

        [Test]
        public void Playing_again_raises_the_cue_again()
        {
            var motion = new OneShotMotion(ReleaseSeconds, ThrowSeconds);
            var released = 0;
            motion.Cued += () => released++;

            motion.TryPlay();
            motion.Tick(ThrowSeconds);
            motion.TryPlay();
            motion.Tick(ThrowSeconds);

            Assert.That(released, Is.EqualTo(2));
        }

        [Test]
        public void A_long_step_raises_the_cue_before_the_end()
        {
            var motion = new OneShotMotion(ReleaseSeconds, ThrowSeconds);
            var events = new List<string>();
            motion.Cued += () => events.Add("released");
            motion.Finished += () => events.Add("finished");
            motion.TryPlay();

            motion.Tick(5f);

            Assert.That(events, Is.EqualTo(new[] { "released", "finished" }));
        }

        [Test]
        public void Does_nothing_until_played()
        {
            var motion = new OneShotMotion(ReleaseSeconds, ThrowSeconds);
            var raised = false;
            motion.Cued += () => raised = true;
            motion.Finished += () => raised = true;

            motion.Tick(5f);

            Assert.That(raised, Is.False);
            Assert.That(motion.IsPlaying, Is.False);
        }

        [Test]
        public void Rejects_a_cue_outside_the_motion()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OneShotMotion(1.5f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OneShotMotion(-0.1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OneShotMotion(0f, -1f));
        }

        [Test]
        public void Ticking_does_not_allocate()
        {
            var motion = new OneShotMotion(ReleaseSeconds, ThrowSeconds);
            motion.TryPlay();

            AllocationAssert.DoesNotAllocate(() => motion.Tick(0.016f));
        }
    }
}
