﻿using System;
using System.Linq;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public class GlyphRunPage : UserControl
    {
        public GlyphRunPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void Render(DrawingContext drawingContext)
        {
            var glyphTypeface = Typeface.Default.GlyphTypeface;

            var bytes = Encoding.UTF32.GetBytes("1234567890");

            var codePoints = new int[bytes.Length / 4];

            Buffer.BlockCopy(bytes, 0, codePoints, 0, bytes.Length);

            var glyphs = glyphTypeface.GetGlyphs(codePoints);

            var baselineOrigin = new Point(Bounds.X, Bounds.Y);

            for (var i = 18; i < 30; i++)
            {
                var scale = (float)i / glyphTypeface.DesignEmHeight;

                baselineOrigin += new Point(0, i * 1.5f);

                var advances = Enumerable.Repeat(i * 0.6f, 10).ToArray();

                var offsets = new Vector[10];

                var offsetY = 0.0d;

                for (var j = 0; j < 5; j++)
                {
                    offsets[j] = new Vector(0, offsetY++ * i * 0.06);
                }

                for (var j = 5; j < 10; j++)
                {
                    offsets[j] = new Vector(0, offsetY-- * i * 0.06);
                }

                var glyphRun = new GlyphRun(glyphTypeface, i, baselineOrigin, glyphs, advances, offsets);

                drawingContext.DrawGlyphRun(Brushes.Black, glyphRun);

                var overline = baselineOrigin + new Point(0, glyphTypeface.Ascent * scale);

                drawingContext.DrawLine(new Pen(Brushes.Red), overline, overline + new Point(glyphRun.Size.Width, 0));

                drawingContext.DrawLine(new Pen(Brushes.Transparent), overline, overline - new Point(glyphRun.Size.Width, 0));

                drawingContext.DrawLine(new Pen(Brushes.Blue), baselineOrigin, baselineOrigin + new Point(glyphRun.Size.Width, 0));

                drawingContext.DrawLine(new Pen(Brushes.Transparent), baselineOrigin, baselineOrigin - new Point(glyphRun.Size.Width, 0));

                var underline = baselineOrigin + new Point(0, (glyphTypeface.Descent + glyphTypeface.LineGap) * scale);

                drawingContext.DrawLine(new Pen(Brushes.Green), underline, underline + new Point(glyphRun.Size.Width, 0));

                drawingContext.DrawLine(new Pen(Brushes.Transparent), underline, underline - new Point(glyphRun.Size.Width, 0));
            }
        }
    }
}
