using System;

// ReSharper disable once CheckNamespace
namespace Avalonia.Media;

public static class EffectExtensions
{
    static double AdjustPaddingRadius(double radius)
    {
        if (radius <= 0)
            return 0;
        return Math.Ceiling(radius) + 1;
    }
    internal static Thickness GetEffectOutputPadding(this IEffect? effect)
    {
        if (effect == null)
            return default;
        if (effect is IBlurEffect blur)
            return new Thickness(AdjustPaddingRadius(blur.Radius));
        if (effect is IDropShadowEffect dropShadowEffect)
        {
            var radius = AdjustPaddingRadius(dropShadowEffect.BlurRadius);
            var rc = new Rect(-radius, -radius,
                radius * 2, radius * 2);
            rc = rc.Translate(new(dropShadowEffect.OffsetX, dropShadowEffect.OffsetY));
            return new Thickness(Math.Max(0, 0 - rc.X),
                Math.Max(0, 0 - rc.Y), Math.Max(0, rc.Right), Math.Max(0, rc.Bottom));
        }
        if (effect is IShaderEffect)
        {
            // Shader effect should not have padding.
            return default;
        }

        throw new ArgumentException("Unknown effect type: " + effect.GetType());
    }

    /// <summary>
    /// Converts a effect to an immutable effect.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <returns>
    /// The result of calling <see cref="IMutableEffect.ToImmutable"/> if the effect is mutable,
    /// otherwise <paramref name="effect"/>.
    /// </returns>
    public static IImmutableEffect ToImmutable(this IEffect effect)
    {
        _ = effect ?? throw new ArgumentNullException(nameof(effect));

        return (effect as IMutableEffect)?.ToImmutable() ?? (IImmutableEffect)effect;
    }

    internal static bool EffectEquals(this IImmutableEffect? immutable, IEffect? right)
    {
        if (immutable == null && right == null)
            return true;
        if (immutable != null && right != null)
            return immutable.Equals(right);
        return false;
    }
}
