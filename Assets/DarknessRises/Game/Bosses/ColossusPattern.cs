using System;
using DarknessRises.Game.Randomness;

namespace DarknessRises.Game.Bosses
{
    /// <summary>
    /// The Colossus's attack pattern, phase 1: it growls (the Rumble), asks Lightning to flash, and the flash shows the
    /// shadow of its raised foot over one Position. The player has the Relocate window to dash off it; then the foot
    /// lands, stomping the character if it is still on that Position. The planted foot stays down for the damage
    /// window, when only its weak spot takes damage; then it lifts, rests, and growls again. A stomped character is
    /// knocked out from under the foot to the nearest other Position.
    /// </summary>
    /// <remarks>
    /// Plain C# (ADR 0003). Time comes in through <see cref="Tick"/> in Enemy time, so Slow-mo stretches every timing.
    /// The pattern, not a timer, decides when Lightning flashes: it raises <see cref="FlashRequested"/> as it growls,
    /// for the Lightning scheduler's Boss mode. Phases 2 and 3 (#15) build on <see cref="Phase"/>.
    /// </remarks>
    public sealed class ColossusPattern
    {
        // Resting, growling, raised, falling, planted, lifting.
        private const int StepsPerStomp = 6;

        private readonly ColossusStompTiming timing;
        private readonly ColossusPhaseThresholds phases;
        private readonly float characterTargetChance;
        private readonly Relocation relocation;
        private readonly IRandom random;
        private float stepSecondsLeft;

        /// <param name="hitPoints">Its hit points in this fight, already scaled by the Ramp.</param>
        /// <param name="characterTargetChance">
        /// The chance, from 0 to 1, that a stomp aims at the Position the character stands on; otherwise it aims at another.
        /// </param>
        /// <param name="relocation">The character's Relocate, which the pattern opens for each Relocate window.</param>
        public ColossusPattern(
            ColossusStompTiming timing,
            ColossusPhaseThresholds phases,
            float characterTargetChance,
            int hitPoints,
            Relocation relocation,
            IRandom random)
        {
            if (hitPoints < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(hitPoints), hitPoints, "The Colossus needs at least one hit point.");
            }

            this.timing = timing ?? throw new ArgumentNullException(nameof(timing));
            this.phases = phases ?? throw new ArgumentNullException(nameof(phases));
            this.relocation = relocation ?? throw new ArgumentNullException(nameof(relocation));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.characterTargetChance = characterTargetChance;
            MaxHitPoints = hitPoints;
            HitPoints = hitPoints;
            Phase = 1;
            Target = -1;
        }

        /// <summary>The growl starts. Lightning should flash this many seconds from now; the argument is the Rumble lead.</summary>
        public event Action<float> FlashRequested;

        /// <summary>The flash: the raised foot's shadow shows over this Position, and the Relocate window opens.</summary>
        public event Action<int> ShadowShown;

        /// <summary>The Relocate window closes; the foot comes down.</summary>
        public event Action RelocateWindowClosed;

        /// <summary>The foot lands on this Position. True if it stomped the character.</summary>
        public event Action<int, bool> Landed;

        /// <summary>The planted foot lifts; its weak spot goes dark.</summary>
        public event Action Lifted;

        /// <summary>A hit on the weak spot landed.</summary>
        public event Action Hurt;

        /// <summary>The phase changed. Carries the new phase, from 1 to 3.</summary>
        public event Action<int> PhaseChanged;

        /// <summary>Raised once, when the last hit point goes.</summary>
        public event Action Defeated;

        public ColossusStep Step { get; private set; } = ColossusStep.Waiting;

        /// <summary>Seconds of Enemy time left in the current step.</summary>
        public float StepSecondsLeft => stepSecondsLeft;

        /// <summary>The Position the current stomp aims at, from the flash to the lift, or -1.</summary>
        public int Target { get; private set; }

        public int HitPoints { get; private set; }

        public int MaxHitPoints { get; }

        /// <summary>From 1 to 3, by the share of hit points left.</summary>
        public int Phase { get; private set; }

        public bool IsDefeated => Step == ColossusStep.Defeated;

        /// <summary>Whether the Relocate window is open: from the flash until the foot starts to fall.</summary>
        public bool IsRelocateWindowOpen => Step == ColossusStep.Raised;

