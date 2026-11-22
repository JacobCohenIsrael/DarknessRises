#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

namespace DarknessRises.Game.Haptics
{
    /// <summary>
    /// The phone's own vibrator (Android 8, API 26, and later: the game's minimum), through <c>Vibrator</c> and
    /// <c>VibrationEffect</c>. Every waveform becomes a <c>VibrationEffect</c> once, when it's prepared: a one-shot with
    /// an amplitude for a single pulse, a waveform with amplitudes for a pattern, the phone's predefined click (API 29+)
    /// where one is authored, and duration-only timings where the motor has no amplitude control. Playing one is a
    /// single JNI call with a method ID and arguments made in advance: nothing is looked up or allocated per effect.
    /// The <c>VIBRATE</c> permission this needs is in the manifest because the game references
    /// <see cref="Handheld.Vibrate"/> (<see cref="HandheldHapticDevice"/>): that is how Unity adds it.
    /// </summary>
    public sealed class AndroidHapticDevice : IHapticDevice
    {
        private const int OneShotDefaultAmplitude = -1;
        private const int PredefinedSdk = 29;
        private const int VibratorManagerSdk = 31;

        private readonly AndroidJavaObject vibrator;
        private readonly IntPtr vibrateMethod;
        private readonly IntPtr cancelMethod;
        private readonly int sdk;
        private readonly bool hasAmplitudeControl;
        private readonly jvalue[] noArguments = Array.Empty<jvalue>();
        private readonly List<AndroidJavaObject> effects = new();
        private jvalue[][] playArguments = Array.Empty<jvalue[]>();

        private AndroidHapticDevice(AndroidJavaObject vibrator, int sdk)
        {
            this.vibrator = vibrator;
            this.sdk = sdk;
            hasAmplitudeControl = vibrator.Call<bool>("hasAmplitudeControl");
            var vibratorClass = vibrator.GetRawClass();
            vibrateMethod = AndroidJNI.GetMethodID(vibratorClass, "vibrate", "(Landroid/os/VibrationEffect;)V");
            cancelMethod = AndroidJNI.GetMethodID(vibratorClass, "cancel", "()V");
        }

        public bool CanVibrate => true;

        /// <summary>The system vibrator, or null when the phone has none or it can't be reached.</summary>
        public static AndroidHapticDevice TryCreate()
        {
            try
            {
                int sdk;
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    sdk = version.GetStatic<int>("SDK_INT");
                }

                var context = AndroidApplication.currentContext;
                AndroidJavaObject vibrator;
                if (sdk >= VibratorManagerSdk)
                {
                    using var manager = context.Call<AndroidJavaObject>("getSystemService", "vibrator_manager");
                    vibrator = manager?.Call<AndroidJavaObject>("getDefaultVibrator");
                }
                else
                {
                    vibrator = context.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
                {
                    vibrator?.Dispose();
                    Log($"[Haptics] Android API {sdk}: no vibrator.");
                    return null;
                }

                var device = new AndroidHapticDevice(vibrator, sdk);
                Log($"[Haptics] Android API {sdk}: vibrator found, amplitude control {(device.hasAmplitudeControl ? "yes" : "no")}.");
                return device;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Haptics] The Android vibrator can't be reached: {exception.Message}");
                return null;
            }
        }

        public void Prepare(IReadOnlyList<HapticWaveform> waveforms)
        {
            DisposeEffects();
            playArguments = new jvalue[waveforms.Count][];
            var predefinedCount = 0;
            using var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
            for (var i = 0; i < waveforms.Count; i++)
            {
                var effect = Create(effectClass, waveforms[i], ref predefinedCount);
                effects.Add(effect);
                playArguments[i] = effect == null ? null : new[] { new jvalue { l = effect.GetRawObject() } };
            }

            Log($"[Haptics] Android: {effects.Count} VibrationEffects cached ({predefinedCount} predefined), vibrate method {vibrateMethod}.");
        }

        public void Play(int index)
        {
            var arguments = (uint)index < (uint)playArguments.Length ? playArguments[index] : null;
            if (arguments == null)
            {
                return;
            }

            AndroidJNI.CallVoidMethod(vibrator.GetRawObject(), vibrateMethod, arguments);
            ClearException();
        }

        public void Cancel()
        {
            AndroidJNI.CallVoidMethod(vibrator.GetRawObject(), cancelMethod, noArguments);
            ClearException();
        }

        private AndroidJavaObject Create(AndroidJavaClass effectClass, HapticWaveform waveform, ref int predefinedCount)
        {
            if (waveform.IsEmpty)
            {
                return null;
            }

            if (waveform.Predefined != HapticPredefined.None && sdk >= PredefinedSdk)
            {
                predefinedCount++;
                return effectClass.CallStatic<AndroidJavaObject>("createPredefined", (int)waveform.Predefined);
            }

            if (!hasAmplitudeControl)
            {
                var timings = waveform.DurationOnlyTimings;
                return timings.Length == 2 && timings[0] == 0
                    ? effectClass.CallStatic<AndroidJavaObject>("createOneShot", timings[1], OneShotDefaultAmplitude)
                    : effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, -1);
            }

            return waveform.IsOneShot
                ? effectClass.CallStatic<AndroidJavaObject>("createOneShot", waveform.Timings[1], waveform.Amplitudes[1])
                : effectClass.CallStatic<AndroidJavaObject>("createWaveform", waveform.Timings, waveform.Amplitudes, -1);
        }

        private void DisposeEffects()
        {
            foreach (var effect in effects)
            {
                effect?.Dispose();
            }

            effects.Clear();
        }

        // A Java exception left pending would break the next JNI call: a vibration that failed is only a vibration.
        private static void ClearException()
        {
            var exception = AndroidJNI.ExceptionOccurred();
            if (exception != IntPtr.Zero)
            {
                AndroidJNI.ExceptionClear();
                AndroidJNI.DeleteLocalRef(exception);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void Log(string line) => Debug.Log(line);
    }
}
#endif
