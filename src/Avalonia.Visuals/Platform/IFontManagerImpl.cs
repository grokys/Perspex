﻿// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Avalonia.Platform
{
    public interface IFontManagerImpl
    {
        /// <summary>
        ///     Gets the system's default font family's name.
        /// </summary>
        string GetDefaultFontFamilyName();

        /// <summary>
        ///     Get all installed fonts in the system.
        /// <param name="checkForUpdates">If <c>true</c> the font collection is updated.</param>
        /// </summary>
        IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false);

        /// <summary>
        ///     Tries to match a specified character to a typeface that supports specified font properties.
        /// </summary>
        /// <param name="codepoint">The codepoint to match against.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="fontStyle">The font style.</param>
        /// <param name="fontFamily">The font family. This is optional and used for fallback lookup.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>
        ///     The typeface.
        /// </returns>
        FontKey MatchCharacter(int codepoint, FontWeight fontWeight = default, FontStyle fontStyle = default,
            FontFamily fontFamily = null, CultureInfo culture = null);
    }
}
