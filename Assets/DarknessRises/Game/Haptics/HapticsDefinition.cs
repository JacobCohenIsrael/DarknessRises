using System;
using UnityEngine;

namespace DarknessRises.Game.Haptics
{
    /// <summary>
    /// Haptics, authored: how each <see cref="HapticEffect"/> feels, and the rules that keep them from turning into
    /// one long buzz. Read by the boot, which owns the game's <see cref="HapticsPlayer"/>. Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Haptics Definition", fileName = "HapticsDefinition")]
    public sealed class HapticsDefinition : ScriptableObject
    {
        [Header("Rules")]
        [SerializeField, Tooltip("Whether Haptics are on for a player who never touched the VIBRATION switch.")]
        private bool onByDefault = true;

        [SerializeField, Min(0f), Tooltip("Seconds between any two effects. A stronger effect than the last one skips it.")]
        private float minGapSeconds = 0.04f;

        [SerializeField, Min(1), Tooltip("At most this many effects start in any one second. A stronger effect than the last one skips the cap.")]
        private int maxPerSecond = 12;

        [SerializeField, Range(1, 8), Tooltip("How many strengths an effect that scales (being hurt) is prepared at. More is finer, and costs a little memory at start.")]
        private int strengthSteps = 4;

        [SerializeField, Min(1), Tooltip("Milliseconds of the shortest pulse a phone without amplitude control (and the Web) plays, when a weak pulse is shortened instead of softened.")]
        private int shortestPulseMs = 8;

        [SerializeField, Range(0f, 1f), Tooltip("How strong being hurt feels for one hit point of damage, before the next ones add to it.")]
        private float hurtBaseStrength = 0.6f;

        [SerializeField, Range(0f, 1f), Tooltip("How much stronger being hurt feels for each hit point of damage after the first.")]
        private float hurtStrengthPerHitPoint = 0.2f;

        [SerializeField, Tooltip("A very light tick on menu taps (PLAY, the Shop's tabs). Off: only the game's moments vibrate.")]
        private bool tickOnButtons;

        [Header("In a Level")]
        [SerializeField, Tooltip("A Grenade goes off: heavy and short, fading.")]
        private HapticPattern grenadeExplosion = new(75, 0.1f, 0f, HapticPredefined.None,
            new HapticPulse(0, 60, 1f), new HapticPulse(0, 110, 0.6f), new HapticPulse(0, 80, 0.25f));

        [SerializeField, Tooltip("A Shot hurts an Enemy: a light tick. Its cooldown keeps the Machine gun and the Flamethrower from buzzing.")]
        private HapticPattern enemyHit = HapticPattern.Pulse(10, 0.09f, 12, 0.35f, HapticPredefined.Tick);

        [SerializeField, Tooltip("An Enemy is killed: a little stronger than a hit.")]
        private HapticPattern enemyKilled = HapticPattern.Pulse(30, 0.06f, 28, 0.65f, HapticPredefined.Click);

        [SerializeField, Tooltip("The Colossus growls before a stomp: a faint, rising rumble as a warning.")]
        private HapticPattern bossWindUp = new(40, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 140, 0.06f), new HapticPulse(0, 140, 0.1f), new HapticPulse(0, 140, 0.15f), new HapticPulse(0, 140, 0.22f));

