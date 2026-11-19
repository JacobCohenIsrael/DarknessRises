using DarknessRises.Game.Player;
using DarknessRises.Game.Routes;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Player
{
    public sealed class PlantStanceTests
    {
        private const float Tolerance = 1e-4f;

        // How far below the hip a leg's foot ends up, for a thigh and a shin of length 1 each.
        private static float FootDepth(LegBend bend)
        {
            return Mathf.Cos(bend.ThighForwardDegrees * Mathf.Deg2Rad) + Mathf.Cos(bend.ShinBackDegrees * Mathf.Deg2Rad);
        }

        // How far ahead of the hip the foot ends up.
        private static float FootAhead(LegBend bend)
        {
            return Mathf.Sin(bend.ThighForwardDegrees * Mathf.Deg2Rad) - Mathf.Sin(bend.ShinBackDegrees * Mathf.Deg2Rad);
        }

        [TestCase(StationBeat.Walking, false)]
        [TestCase(StationBeat.Ready, true)]
        [TestCase(StationBeat.Fight, true)]
        [TestCase(StationBeat.Clear, true)]
        [TestCase(StationBeat.Quiet, false)]
        [TestCase(StationBeat.Go, false)]
        public void Character_is_planted_from_the_Ready_beat_until_the_clear(StationBeat beat, bool planted)
        {
            Assert.That(PlantStance.IsPlantedIn(beat), Is.EqualTo(planted));
        }

        [Test]
        public void Standing_bends_nothing()
        {
            var lead = PlantStance.Lead(26f, 14f, 0f);
            var rear = PlantStance.Rear(26f, 14f, 0f);

            Assert.That(lead.ThighForwardDegrees, Is.Zero);
            Assert.That(lead.KneeDegrees, Is.Zero);
            Assert.That(rear.ThighForwardDegrees, Is.Zero);
            Assert.That(rear.KneeDegrees, Is.Zero);
        }

        [Test]
        public void Lead_foot_goes_forward_and_rear_foot_back()
        {
            Assert.That(FootAhead(PlantStance.Lead(26f, 14f, 1f)), Is.GreaterThan(0f));
            Assert.That(FootAhead(PlantStance.Rear(26f, 14f, 1f)), Is.LessThan(0f));
        }

        [TestCase(26f, 14f, 1f)]
        [TestCase(40f, 5f, 1f)]
        [TestCase(26f, 14f, 0.4f)]
        [TestCase(20f, 20f, 1f)]
        public void Both_feet_stay_at_the_same_height(float leadDegrees, float rearDegrees, float weight)
        {
            var lead = PlantStance.Lead(leadDegrees, rearDegrees, weight);
            var rear = PlantStance.Rear(leadDegrees, rearDegrees, weight);

            Assert.That(FootDepth(lead), Is.EqualTo(FootDepth(rear)).Within(Tolerance));
            Assert.That(FootDepth(lead), Is.LessThan(2f), "The knees bend, so the hips come down.");
        }

        [Test]
        public void Knees_only_ever_bend_backwards()
        {
            Assert.That(PlantStance.Lead(26f, 14f, 1f).KneeDegrees, Is.GreaterThan(0f));
            Assert.That(PlantStance.Rear(26f, 14f, 1f).KneeDegrees, Is.GreaterThan(0f));
        }
    }
}
