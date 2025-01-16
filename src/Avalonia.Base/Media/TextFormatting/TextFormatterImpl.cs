// ReSharper disable ForCanBeConvertedToForeach
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;
using static Avalonia.Media.TextFormatting.FormattingObjectPool;

namespace Avalonia.Media.TextFormatting
{
    internal sealed class TextFormatterImpl : TextFormatter
    {
        private static readonly char[] s_empty = { ' ' };
        private static readonly string s_defaultText = new string('a', TextRun.DefaultTextSourceLength);

        [ThreadStatic] private static BidiData? t_bidiData;
        [ThreadStatic] private static BidiAlgorithm? t_bidiAlgorithm;

        /// <inheritdoc cref="TextFormatter.FormatLine"/>
        public override TextLine? FormatLine(ITextSource textSource, int firstTextSourceIndex, double paragraphWidth,
            TextParagraphProperties paragraphProperties, TextLineBreak? previousLineBreak = null)
        {
            TextLineBreak? nextLineBreak = null;
            var objectPool = FormattingObjectPool.Instance;
            var fontManager = FontManager.Current;

            // we've wrapped the previous line and need to continue wrapping: ignore the textSource and do that instead
            if (previousLineBreak is WrappingTextLineBreak wrappingTextLineBreak
                && wrappingTextLineBreak.AcquireRemainingRuns() is { } remainingRuns
                && paragraphProperties.TextWrapping != TextWrapping.NoWrap)
            {
                return PerformTextWrapping(remainingRuns, true, firstTextSourceIndex, paragraphWidth,
                    paragraphProperties, previousLineBreak.FlowDirection, previousLineBreak, objectPool);
            }

            RentedList<TextRun>? fetchedRuns = null;
            RentedList<TextRun>? shapedTextRuns = null;
            try
            {
                fetchedRuns = FetchTextRuns(textSource, firstTextSourceIndex, objectPool, out var textEndOfLine,
                    out var textSourceLength);

                if (fetchedRuns.Count == 0)
                {
                    return null;
                }

                shapedTextRuns = ShapeTextRuns(fetchedRuns, paragraphProperties, objectPool, fontManager,
                    out var resolvedFlowDirection);

                if (nextLineBreak == null && textEndOfLine != null)
                {
                    nextLineBreak = new TextLineBreak(textEndOfLine, resolvedFlowDirection);
                }

                switch (paragraphProperties.TextWrapping)
                {
                    case TextWrapping.NoWrap:
                        {
                            var textLine = new TextLineImpl(shapedTextRuns.ToArray(), firstTextSourceIndex,
                                textSourceLength,
                                paragraphWidth, paragraphProperties, resolvedFlowDirection, nextLineBreak);

                            textLine.FinalizeLine();

                            return textLine;
                        }
                    case TextWrapping.WrapWithOverflow:
                    case TextWrapping.Wrap:
                        {
                            return PerformTextWrapping(shapedTextRuns, false, firstTextSourceIndex, paragraphWidth,
                                paragraphProperties, resolvedFlowDirection, nextLineBreak, objectPool);
                        }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(paragraphProperties.TextWrapping));
                }
            }
            finally
            {
                objectPool.TextRunLists.Return(ref shapedTextRuns);
                objectPool.TextRunLists.Return(ref fetchedRuns);
            }
        }

