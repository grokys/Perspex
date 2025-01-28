using System;
using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a multi line text layout.
    /// </summary>
    public class TextLayout : IDisposable
    {
        private readonly ITextSource _textSource;
        private readonly TextParagraphProperties _paragraphProperties;
        private readonly TextTrimming _textTrimming;
        private readonly TextLine[] _textLines;
        private readonly CachedMetrics _metrics = new();

        private int _textSourceLength;

        // TODO12: Remove in 12.0.0 and make fontFeatures parameter in main ctor optional
        /// <summary>
        /// Initializes a new instance of the <see cref="TextLayout" /> class.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="typeface">The typeface.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="foreground">The foreground.</param>
        /// <param name="textAlignment">The text alignment.</param>
        /// <param name="textWrapping">The text wrapping.</param>
        /// <param name="textTrimming">The text trimming.</param>
        /// <param name="textDecorations">The text decorations.</param>
        /// <param name="flowDirection">The text flow direction.</param>
        /// <param name="maxWidth">The maximum width.</param>
        /// <param name="maxHeight">The maximum height.</param>
        /// <param name="lineHeight">The height of each line of text.</param>
        /// <param name="letterSpacing">The letter spacing that is applied to rendered glyphs.</param>
        /// <param name="maxLines">The maximum number of text lines.</param>
        /// <param name="textStyleOverrides">The text style overrides.</param>
        public TextLayout(
            string? text,
            Typeface typeface,
            double fontSize,
            IBrush? foreground,
            TextAlignment textAlignment = TextAlignment.Left,
            TextWrapping textWrapping = TextWrapping.NoWrap,
            TextTrimming? textTrimming = null,
            TextDecorationCollection? textDecorations = null,
            FlowDirection flowDirection = FlowDirection.LeftToRight,
            double maxWidth = double.PositiveInfinity,
            double maxHeight = double.PositiveInfinity,
            double lineHeight = double.NaN,
            double letterSpacing = 0,
            int maxLines = 0,
            IReadOnlyList<ValueSpan<TextRunProperties>>? textStyleOverrides = null)
            : this(text, typeface, null, fontSize, foreground, textAlignment, textWrapping, textTrimming, textDecorations, 
            flowDirection, maxWidth, maxHeight, lineHeight, letterSpacing, maxLines, textStyleOverrides)
        {
        }
        
        // TODO12:Change signature in 12.0.0
        /// <summary>
        /// Initializes a new instance of the <see cref="TextLayout" /> class.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="typeface">The typeface.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="foreground">The foreground.</param>
        /// <param name="textAlignment">The text alignment.</param>
        /// <param name="textWrapping">The text wrapping.</param>
        /// <param name="textTrimming">The text trimming.</param>
        /// <param name="textDecorations">The text decorations.</param>
        /// <param name="flowDirection">The text flow direction.</param>
        /// <param name="maxWidth">The maximum width.</param>
        /// <param name="maxHeight">The maximum height.</param>
        /// <param name="lineHeight">The height of each line of text.</param>
        /// <param name="letterSpacing">The letter spacing that is applied to rendered glyphs.</param>
        /// <param name="maxLines">The maximum number of text lines.</param>
        /// <param name="textStyleOverrides">The text style overrides.</param>
        /// <param name="fontFeatures">Optional list of turned on/off features.</param>
        public TextLayout(
            string? text,
            Typeface typeface,
            FontFeatureCollection? fontFeatures,
            double fontSize,
            IBrush? foreground,
            TextAlignment textAlignment = TextAlignment.Left,
            TextWrapping textWrapping = TextWrapping.NoWrap,
            TextTrimming? textTrimming = null,
            TextDecorationCollection? textDecorations = null,
            FlowDirection flowDirection = FlowDirection.LeftToRight,
            double maxWidth = double.PositiveInfinity,
            double maxHeight = double.PositiveInfinity,
            double lineHeight = double.NaN,
            double letterSpacing = 0,
            int maxLines = 0,
            IReadOnlyList<ValueSpan<TextRunProperties>>? textStyleOverrides = null)
        {
            _paragraphProperties =
                CreateTextParagraphProperties(typeface, fontSize, foreground, textAlignment, textWrapping,
                    textDecorations, flowDirection, lineHeight, letterSpacing, fontFeatures);

            _textSource = new FormattedTextSource(text ?? "", _paragraphProperties.DefaultTextRunProperties, textStyleOverrides);

            _textTrimming = textTrimming ?? TextTrimming.None;

            MaxWidth = maxWidth;

            MaxHeight = maxHeight;

            MaxLines = maxLines;

            _textLines = CreateTextLines();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextLayout" /> class.
        /// </summary>
        /// <param name="textSource">The text source.</param>
        /// <param name="paragraphProperties">The default text paragraph properties.</param>
        /// <param name="textTrimming">The text trimming.</param>
        /// <param name="maxWidth">The maximum width.</param>
        /// <param name="maxHeight">The maximum height.</param>
        /// <param name="maxLines">The maximum number of text lines.</param>
        public TextLayout(
            ITextSource textSource,
            TextParagraphProperties paragraphProperties,
            TextTrimming? textTrimming = null,
            double maxWidth = double.PositiveInfinity,
            double maxHeight = double.PositiveInfinity,
            int maxLines = 0)
        {
            _textSource = textSource;

            _paragraphProperties = paragraphProperties;

            _textTrimming = textTrimming ?? TextTrimming.None;

            MaxWidth = maxWidth;

            MaxHeight = maxHeight;

            MaxLines = maxLines;

            _textLines = CreateTextLines();
        }

        /// <summary>
        /// Gets or sets the height of each line of text.
        /// </summary>
        /// <remarks>
        /// A value of NaN (equivalent to an attribute value of "Auto") indicates that the line height
        /// is determined automatically from the current font characteristics. The default is NaN.
        /// </remarks>
        public double LineHeight => _paragraphProperties.LineHeight;

        /// <summary>
        /// Gets the maximum width.
        /// </summary>
        public double MaxWidth { get; }

        /// <summary>
        /// Gets the maximum height.
        /// </summary>
        public double MaxHeight { get; }

        /// <summary>
        /// Gets the maximum number of text lines.
        /// </summary>
        public int MaxLines { get; }

        /// <summary>
        /// Gets the text spacing.
        /// </summary>
        public double LetterSpacing => _paragraphProperties.LetterSpacing;

        /// <summary>
        /// Gets the text lines.
        /// </summary>
        /// <value>
        /// The text lines.
        /// </value>
        public IReadOnlyList<TextLine> TextLines
            => _textLines;

        /// <summary>
        /// The distance from the top of the first line to the bottom of the last line.
        /// </summary>
        public double Height
        {
            get
            {
                return _metrics.Height;
            }
        }

        /// <summary>
        /// The distance from the topmost black pixel of the first line
        /// to the bottommost black pixel of the last line. 
        /// </summary>
        public double Extent
        {
            get
            {
                return _metrics.Extent;
            }
        }

        /// <summary>
        /// The distance from the top of the first line to the baseline of the first line.
        /// </summary>
        public double Baseline
        {
            get
            {
                return _metrics.Baseline;
            }
        }

        /// <summary>
        /// The distance from the bottom of the last line to the extent bottom.
        /// </summary>
        public double OverhangAfter
        {
            get
            {
                return _metrics.OverhangAfter;
            }
        }

        /// <summary>
        /// The maximum distance from the leading black pixel to the leading alignment point of a line.
        /// </summary>
        public double OverhangLeading
        {
            get
            {
                return _metrics.OverhangLeading;
            }
        }

        /// <summary>
        /// The maximum distance from the trailing black pixel to the trailing alignment point of a line.
        /// </summary>
        public double OverhangTrailing
        {
            get
            {
                return _metrics.OverhangTrailing;
            }
        }

        /// <summary>
        /// The maximum advance width between the leading and trailing alignment points of a line,
        /// excluding the width of whitespace characters at the end of the line.
        /// </summary>
        public double Width
        {
            get
            {
                return _metrics.Width;
            }
        }

        /// <summary>
        /// The maximum advance width between the leading and trailing alignment points of a line,
        /// including the width of whitespace characters at the end of the line.
        /// </summary>
        public double WidthIncludingTrailingWhitespace
        {
            get
            {
                return _metrics.WidthIncludingTrailingWhitespace;
            }
        }

        /// <summary>
        /// Get minimum width of all text lines that can be layouted horizontally without trimming or wrapping.
        /// </summary>
        internal double MinTextWidth => _metrics.MinTextWidth;

        /// <summary>
        /// Draws the text layout.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="origin">The origin.</param>
        public void Draw(DrawingContext context, Point origin)
        {
            if (_textLines.Length == 0)
            {
                return;
            }

            var (currentX, currentY) = origin;

            foreach (var textLine in _textLines)
            {
                textLine.Draw(context, new Point(currentX, currentY));

                currentY += textLine.Height;
            }
        }

        /// <summary>
        /// Get the pixel location relative to the top-left of the layout box given the text position.
        /// </summary>
        /// <param name="textPosition">The text position.</param>
        /// <returns></returns>
        public Rect HitTestTextPosition(int textPosition)
        {
            if (_textLines.Length == 0)
            {
                return new Rect();
            }

            if (textPosition < 0)
            {
                textPosition = _textSourceLength;
            }

            var currentY = 0.0;

            for (var i = 0; i < _textLines.Length; i++)
            {
                var textLine = _textLines[i];

                var end = textLine.FirstTextSourceIndex + textLine.Length;

                if (end <= textPosition && i + 1 < _textLines.Length)
                {
                    currentY += textLine.Height;

                    continue;
                }

                var characterHit = new CharacterHit(textPosition);

                var startX = textLine.GetDistanceFromCharacterHit(characterHit);

                var nextCharacterHit = textLine.GetNextCaretCharacterHit(characterHit);

                var endX = textLine.GetDistanceFromCharacterHit(nextCharacterHit);

                return new Rect(startX, currentY, endX - startX, textLine.Height);
            }

            return new Rect();
        }

        public IEnumerable<Rect> HitTestTextRange(int start, int length)
        {
            if (start + length <= 0)
            {
                return Array.Empty<Rect>();
            }

            var result = new List<Rect>(_textLines.Length);

            var currentY = 0d;

            foreach (var textLine in _textLines)
            {
                //Current line isn't covered.
                if (textLine.FirstTextSourceIndex + textLine.Length <= start)
                {
                    currentY += textLine.Height;

                    continue;
                }

                var textBounds = textLine.GetTextBounds(start, length);

                if (textBounds.Count > 0)
                {
                    foreach (var bounds in textBounds)
                    {
                        Rect? last = result.Count > 0 ? result[result.Count - 1] : null;

                        if (last.HasValue && MathUtilities.AreClose(last.Value.Right, bounds.Rectangle.Left) && MathUtilities.AreClose(last.Value.Top, currentY))
                        {
                            result[result.Count - 1] = last.Value.WithWidth(last.Value.Width + bounds.Rectangle.Width);
                        }
                        else
                        {
                            result.Add(bounds.Rectangle.WithY(currentY));
                        }

                        foreach (var runBounds in bounds.TextRunBounds)
                        {
                            start += runBounds.Length;
                            length -= runBounds.Length;
                        }
                    }
                }

                if (textLine.FirstTextSourceIndex + textLine.Length >= start + length)
                {
                    break;
                }

                currentY += textLine.Height;
            }

            return result;
        }

        public TextHitTestResult HitTestPoint(in Point point)
        {
            var currentY = 0d;

            TextLine? currentLine = null;
            CharacterHit characterHit;

            for (var lineIndex = 0; lineIndex < _textLines.Length; lineIndex++)
            {
                currentLine = _textLines[lineIndex];

                if (currentY + currentLine.Height > point.Y)
                {
                    characterHit = currentLine.GetCharacterHitFromDistance(point.X);

                    return GetHitTestResult(currentLine, characterHit, point);
                }

                currentY += currentLine.Height;
            }

            if (currentLine is null)
            {
                return new TextHitTestResult();
            }

            characterHit = currentLine.GetCharacterHitFromDistance(point.X);

            return GetHitTestResult(currentLine, characterHit, point);
        }


        public int GetLineIndexFromCharacterIndex(int charIndex, bool trailingEdge)
        {
            if (charIndex < 0)
            {
                return 0;
            }

            if (charIndex > _textSourceLength)
            {
                return _textLines.Length - 1;
            }

            for (var index = 0; index < _textLines.Length; index++)
            {
                var textLine = _textLines[index];

                if (textLine.FirstTextSourceIndex + textLine.Length < charIndex)
                {
                    continue;
                }

                if (charIndex >= textLine.FirstTextSourceIndex &&
                    charIndex <= textLine.FirstTextSourceIndex + textLine.Length - (trailingEdge ? 0 : 1))
                {
                    return index;
                }
            }

            return _textLines.Length - 1;
        }

        private TextHitTestResult GetHitTestResult(TextLine textLine, CharacterHit characterHit, Point point)
        {
            var (x, y) = point;

            var isInside = x >= 0 && x <= textLine.Width && y >= 0 && y <= textLine.Height;

            var lastTrailingIndex = 0;

            if (_paragraphProperties.FlowDirection == FlowDirection.LeftToRight)
            {
                lastTrailingIndex = textLine.FirstTextSourceIndex + textLine.Length;

                if (x >= textLine.Width && textLine.Length > 0 && textLine.NewLineLength > 0)
                {
                    lastTrailingIndex -= textLine.NewLineLength;
                }

                if (textLine.TextLineBreak?.TextEndOfLine is TextEndOfLine textEndOfLine)
                {
                    lastTrailingIndex -= textEndOfLine.Length;
                }
            }
            else
            {
                if (x <= textLine.WidthIncludingTrailingWhitespace - textLine.Width && textLine.Length > 0 && textLine.NewLineLength > 0)
                {
                    lastTrailingIndex += textLine.NewLineLength;
                }

                if (textLine.TextLineBreak?.TextEndOfLine is TextEndOfLine textEndOfLine)
                {
                    lastTrailingIndex += textEndOfLine.Length;
                }
            }

            var textPosition = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            var isTrailing = lastTrailingIndex == textPosition && characterHit.TrailingLength > 0 ||
                             y > Height;

            if (textPosition == textLine.FirstTextSourceIndex + textLine.Length)
            {
                textPosition -= textLine.NewLineLength;
            }

            if (textLine.NewLineLength > 0 && textPosition + textLine.NewLineLength ==
                characterHit.FirstCharacterIndex + characterHit.TrailingLength)
            {
                characterHit = new CharacterHit(characterHit.FirstCharacterIndex);
            }

            return new TextHitTestResult(characterHit, textPosition, isInside, isTrailing);
        }

        /// <summary>
        /// Creates the default <see cref="TextParagraphProperties"/> that are used by the <see cref="TextFormatter"/>.
        /// </summary>
        /// <param name="typeface">The typeface.</param>
        /// <param name="fontSize">The font size.</param>
        /// <param name="foreground">The foreground.</param>
        /// <param name="textAlignment">The text alignment.</param>
        /// <param name="textWrapping">The text wrapping.</param>
        /// <param name="textDecorations">The text decorations.</param>
        /// <param name="flowDirection">The text flow direction.</param>
        /// <param name="lineHeight">The height of each line of text.</param>
        /// <param name="letterSpacing">The letter spacing that is applied to rendered glyphs.</param>
        /// <param name="features">Optional list of turned on/off features.</param>
        /// <returns></returns>
        internal static TextParagraphProperties CreateTextParagraphProperties(Typeface typeface, double fontSize,
            IBrush? foreground, TextAlignment textAlignment, TextWrapping textWrapping,
            TextDecorationCollection? textDecorations, FlowDirection flowDirection, double lineHeight,
            double letterSpacing, FontFeatureCollection? features)
        {
            var textRunStyle = new GenericTextRunProperties(typeface, features, fontSize, textDecorations, foreground);

            return new GenericTextParagraphProperties(flowDirection, textAlignment, true, false,
                textRunStyle, textWrapping, lineHeight, 0, letterSpacing);
        }

        private TextLine[] CreateTextLines()
        {
            var objectPool = FormattingObjectPool.Instance;

            var first = true;

            if (MathUtilities.IsZero(MaxWidth) || MathUtilities.IsZero(MaxHeight))
            {
                var textLine = TextFormatterImpl.CreateEmptyTextLine(0, double.PositiveInfinity, _paragraphProperties);

                UpdateMetrics(textLine, ref first);

                return new TextLine[] { textLine };
            }

            var textLines = objectPool.TextLines.Rent();

            try
            {
                _textSourceLength = 0;

                TextLineImpl? previousLine = null;

                var textFormatter = TextFormatter.Current;

                while (true)
                {
                    var textLine = textFormatter.FormatLine(_textSource, _textSourceLength, MaxWidth,
                        _paragraphProperties, previousLine?.TextLineBreak) as TextLineImpl;

                    if (textLine is null)
                    {
                        if (previousLine != null && previousLine.NewLineLength > 0)
                        {
                            var emptyTextLine = TextFormatterImpl.CreateEmptyTextLine(_textSourceLength, MaxWidth,
                                _paragraphProperties);

                            textLines.Add(emptyTextLine);

                            UpdateMetrics(emptyTextLine, ref first);
                        }

                        break;
                    }

                    _textSourceLength += textLine.Length;

                    //Fulfill max height constraint
                    if (textLines.Count > 0 && !double.IsPositiveInfinity(MaxHeight)
                        && Height + textLine.Height > MaxHeight)
                    {
                        if (previousLine?.TextLineBreak != null && _textTrimming != TextTrimming.None)
                        {
                            var collapsedLine =
                                previousLine.Collapse(GetCollapsingProperties(MaxWidth, previousLine._resolvedFlowDirection));

                            textLines[textLines.Count - 1] = collapsedLine;
                        }

                        break;
                    }

                    var hasOverflowed = textLine.HasOverflowed;

                    if (hasOverflowed && _textTrimming != TextTrimming.None)
                    {
                        textLine = (TextLineImpl)textLine.Collapse(GetCollapsingProperties(MaxWidth, textLine._resolvedFlowDirection));
                    }

                    textLines.Add(textLine);

                    UpdateMetrics(textLine, ref first);

                    previousLine = textLine;

                    //Fulfill max lines constraint
                    if (MaxLines > 0 && textLines.Count >= MaxLines)
                    {
                        if (textLine.TextLineBreak is { IsSplit: true })
                        {
                            textLines[textLines.Count - 1] = textLine.Collapse(GetCollapsingProperties(WidthIncludingTrailingWhitespace, textLine._resolvedFlowDirection));
                        }

                        break;
                    }

                    if (textLine.TextLineBreak?.TextEndOfLine is TextEndOfParagraph)
                    {
                        break;
                    }
                }

                if (textLines.Count == 0)
                {
                    var textLine = TextFormatterImpl.CreateEmptyTextLine(0, MaxWidth, _paragraphProperties);

                    textLines.Add(textLine);

                    UpdateMetrics(textLine, ref first);
                }

                if (_paragraphProperties.TextAlignment == TextAlignment.Justify)
                {
                    var justificationWidth = MaxWidth;

                    if (_paragraphProperties.TextWrapping != TextWrapping.NoWrap)
                    {
                        justificationWidth = WidthIncludingTrailingWhitespace;
                    }

                    if (justificationWidth > 0)
                    {
                        var justificationProperties = new InterWordJustification(justificationWidth);

                        for (var i = 0; i < textLines.Count; i++)
                        {
                            var line = textLines[i];

                            line.Justify(justificationProperties);
                        }
                    }
                }

                return textLines.ToArray();
            }
            finally
            {
                objectPool.TextLines.Return(ref textLines);
                objectPool.VerifyAllReturned();
            }
        }

        private void UpdateMetrics(TextLineImpl currentLine, ref bool first)
        {
            _metrics.InkBounds = _metrics.InkBounds.Union(new Rect(new Point(0, _metrics.Bounds.Bottom) + currentLine.InkBounds.Position, currentLine.InkBounds.Size));
            _metrics.Bounds = _metrics.Bounds.Union(new Rect(new Point(0, _metrics.Bounds.Bottom) + currentLine.Bounds.Position, currentLine.Bounds.Size));

            _metrics.MinTextWidth = Math.Max(_metrics.MinTextWidth, currentLine.Bounds.Width);
            _metrics.MinTextWidth = Math.Max(_metrics.MinTextWidth, currentLine.InkBounds.Width);

            _metrics.Height = _metrics.Bounds.Height;
            _metrics.Width = _metrics.InkBounds.Width;
            _metrics.WidthIncludingTrailingWhitespace = _metrics.Bounds.Width;
            _metrics.Extent = _metrics.InkBounds.Height;
            _metrics.OverhangLeading = Math.Max(0, _metrics.Bounds.Left - _metrics.InkBounds.Left);
            _metrics.OverhangTrailing = Math.Max(0, _metrics.InkBounds.Right - _metrics.Bounds.Right);
            _metrics.OverhangAfter = Math.Max(0, _metrics.InkBounds.Bottom - _metrics.Bounds.Bottom);

            if (first)
            {
                _metrics.Baseline = currentLine.Baseline;
                first = false;
            }
        }

        /// <summary>
        /// Gets the <see cref="TextCollapsingProperties"/> for current text trimming mode.
        /// </summary>
        /// <param name="width">The collapsing width.</param>
        /// <param name="resolvedFlowDirection">The resolved flow direction.</param>
        /// <returns>The <see cref="TextCollapsingProperties"/>.</returns>
        private TextCollapsingProperties? GetCollapsingProperties(double width, FlowDirection resolvedFlowDirection)
        {
            if (_textTrimming == TextTrimming.None)
            {
                return null;
            }

            return _textTrimming.CreateCollapsingProperties(
                new TextCollapsingCreateInfo(width, _paragraphProperties.DefaultTextRunProperties, resolvedFlowDirection));
        }

        public void Dispose()
        {
            foreach (var line in _textLines)
            {
                line.Dispose();
            }
        }

        private class CachedMetrics
        {
            // vertical
            public double Height;
            public double Baseline;

            // horizontal
            public double Width;
            public double WidthIncludingTrailingWhitespace;

            // vertical bounding box metrics
            public double Extent;
            public double OverhangAfter;

            // horizontal bounding box metrics
            public double OverhangLeading;
            public double OverhangTrailing;

            public Rect Bounds;
            public Rect InkBounds;

            public double MinTextWidth;
        }
    }
}
