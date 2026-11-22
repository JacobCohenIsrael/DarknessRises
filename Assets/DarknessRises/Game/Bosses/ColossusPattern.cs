using System;
using DarknessRises.Game.Randomness;

namespace DarknessRises.Game.Bosses
{
    /// <summary>
    /// The Colossus's attack pattern. It growls (its warning), asks Lightning to flash, and the flash shows the shadow of
    /// its raised foot over one Position. The player has the Relocate window to dash off it; then the foot lands,
    /// stomping the character if it is still on that Position, and a stomped character is knocked out from under it to
    /// the nearest other Position. The planted foot stays down for the damage window, when only its weak spot takes
    /// damage; then it lifts, rests, and growls again.
    /// <para>
    /// Phase 2 (below half health) adds feints: the flash shows two shadows, only one foot comes down, and a second,
    /// late flash reveals which. Shamblers drop from its back after each stomp. Phase 3 (the last hit points) breaks the
    /// rhythm: Lightning flashes twice in quick succession, one shadow each, and the two stomps chain.
    /// </para>
    /// <para>Flare reveals the next stomp's shadows early, before any Lightning, and opens Relocate at once.</para>
    /// </summary>
    /// <remarks>
    /// Plain C# (ADR 0003). Time comes in through <see cref="Tick"/> in Enemy time, so Adrenaline stretches every timing.
    /// The pattern, not a timer, decides when Lightning flashes: it raises <see cref="FlashRequested"/> for the
    /// Lightning scheduler's Boss mode.
    /// </remarks>
    public sealed class ColossusPattern
    {
        // Resting, growling, raised, falling, chaining, planted, lifting.
        private const int StepsPerStomp = 7;

        private readonly ColossusStompTiming timing;
        private readonly ColossusPhaseThresholds phases;
        private readonly ColossusPhase2Settings phase2;
        private readonly ColossusPhase3Settings phase3;
        private readonly float characterTargetChance;
        private readonly Relocation relocation;
        private readonly IRandom random;

        private float stepSecondsLeft;
        private float stepSeconds;

        // The stomp under way: its kind, whether its shadows are already shown, and the cues still to come in its Raised step.
        private StompKind kind;
        private bool targetsPicked;
        private bool revealedEarly;
        private float secondFlashAt;
        private float secondFlashLead;
        private bool secondFlashRequested;
        private bool secondCueDone;

        /// <param name="hitPoints">Its hit points in this fight, already scaled by the Ramp.</param>
        /// <param name="characterTargetChance">
        /// The chance, from 0 to 1, that a stomp aims at the Position the character stands on; otherwise it aims at another.
        /// </param>
        /// <param name="relocation">The character's Relocate, which the pattern opens for each Relocate window.</param>
        public ColossusPattern(
            ColossusStompTiming timing,
            ColossusPhaseThresholds phases,
            ColossusPhase2Settings phase2,
            ColossusPhase3Settings phase3,
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
            this.phase2 = phase2 ?? throw new ArgumentNullException(nameof(phase2));
            this.phase3 = phase3 ?? throw new ArgumentNullException(nameof(phase3));
            this.relocation = relocation ?? throw new ArgumentNullException(nameof(relocation));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.characterTargetChance = characterTargetChance;
            MaxHitPoints = hitPoints;
            HitPoints = hitPoints;
            Phase = 1;
            Target = -1;
            SecondTarget = -1;
        }

        private enum StompKind
        {
            Single,
            Feint,
            Chain,
        }

        /// <summary>Lightning should flash this many seconds from now; the argument is how long it growls first.</summary>
        public event Action<float> FlashRequested;

        /// <summary>A foot's shadow shows over this Position: by a flash, or early by Flare.</summary>
        public event Action<int> ShadowShown;

        /// <summary>A feint's shadow goes: the late flash showed this Position is safe.</summary>
        public event Action<int> ShadowHidden;

        /// <summary>The Relocate window closes; the foot comes down.</summary>
        public event Action RelocateWindowClosed;

        /// <summary>A foot lands on this Position. True if it stomped the character.</summary>
        public event Action<int, bool> Landed;