        /// <summary>
        /// Split a sequence of runs into two segments at specified length.
        /// </summary>
        /// <param name="textRuns">The text run's.</param>
        /// <param name="length">The length to split at.</param>
        /// <param name="objectPool">A pool used to get reusable formatting objects.</param>
        /// <returns>The split text runs.</returns>
        internal static SplitResult<RentedList<TextRun>> SplitTextRuns(IReadOnlyList<TextRun> textRuns, int length,
            FormattingObjectPool objectPool)
        {
            var first = objectPool.TextRunLists.Rent();
            var currentLength = 0;

            for (var i = 0; i < textRuns.Count; i++)
            {
                var currentRun = textRuns[i];
                var currentRunLength = currentRun.Length;

                if (currentLength + currentRunLength < length)
                {
                    currentLength += currentRunLength;

                    continue;
                }

                var firstCount = currentRunLength >= 1 ? i + 1 : i;

                if (firstCount > 1)
                {
                    for (var j = 0; j < i; j++)
                    {
                        first.Add(textRuns[j]);
                    }
                }

                var secondCount = textRuns.Count - firstCount;

                if (currentLength + currentRunLength == length)
                {
                    var second = secondCount > 0 ? objectPool.TextRunLists.Rent() : null;

                    if (second != null)
                    {
                        var offset = currentRunLength >= 1 ? 1 : 0;

                        for (var j = 0; j < secondCount; j++)
                        {
                            second.Add(textRuns[i + j + offset]);
                        }
                    }

                    first.Add(currentRun);

                    return new SplitResult<RentedList<TextRun>>(first, second);
                }
                else
                {
                    secondCount++;

                    var second = objectPool.TextRunLists.Rent();

                    if (currentRun is ShapedTextRun shapedTextCharacters)
                    {
                        var split = shapedTextCharacters.Split(length - currentLength);

                        first.Add(split.First);

                        second.Add(split.Second!);
                    }

                    for (var j = 1; j < secondCount; j++)
                    {
                        second.Add(textRuns[i + j]);
                    }

                    return new SplitResult<RentedList<TextRun>>(first, second);
                }
            }

            for (var i = 0; i < textRuns.Count; i++)
            {
                first.Add(textRuns[i]);
            }

            return new SplitResult<RentedList<TextRun>>(first, null);
        }

        /// <summary>
        /// Shape specified text runs with specified paragraph embedding.
        /// </summary>
        /// <param name="textRuns">The text runs to shape.</param>
        /// <param name="paragraphProperties">The default paragraph properties.</param>
        /// <param name="resolvedFlowDirection">The resolved flow direction.</param>
        /// <param name="objectPool">A pool used to get reusable formatting objects.</param>
        /// <param name="fontManager">The font manager to use.</param>
        /// <returns>
        /// A list of shaped text characters.
        /// </returns>
        private static RentedList<TextRun> ShapeTextRuns(IReadOnlyList<TextRun> textRuns,
            TextParagraphProperties paragraphProperties, FormattingObjectPool objectPool,
            FontManager fontManager, out FlowDirection resolvedFlowDirection)
        {
            var flowDirection = paragraphProperties.FlowDirection;
            var shapedRuns = objectPool.TextRunLists.Rent();

            if (textRuns.Count == 0)
            {
                resolvedFlowDirection = flowDirection;
                return shapedRuns;
            }

            var bidiData = t_bidiData ??= new();
            bidiData.Reset();
            bidiData.ParagraphEmbeddingLevel = (sbyte)flowDirection;

            for (var i = 0; i < textRuns.Count; ++i)
            {
                var textRun = textRuns[i];

                ReadOnlySpan<char> text;
                if (!textRun.Text.IsEmpty)
                    text = textRun.Text.Span;
                else if (textRun.Length == TextRun.DefaultTextSourceLength)
                    text = s_defaultText.AsSpan();
                else
                {
                    text = new string('a', textRun.Length).AsSpan();
                }

                bidiData.Append(text);
            }

            var bidiAlgorithm = t_bidiAlgorithm ??= new();
            bidiAlgorithm.Process(bidiData);

            var resolvedEmbeddingLevel = bidiAlgorithm.ResolveEmbeddingLevel(bidiData.Classes);

            resolvedFlowDirection =
                (resolvedEmbeddingLevel & 1) == 0 ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;

            var processedRuns = objectPool.TextRunLists.Rent();
            var groupedRuns = objectPool.UnshapedTextRunLists.Rent();

            try
            {
                CoalesceLevels(textRuns, bidiAlgorithm.ResolvedLevels.Span, fontManager, processedRuns);

                bidiData.Reset();
                bidiAlgorithm.Reset();


                var textShaper = TextShaper.Current;

                for (var index = 0; index < processedRuns.Count; index++)
                {
                    var currentRun = processedRuns[index];

                    switch (currentRun)
                    {
                        case UnshapedTextRun shapeableRun:
                            {
                                groupedRuns.Clear();
                                groupedRuns.Add(shapeableRun);

                                var text = shapeableRun.Text;
                                var properties = shapeableRun.Properties;

                                while (index + 1 < processedRuns.Count)
                                {
                                    if (processedRuns[index + 1] is not UnshapedTextRun nextRun)
                                    {
                                        break;
                                    }

                                    if (shapeableRun.BidiLevel == nextRun.BidiLevel
                                        && TryJoinContiguousMemories(text, nextRun.Text, out var joinedText)
                                        && CanShapeTogether(properties, nextRun.Properties))
                                    {
                                        groupedRuns.Add(nextRun);
                                        index++;
                                        shapeableRun = nextRun;
                                        text = joinedText;
                                        continue;
                                    }

                                    break;
                                }

                                var shaperOptions = new TextShaperOptions(
                                    properties.CachedGlyphTypeface, properties.FontFeatures,
                                    properties.FontRenderingEmSize, shapeableRun.BidiLevel, properties.CultureInfo,
                                    paragraphProperties.DefaultIncrementalTab, paragraphProperties.LetterSpacing);

                                ShapeTogether(groupedRuns, text, shaperOptions, textShaper, shapedRuns);

                                break;
                            }
                        default:
                            {
                                shapedRuns.Add(currentRun);

                                break;
                            }
                    }
                }
            }
            finally
            {
                objectPool.TextRunLists.Return(ref processedRuns);
                objectPool.UnshapedTextRunLists.Return(ref groupedRuns);
            }

            return shapedRuns;
        }

