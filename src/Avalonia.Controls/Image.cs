using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;

namespace Avalonia.Controls
{   
    /// <summary>
    /// Displays a <see cref="Bitmap"/> image.
    /// </summary>
    public class Image : Control
    {
        /// <summary>
        /// Defines the <see cref="Source"/> property.
        /// </summary>
        public static readonly StyledProperty<IImage?> SourceProperty =
            AvaloniaProperty.Register<Image, IImage?>(nameof(Source));
        
        /// <summary>
        /// Defines the <see cref="BlendMode"/> property.
        /// </summary>
        public static readonly StyledProperty<BitmapBlendingMode> BlendModeProperty =
            AvaloniaProperty.Register<Image, BitmapBlendingMode>(nameof(BlendMode));

        /// <summary>
        /// Defines the <see cref="Stretch"/> property.
        /// </summary>
        public static readonly StyledProperty<Stretch> StretchProperty =
            AvaloniaProperty.Register<Image, Stretch>(nameof(Stretch), Stretch.Uniform);

        /// <summary>
        /// Defines the <see cref="StretchDirection"/> property.
        /// </summary>
        public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
            AvaloniaProperty.Register<Image, StretchDirection>(
                nameof(StretchDirection),
                StretchDirection.Both);

        static Image()
        {
            AffectsRender<Image>(SourceProperty, StretchProperty, StretchDirectionProperty, BlendModeProperty);
            AffectsMeasure<Image>(SourceProperty, StretchProperty, StretchDirectionProperty);
            AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<Image>(AutomationControlType.Image);
        }

        /// <summary>
        /// Gets or sets the image that will be displayed.
        /// </summary>
        [Content]
        public IImage? Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the blend mode for the image.
        /// </summary>
        public BitmapBlendingMode BlendMode
        {
            get => GetValue(BlendModeProperty);
            set => SetValue(BlendModeProperty, value);
        }

        /// <summary>
        /// Gets or sets a value controlling how the image will be stretched.
        /// </summary>
        public Stretch Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        /// <summary>
        /// Gets or sets a value controlling in what direction the image will be stretched.
        /// </summary>
        public StretchDirection StretchDirection
        {
            get => GetValue(StretchDirectionProperty);
            set => SetValue(StretchDirectionProperty, value);
        }

        /// <inheritdoc />
        protected override bool BypassFlowDirectionPolicies => true;
        
        /// <summary>
        /// Renders the control.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public sealed override void Render(DrawingContext context)
        {
            var source = Source;

            if (source != null && Bounds.Width > 0 && Bounds.Height > 0)
            {
                Rect viewPort = new Rect(Bounds.Size);
                Size sourceSize = source.Size;

                Vector scale = Stretch.CalculateScaling(Bounds.Size, sourceSize, StretchDirection);
                Size scaledSize = sourceSize * scale;
                Rect destRect = viewPort
                    .CenterRect(new Rect(scaledSize))
                    .Intersect(viewPort);
                Rect sourceRect = new Rect(sourceSize)
                    .CenterRect(new Rect(destRect.Size / scale));

                using (context.PushRenderOptions(RenderOptions with { BitmapBlendingMode = BlendMode }))
                {
                    context.DrawImage(source, sourceRect, destRect);
                }
            }
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var source = Source;
            var result = new Size();

            if (source != null)
            {
                result = Stretch.CalculateSize(availableSize, source.Size, StretchDirection);
            }

            return result;
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var source = Source;

            if (source != null)
            {
                var sourceSize = source.Size;
                var result = Stretch.CalculateSize(finalSize, sourceSize);
                return result;
            }
            else
            {
                return new Size();
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ImageAutomationPeer(this);
        }
    }
}
