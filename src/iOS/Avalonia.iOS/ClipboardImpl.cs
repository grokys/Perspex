using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    internal class ClipboardImpl : IClipboard
    {
        public Task<string?> GetTextAsync()
        {
            return Task.FromResult(UIPasteboard.General.String);
        }

        public Task SetTextAsync(string? text)
        {
            UIPasteboard.General.String = text;
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            UIPasteboard.General.Items = Array.Empty<NSDictionary>();
            return Task.CompletedTask;
        }

        public Task SetDataObjectAsync(IDataObject data)
        {
            if (data.Contains(DataFormats.Text))
            {
                UIPasteboard.General.String = data.GetText();
            }

            return Task.CompletedTask;
        }

        public Task<string[]> GetFormatsAsync()
        {
            var formats = new List<string>();
            if (UIPasteboard.General.HasStrings)
            {
                formats.Add(DataFormats.Text);
            }

            return Task.FromResult(formats.ToArray());
        }

        public Task<object?> GetDataAsync(string format)
        {
            if (format == DataFormats.Text)
            {
                return Task.FromResult<object?>(UIPasteboard.General.String);
            }

            return Task.FromResult<object?>(null);
        }

        /// <inheritdoc />
        public Task FlushAsync() =>
            Task.CompletedTask;
    }
}