        /// <summary>
        /// Tries to join two potnetially contiguous memory regions.
        /// </summary>
        /// <param name="x">The first memory region.</param>
        /// <param name="y">The second memory region.</param>
        /// <param name="joinedMemory">On success, a memory region representing the union of the two regions.</param>
        /// <returns>true if the two regions were contigous; false otherwise.</returns>
        private static bool TryJoinContiguousMemories(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y,
            out ReadOnlyMemory<char> joinedMemory)
        {
            if (MemoryMarshal.TryGetString(x, out var xString, out var xStart, out var xLength))
            {
                if (MemoryMarshal.TryGetString(y, out var yString, out var yStart, out var yLength)
                    && ReferenceEquals(xString, yString)
                    && TryGetContiguousStart(xStart, xLength, yStart, yLength, out var joinedStart))
                {
                    joinedMemory = xString.AsMemory(joinedStart, xLength + yLength);
                    return true;
                }
            }

            else if (MemoryMarshal.TryGetArray(x, out var xSegment))
            {
                if (MemoryMarshal.TryGetArray(y, out var ySegment)
                    && ReferenceEquals(xSegment.Array, ySegment.Array)
                    && TryGetContiguousStart(xSegment.Offset, xSegment.Count, ySegment.Offset, ySegment.Count, out var joinedStart))
                {
                    joinedMemory = xSegment.Array.AsMemory(joinedStart, xSegment.Count + ySegment.Count);
                    return true;
                }
            }

            else if (MemoryMarshal.TryGetMemoryManager(x, out MemoryManager<char>? xManager, out xStart, out xLength))
            {
                if (MemoryMarshal.TryGetMemoryManager(y, out MemoryManager<char>? yManager, out var yStart, out var yLength)
                    && ReferenceEquals(xManager, yManager)
                    && TryGetContiguousStart(xStart, xLength, yStart, yLength, out var joinedStart))
                {
                    joinedMemory = xManager.Memory.Slice(joinedStart, xLength + yLength);
                    return true;
                }
            }

            joinedMemory = default;
            return false;

            static bool TryGetContiguousStart(int xStart, int xLength, int yStart, int yLength, out int joinedStart)
            {
                var xRange = (Start: xStart, Length: xLength);
                var yRange = (Start: yStart, Length: yLength);

                var (firstRange, secondRange) = xStart <= yStart ? (xRange, yRange) : (yRange, xRange);
                if (firstRange.Start + firstRange.Length == secondRange.Start)
                {
                    joinedStart = firstRange.Start;
                    return true;
                }

                joinedStart = default;
                return false;
            }
        }

