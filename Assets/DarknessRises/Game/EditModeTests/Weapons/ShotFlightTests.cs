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

        [Test]
        public void TracksHowMuchOfItsRangeIsUsed()
        {
            var flight = new ShotFlight(Vector3.zero, Vector3.right, Speed, Range);
            Assert.That(flight.RangeUsed, Is.EqualTo(0f));

            flight.Advance(flight.StepLength(0.125f));
            Assert.That(flight.RangeUsed, Is.EqualTo(0.25f).Within(1e-5f));

            flight.Stop(Vector3.right * 4f);
            Assert.That(flight.RangeUsed, Is.EqualTo(1f));
        }

        [Test]
        public void TheFirstCheckStartsBackAtTheBreech()
        {
            // The muzzle 0.75 m ahead of the character's middle: an Enemy wrapped around the muzzle is still in the check.
            var flight = new ShotFlight(new Vector3(0f, 1.3f, 0.75f), Vector3.forward, Speed, Range, leadIn: 0.75f);

            Assert.That(flight.CheckOrigin, Is.EqualTo(new Vector3(0f, 1.3f, 0f)));
            Assert.That(flight.CheckLength(0.5f), Is.EqualTo(1.25f).Within(1e-5f));
        }

        [Test]
        public void TheLeadInIsNotPartOfTheRange()
        {
            var flight = new ShotFlight(Vector3.zero, Vector3.forward, Speed, Range, leadIn: 0.75f);

            Assert.That(flight.RangeUsed, Is.EqualTo(0f));
            Assert.That(flight.StepLength(10f), Is.EqualTo(Range));
        }

        [Test]
        public void LaterChecksStartWhereTheShotIs()
        {
            var flight = new ShotFlight(Vector3.zero, Vector3.forward, Speed, Range, leadIn: 0.75f);

            flight.Advance(flight.StepLength(0.1f));

            Assert.That(flight.LeadIn, Is.EqualTo(0f));
            Assert.That(flight.CheckOrigin, Is.EqualTo(flight.Position));
            Assert.That(flight.CheckLength(0.5f), Is.EqualTo(0.5f));
        }

        [Test]
        public void TheFirstStepIsAlsoCheckedBackwardsToTheBreech()
        {
            // Something wrapped around the breech is met from outside, coming back from the far end of the first step.
            var flight = new ShotFlight(new Vector3(0f, 1.3f, 0.75f), Vector3.forward, Speed, Range, leadIn: 0.75f);

            Assert.That(flight.ChecksBack, Is.True);
            Assert.That(flight.BackCheckOrigin(0.5f), Is.EqualTo(new Vector3(0f, 1.3f, 1.25f)));

            // Back along the flight for the whole check, it ends at the breech.
            var end = flight.BackCheckOrigin(0.5f) - flight.Direction * flight.CheckLength(0.5f);
            Assert.That(Vector3.Distance(end, flight.CheckOrigin), Is.LessThan(1e-5f));

            flight.Advance(0.5f);
            Assert.That(flight.ChecksBack, Is.False);
        }

        [Test]
        public void AShotWithoutALeadInNeverChecksBackwards()
        {
            Assert.That(new ShotFlight(Vector3.zero, Vector3.forward, Speed, Range).ChecksBack, Is.False);
        }

        [Test]
        public void ANegativeLeadInChecksFromTheMuzzle()
        {
            var flight = new ShotFlight(Vector3.zero, Vector3.forward, Speed, Range, leadIn: -1f);

            Assert.That(flight.CheckOrigin, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void TheLeadInReachesFromTheBreechToTheMuzzleAlongTheFlight()
        {
            var breech = new Vector3(2f, 1.3f, 3f);
            var muzzle = breech + new Vector3(0f, 0.2f, 0.75f);

            Assert.That(ShotFlight.LeadInFrom(breech, muzzle, Vector3.forward * 4f), Is.EqualTo(0.75f).Within(1e-5f));

            // A Shot spread 90 degrees off the muzzle's line has no lead-in, and one fired backwards neither.
            Assert.That(ShotFlight.LeadInFrom(breech, muzzle, Vector3.right), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(ShotFlight.LeadInFrom(breech, muzzle, Vector3.back), Is.EqualTo(0f));
            Assert.That(ShotFlight.LeadInFrom(breech, muzzle, Vector3.up), Is.EqualTo(0f));
        }
    }
}
