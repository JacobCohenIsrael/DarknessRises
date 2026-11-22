using System;
using DarknessRises.Game.Storage;

namespace DarknessRises.Game.Haptics
{
    /// <summary>
    /// The game's Haptics: decides whether an effect someone asks for actually plays, and plays it on the device. Plain
    /// C# (ADR 0003); real time comes in as an explicit delta. The boot owns the one the game uses and hands it to the
    /// Level and the Shop.
    /// </summary>
    /// <remarks>
    /// The rules, in order:
    /// - The VIBRATION switch off, a device that can't vibrate, or any hold (pause, ad, loading, background): nothing.
    /// - An effect waits for its own cooldown, and an ambient one for a quiet moment.
    /// - While a stronger effect plays, a weaker one is dropped.
    /// - An effect stronger than the last one cuts it (if it still plays) and skips the gap and the cap. Any other waits
    ///   for the minimum gap after the last effect, and for room under the cap of effects per second.
    /// Every waveform is worked out once, here; playing one allocates nothing.
    /// </remarks>
    public sealed class HapticsPlayer : IHaptics, IHapticsSwitch
    {
        /// <summary>The device setting that keeps the VIBRATION switch: 1 on, 0 off.</summary>
        public const string OnSetting = "haptics.on";

        private readonly HapticsDefinition definition;
        private readonly IHapticDevice device;
        private readonly IDeviceSettings settings;
        private readonly HapticPattern[] patterns;
        private readonly double[] cooldownUntil;
        private readonly double[] recentStarts;
        private readonly int steps;
        private int recentNext;
        private HapticsHold holds;
        private double playingUntil;
        private double lastStart = double.NegativeInfinity;
        private int lastPriority = int.MinValue;

        public HapticsPlayer(HapticsDefinition definition, IHapticDevice device, IDeviceSettings settings)
        {
            this.definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            this.device = device ?? throw new ArgumentNullException(nameof(device));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            var effects = HapticsDefinition.Effects;
            steps = definition.StrengthSteps;
            patterns = new HapticPattern[effects.Length];
            cooldownUntil = new double[effects.Length];
            var waveforms = new HapticWaveform[effects.Length * steps];
            for (var e = 0; e < effects.Length; e++)
            {
                var pattern = definition.PatternOf(effects[e]);
                patterns[e] = pattern;
                cooldownUntil[e] = double.NegativeInfinity;
                for (var s = 0; s < steps; s++)
                {
                    waveforms[e * steps + s] = HapticWaveform.Build(pattern, StrengthOfStep(s), definition.ShortestPulseMs);
                }
            }

            Waveforms = waveforms;
            recentStarts = new double[definition.MaxPerSecond];
            for (var i = 0; i < recentStarts.Length; i++)
            {
                recentStarts[i] = double.NegativeInfinity;
            }

            IsOn = settings.TryGetInt(OnSetting, out var on) ? on != 0 : definition.OnByDefault;
            if (device.CanVibrate)
            {
                device.Prepare(waveforms);
            }
        }

        /// <summary>Raised when an effect plays: the effect and the strength it played at.</summary>
        public event Action<HapticEffect, float> Played;

        /// <summary>Raised when an effect asked for doesn't play, and why.</summary>
        public event Action<HapticEffect, HapticDrop> Dropped;

        /// <summary>Every waveform the device was handed, by effect then strength step.</summary>
        public HapticWaveform[] Waveforms { get; }

        /// <summary>Seconds of real time since the game's Haptics started.</summary>
        public double Now { get; private set; }

        public bool IsAvailable => device.CanVibrate;

        public bool IsOn { get; private set; }

        /// <summary>The holds in force now.</summary>
        public HapticsHold Holds => holds;

        /// <summary>Whether an effect is still playing.</summary>
        public bool IsPlaying => Now < playingUntil;

        /// <summary>The definition the effects come from.</summary>
        public HapticsDefinition Definition => definition;

