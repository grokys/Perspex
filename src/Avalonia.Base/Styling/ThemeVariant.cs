using System;
using System.ComponentModel;
using System.Text;
using Avalonia.Platform;

namespace Avalonia.Styling;

/// <summary>
/// Specifies a UI theme variant that should be used for the Control and Application types.
/// </summary>
[TypeConverter(typeof(ThemeVariantTypeConverter))]
[System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1010:AvaloniaProperty objects should be owned by the type in which they are stored",
    Justification = "ActualThemeVariant and RequestedThemeVariant properties are shared Avalonia.Base and Avalonia.Controls projects," +
    "but shouldn't be visible on the StyledElement class." +
    "Ideally we woould introduce readonly styled properties.")]
public sealed record ThemeVariant
{
    /// <summary>
    /// Defines the ActualThemeVariant property.
    /// </summary>
    internal static readonly StyledProperty<ThemeVariant> ActualThemeVariantProperty =
        AvaloniaProperty.Register<StyledElement, ThemeVariant>(
            "ActualThemeVariant",
            inherits: true);

    /// <summary>
    /// Defines the RequestedThemeVariant property.
    /// </summary>
    internal static readonly StyledProperty<ThemeVariant?> RequestedThemeVariantProperty =
        AvaloniaProperty.Register<StyledElement, ThemeVariant?>(
            "RequestedThemeVariant", defaultValue: Default);
    
    /// <summary>
    /// Creates a new instance of the <see cref="ThemeVariant"/>
    /// </summary>
    /// <param name="key">Key of the theme variant by which variants are compared.</param>
    /// <param name="inheritVariant">Reference to a theme variant which should be used, if resource wasn't found for the requested variant.</param>
    /// <exception cref="ArgumentException">Thrown if inheritVariant is a reference to the <see cref="ThemeVariant.Default"/> which is ambiguous value to inherit.</exception>
    /// <exception cref="ArgumentNullException">Thrown if key is null.</exception>
    public ThemeVariant(object key, ThemeVariant? inheritVariant)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        InheritVariant = inheritVariant;

        if (inheritVariant == Default)
        {
            throw new ArgumentException("Inheriting default theme variant is not supported.", nameof(inheritVariant));
        }
    }

    private ThemeVariant(object key)
    {
        Key = key;
    }
    
    /// <summary>
    /// Key of the theme variant by which variants are compared.
    /// </summary>
    public object Key { get; }
    
    /// <summary>
    /// Reference to a theme variant which should be used, if resource wasn't found for the requested variant.
    /// </summary>
    public ThemeVariant? InheritVariant { get; }

    /// <summary>
    /// Inherit theme variant from the parent. If set on Application, system theme is inherited.
    /// Using Default as the ResourceDictionary.Key marks this dictionary as a fallback in case the theme variant or resource key is not found in other theme dictionaries.
    /// </summary>
    public static ThemeVariant Default { get; } = new(nameof(Default));

    /// <summary>
    /// Use the Light theme variant.
    /// </summary>
    public static ThemeVariant Light { get; } = new(nameof(Light));

    /// <summary>
    /// Use the Dark theme variant.
    /// </summary>
    public static ThemeVariant Dark { get; } = new(nameof(Dark));

    public override string ToString()
    {
        return Key.ToString() ?? $"ThemeVariant {{ Key = {Key} }}";
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }

    public bool Equals(ThemeVariant? other)
    {
        return other is not null && Equals(Key, other.Key);
    }

    public static explicit operator ThemeVariant(PlatformThemeVariant themeVariant)
    {
        return themeVariant switch
        {
            PlatformThemeVariant.Light => Light,
            PlatformThemeVariant.Dark => Dark,
            _ => Default,
        };
    }

    public static explicit operator PlatformThemeVariant?(ThemeVariant? themeVariant)
    {
        if (themeVariant == Light)
        {
            return PlatformThemeVariant.Light;
        }
        else if (themeVariant == Dark)
        {
            return PlatformThemeVariant.Dark;
        }
        else if (themeVariant?.InheritVariant is { } inheritVariant)
        {
            return (PlatformThemeVariant?)inheritVariant;
        }

        return null;
    }
}