        private static bool CanShapeTogether(TextRunProperties x, TextRunProperties y)
            => MathUtilities.AreClose(x.FontRenderingEmSize, y.FontRenderingEmSize)
               && x.Typeface == y.Typeface
               && x.BaselineAlignment == y.BaselineAlignment;

        private static void ShapeTogether(IReadOnlyList<UnshapedTextRun> textRuns, ReadOnlyMemory<char> text,
            TextShaperOptions options, TextShaper textShaper, RentedList<TextRun> results)
        {
            var shapedBuffer = textShaper.ShapeText(text, options);

            for (var i = 0; i < textRuns.Count; i++)
            {
                var currentRun = textRuns[i];

                var splitResult = shapedBuffer.Split(currentRun.Length);

                results.Add(new ShapedTextRun(splitResult.First, currentRun.Properties));

                shapedBuffer = splitResult.Second!;
            }
        }

        /// <summary>
        /// Coalesces ranges of the same bidi level to form <see cref="UnshapedTextRun"/>
        /// </summary>
        /// <param name="textCharacters">The text characters to form <see cref="UnshapedTextRun"/> from.</param>
        /// <param name="levels">The bidi levels.</param>
        /// <param name="fontManager">The font manager to use.</param>
        /// <param name="processedRuns">A list that will be filled with the processed runs.</param>
        /// <returns></returns>
        private static void CoalesceLevels(IReadOnlyList<TextRun> textCharacters, ReadOnlySpan<sbyte> levels,
            FontManager fontManager, RentedList<TextRun> processedRuns)
        {
            if (levels.Length == 0)
            {
                return;
            }

            var levelIndex = 0;
            var runLevel = levels[0];

            TextRunProperties? previousProperties = null;
            TextCharacters? currentRun = null;
            ReadOnlyMemory<char> runText = default;

            for (var i = 0; i < textCharacters.Count; i++)
            {
                var j = 0;
                currentRun = textCharacters[i] as TextCharacters;

                if (currentRun == null)
                {
                    var drawableRun = textCharacters[i];

                    processedRuns.Add(drawableRun);

                    levelIndex += drawableRun.Length;

                    continue;
                }

                runText = currentRun.Text;
                var runTextSpan = runText.Span;

                for (; j < runTextSpan.Length;)
                {
                    Codepoint.ReadAt(runTextSpan, j, out var count);

                    if (levelIndex + 1 == levels.Length)
                    {
                        break;
                    }

                    levelIndex++;
                    j += count;

                    if (j == runTextSpan.Length)
                    {
                        currentRun.GetShapeableCharacters(runText.Slice(0, j), runLevel, fontManager,
                            ref previousProperties, processedRuns);

                        runLevel = levels[levelIndex];

                        continue;
                    }

                    if (levels[levelIndex] == runLevel)
                    {
                        continue;
                    }

                    // End of this run
                    currentRun.GetShapeableCharacters(runText.Slice(0, j), runLevel, fontManager,
                        ref previousProperties, processedRuns);

                    runText = runText.Slice(j);
                    runTextSpan = runText.Span;

                    j = 0;

                    // Move to next run
                    runLevel = levels[levelIndex];
                }
            }

            if (currentRun is null || runText.IsEmpty)
            {
                return;
            }

            currentRun.GetShapeableCharacters(runText, runLevel, fontManager, ref previousProperties, processedRuns);
        }

