using System;

namespace DarknessRises.Game.Enemies
{
    /// <summary>
    /// The pitch an Enemy sound plays at: its own pitch (1, or a Tell's random pitch) slowed by the Enemy time scale,
    /// so Slow-mo slows Enemy sounds the way it slows Enemies. Applied per AudioSource, with no AudioMixer, so it
    /// works on every platform (WebGL has no AudioMixer).
    /// </summary>
    public static class EnemyVoicePitch
    {
        /// <summary>The pitch for a sound whose own pitch is <paramref name="basePitch"/>, at Enemy time <paramref name="scale"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException">A negative pitch or scale.</exception>
        public static float For(float basePitch, float scale)
        {
            if (basePitch < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(basePitch), basePitch, "Enemy sounds can't play backwards.");
            }

            if (scale < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(scale), scale, "Enemy time can't run backwards.");
            }

            return basePitch * scale;
        }
    }
}
