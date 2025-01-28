// ReSharper disable ForCanBeConvertedToForeach
using System;
using System.Collections.Generic;
using static Avalonia.Media.TextFormatting.FormattingObjectPool;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Ellipsis based on a fixed length leading prefix and suffix growing from the end at character granularity.
    /// </summary>
    public sealed class TextLeadingPrefixCharacterEllipsis : TextCollapsingProperties
    {
        private readonly int _prefixLength;

        /// <summary>
        /// Construct a text trailing word ellipsis collapsing properties.
        /// </summary>
        /// <param name="ellipsis">Text used as collapsing symbol.</param>
        /// <param name="prefixLength">Length of leading prefix.</param>
        /// <param name="width">width in which collapsing is constrained to</param>
        /// <param name="textRunProperties">text run properties of ellipsis symbol</param>
        /// <param name="flowDirection">the flow direction of the collapes line.</param>
        public TextLeadingPrefixCharacterEllipsis(
            string ellipsis,
            int prefixLength,
            double width,
            TextRunProperties textRunProperties,
            FlowDirection flowDirection)
        {
            if (_prefixLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(prefixLength));
            }

            _prefixLength = prefixLength;
            Width = width;
            Symbol = new TextCharacters(ellipsis, textRunProperties);
            FlowDirection = flowDirection;
        }

        /// <inheritdoc/>
        public override double Width { get; }

        /// <inheritdoc/>
        public override TextRun Symbol { get; }

        public override FlowDirection FlowDirection { get; }

        /// <inheritdoc />
        public override TextRun[]? Collapse(TextLine textLine)
        {
            var textRuns = textLine.TextRuns;

            var runIndex = 0;
            var currentWidth = 0.0;
            var shapedSymbol = TextFormatter.CreateSymbol(Symbol, FlowDirection.LeftToRight);

            if (Width < shapedSymbol.GlyphRun.Bounds.Width)
            {
                return Array.Empty<TextRun>();
            }

            // Overview of ellipsis structure
            // Prefix length run | Ellipsis symbol | Post split run growing from the end |
            var availableWidth = Width - shapedSymbol.Size.Width;

            while (runIndex < textRuns.Count)
            {
                var currentRun = textRuns[runIndex];

                switch (currentRun)
                {
                    case ShapedTextRun shapedRun:
                        {
                            currentWidth += shapedRun.Size.Width;

                            if (currentWidth > availableWidth)
                            {
                                shapedRun.TryMeasureCharacters(availableWidth, out var measuredLength);

                                if (measuredLength > 0)
                                {
                                    var objectPool = FormattingObjectPool.Instance;

                                    var collapsedRuns = objectPool.TextRunLists.Rent();

                                    RentedList<TextRun>? rentedPreSplitRuns = null;
                                    RentedList<TextRun>? rentedPostSplitRuns = null;

                                    try
                                    {
                                        IReadOnlyList<TextRun>? effectivePostSplitRuns;

                                        if (_prefixLength > 0)
                                        {
                                            (rentedPreSplitRuns, rentedPostSplitRuns) = TextFormatterImpl.SplitTextRuns(
                                                textRuns, Math.Min(_prefixLength, measuredLength), objectPool);

                                            effectivePostSplitRuns = rentedPostSplitRuns;

                                            foreach (var preSplitRun in rentedPreSplitRuns)
                                            {
                                                collapsedRuns.Add(preSplitRun);
                                            }
                                        }
                                        else
                                        {
                                            effectivePostSplitRuns = textRuns;
                                        }

                                        collapsedRuns.Add(shapedSymbol);

                                        if (measuredLength <= _prefixLength || effectivePostSplitRuns is null)
                                        {
                                            return collapsedRuns.ToArray();
                                        }

                                        var availableSuffixWidth = availableWidth;

                                        if (rentedPreSplitRuns is not null)
                                        {
                                            foreach (var run in rentedPreSplitRuns)
                                            {
                                                if (run is DrawableTextRun drawableTextRun)
                                                {
                                                    availableSuffixWidth -= drawableTextRun.Size.Width;
                                                }
                                            }
                                        }

                                        for (var i = effectivePostSplitRuns.Count - 1; i >= 0; i--)
                                        {
                                            var run = effectivePostSplitRuns[i];

                                            switch (run)
                                            {
                                                case ShapedTextRun endShapedRun:
                                                {
                                                    if (endShapedRun.TryMeasureCharactersBackwards(availableSuffixWidth,
                                                            out var suffixCount, out var suffixWidth))
                                                    {
                                                        availableSuffixWidth -= suffixWidth;

                                                        if (suffixCount > 0)
                                                        {
                                                            var splitSuffix =
                                                                endShapedRun.Split(run.Length - suffixCount);

                                                            collapsedRuns.Add(splitSuffix.Second!);
                                                        }
                                                    }

                                                    break;
                                                }
                                            }
                                        }

                                        return collapsedRuns.ToArray();
                                    }
                                    finally
                                    {
                                        objectPool.TextRunLists.Return(ref rentedPreSplitRuns);
                                        objectPool.TextRunLists.Return(ref rentedPostSplitRuns);
                                        objectPool.TextRunLists.Return(ref collapsedRuns);
                                    }
                                }

                                return new TextRun[] { shapedSymbol };
                            }

                            availableWidth -= shapedRun.Size.Width;

                            break;
                        }
                    case DrawableTextRun drawableTextRun:
                        {
                            availableWidth -= drawableTextRun.Size.Width;

                            break;
                        }
                }            

                runIndex++;
            }

            return null;
        }
    }
}
