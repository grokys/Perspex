using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Media
#endif
{
    public class RadialGradientBrushTests : TestBase
    {
        public RadialGradientBrushTests() : base(@"Media\RadialGradientBrush")
        {
        }

        [Fact]
        public async Task RadialGradientBrush_Partial_Cover()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new RadialGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.White, Offset = 0 },
                            new GradientStop { Color = Color.Parse("#00DD00"), Offset = 0.7 }
                        },
                        GradientOrigin = new RelativePoint(0.7, 0.15, RelativeUnit.Relative)
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task RadialGradientBrush_RedBlue()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new RadialGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        }
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        /// <summary>
        /// Tests using a GradientOrigin that falls inside of the circle described by Center/Radius.
        /// </summary>
        [Fact]
        public async Task RadialGradientBrush_RedBlue_Offset_Inside()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new RadialGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        },
                        GradientOrigin = new RelativePoint(0.25, 0.25, RelativeUnit.Relative)
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        /// <summary>
        /// Tests using a GradientOrigin that falls outside of the circle described by Center/Radius.
        /// </summary>
        [Fact]
        public async Task RadialGradientBrush_RedBlue_Offset_Outside()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new RadialGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        },
                        GradientOrigin = new RelativePoint(0.1, 0.1, RelativeUnit.Relative)
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        /// <summary>
        /// Tests using a GradientOrigin that falls inside of the circle described by Center/Radius.
        /// </summary>
        [Fact]
        public async Task RadialGradientBrush_RedGreenBlue_Offset_Inside()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new RadialGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Green, Offset = 0.5 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        },
                        GradientOrigin = new RelativePoint(0.25, 0.25, RelativeUnit.Relative),
                        Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                        RadiusX = RelativeScalar.Middle,
                        RadiusY = RelativeScalar.Middle
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        /// <summary>
        /// Tests using a GradientOrigin that falls outside of the circle described by Center/Radius.
        /// </summary>
        [Fact]
        public async Task RadialGradientBrush_RedGreenBlue_Offset_Outside()
        {
            Decorator target = new Decorator
            {
                Padding = new Thickness(8),
                Width = 200,
                Height = 200,
                Child = new Border
                {
                    Background = new RadialGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0 },
                            new GradientStop { Color = Colors.Green, Offset = 0.25 },
                            new GradientStop { Color = Colors.Blue, Offset = 1 }
                        },
                        GradientOrigin = new RelativePoint(0.1, 0.1, RelativeUnit.Relative),
                        Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                        RadiusX = RelativeScalar.Middle,
                        RadiusY = RelativeScalar.Middle
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }

        [Fact]
        public async Task RadialGradientBrush_DrawingContext()
        {
            var brush = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop { Color = Colors.Red, Offset = 0 },
                    new GradientStop { Color = Colors.Blue, Offset = 1 }
                }
            };

            Decorator target = new Decorator
            {
                Width = 200,
                Height = 200,
                Child = new DrawnControl(c =>
                {
                    c.DrawRectangle(brush, null, new Rect(0, 0, 100, 100));
                    using (c.PushTransform(Matrix.CreateTranslation(100, 100)))
                        c.DrawRectangle(brush, null, new Rect(0, 0, 100, 100));
                }),
            };

            await RenderToFile(target);
            CompareImages();
        }

        private class DrawnControl : Control
        {
            private readonly Action<DrawingContext> _render;
            public DrawnControl(Action<DrawingContext> render) => _render = render;
            public override void Render(DrawingContext context) => _render(context);
        }
        
        
        [Theory,
         InlineData(false, false),
         InlineData(false, true),
         InlineData(true, false),
         InlineData(true, true),
        ]
        public async Task RadialGradientBrush_Is_Properly_Mapped(bool relative, bool moveOrigin)
        {
            var center = relative ? RelativePoint.Center : new RelativePoint(128, 128, RelativeUnit.Absolute);
            var brush = new RadialGradientBrush
            {
                Center = center,
                GradientStops =
                {
                    new GradientStop { Color = Colors.Red, Offset = 0 },
                    new GradientStop { Color = Colors.Blue, Offset = 1 }
                },
                SpreadMethod = moveOrigin ? GradientSpreadMethod.Pad : GradientSpreadMethod.Repeat,
                RadiusX = relative ? RelativeScalar.Middle : new RelativeScalar(128, RelativeUnit.Absolute),
                RadiusY = relative ? RelativeScalar.Middle : new RelativeScalar(64, RelativeUnit.Absolute),
                GradientOrigin = moveOrigin
                    ? (relative
                        ? new RelativePoint(0.1, 0.1, RelativeUnit.Relative)
                        : new RelativePoint(32, 32, RelativeUnit.Absolute))
                    : center
            };

            var testName =
                $"{nameof(RadialGradientBrush_Is_Properly_Mapped)}_{(relative ? "Relative" : "Absolute")}_{(moveOrigin ? "MovedOrigin" : "CenterOrigin")}";
            await RenderToFile(new RelativePointTestPrimitivesHelper(brush, !relative), testName);
            CompareImages(testName);
        }


        [Theory(
#if !AVALONIA_SKIA
            Skip = "Direct2D backend doesn't seem to support brush transforms while Direct2D is certainly capable of doing that. I'm not fixing it in this PR however"
#endif
            ),
         InlineData(false),
         InlineData(true)
        ]
        public async Task RadialGradientBrush_With_Different_Radius_Is_Properly_Rotated(bool moveOrigin)
        {
            var brush = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop { Color = Colors.Red, Offset = 0 },
                    new GradientStop { Color = Colors.Blue, Offset = 1 }
                },
                GradientOrigin = moveOrigin ? new RelativePoint(0.1, 0.1, RelativeUnit.Relative) : RelativePoint.Center,
                RadiusY = new RelativeScalar(0.25, RelativeUnit.Relative),
                Transform = new RotateTransform(45),
                TransformOrigin = RelativePoint.Center
            };

            var testName =
                $"{nameof(RadialGradientBrush_With_Different_Radius_Is_Properly_Rotated)}_{(moveOrigin ? "MovedOrigin" : "CenterOrigin")}";
            
            await RenderToFile(new Border()
            {
                Background = brush,
                Width = 256,
                Height = 256,
                MinHeight = 256
            }, testName);
            CompareImages(testName);
        }
    }
}
