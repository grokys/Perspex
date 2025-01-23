using System;
using System.Threading.Tasks;
using Android.Content;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Android.Platform
{
    internal class ClipboardImpl : IClipboard
    {
        private readonly ClipboardManager? _clipboardManager;

        internal ClipboardImpl(ClipboardManager? value)
        {
            _clipboardManager = value;
        }

        public Task<string?> GetTextAsync()
        {
            if (_clipboardManager?.HasPrimaryClip == true)
            {
                return Task.FromResult(_clipboardManager.PrimaryClip?.GetItemAt(0)?.Text);
            }

            return Task.FromResult<string?>(null);
        }

        public Task SetTextAsync(string? text)
        {
            if(_clipboardManager == null)
            {
                return Task.CompletedTask;
            }

            var clip = ClipData.NewPlainText("text", text);
            _clipboardManager.PrimaryClip = clip;

            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            if (_clipboardManager == null)
            {
                return Task.CompletedTask;
            }

            _clipboardManager.PrimaryClip = null;

            return Task.CompletedTask;
        }

        public Task SetDataObjectAsync(IDataObject data) => throw new PlatformNotSupportedException();

        public Task<string[]> GetFormatsAsync() => throw new PlatformNotSupportedException();

        public Task<object?> GetDataAsync(string format) => throw new PlatformNotSupportedException();

        /// <inheritdoc />
        public Task FlushAsync() =>
            Task.CompletedTask;
    }
}