        /// <summary>The planted foot lifts; its weak spot goes dark.</summary>
        public event Action Lifted;

        /// <summary>This many Shamblers drop from its back. Whoever spawns them reports each with <see cref="AddSpawned"/>.</summary>
        public event Action<int> AddsDue;

        /// <summary>A hit on the weak spot landed.</summary>
        public event Action Hurt;

        /// <summary>A hit on the weak spot took hit points: the damage dealt, and the hit points left.</summary>
        public event Action<int, int> Damaged;

        /// <summary>The phase changed. Carries the new phase, from 1 to 3.</summary>
        public event Action<int> PhaseChanged;

        /// <summary>Raised once, when the last hit point goes.</summary>
        public event Action Defeated;

        public ColossusStep Step { get; private set; } = ColossusStep.Waiting;

        /// <summary>Seconds of Enemy time left in the current step.</summary>
        public float StepSecondsLeft => stepSecondsLeft;

        /// <summary>Seconds of Enemy time the current step lasts in all.</summary>
        public float StepSeconds => stepSeconds;

        /// <summary>The Position the current foot comes down on, once picked, or -1.</summary>
        public int Target { get; private set; }

        /// <summary>
        /// The other Position of the current stomp, or -1: a feint's false shadow, or in phase 3 where the second foot lands.
        /// </summary>
        public int SecondTarget { get; private set; }

        /// <summary>Whether the current stomp is a feint: two shadows, one foot.</summary>
        public bool IsFeint => kind == StompKind.Feint;

        /// <summary>Whether the current stomp is a phase 3 chain: two feet, one after the other.</summary>
        public bool IsChain => kind == StompKind.Chain;

        /// <summary>The Position whose shadow shows right now for <see cref="Target"/>, or -1.</summary>
        public int ShownShadow { get; private set; } = -1;

        /// <summary>The Position whose shadow shows right now for <see cref="SecondTarget"/>, or -1.</summary>
        public int ShownSecondShadow { get; private set; } = -1;

        public int HitPoints { get; private set; }

        public int MaxHitPoints { get; }

        /// <summary>The shares of its hit points where phases 2 and 3 start.</summary>
        public ColossusPhaseThresholds Phases => phases;

        /// <summary>From 1 to 3, by the share of hit points left.</summary>
        public int Phase { get; private set; }

        /// <summary>Adds alive right now, as reported by whoever spawns them.</summary>
        public int AddsAlive { get; private set; }

        public bool IsDefeated => Step == ColossusStep.Defeated;

        /// <summary>
        /// Whether the Relocate window is open: from the flash until the foot starts to fall, or from an early reveal by Flare.
        /// </summary>
        public bool IsRelocateWindowOpen =>
            Step == ColossusStep.Raised
            || (revealedEarly && (Step == ColossusStep.Resting || Step == ColossusStep.Growling));

        /// <summary>Whether the weak spot is lit and takes damage: only while the foot is planted.</summary>
        public bool IsWeakSpotExposed => Step == ColossusStep.Planted;

        /// <summary>Whether Flare can show the next stomp's shadows now: between stomps or during the growl, once per stomp.</summary>
        public bool CanRevealEarly => !revealedEarly && (Step == ColossusStep.Resting || Step == ColossusStep.Growling);

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
            for (var steps = 0; Step != ColossusStep.Defeated && steps < StepsPerStomp; steps++)
            {
                RunCues();
                if (stepSecondsLeft > 0f || Step == ColossusStep.Defeated)
                {
                    break;
                }

                var overshoot = -stepSecondsLeft;
                Advance();
                stepSecondsLeft -= overshoot;
            }
        }