        /// <summary>Haptics that play nothing anywhere: a scene played on its own, without the boot.</summary>
        public static HapticsPlayer Silent(HapticsDefinition definition) =>
            new(definition, new SilentHapticDevice(false), new MemoryDeviceSettings());

        public void Tick(float realDeltaSeconds)
        {
            if (realDeltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(realDeltaSeconds), realDeltaSeconds, "Time can't run backwards.");
            }

            Now += realDeltaSeconds;
        }

        public void Play(HapticEffect effect) => Play(effect, 1f);

        public void Play(HapticEffect effect, float strength)
        {
            var index = (int)effect;
            if (index < 0 || index >= patterns.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(effect), effect, "There's no such effect.");
            }

            var drop = Check(effect, index);
            if (drop.HasValue)
            {
                Dropped?.Invoke(effect, drop.Value);
                return;
            }

            var pattern = patterns[index];
            var step = StepOf(strength);
            var waveform = Waveforms[index * steps + step];
            device.Play(index * steps + step);
            lastStart = Now;
            lastPriority = pattern.Priority;
            playingUntil = Now + waveform.TotalMs / 1000.0;
            cooldownUntil[index] = Now + pattern.CooldownSeconds;
            recentStarts[recentNext] = Now;
            recentNext = (recentNext + 1) % recentStarts.Length;
            Played?.Invoke(effect, waveform.Strength);
        }

        public void SetHeld(HapticsHold hold, bool held)
        {
            var before = holds;
            holds = held ? holds | hold : holds & ~hold;
            if (before == HapticsHold.None && holds != HapticsHold.None)
            {
                Stop();
            }
        }

        public void SetOn(bool on)
        {
            IsOn = on;
            settings.SetInt(OnSetting, on ? 1 : 0);
            settings.Flush();
            if (!on)
            {
                Stop();
            }
        }

        /// <summary>The strength (0 to 1) step <paramref name="step"/> is prepared at: the top step is full strength.</summary>
        public float StrengthOfStep(int step) => (step + 1) / (float)steps;

        /// <summary>The step <paramref name="strength"/> plays at: the nearest one at or above it.</summary>
        public int StepOf(float strength)
        {
            if (!(strength > 0f))
            {
                return 0;
            }

            return Math.Clamp((int)Math.Ceiling(strength * steps - 0.0001f) - 1, 0, steps - 1);
        }

        // Stops whatever plays, at once.
        private void Stop()
        {
            if (!IsPlaying)
            {
                return;
            }

            playingUntil = Now;
            device.Cancel();
        }

        private HapticDrop? Check(HapticEffect effect, int index)
        {
            if (!device.CanVibrate)
            {
                return HapticDrop.Unsupported;
            }

            if (!IsOn)
            {
                return HapticDrop.Off;
            }

            if (holds != HapticsHold.None)
            {
                return HapticDrop.Held;
            }

            var pattern = patterns[index];
            if (pattern.IsEmpty || (effect == HapticEffect.ButtonTap && !definition.TickOnButtons))
            {
                return HapticDrop.Unauthored;
            }

            if (Now < cooldownUntil[index])
            {
                return HapticDrop.Cooldown;
            }

            if (pattern.QuietBeforeSeconds > 0f && (IsPlaying || Now - lastStart < pattern.QuietBeforeSeconds))
            {
                return HapticDrop.NotQuiet;
            }

            if (IsPlaying && pattern.Priority < lastPriority)
            {
                return HapticDrop.Weaker;
            }

            if (pattern.Priority > lastPriority)
            {
                return null;
            }

            if (Now - lastStart < definition.MinGapSeconds)
            {
                return HapticDrop.TooSoon;
            }

            // The oldest start of the last few is the one to replace: if it's within a second, the cap is reached.
            return Now - recentStarts[recentNext] < 1.0 ? HapticDrop.OverCap : null;
        }
    }
}
