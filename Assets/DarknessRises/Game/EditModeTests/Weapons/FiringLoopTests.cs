using System;
using System.Collections.Generic;
using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Weapons
{
    public sealed class FiringLoopTests
    {
        private const float Frame = 1f / 60f;
        private const float Tolerance = 1e-3f;

        // The Flamethrower's: Dragon Spit Fire 1, from 0.6 s, repeating 1.05-3.45 s.
        private static FiringLoopTiming Timing(float fadeIn = 0.04f, float fadeOut = 0.35f) =>
            new(8.64f, 0.6f, 1.05f, 3.45f, 0.3f, fadeIn, fadeOut, 0.2f);

        [Test]
        public void Starts_with_the_first_Shot_from_late_in_the_build_up()
        {
            var loop = new FiringLoop(Timing());
            var started = Record(loop, out _);

            Assert.That(loop.IsFiring, Is.False);
            loop.Shot();

            Assert.That(loop.IsFiring, Is.True);
            Assert.That(started, Is.EqualTo(new[] { (0, 0.6f) }));

            // Shots that follow keep it going without restarting it.
            loop.Shot();
            Tick(loop, 0.1f);
            loop.Shot();
            Assert.That(started.Count, Is.EqualTo(1));
        }

        [Test]
        public void Fades_in_over_its_fade_in_time()
        {
            var loop = new FiringLoop(Timing(fadeIn: 0.1f));
            loop.Shot();

            Assert.That(loop.Volume(0), Is.EqualTo(0f));
            loop.Tick(0.05f);
            Assert.That(loop.Volume(0), Is.EqualTo((float)Math.Sin(0.5 * Math.PI * 0.5)).Within(Tolerance));
            loop.Tick(0.05f);
            Assert.That(loop.Volume(0), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Crossfades_each_pass_into_the_next_at_the_loop_start()
        {
            var loop = new FiringLoop(Timing());
            var started = Record(loop, out var stopped);
            loop.Shot();

            // The first pass reaches the crossfade at 3.15 s in the clip: 2.55 s after it started at 0.6 s.
            KeepFiring(loop, 2.5f);
            Assert.That(started.Count, Is.EqualTo(1));
            KeepFiring(loop, 0.1f);
            Assert.That(started, Is.EqualTo(new[] { (0, 0.6f), (1, 1.05f) }));
            Assert.That(loop.IsPlaying(0), Is.True);
            Assert.That(loop.IsPlaying(1), Is.True);

            // The old pass has faded out by the end of the loop.
            KeepFiring(loop, 0.32f);
            Assert.That(stopped, Is.EqualTo(new[] { 0 }));
            Assert.That(loop.IsPlaying(1), Is.True);
            Assert.That(loop.ClipSeconds(1), Is.LessThan(3.45f));

            // Every later pass is the steady part: 2.1 s from the loop start to the next crossfade.
            KeepFiring(loop, 2.1f);
            Assert.That(started[started.Count - 1], Is.EqualTo((0, 1.05f)));
        }

        [Test]
        public void An_equal_power_crossfade_never_dips()
        {
            var loop = new FiringLoop(Timing());
            loop.Shot();
            KeepFiring(loop, 2.56f);
            for (var frame = 0; frame < 18; frame++)
            {
                loop.Shot();
                loop.Tick(Frame);
                var power = loop.Volume(0) * loop.Volume(0) + loop.Volume(1) * loop.Volume(1);
                Assert.That(power, Is.EqualTo(1f).Within(0.02f));
            }
        }

        [Test]
        public void Fades_out_when_firing_stops()
        {
            var loop = new FiringLoop(Timing(fadeOut: 0.35f));
            Record(loop, out var stopped);
            loop.Shot();
            KeepFiring(loop, 1f);

            loop.Release();
            Assert.That(loop.IsFiring, Is.False);
            Assert.That(loop.IsPlaying(0), Is.True);

            Tick(loop, 0.2f);
            Assert.That(loop.Volume(0), Is.GreaterThan(0f));
            Assert.That(loop.Volume(0), Is.LessThan(1f));

            Tick(loop, 0.2f);
            Assert.That(loop.IsPlaying(0), Is.False);
            Assert.That(stopped, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Fades_out_by_itself_once_Shots_stop_coming()
        {
            var loop = new FiringLoop(Timing());
            loop.Shot();
            Tick(loop, 0.15f);
            Assert.That(loop.IsFiring, Is.True);

            Tick(loop, 0.1f);
            Assert.That(loop.IsFiring, Is.False);
        }

        [Test]
        public void Firing_again_while_fading_out_starts_a_fresh_pass_on_the_other_voice()
        {
            var loop = new FiringLoop(Timing());
            var started = Record(loop, out _);
            loop.Shot();
            KeepFiring(loop, 1f);
            loop.Release();
            Tick(loop, 0.1f);

            loop.Shot();

            Assert.That(started[started.Count - 1], Is.EqualTo((1, 0.6f)));
            Assert.That(loop.IsPlaying(0), Is.True, "The last burst keeps fading out.");
        }

        [Test]
        public void The_pause_holds_it()
        {
            var loop = new FiringLoop(Timing());
            loop.Shot();
            Tick(loop, 0.1f);
            var clipSeconds = loop.ClipSeconds(0);

            for (var i = 0; i < 600; i++)
            {
                loop.Tick(0f);
            }

            Assert.That(loop.IsFiring, Is.True);
            Assert.That(loop.ClipSeconds(0), Is.EqualTo(clipSeconds));
        }

        [Test]
        public void Rejects_timing_that_does_not_fit_the_clip()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FiringLoopTiming(3f, 0.6f, 1.05f, 3.45f, 0.3f, 0f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FiringLoopTiming(8f, 0.6f, 1f, 1.5f, 0.3f, 0f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FiringLoopTiming(8f, 3.3f, 1.05f, 3.45f, 0.3f, 0f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FiringLoopTiming(8f, 0f, 1.05f, 3.45f, -0.3f, 0f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FiringLoop(Timing()).Tick(-1f));
        }

        [Test]
        public void Ticking_does_not_allocate()
        {
            var loop = new FiringLoop(Timing());
            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    loop.Shot();
                    loop.Tick(Frame);
                    loop.Volume(0);
                    loop.Volume(1);
                }

                loop.Release();
                loop.Tick(1f);
            });
        }

        private static List<(int voice, float clipSeconds)> Record(FiringLoop loop, out List<int> stopped)
        {
            var started = new List<(int, float)>();
            var stops = new List<int>();
            loop.VoiceStarted += (voice, clipSeconds) => started.Add((voice, (float)Math.Round(clipSeconds, 3)));
            loop.VoiceStopped += voice => stops.Add(voice);
            stopped = stops;
            return started;
        }

        private static void KeepFiring(FiringLoop loop, float seconds)
        {
            var frames = (int)Math.Round(seconds / Frame);
            for (var i = 0; i < frames; i++)
            {
                loop.Shot();
                loop.Tick(Frame);
            }
        }

        private static void Tick(FiringLoop loop, float seconds)
        {
            var frames = (int)Math.Round(seconds / Frame);
            for (var i = 0; i < frames; i++)
            {
                loop.Tick(Frame);
            }
        }
    }
}
