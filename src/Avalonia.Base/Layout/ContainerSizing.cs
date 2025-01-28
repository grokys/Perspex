namespace Avalonia.Layout
{
    /// <summary>
    /// Defines how a container is queried.
    /// </summary>
    public enum ContainerSizing
    {
        /// <summary>
        /// The container is not included in any size queries.
        /// </summary>
        Normal,

        /// <summary>
        /// The container size can be queried for width.
        /// </summary>
        Width,

        /// <summary>
        /// The container size can be queried for height.
        /// </summary>
        Height,

        /// <summary>
        /// The container size can be queried for width and height.
        /// </summary>
        WidthAndHeight
    }
}