        /// <summary>
        /// Fetches text runs.
        /// </summary>
        /// <param name="textSource">The text source.</param>
        /// <param name="firstTextSourceIndex">The first text source index.</param>
        /// <param name="objectPool">A pool used to get reusable formatting objects.</param>
        /// <param name="endOfLine">On return, the end of line, if any.</param>
        /// <param name="textSourceLength">On return, the processed text source length.</param>
        /// <returns>
        /// The formatted text runs.
        /// </returns>
        private static RentedList<TextRun> FetchTextRuns(ITextSource textSource, int firstTextSourceIndex,
            FormattingObjectPool objectPool, out TextEndOfLine? endOfLine, out int textSourceLength)
        {
            textSourceLength = 0;

            endOfLine = null;

            var textRuns = objectPool.TextRunLists.Rent();

            var textRunEnumerator = new TextRunEnumerator(textSource, firstTextSourceIndex);

            while (textRunEnumerator.MoveNext())
            {
                TextRun textRun = textRunEnumerator.Current!;

                if (textRun is TextEndOfLine textEndOfLine)
                {
                    endOfLine = textEndOfLine;

                    textSourceLength += textEndOfLine.Length;

                    textRuns.Add(textRun);

                    break;
                }

                switch (textRun)
                {
                    case TextCharacters textCharacters:
                        {
                            if (TryGetLineBreak(textCharacters, out var runLineBreak))
                            {
                                var splitResult = new TextCharacters(textCharacters.Text.Slice(0, runLineBreak.PositionWrap),
                                    textCharacters.Properties);

                                textRuns.Add(splitResult);

                                textSourceLength += runLineBreak.PositionWrap;

                                return textRuns;
                            }

                            textRuns.Add(textCharacters);

                            break;
                        }
                    default:
                        {
                            textRuns.Add(textRun);
                            break;
                        }
                }

                textSourceLength += textRun.Length;
            }

            return textRuns;
        }

        private static bool TryGetLineBreak(TextRun textRun, out LineBreak lineBreak)
        {
            lineBreak = default;

            var text = textRun.Text;

            if (text.IsEmpty)
            {
                return false;
            }

            var lineBreakEnumerator = new LineBreakEnumerator(text.Span);

            while (lineBreakEnumerator.MoveNext(out lineBreak))
            {
                if (!lineBreak.Required)
                {
                    continue;
                }

                return lineBreak.PositionWrap >= textRun.Length || true;
            }

            return false;
        }

