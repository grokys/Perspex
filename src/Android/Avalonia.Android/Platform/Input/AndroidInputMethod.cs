using System;
using System.Diagnostics.CodeAnalysis;
using Android.Content;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia.Input.TextInput;

namespace Avalonia.Android.Platform.Input
{
    internal interface IAndroidInputMethod
    {
        public View View { get; }

        public TextInputMethodClient? Client { get; }

        [MemberNotNullWhen(true, nameof(Client))]
        public bool IsActive { get; }

        public InputMethodManager IMM { get; }

        void OnBatchEditEnded();
    }

    enum CustomImeFlags
    {
        ActionNone = 0x00000001,
        ActionGo = 0x00000002,
        ActionSearch = 0x00000003,
        ActionSend = 0x00000004,
        ActionNext = 0x00000005,
        ActionDone = 0x00000006,
        ActionPrevious = 0x00000007,
    }

    internal class AndroidInputMethod<TView> : ITextInputMethodImpl, IAndroidInputMethod
        where TView : View, IInitEditorInfo
    {
        private readonly TView _host;
        private readonly InputMethodManager _imm;
        private TextInputMethodClient? _client;
        private AvaloniaInputConnection? _inputConnection;

        public AndroidInputMethod(TView host)
        {
            if (host.OnCheckIsTextEditor() == false)
                throw new InvalidOperationException("Host should return true from OnCheckIsTextEditor()");

            _host = host;
            _imm = host.Context?.GetSystemService(Context.InputMethodService).JavaCast<InputMethodManager>()
                   ?? throw new InvalidOperationException("Context.InputMethodService is expected to be not null.");

            _host.Focusable = true;
            _host.FocusableInTouchMode = true;
        }

        public View View => _host;

        [MemberNotNullWhen(true, nameof(Client))]
        [MemberNotNullWhen(true, nameof(_client))]
        public bool IsActive => Client != null;

        public TextInputMethodClient? Client => _client;

        public InputMethodManager IMM => _imm;

        public void Reset()
        {

        }

        public void SetClient(TextInputMethodClient? client)
        {
            _client = client;

            if (IsActive)
            {
                _host.RequestFocus();

                _imm.RestartInput(View);

                _imm.ShowSoftInput(_host, ShowFlags.Implicit);

                _inputConnection?.UpdateState();

                _client.SurroundingTextChanged += _client_SurroundingTextChanged;
                _client.SelectionChanged += _client_SelectionChanged;
            }
            else
            {
                _imm.HideSoftInputFromWindow(_host.WindowToken, HideSoftInputFlags.ImplicitOnly);
            }
        }

        private void _client_SelectionChanged(object? sender, EventArgs e)
        {
            if (_inputConnection is null || _inputConnection.IsInBatchEdit || _inputConnection.IsInUpdate)
                return;
            OnSelectionChanged();
        }

        private void OnSelectionChanged()
        {
            if (Client is null || _inputConnection is null || _inputConnection.IsInUpdate)
            {
                return;
            }

            OnSurroundingTextChanged();

            _inputConnection.IsInUpdate = true;

            var selection = Client.Selection;

            var composition = _inputConnection.EditBuffer.HasComposition ? _inputConnection.EditBuffer.Composition!.Value : new TextSelection(-1,-1);

            _imm.UpdateSelection(_host, selection.Start, selection.End, composition.Start, composition.End);

            _inputConnection.IsInUpdate = false;
        }

        private void _client_SurroundingTextChanged(object? sender, EventArgs e)
        {
            if (_inputConnection is null || _inputConnection.IsInBatchEdit || _inputConnection.IsInUpdate)
                return;
            OnSurroundingTextChanged();
        }

        public void OnBatchEditEnded()
        {
            if (_inputConnection is null || _inputConnection.IsInBatchEdit)
                return;
            OnSelectionChanged();
        }

        private void OnSurroundingTextChanged()
        {
            _inputConnection?.UpdateState();
        }

        public void SetCursorRect(Rect rect)
        {

        }

        public void SetOptions(TextInputOptions options)
        {
            _host.InitEditorInfo((topLevel, outAttrs) =>
            {
                if (_client == null)
                    return null!;

                _inputConnection = new AvaloniaInputConnection(topLevel, this);

                outAttrs.InputType = options.ContentType switch
                {
                    TextInputContentType.Email => InputTypes.TextVariationEmailAddress,
                    TextInputContentType.Number => InputTypes.ClassNumber,
                    TextInputContentType.Password => InputTypes.TextVariationPassword,
                    TextInputContentType.Digits => InputTypes.ClassPhone,
                    TextInputContentType.Url => InputTypes.TextVariationUri,
                    _ => InputTypes.ClassText
                };

                if (options.AutoCapitalization)
                {
                    outAttrs.InitialCapsMode = CapitalizationMode.Sentences;
                    outAttrs.InputType |= InputTypes.TextFlagCapSentences;
                }

                if (options.Multiline)
                    outAttrs.InputType |= InputTypes.TextFlagMultiLine;

                if (outAttrs.InputType is InputTypes.ClassText && options.ShowSuggestions == false)
                    outAttrs.InputType |= InputTypes.TextVariationPassword | InputTypes.TextFlagNoSuggestions;
                    
                outAttrs.ImeOptions = options.ReturnKeyType switch
                {
                    TextInputReturnKeyType.Return => ImeFlags.NoEnterAction,
                    TextInputReturnKeyType.Go => (ImeFlags)CustomImeFlags.ActionGo,
                    TextInputReturnKeyType.Send => (ImeFlags)CustomImeFlags.ActionSend,
                    TextInputReturnKeyType.Search => (ImeFlags)CustomImeFlags.ActionSearch,
                    TextInputReturnKeyType.Next => (ImeFlags)CustomImeFlags.ActionNext,
                    TextInputReturnKeyType.Previous => (ImeFlags)CustomImeFlags.ActionPrevious,
                    TextInputReturnKeyType.Done => (ImeFlags)CustomImeFlags.ActionDone,
                    _ => options.Multiline ? ImeFlags.NoEnterAction : (ImeFlags)CustomImeFlags.ActionDone
                };

                outAttrs.ImeOptions |= ImeFlags.NoFullscreen | ImeFlags.NoExtractUi;

                return _inputConnection;
            });
        }
    }
}
