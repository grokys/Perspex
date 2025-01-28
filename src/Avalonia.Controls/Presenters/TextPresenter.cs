using System;
using System.Collections.Generic;
using System.Data;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Presenters
{
    public class TextPresenter : Control
    {
        public static readonly StyledProperty<bool> ShowSelectionHighlightProperty =
            AvaloniaProperty.Register<TextPresenter, bool>(nameof(ShowSelectionHighlight), defaultValue: true);

        public static readonly StyledProperty<int> CaretIndexProperty =
            TextBox.CaretIndexProperty.AddOwner<TextPresenter>(new(coerce: TextBox.CoerceCaretIndex));

        public static readonly StyledProperty<bool> RevealPasswordProperty =
            AvaloniaProperty.Register<TextPresenter, bool>(nameof(RevealPassword));

        public static readonly StyledProperty<char> PasswordCharProperty =
            AvaloniaProperty.Register<TextPresenter, char>(nameof(PasswordChar));

        public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
            AvaloniaProperty.Register<TextPresenter, IBrush?>(nameof(SelectionBrush));

        public static readonly StyledProperty<IBrush?> SelectionForegroundBrushProperty =
            AvaloniaProperty.Register<TextPresenter, IBrush?>(nameof(SelectionForegroundBrush));

        public static readonly StyledProperty<IBrush?> CaretBrushProperty =
            AvaloniaProperty.Register<TextPresenter, IBrush?>(nameof(CaretBrush));

        public static readonly StyledProperty<TimeSpan> CaretBlinkIntervalProperty =
            TextBox.CaretBlinkIntervalProperty.AddOwner<TextPresenter>();

        public static readonly StyledProperty<int> SelectionStartProperty =
            TextBox.SelectionStartProperty.AddOwner<TextPresenter>(new(coerce: TextBox.CoerceCaretIndex));

        public static readonly StyledProperty<int> SelectionEndProperty =
            TextBox.SelectionEndProperty.AddOwner<TextPresenter>(new(coerce: TextBox.CoerceCaretIndex));

        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> TextProperty =
            TextBlock.TextProperty.AddOwner<TextPresenter>(new(string.Empty));

        /// <summary>
        /// Defines the <see cref="PreeditText"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> PreeditTextProperty =
            AvaloniaProperty.Register<TextPresenter, string?>(nameof(PreeditText));
        
        /// <summary>
        /// Defines the <see cref="PreeditText"/> property.
        /// </summary>
        public static readonly StyledProperty<int?> PreeditTextCursorPositionProperty =
            AvaloniaProperty.Register<TextPresenter, int?>(nameof(PreeditTextCursorPosition));

        /// <summary>
        /// Defines the <see cref="TextAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            TextBlock.TextAlignmentProperty.AddOwner<TextPresenter>();

        /// <summary>
        /// Defines the <see cref="TextWrapping"/> property.
        /// </summary>
        public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
            TextBlock.TextWrappingProperty.AddOwner<TextPresenter>();

        /// <summary>
        /// Defines the <see cref="LineHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> LineHeightProperty =
            TextBlock.LineHeightProperty.AddOwner<TextPresenter>();

        /// <summary>
        /// Defines the <see cref="LetterSpacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> LetterSpacingProperty =
            TextBlock.LetterSpacingProperty.AddOwner<TextPresenter>();

        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<TextPresenter>();

        private DispatcherTimer? _caretTimer;
        private bool _caretBlink;
        private TextLayout? _textLayout;
        private Size _constraint;

        private CharacterHit _lastCharacterHit;
        private Rect _caretBounds;
        private Point _navigationPosition;
        private Point? _previousOffset;
        private TextSelectorLayer? _layer;

        static TextPresenter()
        {
            AffectsRender<TextPresenter>(CaretBrushProperty, SelectionBrushProperty, SelectionForegroundBrushProperty, TextElement.ForegroundProperty, ShowSelectionHighlightProperty);
        }

        public TextPresenter() { }

        public event EventHandler? CaretBoundsChanged;

        /// <summary>
        /// Gets or sets a brush used to paint the control's background.
        /// </summary>
        public IBrush? Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that determines whether the TextPresenter shows a selection highlight.
        /// </summary>
        public bool ShowSelectionHighlight
        {
            get => GetValue(ShowSelectionHighlightProperty);
            set => SetValue(ShowSelectionHighlightProperty, value);
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        [Content]
        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string? PreeditText
        {
            get => GetValue(PreeditTextProperty);
            set => SetValue(PreeditTextProperty, value);
        }
        
        public int? PreeditTextCursorPosition
        {
            get => GetValue(PreeditTextCursorPositionProperty);
            set => SetValue(PreeditTextCursorPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        public FontFamily FontFamily
        {
            get => TextElement.GetFontFamily(this);
            set => TextElement.SetFontFamily(this, value);
        }

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        public FontFeatureCollection? FontFeatures
        {
            get => TextElement.GetFontFeatures(this);
            set => TextElement.SetFontFeatures(this, value);
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public double FontSize
        {
            get => TextElement.GetFontSize(this);
            set => TextElement.SetFontSize(this, value);
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get => TextElement.GetFontStyle(this);
            set => TextElement.SetFontStyle(this, value);
        }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        public FontWeight FontWeight
        {
            get => TextElement.GetFontWeight(this);
            set => TextElement.SetFontWeight(this, value);
        }

        /// <summary>
        /// Gets or sets the font stretch.
        /// </summary>
        public FontStretch FontStretch
        {
            get => TextElement.GetFontStretch(this);
            set => TextElement.SetFontStretch(this, value);
        }

        /// <summary>
        /// Gets or sets a brush used to paint the text.
        /// </summary>
        public IBrush? Foreground
        {
            get => TextElement.GetForeground(this);
            set => TextElement.SetForeground(this, value);
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
        /// Gets or sets the line height. By default, this is set to <see cref="double.NaN"/>, which determines the appropriate height automatically.
        /// </summary>
        public double LineHeight
        {
            get => GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
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
        /// Gets or sets the text alignment.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get => GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        /// <summary>
        /// Gets the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        public TextLayout TextLayout
        {
            get
            {
                if (_textLayout != null)
                {
                    return _textLayout;
                }

                _textLayout = CreateTextLayout();

                UpdateCaret(_lastCharacterHit, false);

                return _textLayout;
            }
        }

        public int CaretIndex
        {
            get => GetValue(CaretIndexProperty);
            set => SetValue(CaretIndexProperty, value);
        }

        public char PasswordChar
        {
            get => GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }

        public bool RevealPassword
        {
            get => GetValue(RevealPasswordProperty);
            set => SetValue(RevealPasswordProperty, value);
        }

        public IBrush? SelectionBrush
        {
            get => GetValue(SelectionBrushProperty);
            set => SetValue(SelectionBrushProperty, value);
        }

        public IBrush? SelectionForegroundBrush
        {
            get => GetValue(SelectionForegroundBrushProperty);
            set => SetValue(SelectionForegroundBrushProperty, value);
        }

        public IBrush? CaretBrush
        {
            get => GetValue(CaretBrushProperty);
            set => SetValue(CaretBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the caret blink rate
        /// </summary>
        public TimeSpan CaretBlinkInterval
        {
            get => GetValue(CaretBlinkIntervalProperty);
            set => SetValue(CaretBlinkIntervalProperty, value);
        }

        public int SelectionStart
        {
            get => GetValue(SelectionStartProperty);
            set => SetValue(SelectionStartProperty, value);
        }

        public int SelectionEnd
        {
            get => GetValue(SelectionEndProperty);
            set => SetValue(SelectionEndProperty, value);
        }

        protected override bool BypassFlowDirectionPolicies => true;

        internal TextSelectionHandleCanvas? TextSelectionHandleCanvas { get; set; }

        /// <summary>
        /// Creates the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        /// <param name="constraint">The constraint of the text.</param>
        /// <param name="text">The text to format.</param>
        /// <param name="typeface"></param>
        /// <param name="textStyleOverrides"></param>
        /// <returns>A <see cref="TextLayout"/> object.</returns>
        private TextLayout CreateTextLayoutInternal(Size constraint, string? text, Typeface typeface,
            IReadOnlyList<ValueSpan<TextRunProperties>>? textStyleOverrides)
        {
            var foreground = Foreground;
            var maxWidth = MathUtilities.IsZero(constraint.Width) ? double.PositiveInfinity : constraint.Width;
            var maxHeight = MathUtilities.IsZero(constraint.Height) ? double.PositiveInfinity : constraint.Height;

            var textLayout = new TextLayout(text, typeface, FontFeatures, FontSize, foreground, TextAlignment,
                TextWrapping, maxWidth: maxWidth, maxHeight: maxHeight, textStyleOverrides: textStyleOverrides,
                flowDirection: FlowDirection, lineHeight: LineHeight, letterSpacing: LetterSpacing);

            return textLayout;
        }

        /// <summary>
        /// Renders the <see cref="TextPresenter"/> to a drawing context.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        private void RenderInternal(DrawingContext context)
        {
            var background = Background;

            if (background != null)
            {
                context.FillRectangle(background, new Rect(Bounds.Size));
            }

            var top = 0d;
            var left = 0.0;

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

            TextLayout.Draw(context, new Point(left, top));
        }

        public sealed override void Render(DrawingContext context)
        {
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var selectionBrush = SelectionBrush;

            if (ShowSelectionHighlight && selectionStart != selectionEnd && selectionBrush != null)
            {
                var start = Math.Min(selectionStart, selectionEnd);
                var length = Math.Max(selectionStart, selectionEnd) - start;

                var rects = TextLayout.HitTestTextRange(start, length);

                foreach (var rect in rects)
                {
                    context.FillRectangle(selectionBrush, PixelRect.FromRect(rect, 1).ToRect(1));
                }
            }

            if(VisualRoot is Visual root)
            {
                var offset = this.TranslatePoint(Bounds.Position, root);

                if(_previousOffset != offset)
                {
                    _previousOffset = offset;
                }
            }

            RenderInternal(context);

            if ((selectionStart != selectionEnd || !_caretBlink))
            {
                return;
            }

            var caretBrush = CaretBrush?.ToImmutable();

            if (caretBrush is null)
            {
                var backgroundColor = (Background as ISolidColorBrush)?.Color;

                if (backgroundColor.HasValue)
                {
                    var red = (byte)~(backgroundColor.Value.R);
                    var green = (byte)~(backgroundColor.Value.G);
                    var blue = (byte)~(backgroundColor.Value.B);

                    caretBrush = new ImmutableSolidColorBrush(Color.FromRgb(red, green, blue));
                }
                else
                {
                    caretBrush = Brushes.Black;
                }
            }

            var (p1, p2) = GetCaretPoints();

            context.DrawLine(new ImmutablePen(caretBrush), p1, p2);
        }

        internal (Point, Point) GetCaretPoints()
        {
            var x = Math.Floor(_caretBounds.X) + 0.5;
            var y = Math.Floor(_caretBounds.Y) + 0.5;
            var b = Math.Ceiling(_caretBounds.Bottom) - 0.5;

            var caretIndex = _lastCharacterHit.FirstCharacterIndex + _lastCharacterHit.TrailingLength;
            var lineIndex = TextLayout.GetLineIndexFromCharacterIndex(caretIndex, _lastCharacterHit.TrailingLength > 0);
            var textLine = TextLayout.TextLines[lineIndex];

            if (_caretBounds.X > 0 && _caretBounds.X >= textLine.WidthIncludingTrailingWhitespace)
            {
                x -= 1;
            }

            return (new Point(x, y), new Point(x, b));
        }

        public void ShowCaret()
        {
            EnsureCaretTimer();
            EnsureTextSelectionLayer();
            _caretBlink = true;
            _caretTimer?.Start();
            InvalidateVisual();
        }

        public void HideCaret()
        {
            _caretBlink = false;
            RemoveTextSelectionCanvas();
            _caretTimer?.Stop();
            InvalidateTextLayout();
        }

        internal void CaretChanged()
        {
            if (this.GetVisualParent() == null)
            {
                return;
            }

            EnsureCaretTimer();

            if (_caretTimer?.IsEnabled ?? false)
            {
                _caretBlink = true;
                _caretTimer?.Stop();
                _caretTimer?.Start();
                InvalidateVisual();
            }
            else
            {
                _caretTimer?.Start();
                InvalidateVisual();
                _caretTimer?.Stop();
            }

            if (IsMeasureValid)
            {
                this.BringIntoView(_caretBounds);
            }
            else
            {
                // The measure is currently invalid so there's no point trying to bring the 
                // current char into view until a measure has been carried out as the scroll
                // viewer extents may not be up-to-date.
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        this.BringIntoView(_caretBounds);
                    },
                    DispatcherPriority.AfterRender);
            }
        }

        /// <summary>
        /// Creates the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        /// <returns>A <see cref="TextLayout"/> object.</returns>
        protected virtual TextLayout CreateTextLayout()
        {
            TextLayout result;

            var caretIndex = CaretIndex;
            var preeditText = PreeditText;
            var text = GetCombinedText(Text, caretIndex, preeditText);
            var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var start = Math.Min(selectionStart, selectionEnd);
            var length = Math.Max(selectionStart, selectionEnd) - start;

            IReadOnlyList<ValueSpan<TextRunProperties>>? textStyleOverrides = null;

            var foreground = Foreground;

            if (!string.IsNullOrEmpty(preeditText))
            {
                var preeditHighlight = new ValueSpan<TextRunProperties>(caretIndex, preeditText.Length,
                        new GenericTextRunProperties(typeface, FontFeatures, FontSize,
                        foregroundBrush: foreground,
                        textDecorations: TextDecorations.Underline));

                textStyleOverrides = new[]
                {
                    preeditHighlight
                };
            }
            else
            {
                if (ShowSelectionHighlight && length > 0 && SelectionForegroundBrush != null)
                {
                    textStyleOverrides = new[]
                    {
                        new ValueSpan<TextRunProperties>(start, length,
                        new GenericTextRunProperties(typeface, FontFeatures, FontSize,
                            foregroundBrush: SelectionForegroundBrush))
                    };
                }
            }

            if (PasswordChar != default(char) && !RevealPassword)
            {
                result = CreateTextLayoutInternal(_constraint, new string(PasswordChar, text?.Length ?? 0), typeface,
                    textStyleOverrides);
            }
            else
            {
                result = CreateTextLayoutInternal(_constraint, text, typeface, textStyleOverrides);
            }

            return result;
        }

        private static string? GetCombinedText(string? text, int caretIndex, string? preeditText)
        {
            if (string.IsNullOrEmpty(preeditText))
            {
                return text;
            }

            if (string.IsNullOrEmpty(text))
            {
                return preeditText;
            }

            var sb = StringBuilderCache.Acquire(text.Length + preeditText.Length);

            sb.Append(text.Substring(0, caretIndex));
            sb.Insert(caretIndex, preeditText);
            sb.Append(text.Substring(caretIndex));

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        protected virtual void InvalidateTextLayout()
        {
            _textLayout?.Dispose();
            _textLayout = null;

            InvalidateVisual();
            InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _constraint = availableSize;

            _textLayout?.Dispose();
            _textLayout = null;

            InvalidateArrange();

            var textWidth = Math.Ceiling(TextLayout.MinTextWidth);

            return new Size(textWidth, TextLayout.Height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var textWidth = Math.Ceiling(TextLayout.MinTextWidth);
            
            var finalWidth = finalSize.Width;

            if (finalSize.Width < textWidth)
            {
                finalSize = finalSize.WithWidth(textWidth);
            }

            // Check if the '_constraint' has changed since the last measure,
            // if so recalculate the TextLayout according to the new size
            // NOTE: It is important to check this against the actual final size
            // (excluding the trailing whitespace) to avoid TextLayout overflow.
            if (MathUtilities.AreClose(_constraint.Width, finalWidth) == false)
            {
                _constraint = new Size(Math.Ceiling(finalWidth), double.PositiveInfinity);

                _textLayout?.Dispose();
                _textLayout = null;
            }

            return finalSize;
        }

        private void CaretTimerTick(object? sender, EventArgs e)
        {
            _caretBlink = !_caretBlink;

            InvalidateVisual();
        }

        public void MoveCaretToTextPosition(int textPosition, bool trailingEdge = false)
        {
            var lineIndex = TextLayout.GetLineIndexFromCharacterIndex(textPosition, trailingEdge);
            var textLine = TextLayout.TextLines[lineIndex];

            var characterHit = textLine.GetPreviousCaretCharacterHit(new CharacterHit(textPosition));

            var nextCaretCharacterHit = textLine.GetNextCaretCharacterHit(characterHit);

            if (nextCaretCharacterHit.FirstCharacterIndex <= textPosition)
            {
                characterHit = nextCaretCharacterHit;
            }

            if (textPosition == characterHit.FirstCharacterIndex + characterHit.TrailingLength)
            {
                UpdateCaret(characterHit);
            }
            else
            {
                UpdateCaret(trailingEdge ? characterHit : new CharacterHit(characterHit.FirstCharacterIndex));
            }

            _navigationPosition = _caretBounds.Position;

            CaretChanged();
        }

        public void MoveCaretToPoint(Point point)
        {
            var hit = TextLayout.HitTestPoint(point);

            UpdateCaret(hit.CharacterHit);

            _navigationPosition = _caretBounds.Position;

            CaretChanged();
        }

        public void MoveCaretVertical(LogicalDirection direction = LogicalDirection.Forward)
        {
            var lineIndex = TextLayout.GetLineIndexFromCharacterIndex(CaretIndex, _lastCharacterHit.TrailingLength > 0);

            if (lineIndex < 0)
            {
                return;
            }

            var (currentX, currentY) = _navigationPosition;

            if (direction == LogicalDirection.Forward)
            {
                if (lineIndex + 1 > TextLayout.TextLines.Count - 1)
                {
                    return;
                }

                var textLine = TextLayout.TextLines[lineIndex];

                currentY += textLine.Height;
            }
            else
            {
                if (lineIndex - 1 < 0)
                {
                    return;
                }

                var textLine = TextLayout.TextLines[--lineIndex];

                currentY -= textLine.Height;
            }

            var navigationPosition = _navigationPosition;

            MoveCaretToPoint(new Point(currentX, currentY));

            _navigationPosition = navigationPosition.WithY(_caretBounds.Y);

            CaretChanged();
        }
        
        private void EnsureCaretTimer()
        {
            if (_caretTimer == null)
            {
                ResetCaretTimer();
            }
        }

        private void ResetCaretTimer()
        {
            bool isEnabled = false;

            if (_caretTimer != null)
            {
                _caretTimer.Tick -= CaretTimerTick;

                if (_caretTimer.IsEnabled)
                {
                    _caretTimer.Stop();
                    isEnabled = true;
                }

                _caretTimer = null;
            }

            if (CaretBlinkInterval.TotalMilliseconds > 0) 
            {
                _caretTimer = new DispatcherTimer { Interval = CaretBlinkInterval };
                _caretTimer.Tick += CaretTimerTick;

                if (isEnabled)
                    _caretTimer.Start();
            }
        }

        public CharacterHit GetNextCharacterHit(LogicalDirection direction = LogicalDirection.Forward)
        {
            if (Text is null)
            {
                return default;
            }

            var characterHit = _lastCharacterHit;
            var caretIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            var lineIndex = TextLayout.GetLineIndexFromCharacterIndex(caretIndex, false);

            if (lineIndex < 0)
            {
                return default;
            }

            if (direction == LogicalDirection.Forward)
            {
                while (lineIndex < TextLayout.TextLines.Count)
                {
                    var textLine = TextLayout.TextLines[lineIndex];

                    characterHit = textLine.GetNextCaretCharacterHit(characterHit);

                    caretIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

                    if (textLine.TrailingWhitespaceLength > 0 && caretIndex == textLine.FirstTextSourceIndex + textLine.Length)
                    {
                        characterHit = new CharacterHit(caretIndex);
                    }

                    if (caretIndex >= Text.Length)
                    {
                        characterHit = new CharacterHit(Text.Length);

                        break;
                    }

                    if (caretIndex - textLine.NewLineLength == textLine.FirstTextSourceIndex + textLine.Length)
                    {
                        break;
                    }

                    if (caretIndex <= CaretIndex)
                    {
                        lineIndex++;

                        continue;
                    }

                    break;
                }
            }
            else
            {
                while (lineIndex >= 0)
                {
                    var textLine = TextLayout.TextLines[lineIndex];

                    characterHit = textLine.GetPreviousCaretCharacterHit(characterHit);

                    caretIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

                    if (caretIndex >= CaretIndex)
                    {
                        lineIndex--;

                        continue;
                    }

                    break;
                }
            }

            return characterHit;
        }

        public void MoveCaretHorizontal(LogicalDirection direction = LogicalDirection.Forward)
        {
            if (FlowDirection == FlowDirection.RightToLeft)
            {
                direction = direction == LogicalDirection.Forward ?
                    LogicalDirection.Backward :
                    LogicalDirection.Forward;
            }

            var characterHit = GetNextCharacterHit(direction);

            UpdateCaret(characterHit);

            _navigationPosition = _caretBounds.Position;

            CaretChanged();
        }

        internal void UpdateCaret(CharacterHit characterHit, bool notify = true)
        {
            _lastCharacterHit = characterHit;

            var caretIndex = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

            var lineIndex = TextLayout.GetLineIndexFromCharacterIndex(caretIndex, characterHit.TrailingLength > 0);
            var textLine = TextLayout.TextLines[lineIndex];
            var distanceX = textLine.GetDistanceFromCharacterHit(characterHit);

            var distanceY = 0d;

            for (var i = 0; i < lineIndex; i++)
            {
                var currentLine = TextLayout.TextLines[i];

                distanceY += currentLine.Height;
            }

            var caretBounds = new Rect(distanceX, distanceY, 0, textLine.Height);

            if (caretBounds != _caretBounds)
            {
                _caretBounds = caretBounds;

                CaretBoundsChanged?.Invoke(this, EventArgs.Empty);
            }

            if (notify)
            {
                SetCurrentValue(CaretIndexProperty, caretIndex);
            }
        }

        internal Rect GetCursorRectangle()
        {
            return _caretBounds;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            ResetCaretTimer();
        }

        private void EnsureTextSelectionLayer()
        {
            if (TextSelectionHandleCanvas == null)
            {
                TextSelectionHandleCanvas = new TextSelectionHandleCanvas();
            }
            TextSelectionHandleCanvas.SetPresenter(this);
            _layer = TextSelectorLayer.GetTextSelectorLayer(this);
            if (TextSelectionHandleCanvas.VisualParent is { } parent && parent != _layer)
            {
                if (parent is TextSelectorLayer l)
                {
                    l.Remove(TextSelectionHandleCanvas);
                }
            }
            if (_layer != null && TextSelectionHandleCanvas.VisualParent != _layer)
                _layer?.Add(TextSelectionHandleCanvas);
        }

        private void RemoveTextSelectionCanvas()
        {
            if(_layer != null && TextSelectionHandleCanvas is { } canvas)
            {
                canvas.SetPresenter(null);
                _layer.Remove(canvas);
            }

            TextSelectionHandleCanvas = null;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            if (TextSelectionHandleCanvas is { } c)
            {
                _layer?.Remove(c);
                c.SetPresenter(null);
            }

            if (_caretTimer != null)
            {
                _caretTimer.Stop();
                _caretTimer.Tick -= CaretTimerTick;
            }
        }
        
        private void OnPreeditChanged(string? preeditText, int? cursorPosition)
        {
            if (string.IsNullOrEmpty(preeditText))
            {
                UpdateCaret(new CharacterHit(CaretIndex), false);
            }
            else
            {
                var cursorPos = cursorPosition is >= 0 && cursorPosition <= preeditText.Length
                    ? cursorPosition.Value
                    : preeditText.Length;
                UpdateCaret(new CharacterHit(CaretIndex + cursorPos), false);
                InvalidateMeasure();
                CaretChanged();
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == CaretIndexProperty)
            {
                MoveCaretToTextPosition(change.GetNewValue<int>());
            }

            if(change.Property == PreeditTextProperty)
            {
                OnPreeditChanged(change.NewValue as string, PreeditTextCursorPosition);
            }
            
            if(change.Property == PreeditTextCursorPositionProperty)
            {
                OnPreeditChanged(PreeditText, PreeditTextCursorPosition);
            }

            if(change.Property == TextProperty)
            {
                if (!string.IsNullOrEmpty(PreeditText))
                {
                    SetCurrentValue(PreeditTextProperty, null);
                }
            }

            if(change.Property == CaretIndexProperty)
            {
                if (!string.IsNullOrEmpty(PreeditText))
                {
                    SetCurrentValue(PreeditTextProperty, null);
                }
            }

            if (change.Property == CaretBlinkIntervalProperty)
            {
                ResetCaretTimer();
            }

            switch (change.Property.Name)
            {
                case nameof(PreeditText):
                case nameof(Foreground):
                case nameof(FontSize):
                case nameof(FontStyle):
                case nameof(FontWeight):
                case nameof(FontFamily):
                case nameof(FontStretch):

                case nameof(Text):
                case nameof(TextAlignment):
                case nameof(TextWrapping):

                case nameof(LineHeight):
                case nameof(LetterSpacing):

                case nameof(SelectionStart):
                case nameof(SelectionEnd):
                case nameof(SelectionForegroundBrush):
                case nameof(ShowSelectionHighlightProperty):

                case nameof(PasswordChar):
                case nameof(RevealPassword):
                case nameof(FlowDirection):
                    {
                        InvalidateTextLayout();
                        break;
                    }
            }
        }
    }
}
