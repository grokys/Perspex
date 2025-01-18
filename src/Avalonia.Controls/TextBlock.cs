using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control that displays a block of text.
    /// </summary>
    public class TextBlock : Control, IInlineHost
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="Padding"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> PaddingProperty =
            Decorator.PaddingProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="FontFamily"/> property.
        /// </summary>
        public static readonly StyledProperty<FontFamily> FontFamilyProperty =
            TextElement.FontFamilyProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="FontSize"/> property.
        /// </summary>
        public static readonly StyledProperty<double> FontSizeProperty =
            TextElement.FontSizeProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="FontStyle"/> property.
        /// </summary>
        public static readonly StyledProperty<FontStyle> FontStyleProperty =
            TextElement.FontStyleProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly StyledProperty<FontWeight> FontWeightProperty =
            TextElement.FontWeightProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="FontStretch"/> property.
        /// </summary>
        public static readonly StyledProperty<FontStretch> FontStretchProperty =
            TextElement.FontStretchProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="Foreground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> ForegroundProperty =
            TextElement.ForegroundProperty.AddOwner<TextBlock>();

        /// <summary>
        /// DependencyProperty for <see cref="BaselineOffset" /> property.
        /// </summary>
        public static readonly AttachedProperty<double> BaselineOffsetProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, double>(
                nameof(BaselineOffset),
                0,
                true);

        /// <summary>
        /// Defines the <see cref="LineHeight"/> property.
        /// </summary>
        public static readonly AttachedProperty<double> LineHeightProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, double>(
                nameof(LineHeight),
                double.NaN,
                validate: IsValidLineHeight,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="LineSpacing"/> property.
        /// </summary>
        public static readonly AttachedProperty<double> LineSpacingProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, double>(
                nameof(LineSpacing),
                0,
                validate: IsValidLineSpacing,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="LetterSpacing"/> property.
        /// </summary>
        public static readonly AttachedProperty<double> LetterSpacingProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, double>(
                nameof(LetterSpacing),
                0,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="MaxLines"/> property.
        /// </summary>
        public static readonly AttachedProperty<int> MaxLinesProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, int>(
                nameof(MaxLines),
                validate: IsValidMaxLines,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> TextProperty =
            AvaloniaProperty.Register<TextBlock, string?>(nameof(Text));

        /// <summary>
        /// Defines the <see cref="TextAlignment"/> property.
        /// </summary>
        public static readonly AttachedProperty<TextAlignment> TextAlignmentProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, TextAlignment>(
                nameof(TextAlignment),
                defaultValue: TextAlignment.Start,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="TextWrapping"/> property.
        /// </summary>
        public static readonly AttachedProperty<TextWrapping> TextWrappingProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, TextWrapping>(nameof(TextWrapping),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="TextTrimming"/> property.
        /// </summary>
        public static readonly AttachedProperty<TextTrimming> TextTrimmingProperty =
            AvaloniaProperty.RegisterAttached<TextBlock, Control, TextTrimming>(nameof(TextTrimming),
                defaultValue: TextTrimming.None,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="TextDecorations"/> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationCollection?> TextDecorationsProperty =
            Inline.TextDecorationsProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="FontFeatures"/> property.
        /// </summary>
        public static readonly StyledProperty<FontFeatureCollection?> FontFeaturesProperty =
            TextElement.FontFeaturesProperty.AddOwner<TextBlock>();

        /// <summary>
        /// Defines the <see cref="Inlines"/> property.
        /// </summary>
        public static readonly DirectProperty<TextBlock, InlineCollection?> InlinesProperty =
            AvaloniaProperty.RegisterDirect<TextBlock, InlineCollection?>(
                nameof(Inlines), t => t.Inlines, (t, v) => t.Inlines = v);

        private TextLayout? _textLayout;
        protected Size _constraint = new(double.NaN, double.NaN);
        protected IReadOnlyList<TextRun>? _textRuns;
        private InlineCollection? _inlines;

        /// <summary>
        /// Initializes static members of the <see cref="TextBlock"/> class.
        /// </summary>
        static TextBlock()
        {
            ClipToBoundsProperty.OverrideDefaultValue<TextBlock>(true);

            AffectsRender<TextBlock>(BackgroundProperty, ForegroundProperty);
        }

        public TextBlock()
        {
            Inlines = new InlineCollection
            {
                LogicalChildren = LogicalChildren,
                InlineHost = this
            };
        }

        /// <summary>
        /// Gets the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        public TextLayout TextLayout => _textLayout ??= CreateTextLayout(Text);

        /// <summary>
        /// Gets or sets the padding to place around the <see cref="Text"/>.
        /// </summary>
        public Thickness Padding
        {
            get => GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        /// <summary>
        /// Gets or sets a brush used to paint the control's background.
        /// </summary>
        public IBrush? Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Gets or sets the font family used to draw the control's text.
        /// </summary>
        public FontFamily FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the control's text in points.
        /// </summary>
        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the font style used to draw the control's text.
        /// </summary>
        public FontStyle FontStyle
        {
            get => GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the font weight used to draw the control's text.
        /// </summary>
        public FontWeight FontWeight
        {
            get => GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the font stretch used to draw the control's text.
        /// </summary>
        public FontStretch FontStretch
        {
            get => GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used to draw the control's text and other foreground elements.
        /// </summary>
        public IBrush? Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of each line of content.
        /// </summary>
        public double LineHeight
        {
            get => GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the extra distance of each line to the next line.
        /// </summary>
        public double LineSpacing
        {
            get => GetValue(LineSpacingProperty);
            set => SetValue(LineSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the letter spacing.
        /// </summary>
        public double LetterSpacing
        {
            get => GetValue(LetterSpacingProperty);
            set => SetValue(LetterSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum number of text lines.
        /// </summary>
        public int MaxLines
        {
            get => GetValue(MaxLinesProperty);
            set => SetValue(MaxLinesProperty, value);
        }

        /// <summary>
        /// Gets or sets the control's text wrapping mode.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get => GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        /// <summary>
        /// Gets or sets the control's text trimming mode.
        /// </summary>
        public TextTrimming TextTrimming
        {
            get => GetValue(TextTrimmingProperty);
            set => SetValue(TextTrimmingProperty, value);
        }

        /// <summary>
        /// Gets or sets the text alignment.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get => GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the text decorations.
        /// </summary>
        public TextDecorationCollection? TextDecorations
        {
            get => GetValue(TextDecorationsProperty);
            set => SetValue(TextDecorationsProperty, value);
        }

        /// <summary>
        /// Gets or sets the font features.
        /// </summary>
        public FontFeatureCollection? FontFeatures
        {
            get => GetValue(FontFeaturesProperty);
            set => SetValue(FontFeaturesProperty, value);
        }

        /// <summary>
        /// Gets or sets the inlines.
        /// </summary>
        [Content]
        public InlineCollection? Inlines
        {
            get => _inlines;
            set => SetAndRaise(InlinesProperty, ref _inlines, value);
        }

        protected override bool BypassFlowDirectionPolicies => true;

        internal bool HasComplexContent => Inlines != null && Inlines.Count > 0;

        private protected Size GetMaxSizeFromConstraint()
        {
            var maxWidth = double.IsNaN(_constraint.Width) ? 0.0 : _constraint.Width;
            var maxHeight = double.IsNaN(_constraint.Height) ? 0.0 : _constraint.Height;
            return new Size(maxWidth, maxHeight);
        }

        /// <summary>
        /// The BaselineOffset property provides an adjustment to baseline offset
        /// </summary>
        public double BaselineOffset
        {
            get => (double)GetValue(BaselineOffsetProperty);
            set => SetValue(BaselineOffsetProperty, value);
        }

        /// <summary>
        /// Reads the attached property from the given element
        /// </summary>
        /// <param name="control">The element to which to read the attached property.</param>
        public static double GetBaselineOffset(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            return control.GetValue(BaselineOffsetProperty);
        }

        /// <summary>
        /// Writes the attached property BaselineOffset to the given element.
        /// </summary>
        /// <param name="control">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetBaselineOffset(Control control, double value)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            control.SetValue(BaselineOffsetProperty, value);
        }

        /// <summary>
        /// Reads the attached property from the given element
        /// </summary>
        /// <param name="control">The element to which to read the attached property.</param>
        public static TextAlignment GetTextAlignment(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            return control.GetValue(TextAlignmentProperty);
        }

        /// <summary>
        /// Writes the attached property BaselineOffset to the given element.
        /// </summary>
        /// <param name="control">The element to which to write the attached property.</param>
        /// <param name="alignment">The property value to set</param>
        public static void SetTextAlignment(Control control, TextAlignment alignment)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            control.SetValue(TextAlignmentProperty, alignment);
        }

        /// <summary>
        /// Reads the attached property from the given element
        /// </summary>
        /// <param name="control">The element to which to read the attached property.</param>
        public static TextWrapping GetTextWrapping(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            return control.GetValue(TextWrappingProperty);
        }

        /// <summary>
        /// Writes the attached property BaselineOffset to the given element.
        /// </summary>
        /// <param name="control">The element to which to write the attached property.</param>
        /// <param name="wrapping">The property value to set</param>
        public static void SetTextWrapping(Control control, TextWrapping wrapping)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            control.SetValue(TextWrappingProperty, wrapping);
        }

        /// <summary>
        /// Reads the attached property from the given element
        /// </summary>
        /// <param name="control">The element to which to read the attached property.</param>
        public static TextTrimming GetTextTrimming(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            return control.GetValue(TextTrimmingProperty);
        }

        /// <summary>
        /// Writes the attached property BaselineOffset to the given element.
        /// </summary>
        /// <param name="control">The element to which to write the attached property.</param>
        /// <param name="trimming">The property value to set</param>
        public static void SetTextTrimming(Control control, TextTrimming trimming)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            control.SetValue(TextTrimmingProperty, trimming);
        }

        /// <summary>
        /// Reads the attached property from the given element
        /// </summary>
        /// <param name="control">The element to which to read the attached property.</param>
        public static double GetLineHeight(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            return control.GetValue(LineHeightProperty);
        }

        /// <summary>
        /// Writes the attached property BaselineOffset to the given element.
        /// </summary>
        /// <param name="control">The element to which to write the attached property.</param>
        /// <param name="height">The property value to set</param>
        public static void SetLineHeight(Control control, double height)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            control.SetValue(LineHeightProperty, height);
        }

        /// <summary>
        /// Reads the attached property from the given element
        /// </summary>
        /// <param name="control">The element to which to read the attached property.</param>
        public static double GetLetterSpacing(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            return control.GetValue(LetterSpacingProperty);
        }

        /// <summary>
        /// Writes the attached property LetterSpacing to the given element.
        /// </summary>
        /// <param name="control">The element to which to write the attached property.</param>
        /// <param name="letterSpacing">The property value to set</param>
        public static void SetLetterSpacing(Control control, double letterSpacing)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            control.SetValue(LetterSpacingProperty, letterSpacing);
        }

        /// <summary>
        /// Reads the attached property from the given element
        /// </summary>
        /// <param name="control">The element to which to read the attached property.</param>
        public static int GetMaxLines(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            return control.GetValue(MaxLinesProperty);
        }

        /// <summary>
        /// Writes the attached property BaselineOffset to the given element.
        /// </summary>
        /// <param name="control">The element to which to write the attached property.</param>
        /// <param name="maxLines">The property value to set</param>
        public static void SetMaxLines(Control control, int maxLines)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            control.SetValue(MaxLinesProperty, maxLines);
        }

        /// <summary>
        /// Renders the <see cref="TextBlock"/> to a drawing context.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public sealed override void Render(DrawingContext context)
        {
            RenderCore(context);
        }

        // Workaround to seal Render method, we need to make so because AccessText was overriding Render method which is sealed now.
        private protected virtual void RenderCore(DrawingContext context)
        {
            var background = Background;

            if (background != null)
            {
                context.FillRectangle(background, new Rect(Bounds.Size));
            }

            var scale = LayoutHelper.GetLayoutScale(this);
            var padding = LayoutHelper.RoundLayoutThickness(Padding, scale, scale);
            var top = padding.Top;
            var textHeight = TextLayout.Height;

            if (Bounds.Height < textHeight)
            {
                switch (VerticalAlignment)
                {
                    case VerticalAlignment.Center:
                        top += (Bounds.Height - textHeight) / 2;
                        break;

                    case VerticalAlignment.Bottom:
                        top += (Bounds.Height - textHeight);
                        break;
                }
            }

            RenderTextLayout(context, new Point(padding.Left, top));
        }

        protected virtual void RenderTextLayout(DrawingContext context, Point origin)
        {
            TextLayout.Draw(context, origin + new Point(TextLayout.OverhangLeading, 0));
        }

        private bool _clearTextInternal;
        internal void ClearTextInternal()
        {
            _clearTextInternal = true;
            try
            {
                SetCurrentValue(TextProperty, null);
            }
            finally
            {
                _clearTextInternal = false;
            }
        }

        /// <summary>
        /// Creates the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        /// <returns>A <see cref="TextLayout"/> object.</returns>
        protected virtual TextLayout CreateTextLayout(string? text)
        {
            var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

            var defaultProperties = new GenericTextRunProperties(
                typeface,
                FontFeatures,
                FontSize,
                TextDecorations,
                Foreground);

            var paragraphProperties = new GenericTextParagraphProperties(FlowDirection, IsMeasureValid ? TextAlignment : TextAlignment.Left, true, false,
                defaultProperties, TextWrapping, LineHeight, 0, LetterSpacing)
            {
                LineSpacing = LineSpacing
            };
            
            UpdateTextRuns();

            ITextSource textSource;

            if (_textRuns != null)
            {
                textSource = new InlinesTextSource(_textRuns);
            }
            else
            {
                textSource = new SimpleTextSource(text ?? "", defaultProperties);
            }

            var maxSize = GetMaxSizeFromConstraint();

            return new TextLayout(
                textSource,
                paragraphProperties,
                TextTrimming,
                maxSize.Width,
                maxSize.Height,
                MaxLines);
        }

        /// <summary>
        /// Invalidates <see cref="TextLayout"/>.
        /// </summary>
        protected void InvalidateTextLayout()
        {
            InvalidateVisual();
            InvalidateMeasure();
        }

        protected override void OnMeasureInvalidated()
        {
            _textLayout?.Dispose();
            _textLayout = null;
            _textRuns = null;

            base.OnMeasureInvalidated();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var scale = LayoutHelper.GetLayoutScale(this);
            var padding = LayoutHelper.RoundLayoutThickness(Padding, scale, scale);
            var deflatedSize = availableSize.Deflate(padding);

            if (_constraint != deflatedSize)
            {
                //Reset TextLayout when the constraint is not matching.
                _textLayout?.Dispose();
                _textLayout = null;
                _constraint = deflatedSize;

                //Force arrange so text will be properly alligned.
                InvalidateArrange();
            }
            
            UpdateTextRuns(true);

            //This implicitly recreated the TextLayout with a new constraint if we previously reset it.
            var textLayout = TextLayout;

            var size = LayoutHelper.RoundLayoutSizeUp(new Size(textLayout.MinTextWidth, textLayout.Height).Inflate(padding), 1, 1);

            return size;
        }

        protected void UpdateTextRuns(bool force = false)
        {
            var inlines = Inlines;

            if (HasComplexContent && (force || _textRuns == null))
            {
                var textRuns = new List<TextRun>();

                foreach (var inline in inlines!)
                {
                    inline.BuildTextRun(textRuns);
                }

                _textRuns = textRuns;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var scale = LayoutHelper.GetLayoutScale(this);
            var padding = LayoutHelper.RoundLayoutThickness(Padding, scale, scale);

            var availableSize = finalSize.Deflate(padding);

            //ToDo: Introduce a text run cache to be able to reuse shaped runs etc.
            _textLayout?.Dispose();
            _textLayout = null;
            _constraint = availableSize;

            //This implicitly recreated the TextLayout with a new constraint.
            var textLayout = TextLayout;

            if (HasComplexContent)
            {
                //Clear visual children before complex run arrangement
                VisualChildren.Clear();

                var currentY = padding.Top;

                foreach (var textLine in textLayout.TextLines)
                {
                    var currentX = padding.Left + textLine.Start;

                    foreach (var run in textLine.TextRuns)
                    {
                        if (run is DrawableTextRun drawable)
                        {
                            if (drawable is EmbeddedControlRun controlRun
                                && controlRun.Control is Control control)
                            {
                                //Add again to prevent clipping
                                //Fixes: #17194
                                VisualChildren.Add(control);

                                control.Arrange(
                                    new Rect(new Point(currentX, currentY),
                                    new Size(control.DesiredSize.Width, textLine.Height)));
                            }

                            currentX += drawable.Size.Width;
                        }
                    }

                    currentY += textLine.Height;
                }
            }

            return finalSize;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TextBlockAutomationPeer(this);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TextProperty)
            {
                if (HasComplexContent && !_clearTextInternal)
                {
                    Inlines?.Clear();
                }
            }

            switch (change.Property.Name)
            {
                case nameof(FontSize):
                case nameof(FontWeight):
                case nameof(FontStyle):
                case nameof(FontFamily):
                case nameof(FontStretch):

                case nameof(TextWrapping):
                case nameof(TextTrimming):
                case nameof(TextAlignment):

                case nameof(FlowDirection):

                case nameof(Padding):
                case nameof(LineHeight):
                case nameof(LetterSpacing):
                case nameof(MaxLines):

                case nameof(Text):
                case nameof(TextDecorations):
                case nameof(FontFeatures):
                case nameof(Foreground):
                    {
                        InvalidateTextLayout();
                        break;
                    }
                case nameof(Inlines):
                    {
                        OnInlinesChanged(change.OldValue as InlineCollection, change.NewValue as InlineCollection);
                        InvalidateTextLayout();
                        break;
                    }
            }
        }

        private static bool IsValidMaxLines(int maxLines) => maxLines >= 0;

        private static bool IsValidLineHeight(double lineHeight) => double.IsNaN(lineHeight) || lineHeight > 0;

        private static bool IsValidLineSpacing(double lineSpacing) => !double.IsNaN(lineSpacing) && !double.IsInfinity(lineSpacing);

        private void OnInlinesChanged(InlineCollection? oldValue, InlineCollection? newValue)
        {
            VisualChildren.Clear();

            if (oldValue is not null)
            {
                oldValue.LogicalChildren = null;
                oldValue.InlineHost = null;
                oldValue.Invalidated -= Invalidated;
            }

            if (newValue is not null)
            {
                newValue.LogicalChildren = LogicalChildren;
                newValue.InlineHost = this;
                newValue.Invalidated += Invalidated;
            }

            return;

            void Invalidated(object? sender, EventArgs e) => InvalidateMeasure();
        }

        void IInlineHost.Invalidate()
        {
            InvalidateMeasure();
        }

        IAvaloniaList<Visual> IInlineHost.VisualChildren => VisualChildren;

        internal override void BuildDebugDisplay(StringBuilder builder, bool includeContent)
        {
            base.BuildDebugDisplay(builder, includeContent);

            if (includeContent)
            {
                DebugDisplayHelper.AppendOptionalValue(builder, nameof(Text), Text ?? Inlines?.Text, true);
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

#pragma warning disable CA1815
        protected readonly struct InlinesTextSource : ITextSource
#pragma warning restore CA1815
        {
            private readonly IReadOnlyList<TextRun> _textRuns;
            private readonly IReadOnlyList<ValueSpan<TextRunProperties>>? _textModifier;

            public InlinesTextSource(IReadOnlyList<TextRun> textRuns, IReadOnlyList<ValueSpan<TextRunProperties>>? textModifier = null)
            {
                _textRuns = textRuns;
                _textModifier = textModifier;
            }

            public IReadOnlyList<TextRun> TextRuns => _textRuns;

            public TextRun? GetTextRun(int textSourceIndex)
            {
                var currentPosition = 0;

                foreach (var textRun in _textRuns)
                {
                    if (textRun.Length == 0)
                    {
                        continue;
                    }

                    if (textSourceIndex >= currentPosition + textRun.Length)
                    {
                        currentPosition += textRun.Length;

                        continue;
                    }

                    if (textRun is TextCharacters textCharacters)
                    {
                        var skip = Math.Max(0, textSourceIndex - currentPosition);

                        var textStyleRun = FormattedTextSource.CreateTextStyleRun(textRun.Text.Slice(skip).Span, textSourceIndex, textCharacters.Properties, _textModifier);

                        return new TextCharacters(textRun.Text.Slice(skip, textStyleRun.Length), textStyleRun.Value);
                    }

                    return textRun;
                }

                return new TextEndOfParagraph();
            }
        }
    }
}
