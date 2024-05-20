using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Vortice.DirectWrite;
using FontMetrics = Avalonia.Media.FontMetrics;
using FontSimulations = Avalonia.Media.FontSimulations;
using GlyphMetrics = Avalonia.Media.GlyphMetrics;

namespace Avalonia.Direct2D1.Media
{
    internal class GlyphTypefaceImpl : IGlyphTypeface
    {
        private bool _isDisposed;

        public GlyphTypefaceImpl(IDWriteFont font)
        {
            DWFont = font;

            FontFace = DWFont.CreateFontFace().QueryInterface<IDWriteFontFace1>();

            Face = new HarfBuzzSharp.Face(GetTable);

            Font = new HarfBuzzSharp.Font(Face);

            Font.SetFunctionsOpenType();

            Font.GetScale(out var xScale, out _);

            if (!Font.TryGetHorizontalFontExtents(out var fontExtents))
            {
                Font.TryGetVerticalFontExtents(out fontExtents);
            }

            Font.OpenTypeMetrics.TryGetPosition(HarfBuzzSharp.OpenTypeMetricsTag.UnderlineOffset, out var underlinePosition);
            Font.OpenTypeMetrics.TryGetPosition(HarfBuzzSharp.OpenTypeMetricsTag.UnderlineSize, out var underlineThickness);
            Font.OpenTypeMetrics.TryGetPosition(HarfBuzzSharp.OpenTypeMetricsTag.StrikeoutOffset, out var strikethroughPosition);
            Font.OpenTypeMetrics.TryGetPosition(HarfBuzzSharp.OpenTypeMetricsTag.StrikeoutSize, out var strikethroughThickness);

            Metrics = new FontMetrics
            {
                DesignEmHeight = (short)xScale,
                Ascent = -fontExtents.Ascender,
                Descent = -fontExtents.Descender,
                LineGap = fontExtents.LineGap,
                UnderlinePosition = underlinePosition,
                UnderlineThickness = underlineThickness,
                StrikethroughPosition = strikethroughPosition,
                StrikethroughThickness = strikethroughThickness,
                IsFixedPitch = FontFace.IsMonospacedFont
            };

            FamilyName = DWFont.FontFamily.FamilyNames.GetString(0);

            Weight = (Avalonia.Media.FontWeight)DWFont.Weight;

            Style = (Avalonia.Media.FontStyle)DWFont.Style;

            Stretch = (Avalonia.Media.FontStretch)DWFont.Stretch;
        }

        private HarfBuzzSharp.Blob GetTable(HarfBuzzSharp.Face face, HarfBuzzSharp.Tag tag)
        {
            var dwTag = (int)SwapBytes(tag);

            if (FontFace.TryGetFontTable(dwTag, out var tableData, out _))
            {
                unsafe
                {
                    return new HarfBuzzSharp.Blob((nint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(tableData)), tableData.Length, HarfBuzzSharp.MemoryMode.Duplicate, () => { });
                }
            }

            return null;
        }

        private static uint SwapBytes(uint x)
        {
            x = (x >> 16) | (x << 16);

            return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }

        public IDWriteFont DWFont { get; }

        public IDWriteFontFace1 FontFace { get; }

        public HarfBuzzSharp.Face Face { get; }

        public HarfBuzzSharp.Font Font { get; }

        public FontMetrics Metrics { get; }

        public int GlyphCount { get; set; }

        public FontSimulations FontSimulations => FontSimulations.None;

        public string FamilyName { get; }

        public Avalonia.Media.FontWeight Weight { get; }

        public Avalonia.Media.FontStyle Style { get; }

        public Avalonia.Media.FontStretch Stretch { get; }

        /// <inheritdoc cref="IGlyphTypeface"/>
        public ushort GetGlyph(uint codepoint)
        {
            if (Font.TryGetGlyph(codepoint, out var glyph))
            {
                return (ushort)glyph;
            }

            return 0;
        }

        public bool TryGetGlyph(uint codepoint, out ushort glyph)
        {
            glyph = GetGlyph(codepoint);

            return glyph != 0;
        }

        /// <inheritdoc cref="IGlyphTypeface"/>
        public ushort[] GetGlyphs(ReadOnlySpan<uint> codepoints)
        {
            var glyphs = new ushort[codepoints.Length];

            for (var i = 0; i < codepoints.Length; i++)
            {
                if (Font.TryGetGlyph(codepoints[i], out var glyph))
                {
                    glyphs[i] = (ushort)glyph;
                }
            }

            return glyphs;
        }

        /// <inheritdoc cref="IGlyphTypeface"/>
        public int GetGlyphAdvance(ushort glyph)
        {
            return Font.GetHorizontalGlyphAdvance(glyph);
        }

        /// <inheritdoc cref="IGlyphTypeface"/>
        public int[] GetGlyphAdvances(ReadOnlySpan<ushort> glyphs)
        {
            var glyphIndices = new uint[glyphs.Length];

            for (var i = 0; i < glyphs.Length; i++)
            {
                glyphIndices[i] = glyphs[i];
            }

            return Font.GetHorizontalGlyphAdvances(glyphIndices);
        }

        public bool TryGetGlyphMetrics(ushort glyph, out GlyphMetrics metrics)
        {
            metrics = default;

            if (!Font.TryGetGlyphExtents(glyph, out var extents))
            {
                return false;
            }

            metrics = new GlyphMetrics
            {
                XBearing = extents.XBearing,
                YBearing = extents.YBearing,
                Width = extents.Width,
                Height = extents.Height
            };

            return true;
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (!disposing)
            {
                return;
            }

            Font?.Dispose();
            Face?.Dispose();
            FontFace?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool TryGetTable(uint tag, out byte[] table)
        {
            table = null;
            var blob = Face.ReferenceTable(tag);

            if (blob.Length > 0)
            {
                table = blob.AsSpan().ToArray();

                return true;
            }

            return false;
        }
    }
}

