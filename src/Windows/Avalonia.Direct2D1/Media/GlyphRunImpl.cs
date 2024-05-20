using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Vortice.DirectWrite;

#nullable enable

namespace Avalonia.Direct2D1.Media
{
    internal class GlyphRunImpl : IGlyphRunImpl
    {
        private readonly GlyphTypefaceImpl _glyphTypefaceImpl;

        private readonly ushort[] _glyphIndices;
        private readonly float[] _glyphAdvances;
        private readonly GlyphOffset[] _glyphOffsets;

        private Vortice.DirectWrite.GlyphRun? _glyphRun;

        public GlyphRunImpl(IGlyphTypeface glyphTypeface, double fontRenderingEmSize,
            IReadOnlyList<GlyphInfo> glyphInfos, Point baselineOrigin)
        {
            _glyphTypefaceImpl = (GlyphTypefaceImpl)glyphTypeface;

            FontRenderingEmSize = fontRenderingEmSize;
            BaselineOrigin = baselineOrigin;

            var glyphCount = glyphInfos.Count;

            _glyphIndices = new ushort[glyphCount];

            for (var i = 0; i < glyphCount; i++)
            {
                _glyphIndices[i] = glyphInfos[i].GlyphIndex;
            }

            _glyphAdvances = new float[glyphCount];

            var width = 0.0;

            for (var i = 0; i < glyphCount; i++)
            {
                var advance = glyphInfos[i].GlyphAdvance;

                width += advance;

                _glyphAdvances[i] = (float)advance;
            }

            _glyphOffsets = new GlyphOffset[glyphCount];

            for (var i = 0; i < glyphCount; i++)
            {
                var (x, y) = glyphInfos[i].GlyphOffset;

                _glyphOffsets[i] = new GlyphOffset
                {
                    AdvanceOffset = (float)x,
                    AscenderOffset = (float)y
                };
            }

            var scale = fontRenderingEmSize / glyphTypeface.Metrics.DesignEmHeight;

            var height = glyphTypeface.Metrics.LineSpacing * scale;

            Bounds = new Rect(baselineOrigin.X, 0, width, height);
        }

        public Vortice.DirectWrite.GlyphRun GlyphRun
        {
            get
            {
                if (_glyphRun != null)
                {
                    return _glyphRun;
                }

                _glyphRun = new Vortice.DirectWrite.GlyphRun
                {
                    FontFace = _glyphTypefaceImpl.FontFace,
                    FontEmSize = (float)FontRenderingEmSize,
                    Advances = _glyphAdvances,
                    Indices = _glyphIndices,
                    Offsets = _glyphOffsets
                };

                return _glyphRun;
            }
        }

        public IGlyphTypeface GlyphTypeface => _glyphTypefaceImpl;

        public double FontRenderingEmSize { get; }

        public Point BaselineOrigin { get; }

        public Rect Bounds { get; }

        public IReadOnlyList<float> GetIntersections(float lowerBound, float upperBound) => Array.Empty<float>();

        public void Dispose()
        {
            //_glyphRun?.Dispose();

            _glyphRun = null;
        }
    }
}
