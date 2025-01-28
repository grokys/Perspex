using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting
{
    internal static class TextEllipsisHelper
    {
        public static TextRun[]? Collapse(TextLine textLine, TextCollapsingProperties properties, bool isWordEllipsis)
        {
            var textRuns = textLine.TextRuns;

            if (textRuns.Count == 0)
            {
                return null;
            }

            var runIndex = 0;
            var collapsedLength = 0;
            var shapedSymbol = TextFormatter.CreateSymbol(properties.Symbol, FlowDirection.LeftToRight);

            if (properties.Width < shapedSymbol.GlyphRun.Bounds.Width)
            {
                //Not enough space to fit in the symbol
                return [];
            }

            var availableWidth = properties.Width - shapedSymbol.Size.Width;

            if(properties.FlowDirection == FlowDirection.LeftToRight)
            {
                while (runIndex < textRuns.Count)
                {
                    var currentRun = textRuns[runIndex];

                    switch (currentRun)
                    {
                        case ShapedTextRun shapedRun:
                            {
                                if (shapedRun.Size.Width > availableWidth)
                                {
                                    if (shapedRun.TryMeasureCharacters(availableWidth, out var measuredLength))
                                    {
                                        if (isWordEllipsis && measuredLength < textLine.Length)
                                        {
                                            var currentBreakPosition = 0;

                                            var lineBreaker = new LineBreakEnumerator(currentRun.Text.Span);

                                            while (currentBreakPosition < measuredLength && lineBreaker.MoveNext(out var lineBreak))
                                            {
                                                var nextBreakPosition = lineBreak.PositionMeasure;

                                                if (nextBreakPosition == 0)
                                                {
                                                    break;
                                                }

                                                if (nextBreakPosition >= measuredLength)
                                                {
                                                    break;
                                                }

                                                currentBreakPosition = nextBreakPosition;
                                            }

                                            measuredLength = currentBreakPosition;
                                        }
                                    }

                                    collapsedLength += measuredLength;

                                    return TextCollapsingProperties.CreateCollapsedRuns(textLine, collapsedLength, FlowDirection.LeftToRight, shapedSymbol);
                                }

                                availableWidth -= shapedRun.Size.Width;

                                break;
                            }

                        case DrawableTextRun drawableRun:
                            {
                                //The whole run needs to fit into available space
                                if (drawableRun.Size.Width > availableWidth)
                                {
                                    return TextCollapsingProperties.CreateCollapsedRuns(textLine, collapsedLength, FlowDirection.LeftToRight, shapedSymbol);
                                }

                                availableWidth -= drawableRun.Size.Width;

                                break;
                            }
                    }

                    collapsedLength += currentRun.Length;

                    runIndex++;
                }
            }
            else
            {
                runIndex = textRuns.Count - 1;

                while (runIndex >= 0)
                {
                    var currentRun = textRuns[runIndex];

                    switch (currentRun)
                    {
                        case ShapedTextRun shapedRun:
                            {
                                if (shapedRun.Size.Width > availableWidth)
                                {
                                    if (shapedRun.TryMeasureCharacters(availableWidth, out var measuredLength))
                                    {
                                        if (isWordEllipsis && measuredLength < textLine.Length)
                                        {
                                            var currentBreakPosition = 0;

                                            var lineBreaker = new LineBreakEnumerator(currentRun.Text.Span);

                                            while (currentBreakPosition < measuredLength && lineBreaker.MoveNext(out var lineBreak))
                                            {
                                                var nextBreakPosition = lineBreak.PositionMeasure;

                                                if (nextBreakPosition == 0)
                                                {
                                                    break;
                                                }

                                                if (nextBreakPosition >= measuredLength)
                                                {
                                                    break;
                                                }

                                                currentBreakPosition = nextBreakPosition;
                                            }

                                            measuredLength = currentBreakPosition;
                                        }
                                    }

                                    collapsedLength += measuredLength;

                                    return TextCollapsingProperties.CreateCollapsedRuns(textLine, collapsedLength, FlowDirection.RightToLeft, shapedSymbol);
                                }

                                availableWidth -= shapedRun.Size.Width;

                                break;
                            }

                        case DrawableTextRun drawableRun:
                            {
                                //The whole run needs to fit into available space
                                if (drawableRun.Size.Width > availableWidth)
                                {
                                    return TextCollapsingProperties.CreateCollapsedRuns(textLine, collapsedLength, FlowDirection.RightToLeft, shapedSymbol);
                                }

                                availableWidth -= drawableRun.Size.Width;

                                break;
                            }
                    }

                    collapsedLength += currentRun.Length;

                    runIndex--;
                }
            }
      
            return null;
        }
    }
}
