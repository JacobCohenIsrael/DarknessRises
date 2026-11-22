using System;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The switch beat: on a switch the Weapon in the hands goes down, the newly equipped one takes its place at the
    /// bottom of the beat and comes up. Plain C#: time arrives as an explicit delta (ADR 0003).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing fires while the old Weapon goes down: its muzzle is dropping out of line, and it's no longer the one
    /// equipped. The new Weapon may fire from the moment it shows, while it's still coming up, so a held trigger fires
    /// again after only the lowering half of the beat and a switch never feels sluggish.
    /// </para>
    /// <para>
    /// A switch while the old Weapon is still going down changes nothing: whichever Weapon is equipped at the bottom
    /// is the one that comes up. A switch while the new one is coming up sends it back down from where it got to.
    /// </para>
    /// </remarks>
    public sealed class WeaponSwitchBeat
    {
        private readonly float lowerSeconds;
        private readonly float raiseSeconds;

        /// <param name="lowerSeconds">Seconds the Weapon in the hands takes to go down. Nothing fires meanwhile.</param>
        /// <param name="raiseSeconds">Seconds the newly equipped Weapon takes to come up.</param>
        public WeaponSwitchBeat(float lowerSeconds, float raiseSeconds)
        {
            if (lowerSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(lowerSeconds), lowerSeconds, "A beat can't take negative time.");
            }

            if (raiseSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(raiseSeconds), raiseSeconds, "A beat can't take negative time.");
            }

            this.lowerSeconds = lowerSeconds;
            this.raiseSeconds = raiseSeconds;
        }

        /// <summary>Raised at the bottom of the beat: the newly equipped Weapon takes the old one's place and starts up.</summary>
        public event Action Swapped;

        /// <summary>Whether the Weapon in the hands is going down.</summary>
        public bool IsLowering { get; private set; }

        /// <summary>Whether the newly equipped Weapon is coming up.</summary>
        public bool IsRaising { get; private set; }

        /// <summary>Whether a beat is under way.</summary>
        public bool IsSwitching => IsLowering || IsRaising;

        /// <summary>Whether the equipped Weapon may fire: always, except while the old one goes down.</summary>
        public bool MayFire => !IsLowering;

        /// <summary>How far down the Weapon in the hands is: 0 held up, 1 at the bottom of the beat.</summary>
        public float Lowered { get; private set; }

        /// <summary>Starts a beat: a different Primary was just equipped.</summary>
        public void Start()
        {
            if (IsLowering)
            {
                return;
            }

            IsRaising = false;
            IsLowering = true;
            if (lowerSeconds <= 0f)
            {
                ReachBottom();
            }
        }

        /// <summary>Cuts a beat short: the equipped Weapon is up and ready at once, as when a Level starts.</summary>
        public void Finish()
        {
            IsLowering = false;
            IsRaising = false;
            Lowered = 0f;
        }

        /// <summary>Advances the beat by <paramref name="deltaSeconds"/>.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time only moves forward.");
            }

            if (IsLowering)
            {
                Lowered += deltaSeconds / lowerSeconds;
                if (Lowered < 1f)
                {
                    return;
                }

                // What's left of the frame goes to the new Weapon coming up.
                var spareSeconds = (Lowered - 1f) * lowerSeconds;
                ReachBottom();
                deltaSeconds = spareSeconds;
            }

            if (!IsRaising)
            {
                return;
            }

            Lowered = raiseSeconds > 0f ? Lowered - deltaSeconds / raiseSeconds : 0f;
            if (Lowered <= 0f)
            {
                Lowered = 0f;
                IsRaising = false;
            }
        }

        private void ReachBottom()
        {
            Lowered = 1f;
            IsLowering = false;
            IsRaising = true;
            Swapped?.Invoke();
            if (raiseSeconds <= 0f)
            {
                Lowered = 0f;
                IsRaising = false;
            }
        }
    }
}
