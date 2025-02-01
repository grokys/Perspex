namespace Avalonia.Media.Imaging
{
    // TODO12 split the enum into two: composite mode and blend mode. (And rename Blending to Blend at the same time).
    /// <summary>
    /// Controls the way the bitmaps are drawn together.
    /// </summary>
    public enum BitmapBlendingMode : byte
    {
        Unspecified,

        /// <summary>
        /// Source is placed over the destination.
        /// </summary>
        SourceOver,
        /// <summary>
        /// Only the source will be present.
        /// </summary>
        Source,
        /// <summary>
        /// Only the destination will be present.
        /// </summary>
        Destination,
        /// <summary>
        /// Destination is placed over the source.
        /// </summary>
        DestinationOver,
        /// <summary>
        /// The source that overlaps the destination, replaces the destination.
        /// </summary>
        SourceIn,
        /// <summary>
        /// Destination which overlaps the source, replaces the source.
        /// </summary>
        DestinationIn,
        /// <summary>
        /// Source is placed, where it falls outside of the destination.
        /// </summary>
        SourceOut,
        /// <summary>
        /// Destination is placed, where it falls outside of the source.
        /// </summary>
        DestinationOut,
        /// <summary>
        /// Source which overlaps the destination, replaces the destination.
        /// </summary>
        SourceAtop,
        /// <summary>
        /// Destination which overlaps the source replaces the source.
        /// </summary>
        DestinationAtop,
        /// <summary>
        /// The non-overlapping regions of source and destination are combined.
        /// </summary>
        Xor,
        /// <summary>
        /// Display the sum of the source image and destination image.
        /// </summary>
        Plus,
        /// <summary>
        /// Multiplies the complements of the backdrop and source color values, then complements the result.
        /// </summary>
        Screen,
        /// <summary>
        /// Multiplies or screens the colors, depending on the backdrop color value.
        /// </summary>
        Overlay,
        /// <summary>
        /// Selects the darker of the backdrop and source colors.
        /// </summary>
        Darken,
        /// <summary>
        /// Selects the lighter of the backdrop and source colors.
        /// </summary>
        Lighten,
        /// <summary>
        /// Darkens the backdrop color to reflect the source color.
        /// </summary>
        ColorDodge,
        /// <summary>
        /// Multiplies or screens the colors, depending on the source color value.
        /// </summary>
        ColorBurn,
        /// <summary>
        /// Darkens or lightens the colors, depending on the source color value.
        /// </summary>
        HardLight,
        /// <summary>
        /// Subtracts the darker of the two constituent colors from the lighter color.
        /// </summary>
        SoftLight,
        /// <summary>
        /// Produces an effect similar to that of the Difference mode but lower in contrast.
        /// </summary>
        Difference,
        /// <summary>
        /// The source color is multiplied by the destination color and replaces the destination
        /// </summary>
        Exclusion,
        /// <summary>
        /// Creates a color with the hue of the source color and the saturation and luminosity of the backdrop color.
        /// </summary>
        Multiply,
        /// <summary>
        /// Creates a color with the hue of the source color and the saturation and luminosity of the backdrop color.
        /// </summary>
        Hue,
        /// <summary>
        /// Creates a color with the saturation of the source color and the hue and luminosity of the backdrop color.
        /// </summary>
        Saturation,
        /// <summary>
        /// Creates a color with the hue and saturation of the source color and the luminosity of the backdrop color.
        /// </summary>
        Color,
        /// <summary>
        /// Creates a color with the luminosity of the source color and the hue and saturation of the backdrop color.
        /// </summary>
        Luminosity
    }
}
