using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

namespace Avalonia.Tizen;

internal class NuiClipboardImpl : IClipboard
{
    private TextEditor _textEditor;
    public NuiClipboardImpl()
    {
        _textEditor = new TextEditor()
        {
            HeightResizePolicy = ResizePolicyType.Fixed,
            WidthResizePolicy = ResizePolicyType.Fixed,
            Position = new Position(-1000, -1000),
            Size = new(1, 1)
        };

        Window.Instance.GetDefaultLayer().Add(_textEditor);
        _textEditor.LowerToBottom();
    }

    public Task ClearAsync() =>
        SetTextAsync("");

    public Task<string?> GetTextAsync()
    {
        _textEditor.Show();
        _textEditor.Text = "";

        //The solution suggested by Samsung, The method PasteTo will execute async and need delay
        TextUtils.PasteTo(_textEditor);

        return Task.Run<string?>(async () =>
        {
            await Task.Delay(10);

            return await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _textEditor.Hide();
                return _textEditor.Text;
            });
        });
    }

    public Task SetTextAsync(string? text)
    {
        _textEditor.Show();
        _textEditor.Text = text;

        //The solution suggested by Samsung, The method SelectWholeText will execute async and need delay
        _textEditor.SelectWholeText();

        return Task.Run(async () =>
        {
            await Task.Delay(10);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TextUtils.CopyToClipboard(_textEditor);
                _textEditor.Hide();
            });
        });
    }

    public Task<object?> GetDataAsync(string format) =>
        throw new PlatformNotSupportedException();

    public Task SetDataObjectAsync(IDataObject data) =>
        throw new PlatformNotSupportedException();

    public Task<string[]> GetFormatsAsync() =>
        throw new PlatformNotSupportedException();

    /// <inheritdoc />
    public Task FlushAsync() => 
        Task.CompletedTask;
}