        private static int MeasureLength(IReadOnlyList<TextRun> textRuns, double paragraphWidth)
        {
            var measuredLength = 0;
            var currentWidth = 0.0;

            for (var i = 0; i < textRuns.Count; ++i)
            {
                var currentRun = textRuns[i];

                switch (currentRun)
                {
                    case ShapedTextRun shapedTextCharacters:
                        {
                            if (shapedTextCharacters.ShapedBuffer.Length > 0)
                            {
                                var runLength = 0;

                                for (var j = 0; j < shapedTextCharacters.ShapedBuffer.Length; j++)
                                {
                                    var currentInfo = shapedTextCharacters.ShapedBuffer[j];

                                    var clusterWidth = currentInfo.GlyphAdvance;

                                    GlyphInfo nextInfo = default;

                                    while (j + 1 < shapedTextCharacters.ShapedBuffer.Length)
                                    {
                                        nextInfo = shapedTextCharacters.ShapedBuffer[j + 1];

                                        if (currentInfo.GlyphCluster == nextInfo.GlyphCluster)
                                        {
                                            clusterWidth += nextInfo.GlyphAdvance;

                                            j++;

                                            continue;
                                        }

                                        break;
                                    }

                                    var clusterLength = Math.Max(0, nextInfo.GlyphCluster - currentInfo.GlyphCluster);

                                    if (clusterLength == 0)
                                    {
                                        clusterLength = currentRun.Length - runLength;
                                    }

                                    if (clusterLength == 0)
                                    {
                                        clusterLength = shapedTextCharacters.GlyphRun.Metrics.FirstCluster + currentRun.Length - currentInfo.GlyphCluster;
                                    }

                                    if (currentWidth + clusterWidth > paragraphWidth)
                                    {
                                        if (runLength == 0 && measuredLength == 0)
                                        {
                                            runLength = clusterLength;
                                        }

                                        return measuredLength + runLength;
                                    }

                                    currentWidth += clusterWidth;
                                    runLength += clusterLength;
                                }

                                measuredLength += runLength;
                            }

                            break;
                        }

                    case DrawableTextRun drawableTextRun:
                        {
                            if (currentWidth + drawableTextRun.Size.Width >= paragraphWidth)
                            {
                                return measuredLength;
                            }

                            measuredLength += currentRun.Length;
                            currentWidth += drawableTextRun.Size.Width;

                            break;
                        }
                    default:
                        {
                            measuredLength += currentRun.Length;

                            break;
                        }
                }
            }

            return measuredLength;
        }

        /// <summary>
        /// Creates an empty text line.
        /// </summary>
        /// <returns>The empty text line.</returns>
        public static TextLineImpl CreateEmptyTextLine(int firstTextSourceIndex, double paragraphWidth,
            TextParagraphProperties paragraphProperties)
        {
            var flowDirection = paragraphProperties.FlowDirection;
            var properties = paragraphProperties.DefaultTextRunProperties;
            var glyphTypeface = properties.CachedGlyphTypeface;
            var glyph = glyphTypeface.GetGlyph(s_empty[0]);
            var glyphInfos = new[] { new GlyphInfo(glyph, firstTextSourceIndex, 0.0) };

            var shapedBuffer = new ShapedBuffer(s_empty.AsMemory(), glyphInfos, glyphTypeface, properties.FontRenderingEmSize,
                (sbyte)flowDirection);

            var textRuns = new TextRun[] { new ShapedTextRun(shapedBuffer, properties) };

            var line = new TextLineImpl(textRuns, firstTextSourceIndex, 0, paragraphWidth, paragraphProperties, flowDirection);

            line.FinalizeLine();

            return line;
        }

