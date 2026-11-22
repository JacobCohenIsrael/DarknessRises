using System;
using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Weapons
{
    public sealed class WeaponSwitchBeatTests
    {
        // Powers of two, so the steps are exact in floats.
        private const float LowerSeconds = 0.125f;
        private const float RaiseSeconds = 0.25f;

        private WeaponSwitchBeat beat;
        private int swaps;

        [SetUp]
        public void SetUp()
        {
            beat = new WeaponSwitchBeat(LowerSeconds, RaiseSeconds);
            swaps = 0;
            beat.Swapped += () => swaps++;
        }

        [Test]
        public void StartsUpAndReady()
        {
            Assert.That(beat.IsSwitching, Is.False);
            Assert.That(beat.Lowered, Is.Zero);
            Assert.That(beat.MayFire, Is.True);
        }

        [Test]
        public void TheOldWeaponGoesDownAndHoldsItsFire()
        {
            beat.Start();
            beat.Tick(LowerSeconds / 2f);

            Assert.That(beat.IsLowering, Is.True);
            Assert.That(beat.Lowered, Is.EqualTo(0.5f));
            Assert.That(beat.MayFire, Is.False);
            Assert.That(swaps, Is.Zero);
        }

        [Test]
        public void TheNewWeaponTakesItsPlaceAtTheBottomAndMayFireAsItComesUp()
        {
            beat.Start();
            beat.Tick(LowerSeconds);

            Assert.That(swaps, Is.EqualTo(1));
            Assert.That(beat.IsRaising, Is.True);
            Assert.That(beat.Lowered, Is.EqualTo(1f));
            Assert.That(beat.MayFire, Is.True);
        }

        [Test]
        public void TheNewWeaponComesUpOverTheRaiseTime()
        {
            beat.Start();
            beat.Tick(LowerSeconds);
            beat.Tick(RaiseSeconds / 2f);
            Assert.That(beat.Lowered, Is.EqualTo(0.5f));

            beat.Tick(RaiseSeconds / 2f);
            Assert.That(beat.IsSwitching, Is.False);
            Assert.That(beat.Lowered, Is.Zero);
        }

        [Test]
        public void WhatsLeftOfAFrameAtTheBottomGoesToComingUp()
        {
            beat.Start();
            beat.Tick(LowerSeconds + RaiseSeconds / 4f);

            Assert.That(swaps, Is.EqualTo(1));
            Assert.That(beat.Lowered, Is.EqualTo(0.75f));
        }

        [Test]
        public void TheWholeBeatTakesTheLowerAndRaiseTimes()
        {
            beat.Start();
            beat.Tick(LowerSeconds + RaiseSeconds);

            Assert.That(beat.IsSwitching, Is.False);
            Assert.That(swaps, Is.EqualTo(1));
        }

        [Test]
        public void ASwitchWhileGoingDownChangesNothing()
        {
            beat.Start();
            beat.Tick(LowerSeconds / 2f);
            beat.Start();
            beat.Tick(LowerSeconds / 2f);

            Assert.That(swaps, Is.EqualTo(1));
            Assert.That(beat.IsRaising, Is.True);
        }

        [Test]
        public void ASwitchWhileComingUpGoesBackDownFromWhereItGotTo()
        {
            beat.Start();
            beat.Tick(LowerSeconds);
            beat.Tick(RaiseSeconds / 2f);

            beat.Start();
            Assert.That(beat.IsLowering, Is.True);
            Assert.That(beat.MayFire, Is.False);
            Assert.That(beat.Lowered, Is.EqualTo(0.5f));

            // Halfway down already: half the lowering time to the bottom.
            beat.Tick(LowerSeconds / 2f);
            Assert.That(swaps, Is.EqualTo(2));
        }

        [Test]
        public void NoLowerTimeSwapsAtOnce()
        {
            var instant = new WeaponSwitchBeat(0f, RaiseSeconds);
            var swapped = false;
            instant.Swapped += () => swapped = true;

            instant.Start();

            Assert.That(swapped, Is.True);
            Assert.That(instant.MayFire, Is.True);
            Assert.That(instant.IsRaising, Is.True);
        }

        [Test]
        public void NoTimeAtAllIsNoBeat()
        {
            var instant = new WeaponSwitchBeat(0f, 0f);
            instant.Start();

            Assert.That(instant.IsSwitching, Is.False);
            Assert.That(instant.Lowered, Is.Zero);
        }

        [Test]
        public void FinishCutsTheBeatShort()
        {
            beat.Start();
            beat.Tick(LowerSeconds / 2f);
            beat.Finish();

            Assert.That(beat.IsSwitching, Is.False);
            Assert.That(beat.Lowered, Is.Zero);
            Assert.That(beat.MayFire, Is.True);
        }

        [Test]
        public void RefusesNegativeTimes()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponSwitchBeat(-1f, RaiseSeconds));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponSwitchBeat(LowerSeconds, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => beat.Tick(-0.1f));
        }

        [Test]
        public void ASwitchAndItsBeatDontAllocate()
        {
            AllocationAssert.DoesNotAllocate(() =>
            {
                beat.Start();
                for (var i = 0; i < 30; i++)
                {
                    beat.Tick(1f / 60f);
                }
            });
        }
    }
}