        /// <summary>
        /// Flare: shows the next stomp's shadows now, without waiting for Lightning, and opens Relocate at once.
        /// Only between stomps or during the growl. A feint's false shadow never shows: Flare lights the real foot.
        /// </summary>
        /// <returns>Whether anything was revealed.</returns>
        public bool RevealEarly()
        {
            if (!CanRevealEarly)
            {
                return false;
            }

            if (Step == ColossusStep.Resting)
            {
                kind = ChooseKind();
            }

            PickTargets();
            revealedEarly = true;
            relocation.IsEnabled = true;
            ShowShadow(Target);
            if (IsChain)
            {
                ShowSecondShadow(SecondTarget);
            }

            return true;
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

            var dealt = Math.Min(damage, HitPoints);
            HitPoints -= dealt;
            Hurt?.Invoke();
            Damaged?.Invoke(dealt, HitPoints);

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

        /// <summary>An add dropped into the Room.</summary>
        public void AddSpawned()
        {
            AddsAlive++;
        }

        /// <summary>An add died.</summary>
        public void AddDied()
        {
            AddsAlive = Math.Max(0, AddsAlive - 1);
        }

        private void Advance()
        {
            switch (Step)
            {
                case ColossusStep.Resting:
                    if (!revealedEarly)
                    {
                        kind = ChooseKind();
                    }

                    Enter(ColossusStep.Growling, timing.GrowlSeconds);
                    FlashRequested?.Invoke(timing.GrowlSeconds);
                    break;

                case ColossusStep.Growling:
                    Flash();
                    break;

                case ColossusStep.Raised:
                    relocation.IsEnabled = false;
                    Enter(ColossusStep.Falling, FlashToImpactSeconds - RelocateWindowSeconds);
                    RelocateWindowClosed?.Invoke();
                    break;

                case ColossusStep.Falling:
                    if (IsChain)
                    {
                        Enter(ColossusStep.Chaining, phase3.ChainSeconds);
                        Land(Target, SecondTarget);
                    }
                    else
                    {
                        Plant(Target);
                    }

                    break;

                case ColossusStep.Chaining:
                    Target = SecondTarget;
                    SecondTarget = -1;
                    Plant(Target);
                    break;

                case ColossusStep.Planted:
                    Enter(ColossusStep.Lifting, timing.LiftSeconds);
                    Lifted?.Invoke();
                    break;

                case ColossusStep.Lifting:
                    EndStomp();
                    break;
            }
        }

        // The flash: the shadows show (unless Flare showed them already) and the Relocate window opens.
        private void Flash()
        {
            PickTargets();
            Enter(ColossusStep.Raised, RelocateWindowSeconds);
            relocation.IsEnabled = true;

            secondFlashRequested = true;
            secondCueDone = true;
            if (IsFeint && !revealedEarly)
            {
                // The late flash reveals which shadow was the real foot.
                secondFlashAt = Math.Min(phase2.RevealSeconds, RelocateWindowSeconds);
                secondFlashLead = Math.Min(phase2.RevealGrowlSeconds, secondFlashAt);
                secondFlashRequested = false;
                secondCueDone = false;
            }
            else if (IsChain)
            {
                // A second flash right after the first shows the second foot.
                secondFlashAt = Math.Min(phase3.DoubleFlashGapSeconds, RelocateWindowSeconds);
                secondFlashLead = 0f;
                secondFlashRequested = false;
                secondCueDone = revealedEarly;
            }

            if (!revealedEarly)
            {
                ShowShadow(Target);
                if (IsFeint)
                {
                    ShowSecondShadow(SecondTarget);
                }
            }
        }

        // The Raised step's second flash, timed from its start.
        private void RunCues()
        {
            if (Step != ColossusStep.Raised || (secondFlashRequested && secondCueDone))
            {
                return;
            }

            var elapsed = stepSeconds - stepSecondsLeft;
            if (!secondFlashRequested && elapsed >= secondFlashAt - secondFlashLead)
            {
                secondFlashRequested = true;
                var lead = secondFlashAt - Math.Max(0f, elapsed);
                FlashRequested?.Invoke(lead > 0f ? lead : 0f);
            }

            if (!secondCueDone && elapsed >= secondFlashAt)
            {
                secondCueDone = true;
                if (IsFeint)
                {
                    HideSecondShadow();
                }
                else if (IsChain)
                {
                    ShowSecondShadow(SecondTarget);
                }
            }
        }

        private void Plant(int position)
        {
            Enter(ColossusStep.Planted, IsChain ? phase3.PlantedSeconds : timing.PlantedSeconds);
            Land(position, -1);
        }

        private void Land(int position, int alsoAvoid)
        {
            // Stomped if the character never made it off: still on the Position, or still dashing away from it.
            var stomped = relocation.Position == position;
            if (position == Target)
            {
                ShownShadow = -1;
            }

            if (position == ShownSecondShadow)
            {
                ShownSecondShadow = -1;
            }

            // A stomped character is knocked out from under the foot, unless a late dash is already carrying it off.
            if (stomped && !relocation.IsDashing && relocation.Positions.Count > 1)
            {
                var knockTo = relocation.NearestOther(position, alsoAvoid);
                if (knockTo >= 0)
                {
                    relocation.MoveTo(knockTo);
                }
            }

            Landed?.Invoke(position, stomped);
        }

        private void EndStomp()
        {
            Target = -1;
            SecondTarget = -1;
            ShownShadow = -1;
            ShownSecondShadow = -1;
            targetsPicked = false;
            revealedEarly = false;
            var wasChain = IsChain;
            kind = StompKind.Single;
            Enter(ColossusStep.Resting, wasChain ? phase3.RestSeconds : timing.RestSeconds);

            // Between stomps in phase 2 and on, Shamblers drop from its back.
            if (Phase >= 2)
            {
                var room = Math.Max(0, phase2.MaxAddsAlive - AddsAlive);
                var count = Math.Min(phase2.AddsPerDrop, room);
                if (count > 0)
                {
                    AddsDue?.Invoke(count);
                }
            }
        }

        private void Enter(ColossusStep step, float seconds)
        {
            Step = step;
            stepSeconds = seconds;
            stepSecondsLeft = seconds;
        }

        private float RelocateWindowSeconds => timing.RelocateWindowSeconds + (IsFeint ? phase2.ExtraWindowSeconds : 0f);

        private float FlashToImpactSeconds => timing.FlashToImpactSeconds + (IsFeint ? phase2.ExtraWindowSeconds : 0f);

        private StompKind ChooseKind()
        {
            if (Phase >= 3 && relocation.Positions.Count > 2)
            {
                return StompKind.Chain;
            }

            if (Phase == 2 && relocation.Positions.Count > 1 && random.Range(0f, 1f) < phase2.FeintChance)
            {
                return StompKind.Feint;
            }

            return StompKind.Single;
        }

        private void PickTargets()
        {
            if (targetsPicked)
            {
                return;
            }

            targetsPicked = true;
            var standing = relocation.Position;
            Target = relocation.Positions.Count == 1 || random.Range(0f, 1f) < characterTargetChance
                ? standing
                : PickOther(standing, -1);

            // A feint's false shadow, or the second foot of a chain, threatens where the character stands when the
            // first one doesn't.
            SecondTarget = kind == StompKind.Single ? -1
                : Target != standing ? standing
                : PickOther(Target, -1);
        }

        // A random Position other than the given ones.
        private int PickOther(int excluded, int alsoExcluded)
        {
            var count = relocation.Positions.Count;
            var candidates = count - 1 - (alsoExcluded >= 0 && alsoExcluded != excluded ? 1 : 0);
            if (candidates <= 0)
            {
                return excluded;
            }

            var pick = Math.Min(candidates - 1, (int)Math.Floor(random.Range(0f, candidates)));
            for (var i = 0; i < count; i++)
            {
                if (i == excluded || i == alsoExcluded)
                {
                    continue;
                }

                if (pick == 0)
                {
                    return i;
                }

                pick--;
            }

            return excluded;
        }

        private void ShowShadow(int position)
        {
            ShownShadow = position;
            ShadowShown?.Invoke(position);
        }

        private void ShowSecondShadow(int position)
        {
            if (position < 0 || ShownSecondShadow == position)
            {
                return;
            }

            ShownSecondShadow = position;
            ShadowShown?.Invoke(position);
        }

        private void HideSecondShadow()
        {
            var hidden = ShownSecondShadow;
            ShownSecondShadow = -1;
            if (hidden >= 0)
            {
                ShadowHidden?.Invoke(hidden);
            }
        }
    }
}
