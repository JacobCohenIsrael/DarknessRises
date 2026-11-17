namespace DarknessRises.Game.Content
{
    /// <summary>Content that has already been loaded, looked up by its key without waiting.</summary>
    public interface ILoadedContent
    {
        /// <summary>The loaded asset at <paramref name="key"/>.</summary>
        /// <exception cref="System.InvalidOperationException">It isn't loaded.</exception>
        T Get<T>(object key) where T : class;
    }
}
