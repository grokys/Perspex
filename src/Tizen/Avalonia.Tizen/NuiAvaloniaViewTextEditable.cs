﻿using Avalonia.Input.TextInput;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Window = Tizen.NUI.Window;

namespace Avalonia.Tizen;

internal class NuiAvaloniaViewTextEditable
{
    private readonly NuiAvaloniaView _avaloniaView;

    private INuiTextInput TextInput => _multiline ? _multiLineTextInput : _singleLineTextInput;
    private NuiSingleLineTextInput _singleLineTextInput;
    private NuiMultiLineTextInput _multiLineTextInput;
    private bool _updating;
    private bool _keyboardPresented;
    private bool _multiline = false;

    private TextInputMethodClient _client;

    public bool IsActive => _client != null && _keyboardPresented;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public NuiAvaloniaViewTextEditable(NuiAvaloniaView avaloniaView)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        _avaloniaView = avaloniaView;
        _singleLineTextInput = new NuiSingleLineTextInput
        {
            HeightResizePolicy = ResizePolicyType.Fixed,
            WidthResizePolicy = ResizePolicyType.Fixed,
            Size = new(1, 1),
            Position = new Position(-1000, -1000),
            FontSizeScale = 0.1f,
        };

        _multiLineTextInput = new NuiMultiLineTextInput
        {
            HeightResizePolicy = ResizePolicyType.Fixed,
            WidthResizePolicy = ResizePolicyType.Fixed,
            Size = new(1, 1),
            Position = new Position(-1000, -1000),
            FontSizeScale = 0.1f,
        };

        SetupTextInput(_singleLineTextInput);
        SetupTextInput(_multiLineTextInput);
    }

    private void SetupTextInput(INuiTextInput input)
    {
        input.Hide();

        input.GetInputMethodContext().StatusChanged += OnStatusChanged;
        input.GetInputMethodContext().EventReceived += OnEventReceived;
    }

    private InputMethodContext.CallbackData OnEventReceived(object source, InputMethodContext.EventReceivedEventArgs e)
    {
        switch (e.EventData.EventName)
        {
            case InputMethodContext.EventType.Preedit:
                _client.SetPreeditText(e.EventData.PredictiveString);
                break;
            case InputMethodContext.EventType.Commit:
                _client.SetPreeditText(null);
                _avaloniaView.TopLevelImpl.TextInput(e.EventData.PredictiveString);
                break;
        }
        return new InputMethodContext.CallbackData();
    }

    private void OnStatusChanged(object? sender, InputMethodContext.StatusChangedEventArgs e)
    {
        _keyboardPresented = e.StatusChanged;
        if (!_keyboardPresented)
            DettachAndHide();
    }

    internal void SetClient(TextInputMethodClient? client)
    {
        if (client == null || !_keyboardPresented)
            DettachAndHide();

        if (client != null)
            AttachAndShow(client);
    }

    internal void SetOptions(TextInputOptions options)
    {
        //TODO: This should be revert when Avalonia used Multiline property
        _multiline = true;
        //if (_multiline != options.Multiline)
        //{
        //    DettachAndHide();
        //    _multiline = options.Multiline;
        //}

        TextInput.Sensitive = options.IsSensitive;
    }

    private void AttachAndShow(TextInputMethodClient client)
    {
        _updating = true;
        try
        {
            TextInput.Text = client.SurroundingText;
            TextInput.PrimaryCursorPosition = client.Selection.Start;
            Window.Instance.GetDefaultLayer().Add((View)TextInput);
            TextInput.Show();
            TextInput.EnableSelection = true;
            _multiLineTextInput.RaiseToTop();

            var inputContext = TextInput.GetInputMethodContext();
            inputContext.Activate();
            inputContext.ShowInputPanel();
            inputContext.RestoreAfterFocusLost();

            _client = client;
            client.TextViewVisualChanged += OnTextViewVisualChanged;
            client.SurroundingTextChanged += OnSurroundingTextChanged;
            client.SelectionChanged += OnClientSelectionChanged;

            TextInput.SelectWholeText();
            OnClientSelectionChanged(this, EventArgs.Empty);
        }
        finally { _updating = false; }
    }

    private void OnClientSelectionChanged(object? sender, EventArgs e) => InvokeUpdate(() =>
    {
        if (_client.Selection.End == 0 || _client.Selection.Start == _client.Selection.End)
            TextInput.PrimaryCursorPosition = _client.Selection.Start;
        else
            TextInput.SelectText(_client.Selection.Start, _client.Selection.End);
    });

    private void OnSurroundingTextChanged(object? sender, EventArgs e) => InvokeUpdate(() =>
    {
        TextInput.Text = _client.SurroundingText;
        TextInput.GetInputMethodContext().SetSurroundingText(_client.SurroundingText);
        OnClientSelectionChanged(sender, e);
    });

    private void OnTextViewVisualChanged(object? sender, EventArgs e) => InvokeUpdate(() =>
    {
        TextInput.Text = _client.SurroundingText;
    });

    private void DettachAndHide()
    {
        if (IsActive)
        {
            _client.TextViewVisualChanged -= OnTextViewVisualChanged;
            _client.SurroundingTextChanged -= OnSurroundingTextChanged;
            _client.SelectionChanged -= OnClientSelectionChanged;
        }

        if (Window.Instance.GetDefaultLayer().Children.Contains((View)TextInput))
            Window.Instance.GetDefaultLayer().Remove((View)TextInput);

        TextInput.Hide();

        var inputContext = TextInput.GetInputMethodContext();
        inputContext.Deactivate();
        inputContext.HideInputPanel();
    }

    private void InvokeUpdate(Action action)
    {
        if (_updating || !IsActive)
            return;

        _updating = true;
        try
        {
            action();
        }
        finally { _updating = false; }
    }
}

internal interface INuiTextInput
{
    string Text { get; set; }
    int PrimaryCursorPosition { get; set; }
    bool EnableSelection { get; set; }
    bool Sensitive { get; set; }
    int SelectedTextStart { get; }
    int SelectedTextEnd { get; }

    void Show();
    InputMethodContext GetInputMethodContext();
    void Hide();
    void SelectText(int selectedTextStart, int value);
    void SelectWholeText();
}

public class NuiMultiLineTextInput : TextEditor, INuiTextInput { }
public class NuiSingleLineTextInput : TextField, INuiTextInput { }
