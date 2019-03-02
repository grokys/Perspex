// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    public class TextBox : TemplatedControl, UndoRedoHelper<TextBox.UndoRedoState>.IUndoRedoHost
    {
        private static readonly string[] InvalidCharacters = { "\u007f" };

        public static readonly StyledProperty<bool> AcceptsReturnProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(AcceptsReturn));

        public static readonly StyledProperty<bool> AcceptsTabProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(AcceptsTab));

        public static readonly DirectProperty<TextBox, int> CaretIndexProperty =
            AvaloniaProperty.RegisterDirect<TextBox, int>(
                nameof(CaretIndex),
                o => o.CaretIndex,
                (o, v) => o.CaretIndex = v);

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(IsReadOnly));

        public static readonly StyledProperty<char> PasswordCharProperty =
            AvaloniaProperty.Register<TextBox, char>(nameof(PasswordChar));

        public static readonly DirectProperty<TextBox, int> SelectionStartProperty =
            AvaloniaProperty.RegisterDirect<TextBox, int>(
                nameof(SelectionStart),
                o => o.SelectionStart,
                (o, v) => o.SelectionStart = v);

        public static readonly DirectProperty<TextBox, int> SelectionEndProperty =
            AvaloniaProperty.RegisterDirect<TextBox, int>(
                nameof(SelectionEnd),
                o => o.SelectionEnd,
                (o, v) => o.SelectionEnd = v);

        public static readonly DirectProperty<TextBox, string> TextProperty = TextBlock.TextProperty.AddOwner<TextBox>(
            o => o.Text,
            (o, v) => o.Text = v,
            defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true);

        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            TextBlock.TextAlignmentProperty.AddOwner<TextBox>();

        public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
            TextBlock.TextWrappingProperty.AddOwner<TextBox>();

        public static readonly StyledProperty<string> WatermarkProperty =
            AvaloniaProperty.Register<TextBox, string>(nameof(Watermark));

        public static readonly StyledProperty<bool> UseFloatingWatermarkProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(UseFloatingWatermark));

        public static readonly DirectProperty<TextBox, string> NewLineProperty =
            AvaloniaProperty.RegisterDirect<TextBox, string>(
                nameof(NewLine),
                textbox => textbox.NewLine,
                (textbox, newline) => textbox.NewLine = newline);

        private string _text;

        private int _caretIndex;

        private int _selectionStart;

        private int _selectionEnd;

        private TextPresenter _presenter;

        private readonly UndoRedoHelper<UndoRedoState> _undoRedoHelper;

        private bool _isUndoingRedoing;

        private bool _ignoreTextChanges;

        private string _newLine = Environment.NewLine;

        private int _currentOffset;

        static TextBox()
        {
            FocusableProperty.OverrideDefaultValue(typeof(TextBox), true);
        }

        public TextBox()
        {
            var horizontalScrollBarVisibility = this.GetObservable(AcceptsReturnProperty).CombineLatest(
                this.GetObservable(TextWrappingProperty),
                (acceptsReturn, wrapping) =>
                {
                    if (acceptsReturn)
                    {
                        return wrapping == TextWrapping.NoWrap
                                   ? ScrollBarVisibility.Auto
                                   : ScrollBarVisibility.Disabled;
                    }

                    return ScrollBarVisibility.Hidden;
                });
            Bind(
                ScrollViewer.HorizontalScrollBarVisibilityProperty,
                horizontalScrollBarVisibility,
                BindingPriority.Style);
            _undoRedoHelper = new UndoRedoHelper<UndoRedoState>(this);
        }

        public bool AcceptsReturn
        {
            get => GetValue(AcceptsReturnProperty);
            set => SetValue(AcceptsReturnProperty, value);
        }

        public bool AcceptsTab
        {
            get => GetValue(AcceptsTabProperty);
            set => SetValue(AcceptsTabProperty, value);
        }

        public int CaretIndex
        {
            get => _caretIndex;

            set
            {
                value = CoerceCaretIndex(value);

                SetAndRaise(CaretIndexProperty, ref _caretIndex, value);

                if (_undoRedoHelper.TryGetLastState(out var state) && state.Text == Text)
                {
                    _undoRedoHelper.UpdateLastState();
                }
            }
        }

        public bool IsReadOnly
        {
            get => GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public char PasswordChar
        {
            get => GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }

        public int SelectionStart
        {
            get => _selectionStart;

            set
            {
                value = CoerceCaretIndex(value);

                SetAndRaise(SelectionStartProperty, ref _selectionStart, value);

                if (SelectionStart == SelectionEnd)
                {
                    CaretIndex = SelectionStart;
                }
            }
        }

        public int SelectionEnd
        {
            get => _selectionEnd;

            set
            {
                value = CoerceCaretIndex(value);

                SetAndRaise(SelectionEndProperty, ref _selectionEnd, value);

                if (SelectionStart == SelectionEnd)
                {
                    CaretIndex = SelectionEnd;
                }
            }
        }

        [Content]
        public string Text
        {
            get => _text;

            set
            {
                if (_ignoreTextChanges)
                {
                    return;
                }

                var caretIndex = CaretIndex;
                SelectionStart = CoerceCaretIndex(SelectionStart, value?.Length ?? 0);
                SelectionEnd = CoerceCaretIndex(SelectionEnd, value?.Length ?? 0);
                CaretIndex = CoerceCaretIndex(caretIndex, value?.Length ?? 0);

                if (SetAndRaise(TextProperty, ref _text, value) && !_isUndoingRedoing)
                {
                    _undoRedoHelper.Clear();
                }
            }
        }

        public TextAlignment TextAlignment
        {
            get => GetValue(TextAlignmentProperty);

            set => SetValue(TextAlignmentProperty, value);
        }

        public string Watermark
        {
            get => GetValue(WatermarkProperty);

            set => SetValue(WatermarkProperty, value);
        }

        public bool UseFloatingWatermark
        {
            get => GetValue(UseFloatingWatermarkProperty);

            set => SetValue(UseFloatingWatermarkProperty, value);
        }

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
            get => _newLine;

            set => SetAndRaise(NewLineProperty, ref _newLine, value);
        }

        UndoRedoState UndoRedoHelper<UndoRedoState>.IUndoRedoHost.UndoRedoState
        {
            get => new UndoRedoState(Text, CaretIndex);

            set
            {
                Text = value.Text;
                SelectionStart = SelectionEnd = CaretIndex = value.CaretPosition;
            }
        }

        private bool IsPasswordBox => PasswordChar != default(char);

        public string RemoveInvalidCharacters(string text)
        {
            for (var i = 0; i < InvalidCharacters.Length; i++)
            {
                text = text.Replace(InvalidCharacters[i], string.Empty);
            }

            return text;
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            _presenter = e.NameScope.Get<TextPresenter>("PART_TextPresenter");

            if (IsFocused)
            {
                DecideCaretVisibility();
            }
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            // when navigating to a textbox via the tab key, select all text if
            //   1) this textbox is *not* a multiline textbox
            //   2) this textbox has any text to select
            if (e.NavigationMethod == NavigationMethod.Tab && !AcceptsReturn && Text?.Length > 0)
            {
                SelectionStart = 0;
                SelectionEnd = Text.Length;
            }
            else
            {
                DecideCaretVisibility();
            }

            e.Handled = true;
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            SelectionStart = 0;
            SelectionEnd = 0;

            _presenter?.HideCaret();
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            if (!e.Handled)
            {
                HandleTextInput(e.Text);

                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;
            var movement = false;
            var selection = false;
            var handled = false;
            var modifiers = e.Modifiers;

            var keymap = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>();

            bool Match(List<KeyGesture> gestures) => gestures.Any(g => g.Matches(e));
            bool DetectSelection() => e.Modifiers.HasFlag(keymap.SelectionModifiers);

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
                    Copy();
                    DeleteSelection();
                }

                handled = true;
            }
            else if (Match(keymap.Paste))
            {
                Paste();
                handled = true;
            }
            else if (Match(keymap.Undo))
            {
                try
                {
                    _isUndoingRedoing = true;
                    _undoRedoHelper.Undo();
                }
                finally
                {
                    _isUndoingRedoing = false;
                }

                handled = true;
            }
            else if (Match(keymap.Redo))
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

                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheStartOfDocument))
            {
                MoveHome(true);
                movement = true;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheEndOfDocument))
            {
                MoveEnd(true);
                movement = true;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheStartOfLine))
            {
                MoveHome(false);
                movement = true;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheEndOfLine))
            {
                MoveEnd(false);
                movement = true;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheStartOfDocumentWithSelection))
            {
                MoveHome(true);
                movement = true;
                selection = true;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheEndOfDocumentWithSelection))
            {
                MoveEnd(true);
                movement = true;
                selection = true;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheStartOfLineWithSelection))
            {
                MoveHome(false);
                movement = true;
                selection = true;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheEndOfLineWithSelection))
            {
                MoveEnd(false);
                movement = true;
                selection = true;
                handled = true;
            }
            else
            {
                var hasWholeWordModifiers = modifiers.HasFlag(keymap.WholeWordTextActionModifiers);
                switch (e.Key)
                {
                    case Key.Left:
                        MoveHorizontal(-1, hasWholeWordModifiers);
                        movement = true;
                        selection = DetectSelection();
                        break;

                    case Key.Right:
                        MoveHorizontal(1, hasWholeWordModifiers);
                        movement = true;
                        selection = DetectSelection();
                        break;

                    case Key.Up:
                        movement = MoveVertical(-1);
                        selection = DetectSelection();
                        break;

                    case Key.Down:
                        movement = MoveVertical(1);
                        selection = DetectSelection();
                        break;

                    case Key.Back:
                        if (hasWholeWordModifiers && SelectionStart == SelectionEnd)
                        {
                            SetSelectionForControlBackspace();
                        }

                        if (!DeleteSelection() && CaretIndex > 0)
                        {
                            var removedCharacters = 1;

                            // handle deleting /r/n
                            // you don't ever want to leave a dangling /r around. So, if deleting /n, check to see if 
                            // a /r should also be deleted.
                            if (CaretIndex > 1 && text[CaretIndex - 1] == '\n' && text[CaretIndex - 2] == '\r')
                            {
                                removedCharacters = 2;
                            }

                            if (caretIndex >= 2 && char.IsSurrogatePair(text[caretIndex - 2], text[caretIndex - 1]))
                            {
                                removedCharacters = 2;
                            }

                            SetTextInternal(
                                text.Substring(0, caretIndex - removedCharacters) + text.Substring(caretIndex));
                            CaretIndex -= removedCharacters;
                            SelectionStart = SelectionEnd = CaretIndex;
                        }

                        handled = true;
                        break;

                    case Key.Delete:
                        if (hasWholeWordModifiers && SelectionStart == SelectionEnd)
                        {
                            SetSelectionForControlDelete();
                        }

                        if (!DeleteSelection() && caretIndex < text.Length)
                        {
                            var removedCharacters = 1 + _currentOffset;

                            SetTextInternal(
                                text.Substring(0, caretIndex) + text.Substring(caretIndex + removedCharacters));
                        }

                        handled = true;
                        break;

                    case Key.Enter:
                        if (AcceptsReturn)
                        {
                            HandleTextInput(NewLine);
                            handled = true;
                        }

                        break;

                    case Key.Tab:
                        if (AcceptsTab)
                        {
                            HandleTextInput("\t");
                            handled = true;
                        }
                        else
                        {
                            base.OnKeyDown(e);
                        }

                        break;
                }
            }

            if (movement && selection)
            {
                SelectionEnd = CaretIndex;
            }
            else if (movement)
            {
                SelectionStart = SelectionEnd = CaretIndex;
            }

            if (handled || movement)
            {
                e.Handled = true;
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            var point = e.GetPosition(_presenter);

            var hitTestResult = _presenter.FormattedText.HitTestPoint(point);

            _currentOffset = hitTestResult.Length - 1;

            var index = CaretIndex = hitTestResult.TextPosition;

            if (hitTestResult.IsTrailing)
            {
                index += hitTestResult.Length;
            }

            var text = Text;

            if (text != null && e.MouseButton == MouseButton.Left)
            {
                switch (e.ClickCount)
                {
                    case 1:
                        SelectionStart = SelectionEnd = index;
                        break;
                    case 2:
                        if (!StringUtils.IsStartOfWord(text, index))
                        {
                            SelectionStart = StringUtils.PreviousWord(text, index);
                        }

                        SelectionEnd = StringUtils.NextWord(text, index);
                        break;
                    case 3:
                        SelectionStart = 0;
                        SelectionEnd = text.Length;
                        break;
                }
            }

            e.Device.Capture(_presenter);
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_presenter != null && e.Device.Captured == _presenter)
            {
                var point = e.GetPosition(_presenter);

                point = new Point(
                    MathUtilities.Clamp(point.X, 0, _presenter.Bounds.Width - 1),
                    MathUtilities.Clamp(point.Y, 0, _presenter.Bounds.Height - 1));
                CaretIndex = SelectionEnd = _presenter.GetCaretIndex(point);
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_presenter != null && e.Device.Captured == _presenter)
            {
                e.Device.Capture(null);
            }
        }

        protected override void UpdateDataValidation(AvaloniaProperty property, BindingNotification status)
        {
            if (property == TextProperty)
            {
                DataValidationErrors.SetError(this, status.Error);
            }
        }

        private void HandleTextInput(string input)
        {
            if (IsReadOnly)
            {
                return;
            }

            input = RemoveInvalidCharacters(input);

            if (string.IsNullOrEmpty(input))
            {
                return;
            }

            DeleteSelection();

            var caretIndex = CaretIndex;
            var text = Text ?? string.Empty;

            SetTextInternal(text.Substring(0, caretIndex) + input + text.Substring(caretIndex));

            CaretIndex += input.Length;

            SelectionStart = SelectionEnd = CaretIndex;

            _undoRedoHelper.DiscardRedo();
        }

        private void DecideCaretVisibility()
        {
            if (!IsReadOnly)
            {
                _presenter?.ShowCaret();
            }
            else
            {
                _presenter?.HideCaret();
            }
        }

        private async void Copy()
        {
            var clipboard = (IClipboard)AvaloniaLocator.Current.GetService(typeof(IClipboard));

            await clipboard.SetTextAsync(GetSelection());
        }

        private async void Paste()
        {
            var clipboard = (IClipboard)AvaloniaLocator.Current.GetService(typeof(IClipboard));

            var text = await clipboard.GetTextAsync();

            if (text == null)
            {
                return;
            }

            _undoRedoHelper.Snapshot();

            HandleTextInput(text);
        }

        private int CoerceCaretIndex(int value) => CoerceCaretIndex(value, Text?.Length ?? 0);

        private int CoerceCaretIndex(int value, int length)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > length)
            {
                return length;
            }

            return value;
        }

        private void MoveHorizontal(int direction, bool wholeWord)
        {
            var text = Text ?? string.Empty;

            var caretIndex = CaretIndex;

            if (!wholeWord)
            {
                var index = caretIndex + direction;

                if (index < 0 || index > text.Length)
                {
                    return;
                }

                if (index == text.Length)
                {
                    CaretIndex = index;

                    return;
                }

                var rect = _presenter.FormattedText.HitTestTextPosition(caretIndex);

                if (rect.Width == 0d)
                {
                    CaretIndex = index;

                    return;
                }

                if (direction > 0)
                {
                    var hitTestResult = _presenter.FormattedText.HitTestPoint(
                        new Point(rect.X + rect.Width, rect.Y));

                    CaretIndex = hitTestResult.TextPosition + hitTestResult.Length;
                }
                else
                {
                    var hitTestResult = _presenter.FormattedText.HitTestPoint(new Point(rect.X, rect.Y));

                    CaretIndex = hitTestResult.TextPosition == CaretIndex ? index : hitTestResult.TextPosition;
                }
            }
            else
            {
                if (direction > 0)
                {
                    CaretIndex += StringUtils.NextWord(text, caretIndex) - caretIndex;
                }
                else
                {
                    CaretIndex += StringUtils.PreviousWord(text, caretIndex) - caretIndex;
                }
            }
        }

        private bool MoveVertical(int count)
        {
            var formattedText = _presenter.FormattedText;
            var lines = formattedText.GetLines().ToList();
            var caretIndex = CaretIndex;
            var lineIndex = GetLine(caretIndex, lines) + count;

            if (lineIndex >= 0 && lineIndex < lines.Count)
            {
                var line = lines[lineIndex];
                var rect = formattedText.HitTestTextPosition(caretIndex);
                var y = count < 0 ? rect.Y : rect.Bottom;
                var point = new Point(rect.X, y + (count * (line.Height / 2)));
                var hit = formattedText.HitTestPoint(point);

                CaretIndex = hit.TextPosition + (hit.IsTrailing ? 1 : 0);

                return true;
            }

            return false;
        }

        private void MoveHome(bool document)
        {
            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;

            if (document)
            {
                caretIndex = 0;
            }
            else
            {
                var lines = _presenter.FormattedText.GetLines();
                var pos = 0;

                foreach (var line in lines)
                {
                    if (pos + line.Length > caretIndex || pos + line.Length == text.Length)
                    {
                        break;
                    }

                    pos += line.Length;
                }

                caretIndex = pos;
            }

            CaretIndex = caretIndex;
        }

        private void MoveEnd(bool document)
        {
            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;

            if (document)
            {
                caretIndex = text.Length;
            }
            else
            {
                var lines = _presenter.FormattedText.GetLines();
                var pos = 0;

                foreach (var line in lines)
                {
                    pos += line.Length;

                    if (pos > caretIndex)
                    {
                        if (pos < text.Length)
                        {
                            --pos;
                            if (pos > 0 && text[pos - 1] == '\r' && text[pos] == '\n')
                            {
                                --pos;
                            }
                        }

                        break;
                    }
                }

                caretIndex = pos;
            }

            CaretIndex = caretIndex;
        }

        private void SelectAll()
        {
            SelectionStart = 0;
            SelectionEnd = Text?.Length ?? 0;
        }

        private bool DeleteSelection()
        {
            if (!IsReadOnly)
            {
                var selectionStart = SelectionStart;
                var selectionEnd = SelectionEnd;

                if (selectionStart != selectionEnd)
                {
                    var start = Math.Min(selectionStart, selectionEnd);
                    var end = Math.Max(selectionStart, selectionEnd);
                    var text = Text;

                    SetTextInternal(text.Substring(0, start) + text.Substring(end));

                    SelectionStart = SelectionEnd = CaretIndex = start;

                    return true;
                }

                return false;
            }

            return true;
        }

        private string GetSelection()
        {
            var text = Text;

            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var start = Math.Min(selectionStart, selectionEnd);
            var end = Math.Max(selectionStart, selectionEnd);

            if (start == end || (Text?.Length ?? 0) < end)
            {
                return string.Empty;
            }

            return text.Substring(start, end - start);
        }

        private int GetLine(int caretIndex, IList<FormattedTextLine> lines)
        {
            var pos = 0;
            int i;

            for (i = 0; i < lines.Count - 1; ++i)
            {
                var line = lines[i];
                pos += line.Length;

                if (pos > caretIndex)
                {
                    break;
                }
            }

            return i;
        }

        private void SetTextInternal(string value)
        {
            try
            {
                _ignoreTextChanges = true;

                SetAndRaise(TextProperty, ref _text, value);
            }
            finally
            {
                _ignoreTextChanges = false;
            }
        }

        private void SetSelectionForControlBackspace()
        {
            SelectionStart = CaretIndex;

            MoveHorizontal(-1, true);

            SelectionEnd = CaretIndex;
        }

        private void SetSelectionForControlDelete()
        {
            SelectionStart = CaretIndex;

            MoveHorizontal(1, true);

            SelectionEnd = CaretIndex;
        }

        private struct UndoRedoState : IEquatable<UndoRedoState>
        {
            public UndoRedoState(string text, int caretPosition)
            {
                Text = text;
                CaretPosition = caretPosition;
            }

            public string Text { get; }

            public int CaretPosition { get; }

            public bool Equals(UndoRedoState other) => ReferenceEquals(Text, other.Text) || Equals(Text, other.Text);
        }
    }
}
