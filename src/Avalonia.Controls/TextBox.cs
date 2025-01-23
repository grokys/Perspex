using Avalonia.Input.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Reactive;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Utilities;
using Avalonia.Controls.Metadata;
using Avalonia.Media.TextFormatting;
using Avalonia.Automation.Peers;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a control that can be used to display or edit unformatted text.
    /// </summary>
    [TemplatePart("PART_TextPresenter", typeof(TextPresenter), IsRequired = true)]
    [TemplatePart("PART_ScrollViewer", typeof(ScrollViewer))]
    [PseudoClasses(":empty")]
    public class TextBox : TemplatedControl, UndoRedoHelper<TextBox.UndoRedoState>.IUndoRedoHost
    {
        /// <summary>
        /// Gets a platform-specific <see cref="KeyGesture"/> for the Cut action
        /// </summary>
        public static KeyGesture? CutGesture => Application.Current?.PlatformSettings?.HotkeyConfiguration.Cut.FirstOrDefault();

        /// <summary>
        /// Gets a platform-specific <see cref="KeyGesture"/> for the Copy action
        /// </summary>
        public static KeyGesture? CopyGesture => Application.Current?.PlatformSettings?.HotkeyConfiguration.Copy.FirstOrDefault();

        /// <summary>
        /// Gets a platform-specific <see cref="KeyGesture"/> for the Paste action
        /// </summary>
        public static KeyGesture? PasteGesture => Application.Current?.PlatformSettings?.HotkeyConfiguration.Paste.FirstOrDefault();

        /// <summary>
        /// Defines the <see cref="IsInactiveSelectionHighlightEnabled"/> property
        /// </summary>
        public static readonly StyledProperty<bool> IsInactiveSelectionHighlightEnabledProperty = 
            AvaloniaProperty.Register<TextBox, bool>(nameof(IsInactiveSelectionHighlightEnabled), defaultValue: true);

        /// <summary>
        /// Defines the <see cref="ClearSelectionOnLostFocus"/> property
        /// </summary>
        public static readonly StyledProperty<bool> ClearSelectionOnLostFocusProperty = 
            AvaloniaProperty.Register<TextBox, bool>(nameof(ClearSelectionOnLostFocus), defaultValue: true);

        /// <summary>
        /// Defines the <see cref="AcceptsReturn"/> property
        /// </summary>
        public static readonly StyledProperty<bool> AcceptsReturnProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(AcceptsReturn));

        /// <summary>
        /// Defines the <see cref="AcceptsTab"/> property
        /// </summary>
        public static readonly StyledProperty<bool> AcceptsTabProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(AcceptsTab));

        /// <summary>
        /// Defines the <see cref="CaretIndex"/> property
        /// </summary>
        public static readonly StyledProperty<int> CaretIndexProperty =
            AvaloniaProperty.Register<TextBox, int>(nameof(CaretIndex),
                coerce: CoerceCaretIndex);

        /// <summary>
        /// Defines the <see cref="IsReadOnly"/> property
        /// </summary>
        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(IsReadOnly));

        /// <summary>
        /// Defines the <see cref="PasswordChar"/> property
        /// </summary>
        public static readonly StyledProperty<char> PasswordCharProperty =
            AvaloniaProperty.Register<TextBox, char>(nameof(PasswordChar));

        /// <summary>
        /// Defines the <see cref="SelectionBrush"/> property
        /// </summary>
        public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
            AvaloniaProperty.Register<TextBox, IBrush?>(nameof(SelectionBrush));

        /// <summary>
        /// Defines the <see cref="SelectionForegroundBrush"/> property
        /// </summary>
        public static readonly StyledProperty<IBrush?> SelectionForegroundBrushProperty =
            AvaloniaProperty.Register<TextBox, IBrush?>(nameof(SelectionForegroundBrush));

        /// <summary>
        /// Defines the <see cref="CaretBrush"/> property
        /// </summary>
        public static readonly StyledProperty<IBrush?> CaretBrushProperty =
            AvaloniaProperty.Register<TextBox, IBrush?>(nameof(CaretBrush));

        /// <summary>
        /// Defines the <see cref="CaretBlinkInterval"/> property
        /// </summary>
        public static readonly StyledProperty<TimeSpan> CaretBlinkIntervalProperty =
            AvaloniaProperty.Register<TextBox, TimeSpan>(nameof(CaretBlinkInterval), defaultValue: TimeSpan.FromMilliseconds(500));

        /// <summary>
        /// Defines the <see cref="SelectionStart"/> property
        /// </summary>
        public static readonly StyledProperty<int> SelectionStartProperty =
            AvaloniaProperty.Register<TextBox, int>(nameof(SelectionStart),
                coerce: CoerceCaretIndex);

        /// <summary>
        /// Defines the <see cref="SelectionEnd"/> property
        /// </summary>
        public static readonly StyledProperty<int> SelectionEndProperty =
            AvaloniaProperty.Register<TextBox, int>(nameof(SelectionEnd),
                coerce: CoerceCaretIndex);

        /// <summary>
        /// Defines the <see cref="MaxLength"/> property
        /// </summary>
        public static readonly StyledProperty<int> MaxLengthProperty =
            AvaloniaProperty.Register<TextBox, int>(nameof(MaxLength));

        /// <summary>
        /// Defines the <see cref="MaxLines"/> property
        /// </summary>
        public static readonly StyledProperty<int> MaxLinesProperty =
            AvaloniaProperty.Register<TextBox, int>(nameof(MaxLines));

        /// <summary>
        /// Defines the <see cref="MinLines"/> property
        /// </summary>
        public static readonly StyledProperty<int> MinLinesProperty =
            AvaloniaProperty.Register<TextBox, int>(nameof(MinLines));

        /// <summary>
        /// Defines the <see cref="Text"/> property
        /// </summary>
        public static readonly StyledProperty<string?> TextProperty =
            TextBlock.TextProperty.AddOwner<TextBox>(new(
                coerce: CoerceText,
                defaultBindingMode: BindingMode.TwoWay,
                enableDataValidation: true));

        /// <summary>
        /// Defines the <see cref="TextAlignment"/> property
        /// </summary>
        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            TextBlock.TextAlignmentProperty.AddOwner<TextBox>();

        /// <summary>
        /// Defines the <see cref="HorizontalAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<TextBox>();

        /// <summary>
        /// Defines the <see cref="VerticalAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<TextBox>();

        public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
            TextBlock.TextWrappingProperty.AddOwner<TextBox>();

        /// <summary>
        /// Defines see <see cref="TextPresenter.LineHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> LineHeightProperty =
            TextBlock.LineHeightProperty.AddOwner<TextBox>(new(defaultValue: double.NaN));

        /// <summary>
        /// Defines see <see cref="TextBlock.LetterSpacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> LetterSpacingProperty =
            TextBlock.LetterSpacingProperty.AddOwner<TextBox>();

        /// <summary>
        /// Defines the <see cref="Watermark"/> property
        /// </summary>
        public static readonly StyledProperty<string?> WatermarkProperty =
            AvaloniaProperty.Register<TextBox, string?>(nameof(Watermark));

        /// <summary>
        /// Defines the <see cref="UseFloatingWatermark"/> property
        /// </summary>
        public static readonly StyledProperty<bool> UseFloatingWatermarkProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(UseFloatingWatermark));

        /// <summary>
        /// Defines the <see cref="NewLine"/> property
        /// </summary>
        public static readonly StyledProperty<string> NewLineProperty =
            AvaloniaProperty.Register<TextBox, string>(nameof(NewLine), Environment.NewLine);

        /// <summary>
        /// Defines the <see cref="InnerLeftContent"/> property
        /// </summary>
        public static readonly StyledProperty<object?> InnerLeftContentProperty =
            AvaloniaProperty.Register<TextBox, object?>(nameof(InnerLeftContent));

        /// <summary>
        /// Defines the <see cref="InnerRightContent"/> property
        /// </summary>
        public static readonly StyledProperty<object?> InnerRightContentProperty =
            AvaloniaProperty.Register<TextBox, object?>(nameof(InnerRightContent));

        /// <summary>
        /// Defines the <see cref="RevealPassword"/> property
        /// </summary>
        public static readonly StyledProperty<bool> RevealPasswordProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(RevealPassword));

        /// <summary>
        /// Defines the <see cref="CanCut"/> property
        /// </summary>
        public static readonly DirectProperty<TextBox, bool> CanCutProperty =
            AvaloniaProperty.RegisterDirect<TextBox, bool>(
                nameof(CanCut),
                o => o.CanCut);

        /// <summary>
        /// Defines the <see cref="CanCopy"/> property
        /// </summary>
        public static readonly DirectProperty<TextBox, bool> CanCopyProperty =
            AvaloniaProperty.RegisterDirect<TextBox, bool>(
                nameof(CanCopy),
                o => o.CanCopy);

        /// <summary>
        /// Defines the <see cref="CanPaste"/> property
        /// </summary>
        public static readonly DirectProperty<TextBox, bool> CanPasteProperty =
            AvaloniaProperty.RegisterDirect<TextBox, bool>(
                nameof(CanPaste),
                o => o.CanPaste);

        /// <summary>
        /// Defines the <see cref="IsUndoEnabled"/> property
        /// </summary>
        public static readonly StyledProperty<bool> IsUndoEnabledProperty =
            AvaloniaProperty.Register<TextBox, bool>(
                nameof(IsUndoEnabled),
                defaultValue: true);

        /// <summary>
        /// Defines the <see cref="UndoLimit"/> property
        /// </summary>
        public static readonly StyledProperty<int> UndoLimitProperty =
            AvaloniaProperty.Register<TextBox, int>(nameof(UndoLimit), UndoRedoHelper<UndoRedoState>.DefaultUndoLimit);

        /// <summary>
        /// Defines the <see cref="CanUndo"/> property
        /// </summary>
        public static readonly DirectProperty<TextBox, bool> CanUndoProperty =
            AvaloniaProperty.RegisterDirect<TextBox, bool>(nameof(CanUndo), x => x.CanUndo);

        /// <summary>
        /// Defines the <see cref="CanRedo"/> property
        /// </summary>
        public static readonly DirectProperty<TextBox, bool> CanRedoProperty =
            AvaloniaProperty.RegisterDirect<TextBox, bool>(nameof(CanRedo), x => x.CanRedo);

        /// <summary>
        /// Defines the <see cref="CopyingToClipboard"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> CopyingToClipboardEvent =
            RoutedEvent.Register<TextBox, RoutedEventArgs>(
                nameof(CopyingToClipboard), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="CuttingToClipboard"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> CuttingToClipboardEvent =
            RoutedEvent.Register<TextBox, RoutedEventArgs>(
                nameof(CuttingToClipboard), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PastingFromClipboard"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> PastingFromClipboardEvent =
            RoutedEvent.Register<TextBox, RoutedEventArgs>(
                nameof(PastingFromClipboard), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="TextChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<TextChangedEventArgs> TextChangedEvent =
            RoutedEvent.Register<TextBox, TextChangedEventArgs>(
                nameof(TextChanged), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="TextChanging"/> event.
        /// </summary>
        public static readonly RoutedEvent<TextChangingEventArgs> TextChangingEvent =
            RoutedEvent.Register<TextBox, TextChangingEventArgs>(
                nameof(TextChanging), RoutingStrategies.Bubble);

        /// <summary>
        /// Stores the state information for available actions in the UndoRedoHelper
        /// </summary>
        readonly struct UndoRedoState : IEquatable<UndoRedoState>
        {
            public string? Text { get; }

            public int CaretPosition { get; }

            public UndoRedoState(string? text, int caretPosition)
            {
                Text = text;
                CaretPosition = caretPosition;
            }

            public bool Equals(UndoRedoState other) => ReferenceEquals(Text, other.Text) || Equals(Text, other.Text);

            public override bool Equals(object? obj) => obj is UndoRedoState other && Equals(other);

            public override int GetHashCode() => Text?.GetHashCode() ?? 0;
        }

        private TextPresenter? _presenter;
        private ScrollViewer? _scrollViewer;
        private readonly TextBoxTextInputMethodClient _imClient = new();
        private readonly UndoRedoHelper<UndoRedoState> _undoRedoHelper;
        private bool _isUndoingRedoing;
        private bool _canCut;
        private bool _canCopy;
        private bool _canPaste;
        private static readonly string[] invalidCharacters = new String[1] { "\u007f" };
        private bool _canUndo;
        private bool _canRedo;

        private int _wordSelectionStart = -1;
        private int _selectedTextChangesMadeSinceLastUndoSnapshot;
        private bool _hasDoneSnapshotOnce;
        private static bool _isHolding;
        private int _currentClickCount;
        private bool _isDoubleTapped;
        private const int _maxCharsBeforeUndoSnapshot = 7;

        static TextBox()
        {
            FocusableProperty.OverrideDefaultValue(typeof(TextBox), true);
            TextInputMethodClientRequestedEvent.AddClassHandler<TextBox>((tb, e) =>
            {
                if (!tb.IsReadOnly)
                {
                    e.Client = tb._imClient;
                }
            });
        }

        public TextBox()
        {
            var horizontalScrollBarVisibility = Observable.CombineLatest(
                this.GetObservable(AcceptsReturnProperty),
                this.GetObservable(TextWrappingProperty),
                (acceptsReturn, wrapping) =>
                {
                    if (wrapping != TextWrapping.NoWrap)
                    {
                        return ScrollBarVisibility.Disabled;
                    }

                    return acceptsReturn ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden;
                });
            this.Bind(
                ScrollViewer.HorizontalScrollBarVisibilityProperty,
                horizontalScrollBarVisibility,
                BindingPriority.Style);

            _undoRedoHelper = new UndoRedoHelper<UndoRedoState>(this);
            _selectedTextChangesMadeSinceLastUndoSnapshot = 0;
            _hasDoneSnapshotOnce = false;
            UpdatePseudoclasses();
        }

        /// <summary>
        /// Gets or sets a value that determines whether the TextBox shows a selection highlight when it is not focused.
        /// </summary>
        public bool IsInactiveSelectionHighlightEnabled
        {
            get => GetValue(IsInactiveSelectionHighlightEnabledProperty);
            set => SetValue(IsInactiveSelectionHighlightEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that determines whether the TextBox clears its selection after it loses focus.
        /// </summary>
        public bool ClearSelectionOnLostFocus
        {
            get=> GetValue(ClearSelectionOnLostFocusProperty);
            set=> SetValue(ClearSelectionOnLostFocusProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that determines whether the TextBox allows and displays newline or return characters
        /// </summary>
        public bool AcceptsReturn
        {
            get => GetValue(AcceptsReturnProperty);
            set => SetValue(AcceptsReturnProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that determins whether the TextBox allows and displays tabs
        /// </summary>
        public bool AcceptsTab
        {
            get => GetValue(AcceptsTabProperty);
            set => SetValue(AcceptsTabProperty, value);
        }

        /// <summary>
        /// Gets or sets the index of the text caret
        /// </summary>
        public int CaretIndex
        {
            get => GetValue(CaretIndexProperty);
            set => SetValue(CaretIndexProperty, value);
        }

        private void OnCaretIndexChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UndoRedoState state;
            if (IsUndoEnabled && _undoRedoHelper.TryGetLastState(out state) && state.Text == Text)
                _undoRedoHelper.UpdateLastState();

            using var _ = _imClient.BeginChange();

            var newValue = e.GetNewValue<int>();
            SetCurrentValue(SelectionStartProperty, newValue);
            SetCurrentValue(SelectionEndProperty, newValue);

           _presenter?.SetCurrentValue(TextPresenter.CaretIndexProperty, newValue);
        }

        /// <summary>
        /// Gets or sets a value whether this TextBox is read-only
        /// </summary>
        public bool IsReadOnly
        {
            get => GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="char"/> that should be used for password masking
        /// </summary>
        public char PasswordChar
        {
            get => GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }

        /// <summary>
        /// Gets or sets a brush that is used to highlight selected text
        /// </summary>
        public IBrush? SelectionBrush
        {
            get => GetValue(SelectionBrushProperty);
            set => SetValue(SelectionBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets a brush that is used for the foreground of selected text
        /// </summary>
        public IBrush? SelectionForegroundBrush
        {
            get => GetValue(SelectionForegroundBrushProperty);
            set => SetValue(SelectionForegroundBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets a brush that is used for the text caret
        /// </summary>
        public IBrush? CaretBrush
        {
            get => GetValue(CaretBrushProperty);
            set => SetValue(CaretBrushProperty, value);
        }

        /// <inheritdoc cref="TextPresenter.CaretBlinkInterval"/>
        public TimeSpan CaretBlinkInterval
        {
            get => GetValue(CaretBlinkIntervalProperty);
            set => SetValue(CaretBlinkIntervalProperty, value);
        }

        /// <summary>
        /// Gets or sets the starting position of the text selected in the TextBox
        /// </summary>
        public int SelectionStart
        {
            get => GetValue(SelectionStartProperty);
            set => SetValue(SelectionStartProperty, value);
        }

        private void OnSelectionStartChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateCommandStates();

            var value = e.GetNewValue<int>();
            if (SelectionEnd == value && CaretIndex != value)
            {
                SetCurrentValue(CaretIndexProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the end position of the text selected in the TextBox
        /// </summary>
        /// <remarks>
        /// When the SelectionEnd is equal to <see cref="SelectionStart"/>, there is no 
        /// selected text and it marks the caret position
        /// </remarks>
        public int SelectionEnd
        {
            get => GetValue(SelectionEndProperty);
            set => SetValue(SelectionEndProperty, value);
        }

        private void OnSelectionEndChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateCommandStates();

            var value = e.GetNewValue<int>();
            if (SelectionStart == value && CaretIndex != value)
            {
                SetCurrentValue(CaretIndexProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of characters that the <see cref="TextBox"/> can accept.
        /// This constraint only applies for manually entered (user-inputted) text.
        /// </summary>
        public int MaxLength
        {
            get => GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum number of visible lines to size to.
        /// </summary>
        public int MaxLines
        {
            get => GetValue(MaxLinesProperty);
            set => SetValue(MaxLinesProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum number of visible lines to size to.
        /// </summary>
        public int MinLines
        {
            get => GetValue(MinLinesProperty);
            set => SetValue(MinLinesProperty, value);
        }

        /// <summary>
        /// Gets or sets the spacing between characters
        /// </summary>
        public double LetterSpacing
        {
            get => GetValue(LetterSpacingProperty);
            set => SetValue(LetterSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the line height.
        /// </summary>
        public double LineHeight
        {
            get => GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the Text content of the TextBox
        /// </summary>
        [Content]
        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static string? CoerceText(AvaloniaObject sender, string? value)
            => ((TextBox)sender).CoerceText(value);

        /// <summary>
        /// Coerces the current text.
        /// </summary>
        /// <param name="value">The initial text.</param>
        /// <returns>A coerced text.</returns>
        /// <remarks>
        /// This method also manages the internal undo/redo state whenever the text changes:
        /// if overridden, ensure that the base is called or undo/redo won't work correctly.
        /// </remarks>
        protected virtual string? CoerceText(string? value)
        {
            // Before #9490, snapshot here was done AFTER text change - this doesn't make sense
            // since initial state would never be no text and you'd always have to make a text
            // change before undo would be available
            // The undo/redo stacks were also cleared at this point, which also doesn't make sense
            // as it is still valid to want to undo a programmatic text set
            // So we snapshot text now BEFORE the change so we can always revert
            // Also don't need to check IsUndoEnabled here, that's done in SnapshotUndoRedo
            if (!_isUndoingRedoing)
            {
                SnapshotUndoRedo();
            }

            return value;
        }

        /// <summary>
        /// Gets or sets the text selected in the TextBox
        /// </summary>
        public string SelectedText
        {
            get => GetSelection();
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _selectedTextChangesMadeSinceLastUndoSnapshot++;
                    SnapshotUndoRedo(ignoreChangeCount: false);
                    DeleteSelection();
                }
                else
                {
                    HandleTextInput(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the control.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get => GetValue(HorizontalContentAlignmentProperty);
            set => SetValue(HorizontalContentAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the control.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get => GetValue(VerticalContentAlignmentProperty);
            set => SetValue(VerticalContentAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="Media.TextAlignment"/> of the TextBox
        /// </summary>
        public TextAlignment TextAlignment
        {
            get => GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the placeholder or descriptive text that is displayed even if the <see cref="Text"/>
        /// property is not yet set.
        /// </summary>
        public string? Watermark
        {
            get => GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Watermark"/> will still be shown above the
        /// <see cref="Text"/> even after a text value is set.
        /// </summary>
        public bool UseFloatingWatermark
        {
            get => GetValue(UseFloatingWatermarkProperty);
            set => SetValue(UseFloatingWatermarkProperty, value);
        }

        /// <summary>
        /// Gets or sets custom content that is positioned on the left side of the text layout box
        /// </summary>
        public object? InnerLeftContent
        {
            get => GetValue(InnerLeftContentProperty);
            set => SetValue(InnerLeftContentProperty, value);
        }

        /// <summary>
        /// Gets or sets custom content that is positioned on the right side of the text layout box
        /// </summary>
        public object? InnerRightContent
        {
            get => GetValue(InnerRightContentProperty);
            set => SetValue(InnerRightContentProperty, value);
        }

        /// <summary>
        /// Gets or sets whether text masked by <see cref="PasswordChar"/> should be revealed
        /// </summary>
        public bool RevealPassword
        {
            get => GetValue(RevealPasswordProperty);
            set => SetValue(RevealPasswordProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="Media.TextWrapping"/> of the TextBox
        /// </summary>
        public TextWrapping TextWrapping
        {
            get => GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        /// <summary>
        /// Gets or sets which characters are inserted when Enter is pressed. Default: <see cref="Environment.NewLine"/>
        /// </summary>
        public string NewLine
        {
            get => GetValue(NewLineProperty);
            set => SetValue(NewLineProperty, value);
        }

        /// <summary>
        /// Clears the current selection, maintaining the <see cref="CaretIndex"/>
        /// </summary>
        public void ClearSelection()
        {
            SetCurrentValue(CaretIndexProperty, SelectionStart);
            SetCurrentValue(SelectionEndProperty, SelectionStart);
        }

        /// <summary>
        /// Property for determining if the Cut command can be executed.
        /// </summary>
        public bool CanCut
        {
            get => _canCut;
            private set => SetAndRaise(CanCutProperty, ref _canCut, value);
        }

        /// <summary>
        /// Property for determining if the Copy command can be executed.
        /// </summary>
        public bool CanCopy
        {
            get => _canCopy;
            private set => SetAndRaise(CanCopyProperty, ref _canCopy, value);
        }

        /// <summary>
        /// Property for determining if the Paste command can be executed.
        /// </summary>
        public bool CanPaste
        {
            get => _canPaste;
            private set => SetAndRaise(CanPasteProperty, ref _canPaste, value);
        }

        /// <summary>
        /// Property for determining whether undo/redo is enabled
        /// </summary>
        public bool IsUndoEnabled
        {
            get => GetValue(IsUndoEnabledProperty);
            set => SetValue(IsUndoEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum number of items that can reside in the Undo stack
        /// </summary>
        public int UndoLimit
        {
            get => GetValue(UndoLimitProperty);
            set => SetValue(UndoLimitProperty, value);
        }

        private void OnUndoLimitChanged(int newValue)
        {
            _undoRedoHelper.Limit = newValue;

            // from docs at
            // https://docs.microsoft.com/en-us/dotnet/api/system.windows.controls.primitives.textboxbase.isundoenabled:
            // "Setting UndoLimit clears the undo queue."
            _undoRedoHelper.Clear();
            _selectedTextChangesMadeSinceLastUndoSnapshot = 0;
            _hasDoneSnapshotOnce = false;
        }

        /// <summary>
        /// Gets a value that indicates whether the undo stack has an action that can be undone
        /// </summary>
        public bool CanUndo
        {
            get => _canUndo;
            private set => SetAndRaise(CanUndoProperty, ref _canUndo, value);
        }

        /// <summary>
        /// Gets a value that indicates whether the redo stack has an action that can be redone
        /// </summary>
        public bool CanRedo
        {
            get => _canRedo;
            private set => SetAndRaise(CanRedoProperty, ref _canRedo, value);
        }

        /// <summary>
        /// Get the number of lines in the TextBox.
        /// </summary>
        /// <value>number of lines in the TextBox, or -1 if no layout information is available</value>
        /// <remarks>
        /// If Wrap == true, changing the width of the TextBox may change this value.
        /// The value returned is the number of lines in the entire TextBox, regardless of how many are
        /// currently in view.
        /// </remarks>
        public int GetLineCount()
        {
            return this._presenter?.TextLayout.TextLines.Count ?? -1;
        }

        /// <summary>
        /// Raised when content is being copied to the clipboard
        /// </summary>
        public event EventHandler<RoutedEventArgs>? CopyingToClipboard
        {
            add => AddHandler(CopyingToClipboardEvent, value);
            remove => RemoveHandler(CopyingToClipboardEvent, value);
        }

        /// <summary>
        /// Raised when content is being cut to the clipboard
        /// </summary>
        public event EventHandler<RoutedEventArgs>? CuttingToClipboard
        {
            add => AddHandler(CuttingToClipboardEvent, value);
            remove => RemoveHandler(CuttingToClipboardEvent, value);
        }

        /// <summary>
        /// Raised when content is being pasted from the clipboard
        /// </summary>
        public event EventHandler<RoutedEventArgs>? PastingFromClipboard
        {
            add => AddHandler(PastingFromClipboardEvent, value);
            remove => RemoveHandler(PastingFromClipboardEvent, value);
        }

        /// <summary>
        /// Occurs asynchronously after text changes and the new text is rendered.
        /// </summary>
        public event EventHandler<TextChangedEventArgs>? TextChanged
        {
            add => AddHandler(TextChangedEvent, value);
            remove => RemoveHandler(TextChangedEvent, value);
        }

        /// <summary>
        /// Occurs synchronously when text starts to change but before it is rendered.
        /// </summary>
        /// <remarks>
        /// This event occurs just after the <see cref="Text"/> property value has been updated.
        /// </remarks>
        public event EventHandler<TextChangingEventArgs>? TextChanging
        {
            add => AddHandler(TextChangingEvent, value);
            remove => RemoveHandler(TextChangingEvent, value);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            _presenter = e.NameScope.Get<TextPresenter>("PART_TextPresenter");

            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
            }

            _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");

            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            }

            _imClient.SetPresenter(_presenter, this);

            if (IsFocused)
            {
                _presenter?.ShowCaret();
            }
        }

        private void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            _presenter?.TextSelectionHandleCanvas?.MoveHandlesToSelection();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (_presenter != null)
            {
                if (IsFocused)
                {
                    _presenter.ShowCaret();
                }
                else
                {
                    if (IsInactiveSelectionHighlightEnabled)
                    {
                        _presenter.ShowSelectionHighlight = true;
                    }
                }

                _presenter.PropertyChanged += PresenterPropertyChanged;
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (_presenter != null)
            {
                _presenter.HideCaret();

                _presenter.PropertyChanged -= PresenterPropertyChanged;
            }

            _imClient.SetPresenter(null, null);
        }

        private void PresenterPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextPresenter.PreeditTextProperty)
            {
                if (string.IsNullOrEmpty(e.OldValue as string) && !string.IsNullOrEmpty(e.NewValue as string))
                {
                    PseudoClasses.Set(":empty", false);

                    DeleteSelection();
                }
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TextProperty)
            {
                CoerceValue(CaretIndexProperty);
                CoerceValue(SelectionStartProperty);
                CoerceValue(SelectionEndProperty);

                RaiseTextChangeEvents();

                UpdatePseudoclasses();
                UpdateCommandStates();
            }
            else if (change.Property == CaretIndexProperty)
            {
                OnCaretIndexChanged(change);
            }
            else if (change.Property == SelectionStartProperty)
            {
                OnSelectionStartChanged(change);
            }
            else if (change.Property == SelectionEndProperty)
            {
                OnSelectionEndChanged(change);
            }
            else if (change.Property == MaxLinesProperty)
            {
                InvalidateMeasure();
            }
            else if (change.Property == MinLinesProperty)
            {
                InvalidateMeasure();
            }
            else if (change.Property == UndoLimitProperty)
            {
                OnUndoLimitChanged(change.GetNewValue<int>());
            }
            else if (change.Property == IsUndoEnabledProperty && change.GetNewValue<bool>() == false)
            {
                // from docs at
                // https://docs.microsoft.com/en-us/dotnet/api/system.windows.controls.primitives.textboxbase.isundoenabled:
                // "Setting this property to false clears the undo stack.
                // Therefore, if you disable undo and then re-enable it, undo commands still do not work
                // because the undo stack was emptied when you disabled undo."
                _undoRedoHelper.Clear();
                _selectedTextChangesMadeSinceLastUndoSnapshot = 0;
                _hasDoneSnapshotOnce = false;
            }
        }

        private void UpdateCommandStates()
        {
            var text = GetSelection();
            var isSelectionNullOrEmpty = string.IsNullOrEmpty(text);
            CanCopy = !IsPasswordBox && !isSelectionNullOrEmpty;
            CanCut = !IsPasswordBox && !isSelectionNullOrEmpty && !IsReadOnly;
            CanPaste = !IsReadOnly;
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            if(_presenter != null)
            {
                _presenter.ShowSelectionHighlight = true;
            }           

            // when navigating to a textbox via the tab key, select all text if
            //   1) this textbox is *not* a multiline textbox
            //   2) this textbox has any text to select
            if (e.NavigationMethod == NavigationMethod.Tab &&
                !AcceptsReturn &&
                Text?.Length > 0)
            {
                SelectAll();
            }

            UpdateCommandStates();

            _imClient.SetPresenter(_presenter, this);

            _presenter?.ShowCaret();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if ((ContextFlyout == null || !ContextFlyout.IsOpen) &&
                (ContextMenu == null || !ContextMenu.IsOpen))
            {
                if (ClearSelectionOnLostFocus)
                {
                    ClearSelection();
                }

                SetCurrentValue(RevealPasswordProperty, false);
            }

            UpdateCommandStates();

            _presenter?.HideCaret();

            _imClient.SetPresenter(null, null);

            if (_presenter != null && !IsInactiveSelectionHighlightEnabled)
            {
                _presenter.ShowSelectionHighlight = false;
            }
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            if (!e.Handled)
            {
                HandleTextInput(e.Text);
                e.Handled = true;
            }
        }

        private void HandleTextInput(string? input)
        {
            if (IsReadOnly)
            {
                return;
            }

            input = SanitizeInputText(input);

            if (string.IsNullOrEmpty(input))
            {
                return;
            }

            _selectedTextChangesMadeSinceLastUndoSnapshot++;
            SnapshotUndoRedo(ignoreChangeCount: false);

            var currentText = Text ?? string.Empty;
            var selectionLength = Math.Abs(SelectionStart - SelectionEnd);
            var newLength = input.Length + currentText.Length - selectionLength;

            if (MaxLength > 0 && newLength > MaxLength)
            {
                input = input.Remove(Math.Max(0, input.Length - (newLength - MaxLength)));
                newLength = MaxLength;
            }

            if (!string.IsNullOrEmpty(input))
            {
                var textBuilder = StringBuilderCache.Acquire(Math.Max(currentText.Length, newLength));
                textBuilder.Append(currentText);

                var caretIndex = CaretIndex;

                if (selectionLength != 0)
                {
                    var (start, _) = GetSelectionRange();

                    textBuilder.Remove(start, selectionLength);

                    caretIndex = start;
                }

                textBuilder.Insert(caretIndex, input);

                var text = StringBuilderCache.GetStringAndRelease(textBuilder);

                SetCurrentValue(TextProperty, text);

                ClearSelection();

                if (IsUndoEnabled)
                {
                    _undoRedoHelper.DiscardRedo();
                }

                //Make sure updated text is in sync
                _presenter?.SetCurrentValue(TextPresenter.TextProperty, text);

                caretIndex += input.Length;

                //Make sure caret is in sync
                _presenter?.MoveCaretToTextPosition(caretIndex);

                SetCurrentValue(CaretIndexProperty, caretIndex);
            }
        }

        private string? SanitizeInputText(string? text)
        {
            if (text is null)
                return null;

            if (!AcceptsReturn)
            {
                var lineBreakStart = 0;
                var graphemeEnumerator = new GraphemeEnumerator(text.AsSpan());

                while (graphemeEnumerator.MoveNext(out var grapheme))
                {
                    if (grapheme.FirstCodepoint.IsBreakChar)
                    {
                        break;
                    }

                    lineBreakStart += grapheme.Length;
                }

                // All lines except the first one are discarded when TextBox does not accept Return key
                text = text.Substring(0, lineBreakStart);
            }

            for (var i = 0; i < invalidCharacters.Length; i++)
            {
                text = text.Replace(invalidCharacters[i], string.Empty);
            }

            return text;
        }

        /// <summary>
        /// Cuts the current text onto the clipboard
        /// </summary>
        public async void Cut()
        {
            var text = GetSelection();

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var eventArgs = new RoutedEventArgs(CuttingToClipboardEvent);
            RaiseEvent(eventArgs);
            if (!eventArgs.Handled)
            {
                SnapshotUndoRedo();

                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

                if (clipboard == null)
                    return;

                await clipboard.SetTextAsync(text);
                DeleteSelection();
            }
        }

        /// <summary>
        /// Copies the current text onto the clipboard
        /// </summary>
        public async void Copy()
        {
            var text = GetSelection();

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var eventArgs = new RoutedEventArgs(CopyingToClipboardEvent);
            RaiseEvent(eventArgs);
            if (!eventArgs.Handled)
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

                if (clipboard != null)
                    await clipboard.SetTextAsync(text);
            }
        }

        /// <summary>
        /// Pastes the current clipboard text content into the TextBox
        /// </summary>
        public async void Paste()
        {
            var eventArgs = new RoutedEventArgs(PastingFromClipboardEvent);
            RaiseEvent(eventArgs);
            if (eventArgs.Handled)
            {
                return;
            }

            string? text = null;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

            if (clipboard != null)
            {
                try
                {
                    text = await clipboard.GetTextAsync();
                }
                catch (TimeoutException)
                {
                    // Silently ignore.
                }
            }

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            SnapshotUndoRedo();
            HandleTextInput(text);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_presenter == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_presenter.PreeditText))
            {
                return;
            }

            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;
            var movement = false;
            var selection = false;
            var handled = false;
            var modifiers = e.KeyModifiers;

            var keymap = Application.Current!.PlatformSettings!.HotkeyConfiguration;

            using var _ = _imClient.BeginChange();

            bool Match(List<KeyGesture> gestures) => gestures.Any(g => g.Matches(e));
            bool DetectSelection() => e.KeyModifiers.HasAllFlags(keymap.SelectionModifiers);

            if (Match(keymap.SelectAll))
            {
                SelectAll();
                handled = true;
            }
            else if (Match(keymap.Copy))
            {
                if (!IsPasswordBox)
                {
                    Copy();
                }

                handled = true;
            }
            else if (Match(keymap.Cut))
            {
                if (!IsPasswordBox)
                {
                    Cut();
                }

                handled = true;
            }
            else if (Match(keymap.Paste))
            {
                Paste();
                handled = true;
            }
            else if (Match(keymap.Undo) && IsUndoEnabled)
            {
                Undo();

                handled = true;
            }
            else if (Match(keymap.Redo) && IsUndoEnabled)
            {
                Redo();

                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheStartOfDocument))
            {
                MoveHome(true);
                movement = true;
                selection = false;
                handled = true;
                SetCurrentValue(CaretIndexProperty, _presenter.CaretIndex);
            }
            else if (Match(keymap.MoveCursorToTheEndOfDocument))
            {
                MoveEnd(true);
                movement = true;
                selection = false;
                handled = true;
                SetCurrentValue(CaretIndexProperty, _presenter.CaretIndex);
            }
            else if (Match(keymap.MoveCursorToTheStartOfLine))
            {
                MoveHome(false);
                movement = true;
                selection = false;
                handled = true;
                SetCurrentValue(CaretIndexProperty, _presenter.CaretIndex);
            }
            else if (Match(keymap.MoveCursorToTheEndOfLine))
            {
                MoveEnd(false);
                movement = true;
                selection = false;
                handled = true;
                SetCurrentValue(CaretIndexProperty, _presenter.CaretIndex);
            }
            else if (Match(keymap.MoveCursorToTheStartOfDocumentWithSelection))
            {
                SetCurrentValue(SelectionStartProperty, caretIndex);
                MoveHome(true);
                SetCurrentValue(SelectionEndProperty, _presenter.CaretIndex);
                movement = true;
                selection = true;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheEndOfDocumentWithSelection))
            {
                SetCurrentValue(SelectionStartProperty, caretIndex);
                MoveEnd(true);
                SetCurrentValue(SelectionEndProperty, _presenter.CaretIndex);
                movement = true;
                selection = true;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheStartOfLineWithSelection))
            {
                SetCurrentValue(SelectionStartProperty, caretIndex);
                MoveHome(false);
                SetCurrentValue(SelectionEndProperty, _presenter.CaretIndex);
                movement = true;
                selection = true;
                handled = true;

            }
            else if (Match(keymap.MoveCursorToTheEndOfLineWithSelection))
            {
                SetCurrentValue(SelectionStartProperty, caretIndex);
                MoveEnd(false);
                SetCurrentValue(SelectionEndProperty, _presenter.CaretIndex);
                movement = true;
                selection = true;
                handled = true;
            }
            else if (Match(keymap.PageLeft))
            {
                MovePageLeft();
                movement = true;
                selection = false;
                handled = true;
            }
            else if (Match(keymap.PageRight))
            {
                MovePageRight();
                movement = true;
                selection = false;
                handled = true;
            }
            else if (Match(keymap.PageUp))
            {
                MovePageUp();
                movement = true;
                selection = false;
                handled = true;
            }
            else if (Match(keymap.PageDown))
            {
                MovePageDown();
                movement = true;
                selection = false;
                handled = true;
            }
            else
            {
                // It's not secure to rely on password field content when moving.
                bool hasWholeWordModifiers = modifiers.HasAllFlags(keymap.WholeWordTextActionModifiers) && !IsPasswordBox;
                switch (e.Key)
                {
                    case Key.Left:
                        selection = DetectSelection();
                        MoveHorizontal(-1, hasWholeWordModifiers, selection, true);
                        if (caretIndex != _presenter.CaretIndex)
                        {
                            movement = true;
                        }
                        break;

                    case Key.Right:
                        selection = DetectSelection();
                        MoveHorizontal(1, hasWholeWordModifiers, selection, true);
                        if (caretIndex != _presenter.CaretIndex)
                        {
                            movement = true;
                        }
                        break;

                    case Key.Up:
                        {
                            selection = DetectSelection();

                            _presenter.MoveCaretVertical(LogicalDirection.Backward);

                            if (caretIndex != _presenter.CaretIndex)
                            {
                                movement = true;
                            }

                            if (selection)
                            {
                                SetCurrentValue(SelectionEndProperty, _presenter.CaretIndex);
                            }
                            else
                            {
                                SetCurrentValue(CaretIndexProperty, _presenter.CaretIndex);
                            }

                            break;
                        }
                    case Key.Down:
                        {
                            selection = DetectSelection();

                            _presenter.MoveCaretVertical();

                            if (caretIndex != _presenter.CaretIndex)
                            {
                                movement = true;
                            }

                            if (selection)
                            {
                                SetCurrentValue(SelectionEndProperty, _presenter.CaretIndex);
                            }
                            else
                            {
                                SetCurrentValue(CaretIndexProperty, _presenter.CaretIndex);
                            }

                            break;
                        }
                    case Key.Back:
                        {
                            SnapshotUndoRedo();

                            if (hasWholeWordModifiers && SelectionStart == SelectionEnd)
                            {
                                SetSelectionForControlBackspace();
                            }

                            if (!DeleteSelection())
                            {
                                var characterHit = _presenter.GetNextCharacterHit(LogicalDirection.Backward);

                                var backspacePosition = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

                                var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(caretIndex, true);

                                var backspaceCharacterHit = _presenter.TextLayout.TextLines[lineIndex]
                                    .GetBackspaceCaretCharacterHit(new CharacterHit(caretIndex));

                                if (backspaceCharacterHit.FirstCharacterIndex > backspacePosition &&
                                    backspaceCharacterHit.FirstCharacterIndex < caretIndex)
                                {
                                    backspacePosition = backspaceCharacterHit.FirstCharacterIndex;
                                }

                                if (caretIndex != backspacePosition)
                                {
                                    var start = Math.Min(backspacePosition, caretIndex);
                                    var end = Math.Max(backspacePosition, caretIndex);

                                    var length = end - start;

                                    var sb = StringBuilderCache.Acquire(text.Length);
                                    sb.Append(text);
                                    sb.Remove(start, end - start);

                                    SetCurrentValue(TextProperty, StringBuilderCache.GetStringAndRelease(sb));

                                    SetCurrentValue(CaretIndexProperty, start);

                                    _presenter.MoveCaretToTextPosition(start);
                                }
                            }

                            SnapshotUndoRedo();

                            handled = true;
                            break;
                        }
                    case Key.Delete:
                        SnapshotUndoRedo();

                        if (hasWholeWordModifiers && SelectionStart == SelectionEnd)
                        {
                            SetSelectionForControlDelete();
                        }

                        if (!DeleteSelection())
                        {
                            var characterHit = _presenter.GetNextCharacterHit();

                            var nextPosition = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

                            if (nextPosition != caretIndex)
                            {
                                var start = Math.Min(nextPosition, caretIndex);
                                var end = Math.Max(nextPosition, caretIndex);

                                var sb = StringBuilderCache.Acquire(text.Length);
                                sb.Append(text);
                                sb.Remove(start, end - start);

                                SetCurrentValue(TextProperty, StringBuilderCache.GetStringAndRelease(sb));
                            }
                        }

                        SnapshotUndoRedo();

                        handled = true;
                        break;

                    case Key.Enter:
                        if (AcceptsReturn)
                        {
                            SnapshotUndoRedo();
                            HandleTextInput(NewLine);
                            handled = true;
                        }

                        break;

                    case Key.Tab:
                        if (AcceptsTab)
                        {
                            SnapshotUndoRedo();
                            HandleTextInput("\t");
                            handled = true;
                        }
                        else
                        {
                            base.OnKeyDown(e);
                        }

                        break;

                    case Key.Space:
                        SnapshotUndoRedo(); // always snapshot in between words
                        break;

                    default:
                        handled = false;
                        break;
                }
            }

            if (movement && !selection)
            {
                ClearSelection();
            }

            if (handled || movement)
            {
                e.Handled = true;
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (_presenter == null)
            {
                return;
            }

            var text = Text;
            var clickInfo = e.GetCurrentPoint(this);

            using var _ = _imClient.BeginChange();

            if (text != null && (e.Pointer.Type == PointerType.Mouse || e.ClickCount >= 2) && clickInfo.Properties.IsLeftButtonPressed &&
                !(clickInfo.Pointer?.Captured is Border))
            {
                _currentClickCount = e.ClickCount;
                var point = e.GetPosition(_presenter);

                _presenter.MoveCaretToPoint(point);

                var caretIndex = _presenter.CaretIndex;
                var clickToSelect = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
                var selectionStart = SelectionStart;
                var selectionEnd = SelectionEnd;

                switch (e.ClickCount)
                {
                    case 1:
                        if (clickToSelect)
                        {
                            if (_wordSelectionStart >= 0)
                            {
                                UpdateWordSelectionRange(caretIndex, ref selectionStart, ref selectionEnd);

                                SetCurrentValue(SelectionStartProperty, selectionStart);
                                SetCurrentValue(SelectionEndProperty, selectionEnd);
                            }
                            else
                            {
                                SetCurrentValue(SelectionEndProperty, caretIndex);
                            }
                        }
                        else
                        {
                            SetCurrentValue(SelectionStartProperty, caretIndex);
                            SetCurrentValue(SelectionEndProperty, caretIndex);
                            _wordSelectionStart = -1;
                        }

                        break;
                    case 2:
                        if (IsPasswordBox && !RevealPassword)
                        {
                            // double-clicking in a cloaked single-line password box selects all text
                            // see https://github.com/AvaloniaUI/Avalonia/issues/14956
                            goto case 3;
                        }

                        if (!StringUtils.IsStartOfWord(text, caretIndex))
                        {
                            selectionStart = StringUtils.PreviousWord(text, caretIndex);
                        }

                        if (!StringUtils.IsEndOfWord(text, caretIndex))
                        {
                            selectionEnd = StringUtils.NextWord(text, caretIndex);
                        }

                        if (selectionStart != selectionEnd)
                        {
                            _wordSelectionStart = selectionStart;
                        }

                        SetCurrentValue(SelectionStartProperty, selectionStart);
                        SetCurrentValue(SelectionEndProperty, selectionEnd);

                        break;
                    case 3:
                        _wordSelectionStart = -1;

                        SelectAll();
                        break;
                }
            }

            _isDoubleTapped = e.ClickCount == 2;
            e.Pointer.Capture(_presenter);
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_presenter == null || _isHolding)
            {
                return;
            }
            using var _ = _imClient.BeginChange();

            // selection should not change during pointer move if the user right clicks
            if (e.Pointer.Captured == _presenter && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var point = e.GetPosition(_presenter);

                point = new Point(
                    MathUtilities.Clamp(point.X, 0, Math.Max(_presenter.Bounds.Width - 1, 0)),
                    MathUtilities.Clamp(point.Y, 0, Math.Max(_presenter.Bounds.Height - 1, 0)));

                var previousIndex = _presenter.CaretIndex;

                _presenter.MoveCaretToPoint(point);

                var caretIndex = _presenter.CaretIndex;

                if (Math.Abs(caretIndex - previousIndex) == 1)
                    e.PreventGestureRecognition();

                if (e.Pointer.Type == PointerType.Mouse || _isDoubleTapped)
                {
                    var selectionStart = SelectionStart;
                    var selectionEnd = SelectionEnd;

                    if (_wordSelectionStart >= 0)
                    {
                        UpdateWordSelectionRange(caretIndex, ref selectionStart, ref selectionEnd);

                        SetCurrentValue(SelectionStartProperty, selectionStart);
                        SetCurrentValue(SelectionEndProperty, selectionEnd);
                    }
                    else
                    {
                        SetCurrentValue(SelectionEndProperty, caretIndex);
                    }
                }
                else
                {
                    SetCurrentValue(SelectionStartProperty, caretIndex);
                    SetCurrentValue(SelectionEndProperty, caretIndex);
                }
            }
        }

        private void UpdateWordSelectionRange(int caretIndex, ref int selectionStart, ref int selectionEnd)
        {
            var text = Text;

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (caretIndex > _wordSelectionStart)
            {
                var nextWord = StringUtils.NextWord(text, caretIndex);

                selectionEnd = nextWord;

                selectionStart = _wordSelectionStart;
            }
            else
            {
                var previousWord = StringUtils.PreviousWord(text, caretIndex);
                selectionStart = previousWord;

                selectionEnd = StringUtils.NextWord(text, _wordSelectionStart);
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_presenter == null)
            {
                return;
            }

            if (e.Pointer.Captured != _presenter)
            {
                return;
            }

            using var _ = _imClient.BeginChange();

            if (e.Pointer.Type != PointerType.Mouse && !_isDoubleTapped)
            {
                _imClient.ShowInputPanel();

                var text = Text;
                var clickInfo = e.GetCurrentPoint(this);
                if (text != null && !(clickInfo.Pointer?.Captured is Border))
                {
                    var point = e.GetPosition(_presenter);

                    _presenter.MoveCaretToPoint(point);

                    var caretIndex = _presenter.CaretIndex;
                    var clickToSelect = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
                    var selectionStart = SelectionStart;
                    var selectionEnd = SelectionEnd;

                    if (clickToSelect)
                    {
                        if (_wordSelectionStart >= 0)
                        {
                            UpdateWordSelectionRange(caretIndex, ref selectionStart, ref selectionEnd);

                            SetCurrentValue(SelectionStartProperty, selectionStart);
                            SetCurrentValue(SelectionEndProperty, selectionEnd);
                        }
                        else
                        {
                            SetCurrentValue(SelectionEndProperty, caretIndex);
                        }
                    }
                    else
                    {
                        SetCurrentValue(SelectionStartProperty, caretIndex);
                        SetCurrentValue(SelectionEndProperty, caretIndex);
                        _wordSelectionStart = -1;
                    }

                    _presenter.TextSelectionHandleCanvas?.MoveHandlesToSelection();
                }
            }

            // Don't update selection if the pointer was held
            if (_isHolding)
            {
                _isHolding = false;
            }
            else if (e.InitialPressMouseButton == MouseButton.Right)
            {
                var point = e.GetPosition(_presenter);

                _presenter.MoveCaretToPoint(point);

                var caretIndex = _presenter.CaretIndex;

                // see if mouse clicked inside current selection
                // if it did not, we change the selection to where the user clicked
                var firstSelection = Math.Min(SelectionStart, SelectionEnd);
                var lastSelection = Math.Max(SelectionStart, SelectionEnd);
                var didClickInSelection = SelectionStart != SelectionEnd &&
                                          caretIndex >= firstSelection && caretIndex <= lastSelection;
                if (!didClickInSelection)
                {
                    SetCurrentValue(CaretIndexProperty, caretIndex);
                    SetCurrentValue(SelectionEndProperty, caretIndex);
                    SetCurrentValue(SelectionStartProperty, caretIndex);
                }
            }
            else if (e.Pointer.Type == PointerType.Touch)
            {
                if (_currentClickCount == 1)
                {
                    var point = e.GetPosition(_presenter);

                    _presenter.MoveCaretToPoint(point);

                    var caretIndex = _presenter.CaretIndex;
                    SetCurrentValue(SelectionStartProperty, caretIndex);
                    SetCurrentValue(SelectionEndProperty, caretIndex);
                }

                if (SelectionStart != SelectionEnd)
                {
                    _presenter.TextSelectionHandleCanvas?.ShowContextMenu();
                }
            }

            e.Pointer.Capture(null);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TextBoxAutomationPeer(this);
        }

        protected override void UpdateDataValidation(
            AvaloniaProperty property,
            BindingValueType state,
            Exception? error)
        {
            if (property == TextProperty)
            {
                DataValidationErrors.SetError(this, error);
            }
        }

        internal static int CoerceCaretIndex(AvaloniaObject sender, int value)
        {
            var text = sender.GetValue(TextProperty); // method also used by TextPresenter and SelectableTextBlock

            if (text == null)
            {
                return 0;
            }
            var length = text.Length;

            if (value < 0)
            {
                return 0;
            }
            else if (value > length)
            {
                return length;
            }
            else if (value > 0 && text[value - 1] == '\r' && value < length && text[value] == '\n')
            {
                return value + 1;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Clears the text in the TextBox
        /// </summary>
        public void Clear() => SetCurrentValue(TextProperty, string.Empty);

        private void MoveHorizontal(int direction, bool wholeWord, bool isSelecting, bool moveCaretPosition)
        {
            if (_presenter == null)
            {
                return;
            }

            using var _ = _imClient.BeginChange();

            var text = Text ?? string.Empty;
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;

            if (!wholeWord)
            {
                if (isSelecting)
                {
                    _presenter.MoveCaretToTextPosition(selectionEnd);

                    _presenter.MoveCaretHorizontal(direction > 0 ?
                        LogicalDirection.Forward :
                        LogicalDirection.Backward);

                    SetCurrentValue(SelectionEndProperty, _presenter.CaretIndex);
                }
                else
                {
                    if (selectionStart != selectionEnd)
                    {
                        _presenter.MoveCaretToTextPosition(direction > 0 ?
                            Math.Max(selectionStart, selectionEnd) :
                            Math.Min(selectionStart, selectionEnd));
                    }
                    else
                    {
                        _presenter.MoveCaretHorizontal(direction > 0 ?
                            LogicalDirection.Forward :
                            LogicalDirection.Backward);
                    }

                    SetCurrentValue(CaretIndexProperty, _presenter.CaretIndex);
                }
            }
            else
            {
                int offset;

                if (direction > 0)
                {
                    offset = StringUtils.NextWord(text, selectionEnd) - selectionEnd;
                }
                else
                {
                    offset = StringUtils.PreviousWord(text, selectionEnd) - selectionEnd;
                }

                SetCurrentValue(SelectionEndProperty, SelectionEnd + offset);

                if (moveCaretPosition)
                {
                    _presenter.MoveCaretToTextPosition(SelectionEnd);
                }

                if (!isSelecting && moveCaretPosition)
                {
                    SetCurrentValue(CaretIndexProperty, SelectionEnd);
                }
                else
                {
                    SetCurrentValue(SelectionStartProperty, selectionStart);
                }
            }
        }

        private void MoveHome(bool document)
        {
            if (_presenter is null)
            {
                return;
            }

            var caretIndex = CaretIndex;

            if (document)
            {
                _presenter.MoveCaretToTextPosition(0);
            }
            else
            {
                var textLines = _presenter.TextLayout.TextLines;
                var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(caretIndex, false);
                var textLine = textLines[lineIndex];

                _presenter.MoveCaretToTextPosition(textLine.FirstTextSourceIndex);
            }
        }

        private void MoveEnd(bool document)
        {
            if (_presenter is null)
            {
                return;
            }

            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;

            if (document)
            {
                _presenter.MoveCaretToTextPosition(text.Length, true);
            }
            else
            {
                var textLines = _presenter.TextLayout.TextLines;
                var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(caretIndex, false);
                var textLine = textLines[lineIndex];

                var textPosition = textLine.FirstTextSourceIndex + textLine.Length - textLine.NewLineLength;

                _presenter.MoveCaretToTextPosition(textPosition, true);
            }
        }

        private void MovePageRight()
        {
            _scrollViewer?.PageRight();
        }

        private void MovePageLeft()
        {
            _scrollViewer?.PageLeft();
        }
        private void MovePageUp()
        {
            _scrollViewer?.PageUp();
        }

        private void MovePageDown()
        {
            _scrollViewer?.PageDown();
        }

        /// <summary>
        /// Scroll the <see cref="TextBox"/> to the specified line index.
        /// </summary>
        /// <param name="lineIndex">The line index to scroll to.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineIndex"/> is less than zero. -or - <paramref name="lineIndex"/> is larger than or equal to the line count.</exception>
        public void ScrollToLine(int lineIndex)
        {
            if (_presenter is null)
            {
                return;
            }

            if (lineIndex < 0 || lineIndex >= _presenter.TextLayout.TextLines.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(lineIndex));
            }

            var textLine = _presenter.TextLayout.TextLines[lineIndex];
            _presenter.MoveCaretToTextPosition(textLine.FirstTextSourceIndex);

        }

        /// <summary>
        /// Select all text in the TextBox
        /// </summary>
        public void SelectAll()
        {
            using var _ = _imClient.BeginChange();

            SetCurrentValue(SelectionStartProperty, 0);
            SetCurrentValue(SelectionEndProperty, Text?.Length ?? 0);
        }

        private (int start, int end) GetSelectionRange()
        {
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;

            return (Math.Min(selectionStart, selectionEnd), Math.Max(selectionStart, selectionEnd));
        }

        internal bool DeleteSelection()
        {
            if (IsReadOnly)
                return true;

            using var _ = _imClient.BeginChange();

            var (start, end) = GetSelectionRange();

            if (start != end)
            {
                var text = Text!;
                var textBuilder = StringBuilderCache.Acquire(text.Length);

                textBuilder.Append(text);
                textBuilder.Remove(start, end - start);

                SetCurrentValue(TextProperty, textBuilder.ToString());

                _presenter?.MoveCaretToTextPosition(start);

                SetCurrentValue(SelectionStartProperty, start);

                ClearSelection();

                return true;
            }

            SetCurrentValue(CaretIndexProperty, SelectionStart);

            return false;
        }

        private string GetSelection()
        {
            var text = Text;

            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var start = Math.Min(selectionStart, selectionEnd);
            var end = Math.Max(selectionStart, selectionEnd);

            if (start == end || (Text?.Length ?? 0) < end)
            {
                return "";
            }

            return text.Substring(start, end - start);
        }

        /// <summary>
        /// Returns the sum of any vertical whitespace added between the <see cref="ScrollViewer"/> and <see cref="TextPresenter"/> in the control template.
        /// </summary>
        /// <returns>The total vertical whitespace.</returns>
        private double GetVerticalSpaceBetweenScrollViewerAndPresenter()
        {
            var verticalSpace = 0.0;
            if (_presenter != null)
            {
                Visual? visual = _presenter;
                while ((visual != null) && (visual != this))
                {
                    if (visual == _scrollViewer)
                    {
                        // ScrollViewer is a stopping point and should only include the Padding
                        verticalSpace += _scrollViewer.Padding.Top + _scrollViewer.Padding.Bottom;
                        break;
                    }

                    var margin = visual.GetValue<Thickness>(Layoutable.MarginProperty);
                    var padding = visual.GetValue<Thickness>(Decorator.PaddingProperty);

                    verticalSpace += margin.Top + padding.Top + padding.Bottom + margin.Bottom;

                    visual = visual.VisualParent;
                }
            }

            return verticalSpace;
        }

        /// <summary>
        /// Raises both the <see cref="TextChanging"/> and <see cref="TextChanged"/> events.
        /// </summary>
        /// <remarks>
        /// This must be called after the <see cref="Text"/> property is set.
        /// </remarks>
        private void RaiseTextChangeEvents()
        {
            // Note the following sequence of these events (following WinUI)
            // 1. TextChanging occurs synchronously when text starts to change but before it is rendered.
            //    This occurs after the Text property is set.
            // 2. TextChanged occurs asynchronously after text changes and the new text is rendered.

            var textChangingEventArgs = new TextChangingEventArgs(TextChangingEvent);
            RaiseEvent(textChangingEventArgs);

            Dispatcher.UIThread.Post(() =>
            {
                var textChangedEventArgs = new TextChangedEventArgs(TextChangedEvent);
                RaiseEvent(textChangedEventArgs);
            }, DispatcherPriority.Normal);
        }

        private void SetSelectionForControlBackspace()
        {
            var text = Text ?? string.Empty;
            var selectionStart = CaretIndex;

            using var _ = _imClient.BeginChange();

            MoveHorizontal(-1, true, false, false);

            if (SelectionEnd > 0 &&
                selectionStart < text.Length && text[selectionStart] == ' ')
            {
                SetCurrentValue(SelectionEndProperty, SelectionEnd - 1);
            }

            SetCurrentValue(SelectionStartProperty, selectionStart);
        }

        private void SetSelectionForControlDelete()
        {
            var textLength = Text?.Length ?? 0;
            if (_presenter == null || textLength == 0)
            {
                return;
            }

            using var _ = _imClient.BeginChange();

            SetCurrentValue(SelectionStartProperty, CaretIndex);

            MoveHorizontal(1, true, true, false);

            if (SelectionEnd < textLength && Text![SelectionEnd] == ' ')
            {
                SetCurrentValue(SelectionEndProperty, SelectionEnd + 1);
            }
        }

        private void UpdatePseudoclasses()
        {
            PseudoClasses.Set(":empty", string.IsNullOrEmpty(Text));
        }

        private bool IsPasswordBox => PasswordChar != default(char);

        UndoRedoState UndoRedoHelper<UndoRedoState>.IUndoRedoHost.UndoRedoState
        {
            get => new UndoRedoState(Text, CaretIndex);
            set
            {
                SetCurrentValue(TextProperty, value.Text);
                SetCurrentValue(CaretIndexProperty, value.CaretPosition);
                ClearSelection();
            }
        }

        private void SnapshotUndoRedo(bool ignoreChangeCount = true)
        {
            if (IsUndoEnabled)
            {
                if (ignoreChangeCount ||
                    !_hasDoneSnapshotOnce ||
                    (!ignoreChangeCount &&
                        _selectedTextChangesMadeSinceLastUndoSnapshot >= _maxCharsBeforeUndoSnapshot))
                {
                    _undoRedoHelper.Snapshot();
                    _selectedTextChangesMadeSinceLastUndoSnapshot = 0;
                    _hasDoneSnapshotOnce = true;
                }
            }
        }

        /// <summary>
        /// Undoes the first action in the undo stack
        /// </summary>
        public void Undo()
        {
            if (IsUndoEnabled && CanUndo)
            {
                try
                {
                    // Snapshot the current Text state - this will get popped on to the redo stack
                    // when we call undo below
                    SnapshotUndoRedo();
                    _isUndoingRedoing = true;
                    _undoRedoHelper.Undo();
                }
                finally
                {
                    _isUndoingRedoing = false;
                }
            }
        }

        /// <summary>
        /// Reapplies the first item on the redo stack
        /// </summary>
        public void Redo()
        {
            if (IsUndoEnabled && CanRedo)
            {
                try
                {
                    _isUndoingRedoing = true;
                    _undoRedoHelper.Redo();
                }
                finally
                {
                    _isUndoingRedoing = false;
                }
            }
        }

        /// <summary>
        /// Called from the UndoRedoHelper when the undo stack is modified
        /// </summary>
        void UndoRedoHelper<UndoRedoState>.IUndoRedoHost.OnUndoStackChanged()
        {
            CanUndo = _undoRedoHelper.CanUndo;
        }

        /// <summary>
        /// Called from the UndoRedoHelper when the redo stack is modified
        /// </summary>
        void UndoRedoHelper<UndoRedoState>.IUndoRedoHost.OnRedoStackChanged()
        {
            CanRedo = _undoRedoHelper.CanRedo;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_scrollViewer != null)
            {
                var maxHeight = double.PositiveInfinity;

                if (MaxLines > 0 && double.IsNaN(Height))
                {
                    var fontSize = FontSize;
                    var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
                    var paragraphProperties = TextLayout.CreateTextParagraphProperties(typeface, fontSize, null, default, default, null, default, LineHeight, default, FontFeatures);
                    var textLayout = new TextLayout(new LineTextSource(MaxLines), paragraphProperties);
                    var verticalSpace = GetVerticalSpaceBetweenScrollViewerAndPresenter();

                    maxHeight = Math.Ceiling(textLayout.Height + verticalSpace);
                }

                _scrollViewer.SetCurrentValue(MaxHeightProperty, maxHeight);


                var minHeight = 0.0;

                if (MinLines > 0 && double.IsNaN(Height))
                {
                    var fontSize = FontSize;
                    var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
                    var paragraphProperties = TextLayout.CreateTextParagraphProperties(typeface, fontSize, null, default, default, null, default, LineHeight, default, FontFeatures);
                    var textLayout = new TextLayout(new LineTextSource(MinLines), paragraphProperties);
                    var verticalSpace = GetVerticalSpaceBetweenScrollViewerAndPresenter();

                    minHeight = Math.Ceiling(textLayout.Height + verticalSpace);
                }

                _scrollViewer.SetCurrentValue(MinHeightProperty, minHeight);
            }

            return base.MeasureOverride(availableSize);
        }

        private class LineTextSource : ITextSource
        {
            private readonly int _lines;

            public LineTextSource(int lines)
            {
                _lines = lines;
            }

            public TextRun? GetTextRun(int textSourceIndex)
            {
                if (textSourceIndex >= _lines)
                {
                    return null;
                }

                return new TextEndOfLine(1);
            }
        }
    }
}
