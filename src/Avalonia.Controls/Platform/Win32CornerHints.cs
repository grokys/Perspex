using System;

namespace Avalonia.Platform;

/// <summary>
/// Specifies hints for window corner appearance.
/// </summary>
[Flags]
public enum Win32WindowCornerHints
{
    /// <summary>
    /// Default Avalonia behavior.
    /// </summary>
    NoHint,

    /// <summary>
    /// The platform's default corner style.
    /// </summary>
    PlatformDefault,

    /// <summary>
    /// Rounded corners for the window.
    /// </summary>
    Rounded,

    /// <summary>
    /// Prevents corners from being rounded.
    /// </summary>
    NotRounded
}
