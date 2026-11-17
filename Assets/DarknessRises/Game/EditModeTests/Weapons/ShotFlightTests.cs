using DarknessRises.Game.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Weapons
{
    public sealed class ShotFlightTests
    {
        private const float Speed = 20f;
        private const float Range = 10f;

        [Test]
        public void FliesStraightAtItsSpeed()
        {
            var flight = new ShotFlight(Vector3.zero, Vector3.forward * 3f, Speed, Range);

            flight.Advance(flight.StepLength(0.25f));

            Assert.That(flight.Position.z, Is.EqualTo(5f).Within(1e-5f));
            Assert.That(flight.IsSpent, Is.False);
        }

        [Test]
        public void NeverFliesPastItsRange()
        {
            var flight = new ShotFlight(Vector3.zero, Vector3.right, Speed, Range);

            flight.Advance(flight.StepLength(0.4f));
            flight.Advance(flight.StepLength(0.4f));

            Assert.That(flight.Position.x, Is.EqualTo(Range).Within(1e-5f));
            Assert.That(flight.IsSpent, Is.True);
            Assert.That(flight.StepLength(1f), Is.EqualTo(0f));
        }

        [Test]
        public void StoppingEndsTheFlightWhereItHit()
        {
            var flight = new ShotFlight(Vector3.zero, Vector3.right, Speed, Range);
            var hitPoint = new Vector3(2f, 0f, 0f);

            flight.Stop(hitPoint);

            Assert.That(flight.IsSpent, Is.True);
            Assert.That(flight.Position, Is.EqualTo(hitPoint));
        }
    }
}
