using System;
using System.Collections.Generic;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Randomness;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DarknessRises.Game.EditModeTests.Lightning
{
    public sealed class LightningSchedulerTests
    {
        private const float MinInterval = 4f;
        private const float MaxInterval = 6f;
        private const float RumbleLead = 1f;
        private const float Tolerance = 1e-4f;

        private ScriptedRandom random;
        private LightningScheduler scheduler;
        private List<string> events;

        [SetUp]
        public void SetUp()
        {
            random = new ScriptedRandom();
            scheduler = new LightningScheduler(new LightningTiming(MinInterval, MaxInterval, RumbleLead), random);
            events = new List<string>();
            scheduler.Rumbled += secondsUntilFlash => events.Add($"rumble {secondsUntilFlash:0.##}");
            scheduler.Flashed += () => events.Add("flash");
        }

        [Test]
        public void AmbientFlashComesAfterARandomIntervalFromTheConfiguredRange()
        {
            random.Next(5f);

            scheduler.EnterRoom(letsLightningIn: true);

            Assert.That(random.LastRange, NUnit.Framework.Is.EqualTo((MinInterval, MaxInterval)));
            scheduler.Tick(4.75f);
            Assert.That(events, Has.No.Member("flash"));
            scheduler.Tick(0.25f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "rumble 0.25", "flash" }));
        }

        [Test]
        public void TheRumbleStartsTheConfiguredLeadBeforeTheFlash()
        {
            random.Next(5f);
            scheduler.EnterRoom(letsLightningIn: true);

            scheduler.Tick(3.75f);
            Assert.That(events, NUnit.Framework.Is.Empty);

            scheduler.Tick(0.25f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "rumble 1" }));

            scheduler.Tick(0.75f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "rumble 1" }));

            scheduler.Tick(0.25f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "rumble 1", "flash" }));
        }

        [Test]
        public void EachFlashSchedulesTheNextFromWhenItWasDue()
        {
            random.Next(5f, 4.5f);
            scheduler.EnterRoom(letsLightningIn: true);

            // Overshoot the first flash by half a second: the next is due 4 seconds later, not 4.5.
            scheduler.Tick(5.5f);
            events.Clear();

            Assert.That(scheduler.SecondsUntilFlash, NUnit.Framework.Is.EqualTo(4f).Within(Tolerance));
            scheduler.Tick(3f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "rumble 1" }));
            scheduler.Tick(1f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "rumble 1", "flash" }));
        }

        [Test]
        public void ALongFrameStillRumblesBeforeEveryFlashItCovers()
        {
            random.Next(4f, 4f, 4f);
            scheduler.EnterRoom(letsLightningIn: true);

            scheduler.Tick(8f);

            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "rumble 0", "flash", "rumble 0", "flash" }));
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
        public void LeavingForARoomWithoutWindowsCancelsTheComingFlash()
        {
            random.Next(5f);
            scheduler.EnterRoom(letsLightningIn: true);
            scheduler.Tick(4.5f);
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
        public void SwitchedOffLightningNeitherRumblesNorFlashesUntilSwitchedBackOn()
        {
            random.Next(5f, 4f);
            scheduler.EnterRoom(letsLightningIn: true);

            scheduler.SetEnabled(false);
            scheduler.Tick(60f);
            Assert.That(events, NUnit.Framework.Is.Empty);

            scheduler.SetEnabled(true);
            scheduler.Tick(4f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "rumble 0", "flash" }));
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
        public void ABossRequestRumblesAtOnceAndFlashesAfterTheConfiguredLead()
        {
            scheduler.EnterRoom(letsLightningIn: true);
            scheduler.EnterBossDrivenMode();

            scheduler.RequestFlash();
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "rumble 1" }));

            scheduler.Tick(0.75f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "rumble 1" }));

            scheduler.Tick(0.25f);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "rumble 1", "flash" }));
            Assert.That(scheduler.IsFlashPending, NUnit.Framework.Is.False);
        }

        [Test]
        public void ABossRequestCanSetItsOwnLeadOrFlashAtOnce()
        {
            scheduler.EnterBossDrivenMode();

            scheduler.RequestFlash(2.5f);
            scheduler.Tick(2.5f);
            scheduler.RequestFlash(0f);

            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "rumble 2.5", "flash", "rumble 0", "flash" }));
        }

        [Test]
        public void OnlyTheBossCanRequestAFlash()
        {
            Assert.Throws<InvalidOperationException>(() => scheduler.RequestFlash());
        }

        [Test]
        public void ReturningToAmbientModeResumesTheWeather()
        {
            random.Next(5f, 5f);
            scheduler.EnterRoom(letsLightningIn: true);
            scheduler.EnterBossDrivenMode();
            scheduler.RequestFlash();
            events.Clear();

            scheduler.EnterAmbientMode();
            scheduler.Tick(5f);

            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "rumble 0", "flash" }));
        }

        [Test]
        public void ALeadLongerThanTheIntervalRumblesAsSoonAsTheFlashIsScheduled()
        {
            var shortScheduler = new LightningScheduler(new LightningTiming(0.5f, 0.5f, 2f), random);
            var rumbles = 0;
            shortScheduler.Rumbled += _ => rumbles++;
            random.Next(0.5f);

            shortScheduler.EnterRoom(letsLightningIn: true);
            shortScheduler.Tick(0f);

            Assert.That(rumbles, NUnit.Framework.Is.EqualTo(1));
        }

        [TestCase(0f, 5f, 1f)]
        [TestCase(5f, 4f, 1f)]
        [TestCase(4f, 5f, -1f)]
        public void TimingRejectsImpossibleValues(float min, float max, float lead)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new LightningTiming(min, max, lead));
        }

        [Test]
        public void TickingDoesNotAllocate()
        {
            var quietScheduler = new LightningScheduler(new LightningTiming(MinInterval, MaxInterval, RumbleLead), new SystemRandom(7));
            quietScheduler.Rumbled += _ => { };
            quietScheduler.Flashed += () => { };
            quietScheduler.EnterRoom(letsLightningIn: true);
            quietScheduler.Tick(1f / 30f);

            Assert.That(() =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    quietScheduler.Tick(1f / 30f);
                }
            }, Is.Not.AllocatingGCMemory());
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
