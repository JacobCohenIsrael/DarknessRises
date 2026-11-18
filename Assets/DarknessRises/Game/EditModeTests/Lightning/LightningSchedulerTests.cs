using System;
using System.Collections.Generic;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Randomness;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Lightning
{
    public sealed class LightningSchedulerTests
    {
        private const float MinInterval = 4f;
        private const float MaxInterval = 6f;
        private const float MinRumbleDelay = 0.2f;
        private const float MaxRumbleDelay = 1f;
        private const float Tolerance = 1e-4f;

        private ScriptedRandom random;
        private LightningScheduler scheduler;
        private List<string> events;

        [SetUp]
        public void SetUp()
        {
            random = new ScriptedRandom();
            scheduler = new LightningScheduler(Timing(), random);
            events = new List<string>();
            scheduler.Warned += secondsUntilFlash => events.Add($"warning {secondsUntilFlash:0.##}");
            scheduler.Flashed += () => events.Add("flash");
            scheduler.Rumbled += () => events.Add("rumble");
        }

        private static LightningTiming Timing() => new(MinInterval, MaxInterval, MinRumbleDelay, MaxRumbleDelay);

        [Test]
        public void AmbientFlashComesAfterARandomIntervalWithNothingBeforeIt()
        {
            random.Next(5f);

            scheduler.EnterRoom(letsLightningIn: true);

            Assert.That(random.LastRange, NUnit.Framework.Is.EqualTo((MinInterval, MaxInterval)));
            scheduler.Tick(4.75f);
            Assert.That(events, NUnit.Framework.Is.Empty);
            scheduler.Tick(0.25f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "flash" }));
        }

        [Test]
        public void TheRumbleFollowsTheFlashAfterARandomDelayFromTheConfiguredRange()
        {
            random.Next(5f, 0.75f);
            scheduler.EnterRoom(letsLightningIn: true);

            scheduler.Tick(5f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "flash" }));
            Assert.That(scheduler.SecondsUntilRumble, NUnit.Framework.Is.EqualTo(0.75f).Within(Tolerance));

            scheduler.Tick(0.5f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "flash" }));

            scheduler.Tick(0.25f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "flash", "rumble" }));
        }

        [Test]
        public void ForManySeededStrikesEveryRumbleComesAfterItsFlashWithinTheDelayRange()
        {
            var seeded = new LightningScheduler(Timing(), new SystemRandom(54));
            var now = 0f;
            var lastFlash = float.NaN;
            var delays = new List<float>();
            seeded.Flashed += () =>
            {
                Assert.That(float.IsNaN(lastFlash), NUnit.Framework.Is.True, "A flash came before the last one's Rumble.");
                lastFlash = now;
            };
            seeded.Rumbled += () =>
            {
                Assert.That(float.IsNaN(lastFlash), NUnit.Framework.Is.False, "A Rumble came without a flash before it.");
                delays.Add(now - lastFlash);
                lastFlash = float.NaN;
            };

            seeded.EnterRoom(letsLightningIn: true);
            const float frame = 1f / 60f;
            for (var i = 0; i < 60 * 60 * 10; i++)
            {
                now += frame;
                seeded.Tick(frame);
            }

            Assert.That(delays.Count, NUnit.Framework.Is.GreaterThan(100));
            foreach (var delay in delays)
            {
                // Rounded up to whole frames.
                Assert.That(delay, NUnit.Framework.Is.InRange(MinRumbleDelay - Tolerance, MaxRumbleDelay + frame + Tolerance));
            }
        }

        [Test]
        public void EachFlashSchedulesTheNextFromWhenItWasDue()
        {
            random.Next(5f, 0.5f, 4.5f);
            scheduler.EnterRoom(letsLightningIn: true);

            // Overshoot the first flash by half a second: the next is due 4 seconds later, not 4.5.
            scheduler.Tick(5.5f);

            Assert.That(scheduler.SecondsUntilFlash, NUnit.Framework.Is.EqualTo(4f).Within(Tolerance));
        }

        [Test]
        public void ALongFrameKeepsEveryFlashAndRumbleInOrder()
        {
            random.Next(4f, 0.5f, 4f, 0.5f, 4f);
            scheduler.EnterRoom(letsLightningIn: true);

            scheduler.Tick(9f);

            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "flash", "rumble", "flash", "rumble" }));
        }

        [Test]
        public void ARoomWithoutWindowsGetsNoLightning()
        {
            random.Next(5f);
            scheduler.EnterRoom(letsLightningIn: false);

            scheduler.Tick(60f);

            Assert.That(events, NUnit.Framework.Is.Empty);
            Assert.That(scheduler.IsFlashPending, NUnit.Framework.Is.False);
        }

        [Test]
        public void LeavingForARoomWithoutWindowsCancelsTheComingFlashAndRumble()
        {
            random.Next(5f, 0.8f);
            scheduler.EnterRoom(letsLightningIn: true);
            scheduler.Tick(5.2f);
            events.Clear();

            scheduler.EnterRoom(letsLightningIn: false);
            scheduler.Tick(60f);

            Assert.That(events, NUnit.Framework.Is.Empty);
        }

        [Test]
        public void WalkingBetweenRoomsWithWindowsKeepsTheComingFlash()
        {
            random.Next(5f, 6f);
            scheduler.EnterRoom(letsLightningIn: true);
            scheduler.Tick(2f);

            scheduler.EnterRoom(letsLightningIn: true);

            Assert.That(scheduler.SecondsUntilFlash, NUnit.Framework.Is.EqualTo(3f).Within(Tolerance));
        }

        [Test]
        public void SwitchedOffLightningNeitherFlashesNorRumblesUntilSwitchedBackOn()
        {
            random.Next(5f, 4f, 0.5f);
            scheduler.EnterRoom(letsLightningIn: true);

            scheduler.SetEnabled(false);
            scheduler.Tick(60f);
            Assert.That(events, NUnit.Framework.Is.Empty);

            scheduler.SetEnabled(true);
            scheduler.Tick(4.5f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "flash", "rumble" }));
        }

        [Test]
        public void BossDrivenModeHasNoAmbientFlashes()
        {
            random.Next(5f);
            scheduler.EnterRoom(letsLightningIn: true);

            scheduler.EnterBossDrivenMode();
            scheduler.Tick(60f);

            Assert.That(events, NUnit.Framework.Is.Empty);
        }

        [Test]
        public void ABossRequestWarnsAtOnceThenFlashesThenRumbles()
        {
            scheduler.EnterRoom(letsLightningIn: true);
            scheduler.EnterBossDrivenMode();
            random.Next(0.5f);

            scheduler.RequestFlash(1.25f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "warning 1.25" }));

            scheduler.Tick(1f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "warning 1.25" }));

            scheduler.Tick(0.25f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "warning 1.25", "flash" }));
            Assert.That(scheduler.IsFlashPending, NUnit.Framework.Is.False);

            scheduler.Tick(0.25f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "warning 1.25", "flash" }));

            scheduler.Tick(0.25f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "warning 1.25", "flash", "rumble" }));
        }

        [Test]
        public void ABossRequestWithNoWarningFlashesAtOnceAndStillRumblesAfter()
        {
            scheduler.EnterBossDrivenMode();
            random.Next(0.3f);

            scheduler.RequestFlash(0f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "warning 0", "flash" }));

            scheduler.Tick(0.3f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "warning 0", "flash", "rumble" }));
        }

        [Test]
        public void ASecondFlashBeforeTheFirstRumbleSharesIt()
        {
            scheduler.EnterBossDrivenMode();
            random.Next(1f, 0.2f);

            scheduler.RequestFlash(0f);
            scheduler.Tick(0.5f);
            scheduler.RequestFlash(0f);
            scheduler.Tick(0.5f);

            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "warning 0", "flash", "warning 0", "flash", "rumble" }));
        }

        [Test]
        public void OnlyTheBossCanRequestAFlash()
        {
            Assert.Throws<InvalidOperationException>(() => scheduler.RequestFlash(1f));
        }

        [Test]
        public void ReturningToAmbientModeResumesTheWeatherAndKeepsTheLastRumble()
        {
            random.Next(5f, 0.5f, 5f, 0.5f);
            scheduler.EnterRoom(letsLightningIn: true);
            scheduler.EnterBossDrivenMode();
            scheduler.RequestFlash(0f);
            events.Clear();

            scheduler.EnterAmbientMode();
            scheduler.Tick(5f);

            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "rumble", "flash" }));
        }

        [Test]
        public void TheStormRunsInRoomsWithWindowsAndThroughoutABossFight_ButNotWithLightningOff()
        {
            Assert.That(scheduler.IsStormRunning, NUnit.Framework.Is.False);

            scheduler.EnterRoom(letsLightningIn: true);
            Assert.That(scheduler.IsStormRunning, NUnit.Framework.Is.True);

            scheduler.EnterRoom(letsLightningIn: false);
            Assert.That(scheduler.IsStormRunning, NUnit.Framework.Is.False);

            scheduler.EnterBossDrivenMode();
            Assert.That(scheduler.IsStormRunning, NUnit.Framework.Is.True);

            scheduler.SetEnabled(false);
            Assert.That(scheduler.IsStormRunning, NUnit.Framework.Is.False);
        }

        [TestCase(0f, 5f, 0.1f, 1f)]
        [TestCase(5f, 4f, 0.1f, 1f)]
        [TestCase(4f, 5f, -0.1f, 1f)]
        [TestCase(4f, 5f, 1f, 0.5f)]
        public void TimingRejectsImpossibleValues(float min, float max, float minDelay, float maxDelay)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new LightningTiming(min, max, minDelay, maxDelay));
        }

        [Test]
        public void TickingDoesNotAllocate()
        {
            var quietScheduler = new LightningScheduler(Timing(), new SystemRandom(7));
            quietScheduler.Flashed += () => { };
            quietScheduler.Rumbled += () => { };
            quietScheduler.EnterRoom(letsLightningIn: true);
            quietScheduler.Tick(1f / 30f);

            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    quietScheduler.Tick(1f / 30f);
                }
            });
        }

        private sealed class ScriptedRandom : IRandom
        {
            private readonly Queue<float> values = new();

            public (float, float) LastRange { get; private set; }

            public void Next(params float[] next)
            {
                foreach (var value in next)
                {
                    values.Enqueue(value);
                }
            }

            public float Range(float min, float max)
            {
                LastRange = (min, max);
                return values.Count > 0 ? values.Dequeue() : max;
            }
        }
    }
}
