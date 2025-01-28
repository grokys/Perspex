using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{

    public class FormattedTextSourceTests
    {
        [Fact]
        public void GetTextRun_WithTwoTextStyleOverrides_ShouldGenerateCorrectFirstRun()
        {
            //Prepare a sample text: The two "He" at the beginning of each line should be displayed with other TextRunProperties
            string text = "Hello World\r\nHello";
            Typeface typeface = new Typeface();
            GenericTextRunProperties defaultTextRunProperties = new GenericTextRunProperties(typeface);
            IReadOnlyList<ValueSpan<TextRunProperties>> textStyleOverrides = new List<ValueSpan<TextRunProperties>>()
            {
                new ValueSpan<TextRunProperties>(0, 2, new GenericTextRunProperties(typeface, backgroundBrush: Brushes.Aqua)),
                new ValueSpan<TextRunProperties>(13, 2, new GenericTextRunProperties(typeface, backgroundBrush: Brushes.Aqua)),
            };

            FormattedTextSource textSource = new FormattedTextSource(text, defaultTextRunProperties, textStyleOverrides);
            TextRun textRun = textSource.GetTextRun(0);

            Assert.Equal(2, textRun.Length);
            Assert.Equal("He", textRun.Text.ToString());
        }
    }
}