        [SerializeField, Tooltip("A Colossus foot lands, on the character or not: heavy and long.")]
        private HapticPattern bossStomp = new(85, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 120, 1f), new HapticPulse(0, 180, 0.7f), new HapticPulse(0, 200, 0.35f));

        [SerializeField, Tooltip("The Boss is beaten: a long, fading rumble.")]
        private HapticPattern bossDefeated = new(90, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 300, 1f), new HapticPulse(0, 400, 0.6f), new HapticPulse(0, 450, 0.3f), new HapticPulse(0, 450, 0.12f));

        [SerializeField, Tooltip("The character is hurt: strong, scaled by the damage (see the hurt strengths above).")]
        private HapticPattern hurt = new(70, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 90, 1f), new HapticPulse(0, 70, 0.45f));

        [SerializeField, Tooltip("The slow heartbeat on the last few hit points: a double pulse on each strong beat. The gap matches the slow loop's soft beat (0.31 s after the strong one).")]
        private HapticPattern heartbeatSlow = new(35, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 45, 0.55f), new HapticPulse(265, 35, 0.3f));

        [SerializeField, Tooltip("The fast heartbeat on the very last hit point. The gap matches the fast loop's soft beat (0.19 s after the strong one).")]
        private HapticPattern heartbeatFast = new(40, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 40, 0.75f), new HapticPulse(150, 30, 0.45f));

        [SerializeField, Tooltip("The character dies: a long, low buzz.")]
        private HapticPattern death = new(95, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 500, 0.7f), new HapticPulse(0, 500, 0.4f), new HapticPulse(0, 300, 0.2f));

        [SerializeField, Tooltip("The Combo steps up: a tiny tick.")]
        private HapticPattern comboUp = HapticPattern.Pulse(15, 0.1f, 10, 0.3f, HapticPredefined.Tick);

        [SerializeField, Tooltip("A reload is done: a crisp tick.")]
        private HapticPattern reloadDone = HapticPattern.Pulse(20, 0f, 18, 0.7f, HapticPredefined.Click);

        [SerializeField, Tooltip("A Weapon switch, on the switch beat's swap: a tick.")]
        private HapticPattern weaponSwitch = HapticPattern.Pulse(20, 0f, 15, 0.5f, HapticPredefined.Tick);

        [SerializeField, Tooltip("A Primary runs dry: a double tick.")]
        private HapticPattern ranDry = new(25, 0.3f, 0f, HapticPredefined.DoubleClick,
            new HapticPulse(0, 15, 0.6f), new HapticPulse(60, 15, 0.6f));

        [SerializeField, Tooltip("Flare: a soft swell.")]
        private HapticPattern flare = new(45, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 70, 0.1f), new HapticPulse(0, 70, 0.2f), new HapticPulse(0, 70, 0.32f), new HapticPulse(0, 70, 0.45f), new HapticPulse(0, 60, 0.2f));

        [SerializeField, Tooltip("Adrenaline: a heartbeat double pulse.")]
        private HapticPattern adrenaline = new(45, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 50, 0.85f), new HapticPulse(120, 40, 0.5f));

        [SerializeField, Tooltip("The Lure is thrown: a tick.")]
        private HapticPattern lureThrow = HapticPattern.Pulse(20, 0f, 12, 0.4f, HapticPredefined.Tick);

        [SerializeField, Tooltip("The Lure lands: a thud.")]
        private HapticPattern lureLand = HapticPattern.Pulse(30, 0f, 40, 0.75f, HapticPredefined.HeavyClick);

        [SerializeField, Tooltip("A looming Lightning strike, close by: a faint rumble. Rare and ambient: it waits for a quiet moment, so it never lands in a fight's haptics.")]
        private HapticPattern lightningNear = new(5, 2f, 1.5f, HapticPredefined.None,
            new HapticPulse(0, 220, 0.18f), new HapticPulse(0, 220, 0.08f));

        [SerializeField, Tooltip("A Station is cleared: a short, celebratory pattern.")]
        private HapticPattern stationClear = new(50, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 30, 0.6f), new HapticPulse(70, 30, 0.6f), new HapticPulse(70, 60, 0.9f));

        [SerializeField, Tooltip("A Station is cleared with Grade S: the same, a little longer.")]
        private HapticPattern stationClearS = new(55, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 30, 0.6f), new HapticPulse(70, 30, 0.6f), new HapticPulse(70, 30, 0.8f), new HapticPulse(70, 120, 1f));

        [SerializeField, Tooltip("The x2 Score boost starts: a success pattern.")]
        private HapticPattern scoreBoost = new(55, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 30, 0.6f), new HapticPulse(60, 90, 1f));

        [SerializeField, Tooltip("NEW BEST! stamps in on the end screen: a celebratory pattern.")]
        private HapticPattern newBest = new(60, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 40, 0.7f), new HapticPulse(80, 40, 0.7f), new HapticPulse(80, 40, 0.7f), new HapticPulse(80, 160, 1f));

        [SerializeField, Tooltip("The end screen's Credits total lands in the bank: two quick coins.")]
        private HapticPattern creditsLanded = new(45, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 20, 0.5f), new HapticPulse(45, 35, 0.85f));

        [Header("Menus and the Shop")]
        [SerializeField, Tooltip("An Upgrade is bought: a crisp success.")]
        private HapticPattern bought = new(50, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 20, 0.6f), new HapticPulse(50, 40, 1f));

        [SerializeField, Tooltip("A Weapon is unlocked: a bigger success.")]
        private HapticPattern weaponUnlocked = new(60, 0f, 0f, HapticPredefined.None,
            new HapticPulse(0, 30, 0.7f), new HapticPulse(60, 30, 0.7f), new HapticPulse(60, 130, 1f));

        [SerializeField, Tooltip("A tap on something the bank can't cover: a short double tap, \"no\".")]
        private HapticPattern cannotAfford = new(40, 0.25f, 0f, HapticPredefined.None,
            new HapticPulse(0, 25, 0.5f), new HapticPulse(70, 25, 0.5f));

        [SerializeField, Tooltip("A menu tap, when ticks on buttons are on: very light.")]
        private HapticPattern buttonTap = HapticPattern.Pulse(5, 0f, 8, 0.25f, HapticPredefined.Tick);

        /// <summary>Every effect there is.</summary>
        public static readonly HapticEffect[] Effects = (HapticEffect[])Enum.GetValues(typeof(HapticEffect));

        public bool OnByDefault => onByDefault;
        public float MinGapSeconds => Math.Max(0f, minGapSeconds);
        public int MaxPerSecond => Math.Max(1, maxPerSecond);
        public int StrengthSteps => Math.Clamp(strengthSteps, 1, 8);
        public int ShortestPulseMs => Math.Max(1, shortestPulseMs);
        public bool TickOnButtons => tickOnButtons;

        /// <summary>How strong being hurt by <paramref name="damage"/> hit points feels, 0 to 1.</summary>
        public float HurtStrength(int damage) =>
            damage <= 0 ? 0f : Math.Clamp(hurtBaseStrength + hurtStrengthPerHitPoint * (damage - 1), 0f, 1f);

        /// <summary>How <paramref name="effect"/> feels.</summary>
        public HapticPattern PatternOf(HapticEffect effect) => effect switch
        {
            HapticEffect.GrenadeExplosion => grenadeExplosion,
            HapticEffect.EnemyHit => enemyHit,
            HapticEffect.EnemyKilled => enemyKilled,
            HapticEffect.BossWindUp => bossWindUp,
            HapticEffect.BossStomp => bossStomp,
            HapticEffect.BossDefeated => bossDefeated,
            HapticEffect.Hurt => hurt,
            HapticEffect.HeartbeatSlow => heartbeatSlow,
            HapticEffect.HeartbeatFast => heartbeatFast,
            HapticEffect.Death => death,
            HapticEffect.ComboUp => comboUp,
            HapticEffect.ReloadDone => reloadDone,
            HapticEffect.WeaponSwitch => weaponSwitch,
            HapticEffect.RanDry => ranDry,
            HapticEffect.Flare => flare,
            HapticEffect.Adrenaline => adrenaline,
            HapticEffect.LureThrow => lureThrow,
            HapticEffect.LureLand => lureLand,
            HapticEffect.LightningNear => lightningNear,
            HapticEffect.StationClear => stationClear,
            HapticEffect.StationClearS => stationClearS,
            HapticEffect.ScoreBoost => scoreBoost,
            HapticEffect.NewBest => newBest,
            HapticEffect.CreditsLanded => creditsLanded,
            HapticEffect.Bought => bought,
            HapticEffect.WeaponUnlocked => weaponUnlocked,
            HapticEffect.CannotAfford => cannotAfford,
            HapticEffect.ButtonTap => buttonTap,
            _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, "No pattern is authored for this effect."),
        };
    }
}
