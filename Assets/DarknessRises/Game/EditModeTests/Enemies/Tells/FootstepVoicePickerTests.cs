using System;
using DarknessRises.Game.Enemies.Tells;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Enemies.Tells
{
    public sealed class FootstepVoicePickerTests
    {
        private const float Hearing = 25f;

        private static FootstepCandidate Walking(float metres) => new(metres * metres, true, Hearing);

        private static FootstepCandidate Standing(float metres) => new(metres * metres, false, Hearing);

        [Test]
        public void Only_the_nearest_walking_Enemies_are_heard()
        {
            var picker = new FootstepVoicePicker(2);
            var candidates = new[] { Walking(10f), Walking(3f), Walking(7f), Walking(1f) };
            var heard = new bool[candidates.Length];

            var count = picker.Pick(candidates, heard);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(heard, Is.EqualTo(new[] { false, true, false, true }));
        }

        [Test]
        public void An_Enemy_standing_still_is_not_heard_however_near()
        {
            var picker = new FootstepVoicePicker(2);
            var candidates = new[] { Standing(1f), Walking(8f), Walking(9f) };
            var heard = new bool[candidates.Length];

            picker.Pick(candidates, heard);

            Assert.That(heard, Is.EqualTo(new[] { false, true, true }));
        }

        [Test]
        public void An_Enemy_beyond_its_hearing_range_is_not_heard()
        {
            var picker = new FootstepVoicePicker(3);
            var candidates = new[] { Walking(30f), Walking(5f) };
            var heard = new bool[candidates.Length];

            Assert.That(picker.Pick(candidates, heard), Is.EqualTo(1));
            Assert.That(heard, Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void Fewer_walkers_than_the_budget_are_all_heard()
        {
            var picker = new FootstepVoicePicker(3);
            var candidates = new[] { Walking(4f), Standing(2f) };
            var heard = new bool[candidates.Length];

            Assert.That(picker.Pick(candidates, heard), Is.EqualTo(1));
            Assert.That(heard, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void At_equal_distance_the_earlier_Enemy_is_heard()
        {
            var picker = new FootstepVoicePicker(1);
            var candidates = new[] { Walking(5f), Walking(5f) };
            var heard = new bool[candidates.Length];

            picker.Pick(candidates, heard);

            Assert.That(heard, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void A_zero_budget_silences_every_footstep()
        {
            var picker = new FootstepVoicePicker(0);
            var candidates = new[] { Walking(1f) };
            var heard = new[] { true };

            Assert.That(picker.Pick(candidates, heard), Is.EqualTo(0));
            Assert.That(heard[0], Is.False);
        }

        [Test]
        public void Picking_again_clears_who_was_heard_before()
        {
            var picker = new FootstepVoicePicker(1);
            var heard = new bool[2];
            picker.Pick(new[] { Walking(1f), Walking(5f) }, heard);

            picker.Pick(new[] { Standing(1f), Walking(5f) }, heard);

            Assert.That(heard, Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void A_crowd_keeps_the_nearest_whatever_order_it_comes_in()
        {
            var picker = new FootstepVoicePicker(3);
            var metres = new[] { 9f, 2f, 14f, 6f, 1f, 11f, 4f, 20f };
            var candidates = new FootstepCandidate[metres.Length];
            for (var i = 0; i < metres.Length; i++)
            {
                candidates[i] = Walking(metres[i]);
            }

            var heard = new bool[candidates.Length];
            picker.Pick(candidates, heard);

            // 1, 2 and 4 m.
            Assert.That(heard, Is.EqualTo(new[] { false, true, false, false, true, false, true, false }));
        }

        [Test]
        public void Too_few_heard_flags_are_refused()
        {
            var picker = new FootstepVoicePicker(1);

            Assert.Throws<ArgumentException>(() => picker.Pick(new[] { Walking(1f), Walking(2f) }, new bool[1]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FootstepVoicePicker(-1));
        }

        [Test]
        public void Picking_does_not_allocate()
        {
            var picker = new FootstepVoicePicker(3);
            var candidates = new[] { Walking(9f), Walking(2f), Standing(1f), Walking(6f), Walking(4f) };
            var heard = new bool[candidates.Length];

            AllocationAssert.DoesNotAllocate(() => picker.Pick(candidates, heard));
        }
    }
}
