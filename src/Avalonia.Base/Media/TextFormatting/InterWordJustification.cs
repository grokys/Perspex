using System;
using System.Collections.Generic;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting
{
    internal class InterWordJustification : JustificationProperties
    {
        public InterWordJustification(double width)
        {
            Width = width;
        }

        public override double Width { get; }

        public override void Justify(TextLine textLine)
        {
            if (textLine is not TextLineImpl lineImpl)
            {
                return;
            }

            var paragraphWidth = Width;

            if (double.IsInfinity(paragraphWidth))
            {
                return;
            }

            var breakOportunities = new Queue<int>();

            var currentPosition = textLine.FirstTextSourceIndex;

            var whiteSpaceWidth = 0.0;

            for (var i = 0; i < lineImpl.TextRuns.Count; ++i)
            {
                var textRun = lineImpl.TextRuns[i];
                var text = textRun.Text;

                if (text.IsEmpty)
                {
                    continue;
                }

                if (textRun is ShapedTextRun shapedText)
                {
                    var glyphRun = shapedText.GlyphRun;
                    var shapedBuffer = shapedText.ShapedBuffer;

                    var lineBreakEnumerator = new LineBreakEnumerator(text.Span);

                    while (lineBreakEnumerator.MoveNext(out var currentBreak))
                    {
                        //Ignore the break at the end
                        if(currentPosition + currentBreak.PositionWrap == textLine.Length - TextRun.DefaultTextSourceLength)
                        {
                            break;
                        }

                        if (!currentBreak.Required)
                        {
                            breakOportunities.Enqueue(currentPosition + currentBreak.PositionWrap);

                            var offset = Math.Max(0, currentPosition - glyphRun.Metrics.FirstCluster);

                            var characterIndex = currentPosition - offset + currentBreak.PositionWrap - 1;
                            var glyphIndex = glyphRun.FindGlyphIndex(characterIndex);
                            var glyphInfo = shapedBuffer[glyphIndex];

                            if (Codepoint.ReadAt(text.Span, currentBreak.PositionWrap - 1, out _).IsWhiteSpace)
                            {
                                whiteSpaceWidth += glyphInfo.GlyphAdvance;
                            }
                        }
                    }
                }  

                currentPosition += textRun.Length;
            }

            if (breakOportunities.Count == 0)
            {
                return;
            }

            //Adjust remaining space by whiteSpace width
            var remainingSpace = Math.Max(0, paragraphWidth - lineImpl.Width) + whiteSpaceWidth;

            var spacing = remainingSpace / breakOportunities.Count;

            currentPosition = textLine.FirstTextSourceIndex;

            foreach (var textRun in lineImpl.TextRuns)
            {
                var text = textRun.Text;

                if (text.IsEmpty)
                {
                    continue;
                }

                if (textRun is ShapedTextRun shapedText)
                {
                    var glyphRun = shapedText.GlyphRun;
                    var shapedBuffer = shapedText.ShapedBuffer;

                    while (breakOportunities.Count > 0)
                    {
                        var characterIndex = breakOportunities.Dequeue();

                        var offset = Math.Max(0, currentPosition - glyphRun.Metrics.FirstCluster);

                        if (characterIndex + offset < currentPosition)
                        {
                            continue;
                        }

                        var glyphIndex = glyphRun.FindGlyphIndex(characterIndex - offset - 1);
                        var glyphInfo = shapedBuffer[glyphIndex];

                        var isWhitespace = Codepoint.ReadAt(text.Span, characterIndex - 1 - currentPosition, out _).IsWhiteSpace;

                        shapedBuffer[glyphIndex] = new GlyphInfo(glyphInfo.GlyphIndex,
                            glyphInfo.GlyphCluster, isWhitespace ? spacing : glyphInfo.GlyphAdvance + spacing);

                        if (glyphIndex == shapedBuffer.Length - 1)
                        {
                            break;
                        }
                    }

                    glyphRun.GlyphInfos = shapedBuffer;
                }

                currentPosition += textRun.Length;
            }
        }
    }
}
