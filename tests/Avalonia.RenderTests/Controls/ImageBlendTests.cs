using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Controls
#endif
{
    public class ImageBlendTests : TestBase
    {
        private readonly Bitmap _bitmapBase;
        private readonly Bitmap _bitmapOver;

        public ImageBlendTests()
            : base(@"Controls\Image\blend")
        {
            _bitmapBase = new Bitmap(Path.Combine(OutputPath, "Cat.jpg"));
            _bitmapOver = new Bitmap(Path.Combine(OutputPath, "ColourShading - by Stib.png"));
        }

        [Fact]
        public async Task Image_Blend_Nothing() => await TestBlendMode(BitmapBlendingMode.Unspecified);
        [Fact]
        public async Task Image_Blend_Plus() => await TestBlendMode(BitmapBlendingMode.Plus);
        [Fact]
        public async Task Image_Blend_Screen() => await TestBlendMode(BitmapBlendingMode.Screen);
        [Fact]
        public async Task Image_Blend_Overlay() => await TestBlendMode(BitmapBlendingMode.Overlay);
        [Fact]
        public async Task Image_Blend_Darken() => await TestBlendMode(BitmapBlendingMode.Darken);
        [Fact]
        public async Task Image_Blend_Lighten() => await TestBlendMode(BitmapBlendingMode.Lighten);
        [Fact]
        public async Task Image_Blend_ColorDodge() => await TestBlendMode(BitmapBlendingMode.ColorDodge);
        [Fact]
        public async Task Image_Blend_ColorBurn() => await TestBlendMode(BitmapBlendingMode.ColorBurn);
        [Fact]
        public async Task Image_Blend_HardLight() => await TestBlendMode(BitmapBlendingMode.HardLight);
        [Fact]
        public async Task Image_Blend_SoftLight() => await TestBlendMode(BitmapBlendingMode.SoftLight);
        [Fact]
        public async Task Image_Blend_Difference() => await TestBlendMode(BitmapBlendingMode.Difference);
        [Fact]
        public async Task Image_Blend_Exclusion() => await TestBlendMode(BitmapBlendingMode.Exclusion);
        [Fact]
        public async Task Image_Blend_Multiply() => await TestBlendMode(BitmapBlendingMode.Multiply);
        [Fact]
        public async Task Image_Blend_Hue() => await TestBlendMode(BitmapBlendingMode.Hue);
        [Fact]
        public async Task Image_Blend_Saturation() => await TestBlendMode(BitmapBlendingMode.Saturation);
        [Fact]
        public async Task Image_Blend_Color() => await TestBlendMode(BitmapBlendingMode.Color);
        [Fact]
        public async Task Image_Blend_Luminosity() => await TestBlendMode(BitmapBlendingMode.Luminosity);

        private async Task TestBlendMode(BitmapBlendingMode blendMode, [CallerMemberName] string testName = "")
        {
            var panel = new Panel();
            panel.Children.Add(new Image() { Source = _bitmapBase });
            panel.Children.Add(new Image() { Source = _bitmapOver, BlendMode = blendMode });

            var target = new Decorator
            {
                Width = 512,
                Height = 512,
                Child = new Border
                {
                    Background = Brushes.Red,
                    Child = panel
                }
            };

            await RenderToFile(target,testName);
            CompareImages(testName);
        }
    }
}