        /// <summary>
        /// Performs text wrapping returns a list of text lines.
        /// </summary>
        /// <param name="textRuns"></param>
        /// <param name="canReuseTextRunList">Whether <see cref="TextRun"/> can be reused to store the split runs.</param>
        /// <param name="firstTextSourceIndex">The first text source index.</param>
        /// <param name="paragraphWidth">The paragraph width.</param>
        /// <param name="paragraphProperties">The text paragraph properties.</param>
        /// <param name="resolvedFlowDirection"></param>
        /// <param name="currentLineBreak">The current line break if the line was explicitly broken.</param>
        /// <param name="objectPool">A pool used to get reusable formatting objects.</param>
        /// <returns>The wrapped text line.</returns>
        private static TextLineImpl PerformTextWrapping(List<TextRun> textRuns, bool canReuseTextRunList,
            int firstTextSourceIndex, double paragraphWidth, TextParagraphProperties paragraphProperties,
            FlowDirection resolvedFlowDirection, TextLineBreak? currentLineBreak, FormattingObjectPool objectPool)
        {
            if (textRuns.Count == 0)
            {
                return CreateEmptyTextLine(firstTextSourceIndex, paragraphWidth, paragraphProperties);
            }

            var measuredLength = MeasureLength(textRuns, paragraphWidth);

            if(measuredLength == 0)
            {
                if(paragraphProperties.TextWrapping == TextWrapping.NoWrap)
                {
                    for (int i = 0; i < textRuns.Count; i++)
                    {
                        measuredLength += textRuns[i].Length;
                    }
                }
                else
                {
                    var firstRun = textRuns[0];

                    if(firstRun is ShapedTextRun)
                    {
                        var graphemeEnumerator = new GraphemeEnumerator(firstRun.Text.Span);

                        if(graphemeEnumerator.MoveNext(out var grapheme))
                        {
                            measuredLength = grapheme.Length;
                        }
                        else
                        {
                            measuredLength = 1;
                        }
                    }
                    else
                    {
                        measuredLength = firstRun.Length;
                    }
                }
            }

            var currentLength = 0;

            var lastWrapPosition = 0;

            var currentPosition = 0;

            for (var index = 0; index < textRuns.Count; index++)
            {
                var breakFound = false;

                var currentRun = textRuns[index];

                switch (currentRun)
                {
                    case ShapedTextRun:
                        {
                            var lineBreaker = new LineBreakEnumerator(currentRun.Text.Span);

                            while (lineBreaker.MoveNext(out var lineBreak))
                            {
                                if (lineBreak.Required &&
                                    currentLength + lineBreak.PositionMeasure <= measuredLength)
                                {
                                    //Explicit break found
                                    breakFound = true;

                                    currentPosition = currentLength + lineBreak.PositionWrap;

                                    break;
                                }

                                if (currentLength + lineBreak.PositionMeasure > measuredLength)
                                {
                                    if (paragraphProperties.TextWrapping == TextWrapping.WrapWithOverflow)
                                    {
                                        if (lastWrapPosition > 0)
                                        {
                                            currentPosition = lastWrapPosition;

                                            breakFound = true;

                                            break;
                                        }

                                        //Find next possible wrap position (overflow)
                                        if (index < textRuns.Count - 1)
                                        {
                                            if (lineBreak.PositionWrap != currentRun.Length)
                                            {
                                                //We already found the next possible wrap position.
                                                breakFound = true;

                                                currentPosition = currentLength + lineBreak.PositionWrap;

                                                break;
                                            }

                                            while (lineBreaker.MoveNext(out lineBreak))
                                            {
                                                currentPosition += lineBreak.PositionWrap;

                                                if (lineBreak.PositionWrap != currentRun.Length)
                                                {
                                                    break;
                                                }

                                                index++;

                                                if (index >= textRuns.Count)
                                                {
                                                    break;
                                                }

                                                currentRun = textRuns[index];

                                                lineBreaker = new LineBreakEnumerator(currentRun.Text.Span);
                                            }
                                        }
                                        else
                                        {
                                            currentPosition = currentLength + lineBreak.PositionWrap;
                                        }

                                        if (currentPosition == 0 && measuredLength > 0)
                                        {
                                            currentPosition = measuredLength;
                                        }

                                        breakFound = true;

                                        break;
                                    }

                                    //We overflowed so we use the last available wrap position.
                                    currentPosition = lastWrapPosition == 0 ? measuredLength : lastWrapPosition;

                                    breakFound = true;

                                    break;
                                }

                                if (lineBreak.PositionMeasure != lineBreak.PositionWrap || lineBreak.PositionWrap != currentRun.Length)
                                {
                                    lastWrapPosition = currentLength + lineBreak.PositionWrap;
                                }
                            }

                            break;
                        }
                }

                if (!breakFound)
                {
                    currentLength += currentRun.Length;

                    continue;
                }

                measuredLength = currentPosition;

                break;
            }

            var (preSplitRuns, postSplitRuns) = SplitTextRuns(textRuns, measuredLength, objectPool);

            try
            {
                TextLineBreak? textLineBreak;
                if (postSplitRuns?.Count > 0)
                {
                    List<TextRun> remainingRuns;

                    // reuse the list as much as possible:
                    // if canReuseTextRunList == true it's coming from previous remaining runs
                    if (canReuseTextRunList)
                    {
                        remainingRuns = textRuns;
                        remainingRuns.Clear();
                    }
                    else
                    {
                        remainingRuns = new List<TextRun>();
                    }

                    for (var i = 0; i < postSplitRuns.Count; ++i)
                    {
                        remainingRuns.Add(postSplitRuns[i]);
                    }

                    textLineBreak = new WrappingTextLineBreak(null, resolvedFlowDirection, remainingRuns);
                }
                else if (currentLineBreak?.TextEndOfLine is { } textEndOfLine)
                {
                    textLineBreak = new TextLineBreak(textEndOfLine, resolvedFlowDirection);
                }
                else
                {
                    textLineBreak = null;
                }

                if (postSplitRuns?.Count > 0)
                {
                    ResetTrailingWhitespaceBidiLevels(preSplitRuns, paragraphProperties.FlowDirection, objectPool);
                }

                var textLine = new TextLineImpl(preSplitRuns.ToArray(), firstTextSourceIndex, measuredLength,
                    paragraphWidth, paragraphProperties, resolvedFlowDirection,
                    textLineBreak);

                textLine.FinalizeLine();

                return textLine;
            }
            finally
            {
                objectPool.TextRunLists.Return(ref preSplitRuns);
                objectPool.TextRunLists.Return(ref postSplitRuns);
            }
        }

