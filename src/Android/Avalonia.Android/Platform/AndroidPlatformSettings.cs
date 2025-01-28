using System;
using Android.Content;
using Android.Content.Res;
using Android.Provider;
using Avalonia.Platform;
using Color = Avalonia.Media.Color;

namespace Avalonia.Android.Platform;

// TODO: ideally should be created per view/activity.
internal class AndroidPlatformSettings : DefaultPlatformSettings
{
    private PlatformColorValues _latestValues;

    public AndroidPlatformSettings()
    {
        _latestValues = base.GetColorValues();
    }

    public override PlatformColorValues GetColorValues()
    {
        return _latestValues;
    }

    internal void OnViewConfigurationChanged(Context context, Configuration configuration)
    {
        if (context.Resources is null)
        {
            return;
        }

        PlatformThemeVariant systemTheme = (configuration.UiMode & UiMode.NightMask) switch
        {
            UiMode.NightYes => PlatformThemeVariant.Dark,
            UiMode.NightNo => PlatformThemeVariant.Light,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            // See https://developer.android.com/reference/android/R.color
            var accent1 = context.Resources.GetColor(17170494, context.Theme); // Resource.Color.SystemAccent1500
            var accent2 = context.Resources.GetColor(17170507, context.Theme); // Resource.Color.SystemAccent2500
            var accent3 = context.Resources.GetColor(17170520, context.Theme); // Resource.Color.SystemAccent3500

            _latestValues = new PlatformColorValues
            {
                ThemeVariant = systemTheme,
                ContrastPreference = IsHighContrast(context),
                AccentColor1 = new Color(accent1.A, accent1.R, accent1.G, accent1.B),
                AccentColor2 = new Color(accent2.A, accent2.R, accent2.G, accent2.B),
                AccentColor3 = new Color(accent3.A, accent3.R, accent3.G, accent3.B),
            };
        }
        else if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            // See https://developer.android.com/reference/android/R.attr
            var array = context.Theme?.ObtainStyledAttributes(new[] { 16843829 }); // Resource.Attribute.ColorAccent
            if (array is not null)
            {
                try
                {
                    var accent = array.GetColor(0, 0);

                    _latestValues = new PlatformColorValues
                    {
                        ThemeVariant = systemTheme,
                        ContrastPreference = IsHighContrast(context),
                        AccentColor1 = new Color(accent.A, accent.R, accent.G, accent.B)
                    };
                }
                finally
                {
                    array.Recycle();
                }
            }
        }
        else
        {
            _latestValues = _latestValues with { ThemeVariant = systemTheme };
        }

        OnColorValuesChanged(_latestValues);
    }

    private static ColorContrastPreference IsHighContrast(Context context)
    {
        try
        {
            return Settings.Secure.GetInt(context.ContentResolver, "high_text_contrast_enabled", 0) == 1
                ? ColorContrastPreference.High : ColorContrastPreference.NoPreference;
        }
        catch
        {
            return ColorContrastPreference.NoPreference;
        }
    }
}
