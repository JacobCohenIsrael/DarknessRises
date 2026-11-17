using DarknessRises.Game.Grenades;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Grenades
{
    public sealed class GrenadeFlightTests
    {
        private const float Speed = 10f;
        private const float Gravity = 20f;
        private const float MinSeconds = 0.3f;
        private const float Tolerance = 1e-4f;

        private static readonly Vector3 Hand = new(0f, 1.5f, 0f);

        [Test]
        public void FlightTimeIsTheDistanceAcrossTheFloorOverTheSpeed()
        {
            var flight = new GrenadeFlight(Hand, new Vector3(8f, 0f, 6f), Speed, Gravity, MinSeconds);

            Assert.That(flight.FlightSeconds, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void AShortThrowStillTakesTheMinimumTime()
        {
            var flight = new GrenadeFlight(Hand, new Vector3(0.5f, 0f, 0f), Speed, Gravity, MinSeconds);

            Assert.That(flight.FlightSeconds, Is.EqualTo(MinSeconds).Within(Tolerance));
        }

        [Test]
        public void StartsInTheHandAndLandsOnTheTarget()
        {
            var target = new Vector3(8f, 0f, 6f);
            var flight = new GrenadeFlight(Hand, target, Speed, Gravity, MinSeconds);

            Assert.That(Vector3.Distance(flight.Position, Hand), Is.LessThan(Tolerance));
            Assert.That(flight.HasLanded, Is.False);

            flight.Advance(0.5f);
            Assert.That(flight.HasLanded, Is.False);

            flight.Advance(0.5f);
            Assert.That(flight.HasLanded, Is.True);
            Assert.That(Vector3.Distance(flight.Position, target), Is.LessThan(Tolerance));
        }

        [Test]
        public void NeverFliesPastItsTarget()
        {
            var target = new Vector3(8f, 0f, 6f);
            var flight = new GrenadeFlight(Hand, target, Speed, Gravity, MinSeconds);

            flight.Advance(5f);

            Assert.That(flight.SecondsFlown, Is.EqualTo(flight.FlightSeconds));
            Assert.That(Vector3.Distance(flight.Position, target), Is.LessThan(Tolerance));
        }

        [Test]
        public void ArcsAboveTheHand()
        {
            var flight = new GrenadeFlight(Hand, new Vector3(10f, 0f, 0f), Speed, Gravity, MinSeconds);

            var middle = flight.PositionAt(flight.FlightSeconds / 2f);

            Assert.That(middle.y, Is.GreaterThan(Hand.y));
            Assert.That(middle.x, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void StoppingEndsTheFlightWhereItHit()
        {
            var flight = new GrenadeFlight(Hand, new Vector3(10f, 0f, 0f), Speed, Gravity, MinSeconds);
            var wall = new Vector3(3f, 1f, 0f);

            flight.Stop(wall);

            Assert.That(flight.HasLanded, Is.True);
            Assert.That(flight.Position, Is.EqualTo(wall));
        }

        [Test]
        public void AnAimPointInRangeIsKept()
        {
            var aim = new Vector3(3f, 0f, 4f);

            Assert.That(GrenadeFlight.ClampToRange(Hand, aim, 10f), Is.EqualTo(aim));
        }

        [Test]
        public void AnAimPointOutOfRangeIsPulledInAcrossTheFloor()
        {
            var clamped = GrenadeFlight.ClampToRange(Hand, new Vector3(30f, 0f, 40f), 10f);

            Assert.That(clamped.x, Is.EqualTo(6f).Within(Tolerance));
            Assert.That(clamped.z, Is.EqualTo(8f).Within(Tolerance));
            Assert.That(clamped.y, Is.EqualTo(0f));
        }
    }
}
