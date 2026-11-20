using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// Where a Shot is along its straight flight, and when it has used up its range.
    /// </summary>
    /// <remarks>
    /// A Shot leaves the muzzle, which sits ahead of the character. An Enemy pressed up against the character (or leaning
    /// over it in its attack lunge) can wrap around the muzzle, and a ray that starts inside a collider never hits it.
    /// So the first check of a flight starts back at the breech, <see cref="LeadIn"/> metres behind the muzzle, and
    /// anything between the character and the muzzle is hit point-blank. An Enemy that wraps around the breech too
    /// (leaning right over the character) is found by checking that first step backwards, from its far end
    /// (<see cref="BackCheckOrigin"/>): coming from outside, that check meets the Enemy's far side.
    /// </remarks>
    public struct ShotFlight
    {
        private readonly float speed;
        private readonly float range;

        /// <param name="leadIn">
        /// Metres behind <paramref name="origin"/> the first check starts: from the character's middle out to the muzzle.
        /// Not part of the range.
        /// </param>
        public ShotFlight(Vector3 origin, Vector3 direction, float speed, float range, float leadIn = 0f)
        {
            Position = origin;
            Direction = direction.normalized;
            this.speed = speed;
            this.range = range;
            DistanceTravelled = 0f;
            LeadIn = Mathf.Max(0f, leadIn);
        }

        /// <summary>Metres behind <see cref="Position"/> the next check starts: the lead-in on the first step, then 0.</summary>
        public float LeadIn { get; private set; }

        /// <summary>Where the next check for a hit starts.</summary>
        public Vector3 CheckOrigin => Position - Direction * LeadIn;

        /// <summary>How far the next check for a hit reaches from <see cref="CheckOrigin"/>, for a step of <paramref name="step"/> metres.</summary>
        public float CheckLength(float step)
        {
            return LeadIn + step;
        }

        /// <summary>Whether the next step also needs checking backwards, for something wrapped around the breech.</summary>
        public bool ChecksBack => LeadIn > 0f;

        /// <summary>
        /// Where the backward check of a step of <paramref name="step"/> metres starts: the far end of the forward check.
        /// It runs against <see cref="Direction"/> for <see cref="CheckLength"/> metres, back to <see cref="CheckOrigin"/>.
        /// </summary>
        public Vector3 BackCheckOrigin(float step)
        {
            return Position + Direction * step;
        }

        /// <summary>
        /// Metres from <paramref name="breech"/> (the character's middle at muzzle height) to <paramref name="muzzle"/>,
        /// along <paramref name="direction"/> on the floor plane. 0 when the muzzle isn't ahead of the breech.
        /// </summary>
        public static float LeadInFrom(Vector3 breech, Vector3 muzzle, Vector3 direction)
        {
            var reach = muzzle - breech;
            reach.y = 0f;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0f)
            {
                return 0f;
            }

            return Mathf.Max(0f, Vector3.Dot(reach, direction.normalized));
        }

        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; }
        public float DistanceTravelled { get; private set; }
        public bool IsSpent => DistanceTravelled >= range;

        /// <summary>How much of its range the Shot has used up, from 0 at the muzzle to 1 at the end.</summary>
        public float RangeUsed => range > 0f ? Mathf.Clamp01(DistanceTravelled / range) : 1f;

        /// <summary>How far the Shot would move in <paramref name="deltaSeconds"/>, never past the end of its range.</summary>
        public float StepLength(float deltaSeconds)
        {
            return Mathf.Min(speed * deltaSeconds, Mathf.Max(0f, range - DistanceTravelled));
        }

        /// <summary>Moves the Shot <paramref name="distance"/> metres forward.</summary>
        public void Advance(float distance)
        {
            Position += Direction * distance;
            DistanceTravelled += distance;
            LeadIn = 0f;
        }

        /// <summary>Ends the flight early, for example when the Shot hits something.</summary>
        public void Stop(Vector3 at)
        {
            Position = at;
            DistanceTravelled = range;
            LeadIn = 0f;
        }
    }
}
