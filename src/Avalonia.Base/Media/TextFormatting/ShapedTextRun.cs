using System;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that holds shaped characters.
    /// </summary>
    public sealed class ShapedTextRun : DrawableTextRun, IDisposable
    {
        private GlyphRun? _glyphRun;

        public ShapedTextRun(ShapedBuffer shapedBuffer, TextRunProperties properties)
        {
            ShapedBuffer = shapedBuffer;
            Properties = properties;
            TextMetrics = new TextMetrics(properties.CachedGlyphTypeface, properties.FontRenderingEmSize);
        }

        public bool IsReversed { get; private set; }

        public sbyte BidiLevel => ShapedBuffer.BidiLevel;

        public ShapedBuffer ShapedBuffer { get; }

        /// <inheritdoc/>
        public override ReadOnlyMemory<char> Text
            => ShapedBuffer.Text;

        /// <inheritdoc/>
        public override TextRunProperties Properties { get; }

        /// <inheritdoc/>
        public override int Length
            => ShapedBuffer.Text.Length;

        public TextMetrics TextMetrics { get; }

        public override double Baseline => -TextMetrics.Ascent;

        public override Size Size => GlyphRun.Bounds.Size;

        public GlyphRun GlyphRun => _glyphRun ??= CreateGlyphRun();

        /// <inheritdoc/>
        public override void Draw(DrawingContext drawingContext, Point origin)
        {
            using (drawingContext.PushTransform(Matrix.CreateTranslation(origin)))
            {
                if (GlyphRun.GlyphInfos.Count == 0)
                {
                    return;
                }

                if (Properties.Typeface == default)
                {
                    return;
                }

                if (Properties.ForegroundBrush == null)
                {
                    return;
                }

                if (Properties.BackgroundBrush != null)
                {
                    drawingContext.DrawRectangle(Properties.BackgroundBrush, null, GlyphRun.Bounds);
                }

                drawingContext.DrawGlyphRun(Properties.ForegroundBrush, GlyphRun);

                if (Properties.TextDecorations == null)
                {
                    return;
                }

                foreach (var textDecoration in Properties.TextDecorations)
                {
                    textDecoration.Draw(drawingContext, GlyphRun, TextMetrics, Properties.ForegroundBrush);
                }
            }
        }

        internal void Reverse()
        {
            _glyphRun = null;

            ShapedBuffer.Reverse();

            IsReversed = !IsReversed;
        }

        /// <summary>
        /// Measures the number of characters that fit into available width.
        /// </summary>
        /// <param name="availableWidth">The available width.</param>
        /// <param name="length">The count of fitting characters.</param>
        /// <returns>
        /// <c>true</c> if characters fit into the available width; otherwise, <c>false</c>.
        /// </returns>
        public bool TryMeasureCharacters(double availableWidth, out int length)
        {
            length = 0;

            if (IsReversed)
            {
                var currentWidth = GlyphRun.Metrics.WidthIncludingTrailingWhitespace;
                var charactersSpan = GlyphRun.Characters.Span;

                for (var i = 0; i < ShapedBuffer.Length; i++)
                {
                    var advance = ShapedBuffer[i].GlyphAdvance;
                    var currentCluster = ShapedBuffer[i].GlyphCluster;

                    if (i + 1 < ShapedBuffer.Length)
                    {
                        var nextCluster = ShapedBuffer[i + 1].GlyphCluster;

                        //Higher cluster comes first
                        var count = currentCluster - nextCluster;

                        length += count;
                    }
                    else
                    {
                        Codepoint.ReadAt(charactersSpan, length, out var count);

                        length += count;
                    }

                    currentWidth -= advance;

                    if (currentWidth < availableWidth)
                    {
                        break;
                    }
                }

                length = ShapedBuffer.Length - length;
            }
            else
            {
                var currentWidth = 0.0;
                var charactersSpan = GlyphRun.Characters.Span;

                for (var i = 0; i < ShapedBuffer.Length; i++)
                {
                    var advance = ShapedBuffer[i].GlyphAdvance;
                    var currentCluster = ShapedBuffer[i].GlyphCluster;

                    if (currentWidth + advance > availableWidth)
                    {
                        break;
                    }

                    if (i + 1 < ShapedBuffer.Length)
                    {
                        var nextCluster = ShapedBuffer[i + 1].GlyphCluster;

                        var count = nextCluster - currentCluster;

                        length += count;
                    }
                    else
                    {
                        Codepoint.ReadAt(charactersSpan, length, out var count);

                        length += count;
                    }


                    currentWidth += advance;
                }
            }

            return length > 0;
        }

        internal bool TryMeasureCharactersBackwards(double availableWidth, out int length, out double width)
        {
            length = 0;
            width = 0;
            var charactersSpan = GlyphRun.Characters.Span;

            for (var i = ShapedBuffer.Length - 1; i >= 0; i--)
            {
                var advance = ShapedBuffer[i].GlyphAdvance;

                if (width + advance > availableWidth)
                {
                    break;
                }

                Codepoint.ReadAt(charactersSpan, length, out var count);

                length += count;
                width += advance;
            }

            return length > 0;
        }

        internal SplitResult<ShapedTextRun> Split(int length)
        {
            var isReversed = IsReversed;

            if (isReversed)
            {
                Reverse();

                length = Length - length;
            }
#if DEBUG
            if (length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "length must be greater than zero.");
            }
#endif          
            var splitBuffer = ShapedBuffer.Split(length);

            var first = new ShapedTextRun(splitBuffer.First, Properties);

#if DEBUG

            if (first.Length != length)
            {
                throw new InvalidOperationException("Split length mismatch.");
            }
#endif
            var second = new ShapedTextRun(splitBuffer.Second!, Properties);

            if (isReversed)
            {
                return new SplitResult<ShapedTextRun>(second, first);
            }

            return new SplitResult<ShapedTextRun>(first, second);
        }

        internal GlyphRun CreateGlyphRun()
        {
            return new GlyphRun(
                ShapedBuffer.GlyphTypeface,
                ShapedBuffer.FontRenderingEmSize,
                Text,
                ShapedBuffer,
                biDiLevel: BidiLevel);
        }

        public void Dispose()
        {
            _glyphRun?.Dispose();
            ShapedBuffer.Dispose();
        }
    }
}
