﻿// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using SharpDX.DirectWrite;
using FontFamily = Avalonia.Media.FontFamily;
using FontStyle = Avalonia.Media.FontStyle;
using FontWeight = Avalonia.Media.FontWeight;

namespace Avalonia.Direct2D1.Media
{
    internal class FontManagerImpl : IFontManagerImpl
    {
        public string GetDefaultFontFamilyName()
        {
            //ToDo: Implement a real lookup of the system's default font.
            return "segoe ui";
        }

        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            var familyCount = Direct2D1FontCollectionCache.InstalledFontCollection.FontFamilyCount;

            var fontFamilies = new string[familyCount];

            for (var i = 0; i < familyCount; i++)
            {
                fontFamilies[i] = Direct2D1FontCollectionCache.InstalledFontCollection.GetFontFamily(i).FamilyNames.GetString(0);
            }

            return fontFamilies;
        }

        public FontKey MatchCharacter(int codepoint, FontWeight fontWeight = default, FontStyle fontStyle = default,
            FontFamily fontFamily = null, CultureInfo culture = null)
        {
            var fontFamilyName = FontFamily.Default.Name;

            var familyCount = Direct2D1FontCollectionCache.InstalledFontCollection.FontFamilyCount;

            for (var i = 0; i < familyCount; i++)
            {
                var font = Direct2D1FontCollectionCache.InstalledFontCollection.GetFontFamily(i)
                    .GetMatchingFonts((SharpDX.DirectWrite.FontWeight)fontWeight, FontStretch.Normal,
                        (SharpDX.DirectWrite.FontStyle)fontStyle).GetFont(0);

                if (!font.HasCharacter(codepoint))
                {
                    continue;
                }

                fontFamilyName = font.FontFamily.FamilyNames.GetString(0);

                break;
            }

            return new FontKey(new FontFamily(fontFamilyName), fontWeight, fontStyle);
        }
    }
}
