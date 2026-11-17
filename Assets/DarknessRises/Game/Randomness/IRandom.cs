namespace DarknessRises.Game.Randomness
{
    /// <summary>
    /// A source of random numbers for rule objects, so tests can drive them with known values.
    /// </summary>
    public interface IRandom
    {
        /// <summary>A value between <paramref name="min"/> and <paramref name="max"/>, both inclusive.</summary>
        float Range(float min, float max);
    }
}
