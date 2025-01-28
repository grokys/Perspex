#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    public class TextFormatterTests
    {
        [Fact]
        public void Should_Format_TextRuns_With_Default_Style()
        {
            using (Start())
            {
                const string text = "0123456789";

                var defaultProperties =
                    new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                Assert.Single(textLine.TextRuns);

                var textRun = textLine.TextRuns[0];

                Assert.NotNull(textRun.Properties);

                Assert.Equal(defaultProperties.Typeface, textRun.Properties.Typeface);

                Assert.Equal(defaultProperties.ForegroundBrush, textRun.Properties.ForegroundBrush);

                Assert.Equal(text.Length, textRun.Length);
            }
        }

        [Fact]
        public void Should_Format_TextRuns_With_Multiple_Buffers()
        {
            using (Start())
            {
                var defaultProperties =
                    new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

                var textSource = new MultiBufferTextSource(defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                Assert.Equal(5, textLine.TextRuns.Count);

                Assert.Equal(50, textLine.Length);
            }
        }

        private class TextSourceWithDummyRuns : ITextSource
        {
            private readonly TextRunProperties _properties;
            private readonly List<ValueSpan<TextRun>> _textRuns;

            public TextSourceWithDummyRuns(TextRunProperties properties)
            {
                _properties = properties;

                _textRuns = new List<ValueSpan<TextRun>>
                {
                    new ValueSpan<TextRun>(0, 5, new TextCharacters("Hello", _properties)),
                    new ValueSpan<TextRun>(5, 1, new DummyRun()),
                    new ValueSpan<TextRun>(6, 1, new DummyRun()),
                    new ValueSpan<TextRun>(7, 6, new TextCharacters(" World", _properties))
                };
            }

            public TextRun GetTextRun(int textSourceIndex)
            {
                foreach (var run in _textRuns)
                {
                    if (textSourceIndex < run.Start + run.Length)
                    {
                        return run.Value;
                    }
                }

                return new TextEndOfParagraph();
            }

            private class DummyRun : TextRun
            {
                public DummyRun()
                {
                    Length = DefaultTextSourceLength;
                }

                public override int Length { get; }
            }
        }

        [Fact]
        public void Should_Format_TextLine_With_Non_Text_TextRuns()
        {
            using (Start())
            {
                var defaultProperties =
                    new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

                var textSource = new TextSourceWithDummyRuns(defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                Assert.Equal(5, textLine.TextRuns.Count);

                Assert.Equal(14, textLine.Length);
            }
        }

        [Fact]
        public void Should_Format_TextLine_With_Non_Text_TextRuns_RightToLeft()
        {
            using (Start())
            {
                var defaultProperties =
                    new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

                var textSource = new TextSourceWithDummyRuns(defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(FlowDirection.RightToLeft, TextAlignment.Left, true, true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0));

                Assert.NotNull(textLine);

                Assert.Equal(5, textLine.TextRuns.Count);

                Assert.Equal(14, textLine.Length);

                var first = textLine.TextRuns[0] as ShapedTextRun;

                var last = textLine.TextRuns[4] as TextEndOfParagraph;

                Assert.NotNull(first);

                Assert.NotNull(last);

                Assert.Equal("Hello".AsMemory(), first.Text);
            }
        }

        [Fact]
        public void Should_Reset_Bidi_Levels_Of_Trailing_Whitespaces_After_TextWrapping()
        {
            using (Start())
            {
                const string text = "aaa bbb";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var paragraphProperties = new GenericTextParagraphProperties(FlowDirection.RightToLeft, TextAlignment.Right, true,
                                                                             true, defaultProperties, TextWrapping.Wrap, 0, 0, 0);

                var textSource = new SimpleTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var firstLine = formatter.FormatLine(textSource, 0, 50, paragraphProperties);

                Assert.NotNull(firstLine);

                Assert.Equal(2, firstLine.TextRuns.Count);

                var first = firstLine.TextRuns[0] as ShapedTextRun;

                var second = firstLine.TextRuns[1] as ShapedTextRun;

                Assert.NotNull(first);

                Assert.NotNull(second);

                Assert.Equal(" ", first.Text.ToString());

                Assert.Equal("aaa", second.Text.ToString());

                Assert.Equal(1, first.BidiLevel);

                Assert.Equal(2, second.BidiLevel);
            }
        }


        [Fact]
        public void Should_Reset_Bidi_Levels_Of_Trailing_Whitespaces_After_TextWrapping_2()
        {
            using (Start())
            {
                const string text = "אאא בבב";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var paragraphProperties = new GenericTextParagraphProperties(FlowDirection.LeftToRight, TextAlignment.Left, true,
                                                                             true, defaultProperties, TextWrapping.Wrap, 0, 0, 0);

                var textSource = new SimpleTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var firstLine = formatter.FormatLine(textSource, 0, 40, paragraphProperties);

                Assert.NotNull(firstLine);

                Assert.Equal(2, firstLine.TextRuns.Count);

                var first = firstLine.TextRuns[0] as ShapedTextRun;

                var second = firstLine.TextRuns[1] as ShapedTextRun;

                Assert.NotNull(first);

                Assert.NotNull(second);

                Assert.Equal("אאא", first.Text.ToString());

                Assert.Equal(" ", second.Text.ToString());

                Assert.Equal(1, first.BidiLevel);

                Assert.Equal(0, second.BidiLevel);
            }
        }

        [Fact]
        public void Should_Format_TextRuns_With_TextRunStyles()
        {
            using (Start())
            {
                const string text = "0123456789";

                var defaultProperties =
                    new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);

                var GenericTextRunPropertiesRuns = new[]
                {
                    new ValueSpan<TextRunProperties>(0, 3, defaultProperties),
                    new ValueSpan<TextRunProperties>(3, 3,
                        new GenericTextRunProperties(Typeface.Default, 13, foregroundBrush: Brushes.Black)),
                    new ValueSpan<TextRunProperties>(6, 3,
                        new GenericTextRunProperties(Typeface.Default, 14, foregroundBrush: Brushes.Black)),
                    new ValueSpan<TextRunProperties>(9, 1, defaultProperties)
                };

                var textSource = new FormattedTextSource(text, defaultProperties, GenericTextRunPropertiesRuns);

                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                    new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                Assert.Equal(text.Length, textLine.Length);

                for (var i = 0; i < GenericTextRunPropertiesRuns.Length; i++)
                {
                    var GenericTextRunPropertiesRun = GenericTextRunPropertiesRuns[i];

                    var textRun = textLine.TextRuns[i];

                    Assert.Equal(GenericTextRunPropertiesRun.Length, textRun.Length);
                }
            }
        }

        [Theory]
        [InlineData("0123", 1)]
        [InlineData("\r\n", 1)]
        [InlineData("👍b", 2)]
        [InlineData("a👍b", 3)]
        [InlineData("a👍子b", 4)]
        public void Should_Produce_Unique_Runs(string text, int numberOfRuns)
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                Assert.Equal(numberOfRuns, textLine.TextRuns.Count);
            }
        }

        [Fact]
        public void Should_Produce_A_Single_Fallback_Run()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                const string text = "👍 👍 👍 👍";

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                Assert.Equal(1, textLine.TextRuns.Count);
            }
        }

        [Fact]
        public void Should_Split_Run_On_Script()
        {
            using (Start())
            {
                const string text = "ABCDالدولي";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties));

                Assert.NotNull(textLine);

                var firstRun = textLine.TextRuns[0];

                Assert.Equal(4, firstRun.Length);
            }
        }

        [InlineData("𐐷𐐷𐐷𐐷𐐷", 10, 1)]
        [InlineData("01234 56789 01234 56789", 6, 4)]
        [Theory]
        public void Should_Wrap_With_Overflow(string text, int expectedCharactersPerLine, int expectedNumberOfLines)
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.WrapWithOverflow);

                var textSource = new SingleBufferTextSource("ABCDEFHFFHFJHKHFK", defaultProperties, true);

                var formatter = new TextFormatterImpl();

                formatter.FormatLine(textSource, 0, 33, paragraphProperties);

                textSource = new SingleBufferTextSource(text, defaultProperties);

                var numberOfLines = 0;

                var currentPosition = 0;

                while (currentPosition < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, currentPosition, 1, paragraphProperties);

                    Assert.NotNull(textLine);

                    if (text.Length - currentPosition > expectedCharactersPerLine)
                    {
                        Assert.Equal(expectedCharactersPerLine, textLine.Length);
                    }

                    currentPosition += textLine.Length;

                    numberOfLines++;
                }

                Assert.Equal(expectedNumberOfLines, numberOfLines);
            }
        }

        [Theory]
        [InlineData("one שתיים three ארבע", "one שתיים thr…", FlowDirection.LeftToRight, false)]
        [InlineData("one שתיים three ארבע", "…thrשתיים  one", FlowDirection.RightToLeft, false)]
        [InlineData("one שתיים three ארבע", "one שתיים…", FlowDirection.LeftToRight, true)]
        [InlineData("one שתיים three ארבע", "…שתיים  one", FlowDirection.RightToLeft, true)]
        public void TextTrimming_Should_Trim_Correctly(string text, string trimmed, FlowDirection direction, bool wordEllipsis)
        {
            const double Width = 160.0;
            const double EmSize = 20.0;

            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default, EmSize);

                var paragraphProperties = new GenericTextParagraphProperties(direction, TextAlignment.Start, true,
                                                                             true, defaultProperties, TextWrapping.NoWrap, 0, 0, 0);

                var textSource = new SimpleTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine = formatter.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

                Assert.NotNull(textLine);

                var textTrimming = wordEllipsis ? TextTrimming.WordEllipsis : TextTrimming.CharacterEllipsis;

                var collapsingProperties = textTrimming.CreateCollapsingProperties(new TextCollapsingCreateInfo(Width, defaultProperties, direction));

                var collapsedLine = textLine.Collapse(collapsingProperties);

                Assert.NotNull(collapsedLine);

                var trimmedResult = string.Concat(collapsedLine.TextRuns.Select(x => x.Text));

                Assert.Equal(trimmed, trimmedResult);
            }
        }

        [InlineData("Whether to turn off HTTPS. This option only applies if Individual, " +
                    "IndividualB2C, SingleOrg, or MultiOrg aren't used for &#8209;&#8209;auth."
            , "Noto Sans", 40)]
        [InlineData("01234 56789 01234 56789", "Noto Mono", 7)]
        [Theory]
        public void Should_Wrap(string text, string familyName, int numberOfCharactersPerLine)
        {
            using (Start())
            {
                var lineBreaker = new LineBreakEnumerator(text);

                var expected = new List<int>();

                while (lineBreaker.MoveNext(out var lineBreak))
                {
                    expected.Add(lineBreak.PositionWrap - 1);
                }

                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#" +
                                            familyName);

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var glyph = typeface.GlyphTypeface.GetGlyph('a');

                var advance = typeface.GlyphTypeface.GetGlyphAdvance(glyph) *
                              (12.0 / typeface.GlyphTypeface.Metrics.DesignEmHeight);

                var paragraphWidth = advance * numberOfCharactersPerLine;

                var currentPosition = 0;

                while (currentPosition < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, currentPosition, paragraphWidth,
                            new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.Wrap));

                    Assert.NotNull(textLine);

                    var end = textLine.FirstTextSourceIndex + textLine.Length - 1;

                    Assert.True(expected.Contains(end));

                    var index = expected.IndexOf(end);

                    for (var i = 0; i <= index; i++)
                    {
                        expected.RemoveAt(0);
                    }

                    currentPosition += textLine.Length;
                }
            }
        }

        [Fact]
        public void Should_Produce_Fixed_Height_Lines()
        {
            using (Start())
            {
                const string text = "012345";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, double.PositiveInfinity,
                        new GenericTextParagraphProperties(defaultProperties, lineHeight: 50));

                Assert.NotNull(textLine);

                Assert.Equal(50, textLine.Height);
            }
        }

        [Fact]
        public void Should_Not_Produce_TextLine_Wider_Than_ParagraphWidth()
        {
            using (Start())
            {
                const string text =
                    "Multiline TextBlock with TextWrapping.\r\rLorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                    "Vivamus magna. Cras in mi at felis aliquet congue. Ut a est eget ligula molestie gravida. Curabitur massa. " +
                    "Donec eleifend, libero at sagittis mollis, tellus est malesuada tellus, at luctus turpis elit sit amet quam. " +
                    "Vivamus pretium ornare est.";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.Wrap);

                var textSource = new SingleBufferTextSource(text, defaultProperties);

                var formatter = new TextFormatterImpl();

                var textSourceIndex = 0;

                while (textSourceIndex < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, textSourceIndex, 200, paragraphProperties);

                    Assert.NotNull(textLine);

                    Assert.True(textLine.Width <= 200);

                    textSourceIndex += textLine.Length;
                }
            }
        }

        [Fact]
        public void Wrap_Should_Not_Produce_Empty_Lines()
        {
            using (Start())
            {
                const string text = "012345";

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.Wrap);
                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                var textSourceIndex = 0;

                while (textSourceIndex < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, textSourceIndex, 3, paragraphProperties);

                    Assert.NotNull(textLine);

                    Assert.NotEqual(0, textLine.Length);

                    textSourceIndex += textLine.Length;
                }

                Assert.Equal(text.Length, textSourceIndex);
            }
        }

        [InlineData("Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor",
            new[] { "Lorem ipsum ", "dolor sit amet, ", "consectetur ", "adipisicing ", "elit, sed do ", "eiusmod tempor" })]

        [Theory]
        public void Should_Produce_Wrapped_And_Trimmed_Lines(string text, string[] expectedLines)
        {
            using (Start())
            {
                var typeface = Typeface.Default;

                var defaultProperties = new GenericTextRunProperties(typeface, 32, foregroundBrush: Brushes.Black);

                var styleSpans = new[]
                {
                    new ValueSpan<TextRunProperties>(0, 5,
                        new GenericTextRunProperties(typeface, 48)),
                    new ValueSpan<TextRunProperties>(6, 11,
                        new GenericTextRunProperties(new Typeface(FontFamily.Default, weight: FontWeight.Bold), 32)),
                    new ValueSpan<TextRunProperties>(28, 28,
                        new GenericTextRunProperties(new Typeface(FontFamily.Default, FontStyle.Italic),32))
                };

                var textSource = new FormattedTextSource(text, defaultProperties, styleSpans);

                var formatter = new TextFormatterImpl();

                var currentPosition = 0;

                var currentHeight = 0d;

                var currentLineIndex = 0;

                while (currentPosition < text.Length && currentLineIndex < expectedLines.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, currentPosition, 300,
                            new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.WrapWithOverflow));

                    Assert.NotNull(textLine);

                    currentPosition += textLine.Length;

                    if (textLine.Width > 300 || currentHeight + textLine.Height > 240)
                    {
                        textLine = textLine.Collapse(new TextTrailingWordEllipsis(TextTrimming.DefaultEllipsisChar, 300, defaultProperties, FlowDirection.LeftToRight));
                    }

                    currentHeight += textLine.Height;

                    var currentText = text.Substring(textLine.FirstTextSourceIndex, textLine.Length);

                    Assert.Equal(expectedLines[currentLineIndex], currentText);

                    currentLineIndex++;
                }

                Assert.Equal(expectedLines.Length, currentLineIndex);
            }
        }

        [InlineData("0123456789", TextAlignment.Left, FlowDirection.LeftToRight)]
        [InlineData("0123456789", TextAlignment.Center, FlowDirection.LeftToRight)]
        [InlineData("0123456789", TextAlignment.Right, FlowDirection.LeftToRight)]

        [InlineData("0123456789", TextAlignment.Left, FlowDirection.RightToLeft)]
        [InlineData("0123456789", TextAlignment.Center, FlowDirection.RightToLeft)]
        [InlineData("0123456789", TextAlignment.Right, FlowDirection.RightToLeft)]

        [InlineData("שנבגק", TextAlignment.Left, FlowDirection.RightToLeft)]
        [InlineData("שנבגק", TextAlignment.Center, FlowDirection.RightToLeft)]
        [InlineData("שנבגק", TextAlignment.Right, FlowDirection.RightToLeft)]

        [Theory]
        public void Should_Align_TextLine(string text, TextAlignment textAlignment, FlowDirection flowDirection)
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var paragraphProperties = new GenericTextParagraphProperties(flowDirection, textAlignment, true, true,
                    defaultProperties, TextWrapping.NoWrap, 0, 0, 0);

                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, 100, paragraphProperties);

                Assert.NotNull(textLine);

                var expectedOffset = 0d;

                switch (textAlignment)
                {
                    case TextAlignment.Center:
                        expectedOffset = 50 - textLine.Width / 2;
                        break;
                    case TextAlignment.Right:
                        expectedOffset = 100 - textLine.WidthIncludingTrailingWhitespace;
                        break;
                }

                Assert.Equal(expectedOffset, textLine.Start);
            }
        }

        [Fact]
        public void Should_Wrap_Syriac()
        {
            using (Start())
            {
                const string text =
                    "܀ ܁ ܂ ܃ ܄ ܅ ܆ ܇ ܈ ܉ ܊ ܋ ܌ ܍ ܏ ܐ ܑ ܒ ܓ ܔ ܕ ܖ ܗ ܘ ܙ ܚ ܛ ܜ ܝ ܞ ܟ ܠ ܡ ܢ ܣ ܤ ܥ ܦ ܧ ܨ ܩ ܪ ܫ ܬ ܰ ܱ ܲ ܳ ܴ ܵ ܶ ܷ ܸ ܹ ܺ ܻ ܼ ܽ ܾ ܿ ݀ ݁ ݂ ݃ ݄ ݅ ݆ ݇ ݈ ݉ ݊";
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var paragraphProperties =
                    new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.Wrap);

                var textSource = new SingleBufferTextSource(text, defaultProperties);
                var formatter = new TextFormatterImpl();

                var textPosition = 87;
                TextLineBreak? lastBreak = null;

                while (textPosition < text.Length)
                {
                    var textLine =
                        formatter.FormatLine(textSource, textPosition, 50, paragraphProperties, lastBreak);

                    Assert.NotNull(textLine);

                    Assert.Equal(textLine.Length, textLine.TextRuns.Sum(x => x.Length));

                    textPosition += textLine.Length;

                    lastBreak = textLine.TextLineBreak;
                }
            }
        }

        [Fact]
        public void Should_FormatLine_With_Emergency_Breaks()
        {
            using (Start())
            {
                var defaultProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.Wrap);

                var textSource = new SingleBufferTextSource("0123456789_0123456789_0123456789_0123456789", defaultProperties);
                var formatter = new TextFormatterImpl();

                var textLine =
                    formatter.FormatLine(textSource, 0, 33, paragraphProperties);

                Assert.NotNull(textLine);

                var remainingRunsLineBreak = Assert.IsType<WrappingTextLineBreak>(textLine.TextLineBreak);
                var remainingRuns = remainingRunsLineBreak.AcquireRemainingRuns();
                Assert.NotNull(remainingRuns);
                Assert.NotEmpty(remainingRuns);
            }
        }

        [InlineData("פעילות הבינאום, W3C!")]
        [InlineData("abcABC")]
        [InlineData("זה כיף סתם לשמוע איך תנצח קרפד עץ טוב בגן")]
        [InlineData("טטטט abcDEF טטטט")]
        [Theory]
        public void Should_Not_Alter_TextRuns_After_TextStyles_Were_Applied(string text)
        {
            using (Start())
            {
                var formatter = new TextFormatterImpl();

                var defaultProperties = new GenericTextRunProperties(Typeface.Default);

                var paragraphProperties =
                    new GenericTextParagraphProperties(defaultProperties, textWrap: TextWrapping.NoWrap);

                var foreground = new SolidColorBrush(Colors.Red).ToImmutable();

                var expectedTextLine = formatter.FormatLine(new SingleBufferTextSource(text, defaultProperties),
                    0, double.PositiveInfinity, paragraphProperties);

                Assert.NotNull(expectedTextLine);

                var expectedRuns = expectedTextLine.TextRuns.Cast<ShapedTextRun>().ToList();

                var expectedGlyphs = expectedRuns
                    .SelectMany(run => run.GlyphRun.GlyphInfos, (_, glyph) => glyph.GlyphIndex)
                    .ToList();

                for (var i = 0; i < text.Length; i++)
                {
                    for (var j = 1; i + j < text.Length; j++)
                    {
                        var spans = new[]
                        {
                            new ValueSpan<TextRunProperties>(i, j,
                                new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: foreground))
                        };

                        var textSource = new FormattedTextSource(text, defaultProperties, spans);

                        var textLine =
                            formatter.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

                        Assert.NotNull(textLine);

                        var shapedRuns = textLine.TextRuns.Cast<ShapedTextRun>().ToList();

                        var actualGlyphs = shapedRuns
                            .SelectMany(x => x.GlyphRun.GlyphInfos, (_, glyph) => glyph.GlyphIndex)
                            .ToList();

                        Assert.Equal(expectedGlyphs, actualGlyphs);
                    }
                }
            }
        }

        [Fact]
        public void Should_FormatLine_With_DrawableRuns()
        {
            var defaultRunProperties = new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black);
            var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties);
            var textSource = new CustomTextSource("Hello World ->");

            using (Start())
            {
                var textLine =
                    TextFormatter.Current.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

                Assert.NotNull(textLine);

                Assert.Equal(3, textLine.TextRuns.Count);

                Assert.True(textLine.TextRuns[1] is RectangleRun);
            }
        }

        [Fact]
        public void Should_Format_With_EndOfLineRun()
        {
            using (Start())
            {
                var defaultRunProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties);
                var textSource = new EndOfLineTextSource();

                var textLine =
                    TextFormatter.Current.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

                Assert.NotNull(textLine);

                Assert.NotNull(textLine.TextLineBreak);

                Assert.Equal(TextRun.DefaultTextSourceLength, textLine.Length);
            }
        }

        [Fact]
        public void Should_Return_Null_For_Empty_TextSource()
        {
            using (Start())
            {
                var defaultRunProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties);
                var textSource = new EmptyTextSource();

                var textLine = TextFormatter.Current.FormatLine(textSource, 0, double.PositiveInfinity, paragraphProperties);

                Assert.Null(textLine);
            }
        }

        [Fact]
        public void Should_Retain_TextEndOfParagraph_With_TextWrapping()
        {
            using (Start())
            {
                var defaultRunProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties, textWrap: TextWrapping.Wrap);

                var text = "Hello World";

                var textSource = new SimpleTextSource(text, defaultRunProperties);

                var pos = 0;

                TextLineBreak? previousLineBreak = null;
                TextLine? textLine = null;

                while (pos < text.Length)
                {
                    textLine = TextFormatter.Current.FormatLine(textSource, pos, 30, paragraphProperties, previousLineBreak);

                    Assert.NotNull(textLine);

                    pos += textLine.Length;

                    previousLineBreak = textLine.TextLineBreak;
                }

                Assert.NotNull(textLine);
                Assert.NotNull(textLine.TextLineBreak);
                Assert.NotNull(textLine.TextLineBreak.TextEndOfLine);
            }
        }
        
        [Fact]
        public void Should_HitTestStringWithInvisibleRuns()
        {
            var defaultRunProperties = new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black);
            var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties);
            //var textSource = new ListTextSource(

            

            using (Start())
            {
                var hello = new TextCharacters("Hello",
                    new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black));
                var world = new TextCharacters("world",
                    new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Red));

                var source = new ListTextSource(new InvisibleRun(1), hello, new InvisibleRun(1), world);
                
                var textLine =
                    TextFormatter.Current.FormatLine(source, 0, double.PositiveInfinity, paragraphProperties);

                Assert.NotNull(textLine);

                void VerifyHit(int offset)
                {
                    var glyphCenter = textLine.GetTextBounds(offset, 1)[0].Rectangle.Center;
                    var hit = textLine.GetCharacterHitFromDistance(glyphCenter.X);
                    Assert.Equal(offset, hit.FirstCharacterIndex);
                }
                VerifyHit(3);
                VerifyHit(8);
            }
        }
        
        [Fact]
        public void GetTextBounds_For_TextLine_With_ZeroWidthSpaces_Does_Not_Freeze()
        {
            var defaultRunProperties = new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black);
            var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties);

            using (Start())
            {
                var text = new TextCharacters("\u200B\u200B",
                    new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black));

                var source = new ListTextSource(text, new InvisibleRun(1), new TextEndOfParagraph());
                
                var textLine =
                    TextFormatter.Current.FormatLine(source, 0, double.PositiveInfinity, paragraphProperties);

                Assert.NotNull(textLine);

                var bounds = textLine.GetTextBounds(0, 3);

                Assert.Equal(1, bounds.Count);

                var runBounds = bounds[0].TextRunBounds;

                Assert.Equal(2, runBounds.Count);
            }
        }

        
        [Theory]
        [InlineData(TextWrapping.NoWrap),InlineData(TextWrapping.Wrap),InlineData(TextWrapping.WrapWithOverflow)]
        public void Line_Formatting_For_Oversized_Embedded_Runs_Does_Not_Produce_Empty_Lines(TextWrapping wrapping)
        {
            var defaultRunProperties = new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black);
            var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties,
                textWrap: wrapping);

            using (Start())
            {
                var source = new ListTextSource(new RectangleRun(new Rect(0, 0, 200, 10), Brushes.Aqua));
                var textLine = TextFormatter.Current.FormatLine(source, 0, 100, paragraphProperties);
                Assert.NotNull(textLine);
                Assert.Equal(200d, textLine.WidthIncludingTrailingWhitespace);
            }
        }
        
        [Theory]
        [InlineData(TextWrapping.NoWrap),InlineData(TextWrapping.Wrap),InlineData(TextWrapping.WrapWithOverflow)]
        public void Line_Formatting_For_Oversized_Embedded_Runs_Inside_Normal_Text_Does_Not_Produce_Empty_Lines(
            TextWrapping wrapping)
        {
            var defaultRunProperties = new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black);
            var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties,
                textWrap: wrapping);
            
            using (Start())
            {
                var typeface = new Typeface(FontFamily.Parse("resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#DejaVu Sans"));
                
                var text1 = new TextCharacters("Hello",
                    new GenericTextRunProperties(typeface, foregroundBrush: Brushes.Black));
                var text2 = new TextCharacters("world",
                    new GenericTextRunProperties(typeface, foregroundBrush: Brushes.Black));
                
                var source = new ListTextSource(
                    text1,
                    new RectangleRun(new Rect(0, 0, 200, 10), Brushes.Aqua),
                    new InvisibleRun(1),
                    new TextEndOfLine(),
                    text2,
                    new TextEndOfParagraph(1));

                var lines = new List<TextLine>();
                var dcp = 0;
                for (var c = 0;; c++)
                {
                    Assert.True(c < 1000, "Infinite loop");
                    var textLine = TextFormatter.Current.FormatLine(source, dcp, 30, paragraphProperties);
                    Assert.NotNull(textLine);
                    lines.Add(textLine);
                    dcp += textLine.Length;
                    
                    if (textLine.TextLineBreak is {} eol && eol.TextEndOfLine is TextEndOfParagraph)
                        break;
                }

                Assert.NotEmpty(lines);
            }
        }

        class IncrementalTabProperties : TextParagraphProperties
        {
            public IncrementalTabProperties(TextRunProperties defaultTextRunProperties)
            {
                DefaultTextRunProperties = defaultTextRunProperties;
            }

            public override FlowDirection FlowDirection => default;
            public override TextAlignment TextAlignment => default;
            public override double LineHeight => default;
            public override bool FirstLineInParagraph => default;
            public override TextRunProperties DefaultTextRunProperties { get; }
            public override TextWrapping TextWrapping => default;
            public override double Indent => default;
            public override double DefaultIncrementalTab => 64;
        }
        
        [Fact]
        public void Line_With_IncrementalTab_Should_Return_Correct_Backspace_Position()
        {
            using (Start())
            {
                var typeface = new Typeface(FontFamily.Parse("resm:Avalonia.Skia.UnitTests.Fonts?assembly=Avalonia.Skia.UnitTests#DejaVu Sans"));
                
                var defaultRunProperties = new GenericTextRunProperties(typeface, foregroundBrush: Brushes.Black);
                var paragraphProperties = new IncrementalTabProperties(defaultRunProperties);

                var text = new TextCharacters("ff",
                    new GenericTextRunProperties(typeface, foregroundBrush: Brushes.Black));
                
                var source = new ListTextSource(text);
                
                var textLine = TextFormatter.Current.FormatLine(source, 0, double.PositiveInfinity, paragraphProperties);
                Assert.NotNull(textLine);
                
                var backspaceHit = textLine.GetBackspaceCaretCharacterHit(new CharacterHit(2));
                Assert.Equal(1, backspaceHit.FirstCharacterIndex);
                Assert.Equal(0, backspaceHit.TrailingLength);
            }
        }

        [Fact]
        public void Should_Wrap_Chinese()
        {
            using (Start())
            {
                var defaultRunProperties = new GenericTextRunProperties(Typeface.Default);
                var paragraphProperties = new GenericTextParagraphProperties(defaultRunProperties, textWrap: TextWrapping.Wrap);

                var text = "一二三四 TEXT 一二三四五六七八九十零";

                var textLine = TextFormatter.Current.FormatLine(new SimpleTextSource(text, defaultRunProperties), 0, 120, paragraphProperties);

                Assert.NotNull(textLine);
                Assert.Equal(3, textLine.TextRuns.Count);
            }
        }

        protected readonly record struct SimpleTextSource : ITextSource
        {
            private readonly string _text;
            private readonly TextRunProperties _defaultProperties;

            public SimpleTextSource(string text, TextRunProperties defaultProperties)
            {
                _text = text;
                _defaultProperties = defaultProperties;
            }

            public TextRun? GetTextRun(int textSourceIndex)
            {
                if (textSourceIndex > _text.Length)
                {
                    return new TextEndOfParagraph();
                }

                var runText = _text.AsMemory(textSourceIndex);

                if (runText.IsEmpty)
                {
                    return new TextEndOfParagraph();
                }

                return new TextCharacters(runText, _defaultProperties);
            }
        }

        private class EmptyTextSource : ITextSource
        {
            public TextRun? GetTextRun(int textSourceIndex)
            {
                return null;
            }
        }

        private class EndOfLineTextSource : ITextSource
        {
            public TextRun GetTextRun(int textSourceIndex)
            {
                return new TextEndOfLine();
            }
        }

        private class CustomTextSource : ITextSource
        {
            private readonly string _text;

            public CustomTextSource(string text)
            {
                _text = text;
            }

            public TextRun? GetTextRun(int textSourceIndex)
            {
                if (textSourceIndex >= _text.Length + TextRun.DefaultTextSourceLength + _text.Length)
                {
                    return null;
                }

                if (textSourceIndex == _text.Length)
                {
                    return new RectangleRun(new Rect(0, 0, 50, 50), Brushes.Green);
                }

                return new TextCharacters(_text, new GenericTextRunProperties(Typeface.Default, foregroundBrush: Brushes.Black));
            }
        }
        
        private class ListTextSource : ITextSource
        {
            private readonly List<TextRun> _runs;

            public ListTextSource(params TextRun[] runs) : this((IEnumerable<TextRun>)runs)
            {
                
            }
            
            public ListTextSource(IEnumerable<TextRun> runs)
            {
                _runs = runs.ToList();
            }
            
            public TextRun? GetTextRun(int textSourceIndex)
            {
                var off = 0;
                for (var c = 0; c < _runs.Count; c++)
                {
                    var run = _runs[c];
                    if (textSourceIndex >= off && textSourceIndex - off < run.Length)
                    {
                        if (run.Length == 1)
                            return run;
                        var chars = ((TextCharacters)run);
                        return new TextCharacters(chars.Text.Slice(textSourceIndex - off), chars.Properties);
                    }

                    off += run.Length;
                }

                return null;
            }
        }

        private class RectangleRun : DrawableTextRun
        {
            private readonly Rect _rect;
            private readonly IBrush _fill;

            public RectangleRun(Rect rect, IBrush fill)
            {
                _rect = rect;
                _fill = fill;
            }

            public override Size Size => _rect.Size;
            public override double Baseline => 0;
            public override void Draw(DrawingContext drawingContext, Point origin)
            {
                using (drawingContext.PushTransform(Matrix.CreateTranslation(new Vector(origin.X, 0))))
                {
                    drawingContext.FillRectangle(_fill, _rect);
                }
            }
        }
        
        private class InvisibleRun : TextRun
        {
            public InvisibleRun(int length)
            {
                Length = length;
            }
            public override int Length { get; }
        }

        public static IDisposable Start()
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(),
                    textShaperImpl: new TextShaperImpl()));

            AvaloniaLocator.CurrentMutable
                .Bind<FontManager>().ToConstant(new FontManager(new CustomFontManagerImpl()));

            return disposable;
        }
    }
}
