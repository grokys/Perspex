using System.Collections.Concurrent;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Vortice.DirectWrite;
using FontFamily = Avalonia.Media.FontFamily;
using FontStyle = Vortice.DirectWrite.FontStyle;
using FontWeight = Vortice.DirectWrite.FontWeight;
using FontStretch = Vortice.DirectWrite.FontStretch;
using Avalonia.Platform;
using System.Linq;
using System;

namespace Avalonia.Direct2D1.Media
{
    internal static class Direct2D1FontCollectionCache
    {
        private static readonly ConcurrentDictionary<FontFamilyKey, IDWriteFontCollection> s_cachedCollections;
        internal static readonly IDWriteFontCollection InstalledFontCollection;

        static Direct2D1FontCollectionCache()
        {
            s_cachedCollections = new ConcurrentDictionary<FontFamilyKey, IDWriteFontCollection>();

            InstalledFontCollection = Direct2D1Platform.DirectWriteFactory.GetSystemFontCollection(false);
        }

        public static IDWriteFont GetFont(Typeface typeface)
        {
            var fontFamily = typeface.FontFamily;
            var fontCollection = GetOrAddFontCollection(fontFamily);
            int index;

            foreach (var name in fontFamily.FamilyNames)
            {
                if (fontCollection.FindFamilyName(name, out index))
                {
                    return fontCollection.GetFontFamily(index).GetFirstMatchingFont(
                        (FontWeight)typeface.Weight,
                        (FontStretch)typeface.Stretch,
                        (FontStyle)typeface.Style);
                }
            }

            InstalledFontCollection.FindFamilyName("Segoe UI", out index);

            return InstalledFontCollection.GetFontFamily(index).GetFirstMatchingFont(
                (FontWeight)typeface.Weight,
                (FontStretch)typeface.Stretch,
                (FontStyle)typeface.Style);
        }

        private static IDWriteFontCollection GetOrAddFontCollection(FontFamily fontFamily)
        {
            return fontFamily.Key == null ? InstalledFontCollection : s_cachedCollections.GetOrAdd(fontFamily.Key, CreateFontCollection);
        }

        private static IDWriteFontCollection CreateFontCollection(FontFamilyKey key)
        {
            var source = key.BaseUri != null ? new Uri(key.BaseUri, key.Source) : key.Source;

            var assets = FontFamilyLoader.LoadFontAssets(source);

            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

            var fontAssets = assets.Select(x => assetLoader.Open(x)).ToArray();

            var fontLoader = new DWriteResourceFontLoader(Direct2D1Platform.DirectWriteFactory, fontAssets);

            return Direct2D1Platform.DirectWriteFactory.CreateCustomFontCollection(fontLoader, fontLoader.Key.PositionPointer, (int)fontLoader.Key.RemainingLength);
        }
    }
}