        /// <summary>Whether the weak spot is lit and takes damage: only while the foot is planted.</summary>
        public bool IsWeakSpotExposed => Step == ColossusStep.Planted;

        /// <summary>Starts the fight: the first growl comes after the authored delay.</summary>
        public void Begin()
        {
            if (Step != ColossusStep.Waiting)
            {
                return;
            }

            Enter(ColossusStep.Resting, timing.FirstStompDelaySeconds);
        }

        /// <param name="deltaSeconds">Seconds of Enemy time since the last tick.</param>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (Step == ColossusStep.Waiting || Step == ColossusStep.Defeated)
            {
                return;
            }

            stepSecondsLeft -= deltaSeconds;

            // A long frame can run through several steps; each starts where the last one ended.
            // At most one whole stomp per tick, so steps authored at zero seconds can't spin forever.
            for (var steps = 0; stepSecondsLeft <= 0f && Step != ColossusStep.Defeated && steps < StepsPerStomp; steps++)
            {
                var overshoot = -stepSecondsLeft;
                Advance();
                stepSecondsLeft -= overshoot;
            }
        }

        /// <summary>A Shot hit the weak spot. It only counts while the foot is planted.</summary>
        /// <returns>Whether the hit landed.</returns>
        public bool TakeWeakSpotHit(int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage can't be negative.");
            }

            if (damage == 0 || !IsWeakSpotExposed)
            {
                return false;
            }

            HitPoints = Math.Max(0, HitPoints - damage);
            Hurt?.Invoke();

            var phase = phases.PhaseAt(HitPoints, MaxHitPoints);
            if (phase != Phase && HitPoints > 0)
            {
                Phase = phase;
                PhaseChanged?.Invoke(phase);
            }

            if (HitPoints == 0)
            {
                Step = ColossusStep.Defeated;
                stepSecondsLeft = 0f;
                relocation.IsEnabled = false;
                Defeated?.Invoke();
            }

            return true;
        }

        private void Advance()
        {
            switch (Step)
            {
                case ColossusStep.Resting:
                    Enter(ColossusStep.Growling, timing.GrowlSeconds);
                    FlashRequested?.Invoke(timing.GrowlSeconds);
                    break;

                case ColossusStep.Growling:
                    Target = PickTarget();
                    Enter(ColossusStep.Raised, timing.RelocateWindowSeconds);
                    relocation.IsEnabled = true;
                    ShadowShown?.Invoke(Target);
                    break;

                case ColossusStep.Raised:
                    relocation.IsEnabled = false;
                    Enter(ColossusStep.Falling, timing.FlashToImpactSeconds - timing.RelocateWindowSeconds);
                    RelocateWindowClosed?.Invoke();
                    break;

                case ColossusStep.Falling:
                    // Stomped if the character never made it off: still on the target, or still dashing away from it.
                    var stomped = relocation.Position == Target;
                    Enter(ColossusStep.Planted, timing.PlantedSeconds);

                    // A stomped character is knocked out from under the foot, unless a late dash is already carrying it off.
                    if (stomped && !relocation.IsDashing && relocation.Positions.Count > 1)
                    {
                        relocation.MoveTo(relocation.NearestOther(Target));
                    }

                    Landed?.Invoke(Target, stomped);
                    break;

                case ColossusStep.Planted:
                    Enter(ColossusStep.Lifting, timing.LiftSeconds);
                    Lifted?.Invoke();
                    break;

                case ColossusStep.Lifting:
                    Target = -1;
                    Enter(ColossusStep.Resting, timing.RestSeconds);
                    break;
            }
        }

        private void Enter(ColossusStep step, float seconds)
        {
            Step = step;
            stepSecondsLeft = seconds;
        }

        // Mostly where the character stands, so it has to move; otherwise one of the other Positions.
        private int PickTarget()
        {
            var count = relocation.Positions.Count;
            var standing = relocation.Position;
            if (count == 1 || random.Range(0f, 1f) < characterTargetChance)
            {
                return standing;
            }

            var others = count - 1;
            var pick = Math.Min(others - 1, (int)Math.Floor(random.Range(0f, others)));
            return pick >= standing ? pick + 1 : pick;
        }
    }
}