        private static void ResetTrailingWhitespaceBidiLevels(RentedList<TextRun> lineTextRuns, FlowDirection paragraphFlowDirection, FormattingObjectPool objectPool)
        {
            if (lineTextRuns.Count == 0)
            {
                return;
            }

            var lastTextRunIndex = lineTextRuns.Count - 1;

            var lastTextRun = lineTextRuns[lastTextRunIndex];

            if (lastTextRun is not ShapedTextRun shapedText)
            {
                return;
            }

            var paragraphEmbeddingLevel = (sbyte)paragraphFlowDirection;

            if (shapedText.BidiLevel == paragraphEmbeddingLevel)
            {
                return;
            }

            var trailingWhitespaceLength = shapedText.GlyphRun.Metrics.TrailingWhitespaceLength;

            if (trailingWhitespaceLength == 0)
            {
                return;
            }

            var splitIndex = shapedText.Length - trailingWhitespaceLength;

            var (textRuns, trailingWhitespaceRuns) = SplitTextRuns([shapedText], splitIndex, objectPool);

            try
            {
                if (trailingWhitespaceRuns != null)
                {
                    for (var i = 0; i < trailingWhitespaceRuns.Count; i++)
                    {
                        if (trailingWhitespaceRuns[i] is ShapedTextRun shapedTextRun)
                        {
                            shapedTextRun.ShapedBuffer.ResetBidiLevel(paragraphEmbeddingLevel);
                        }
                    }

                    lineTextRuns.RemoveAt(lastTextRunIndex);

                    lineTextRuns.AddRange(textRuns);
                    lineTextRuns.AddRange(trailingWhitespaceRuns);
                }
            }
            finally
            {
                objectPool.TextRunLists.Return(ref textRuns);
                objectPool.TextRunLists.Return(ref trailingWhitespaceRuns);
            }
        }

        private struct TextRunEnumerator
        {
            private readonly ITextSource _textSource;
            private int _pos;

            public TextRunEnumerator(ITextSource textSource, int firstTextSourceIndex)
            {
                _textSource = textSource;
                _pos = firstTextSourceIndex;
                Current = null;
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public TextRun? Current { get; private set; }

            public bool MoveNext()
            {
                Current = _textSource.GetTextRun(_pos);

                if (Current is null)
                {
                    return false;
                }

                if (Current.Length == 0)
                {
                    return false;
                }

                _pos += Current.Length;

                return true;
            }
        }
    }
}
