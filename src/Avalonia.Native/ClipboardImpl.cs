#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Logging;
using Avalonia.Native.Interop;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Native
{
    class ClipboardImpl : IClipboard, IDisposable
    {
        private IAvnClipboard? _native;

        // TODO hide native types behind IAvnClipboard abstraction, so managed side won't depend on macOS.
        private const string NSPasteboardTypeString = "public.utf8-plain-text";
        private const string NSFilenamesPboardType = "NSFilenamesPboardType";

        public ClipboardImpl(IAvnClipboard native)
        {
            _native = native;
        }

        private IAvnClipboard Native
            => _native ?? throw new ObjectDisposedException(nameof(ClipboardImpl));

        public Task ClearAsync()
        {
            Native.Clear();

            return Task.CompletedTask;
        }

        public Task<string?> GetTextAsync()
        {
            using (var text = Native.GetText(NSPasteboardTypeString))
                return Task.FromResult<string?>(text.String);
        }

        public Task SetTextAsync(string? text)
        {
            var native = Native;

            native.Clear();

            if (text != null) 
                native.SetText(NSPasteboardTypeString, text);

            return Task.CompletedTask;
        }

        public IEnumerable<string> GetFormats()
        {
            var rv = new HashSet<string>();
            using (var formats = Native.ObtainFormats())
            {
                var cnt = formats.Count;
                for (uint c = 0; c < cnt; c++)
                {
                    using (var fmt = formats.Get(c))
                    {
                         if(fmt.String == NSPasteboardTypeString)
                             rv.Add(DataFormats.Text);
                         if (fmt.String == NSFilenamesPboardType)
                         {
                             rv.Add(DataFormats.FileNames);
                             rv.Add(DataFormats.Files);
                         }
                         else
                             rv.Add(fmt.String);
                    }
                }
            }

            return rv;
        }
        
        public void Dispose()
        {
            _native?.Dispose();
            _native = null;
        }

        public IEnumerable<string>? GetFileNames()
        {
            using (var strings = Native.GetStrings(NSFilenamesPboardType))
                return strings?.ToStringArray();
        }

        public IEnumerable<IStorageItem>? GetFiles()
        {
            var storageApi = (StorageProviderApi)AvaloniaLocator.Current.GetRequiredService<IStorageProviderFactory>();
    
            // TODO: use non-deprecated AppKit API to get NSUri instead of file names.
            var fileNames = GetFileNames();
            if (fileNames is null)
                return null;

            return fileNames
                .Select(f => StorageProviderHelpers.TryGetUriFromFilePath(f, false) is { } uri
                    ? storageApi.TryGetStorageItem(uri)
                    : null)
                .Where(f => f is not null)!;
        }

        public unsafe Task SetDataObjectAsync(IDataObject data)
        {
            Native.Clear();

            // If there is multiple values with the same "to" format, prefer these that were not mapped.
            var formats = data.GetDataFormats().Select(f =>
                {
                    string from, to;
                    bool mapped;
                    if (f == DataFormats.Text)
                        (from, to, mapped) = (f, NSPasteboardTypeString, true);
                    else if (f == DataFormats.Files || f == DataFormats.FileNames)
                        (from, to, mapped) = (f, NSFilenamesPboardType, true);
                    else (from, to, mapped) = (f, f, false);
                    return (from, to, mapped);
                })
                .GroupBy(p => p.to)
                .Select(g => g.OrderBy(f => f.mapped).First());
                
            
            foreach (var (fromFormat, toFormat, _) in formats)
            {
                var o = data.Get(fromFormat);
                switch (o)
                {
                    case string s:
                        Native.SetText(toFormat, s);
                        break;
                    case IEnumerable<IStorageItem> storageItems:
                        using (var strings = new AvnStringArray(storageItems
                                   .Select(s => s.TryGetLocalPath())
                                   .Where(p => p is not null)))
                        {
                            Native.SetStrings(toFormat, strings);
                        }
                        break;
                    case IEnumerable<string> managedStrings:
                        using (var strings = new AvnStringArray(managedStrings))
                        {
                            Native.SetStrings(toFormat, strings);
                        }
                        break;
                    case byte[] bytes:
                    {
                        fixed (byte* pbytes = bytes)
                            Native.SetBytes(toFormat, pbytes, bytes.Length);
                        break;
                    }
                    default:
                        Logger.TryGet(LogEventLevel.Warning, LogArea.macOSPlatform)?.Log(this,
                            "Unsupported IDataObject value type: {0}", o?.GetType().FullName ?? "(null)");
                        break;
                }
            }
            return Task.CompletedTask;
        }

        public Task<string[]> GetFormatsAsync()
        {
            return Task.FromResult(GetFormats().ToArray());
        }

        public async Task<object?> GetDataAsync(string format)
        {
            if (format == DataFormats.Text || format == NSPasteboardTypeString)
                return await GetTextAsync();
            if (format == DataFormats.FileNames || format == NSFilenamesPboardType)
                return GetFileNames();
            if (format == DataFormats.Files)
                return GetFiles();
            using (var n = Native.GetBytes(format))
                return n.Bytes;
        }

        /// <inheritdoc />
        public Task FlushAsync() =>
            Task.CompletedTask;
    }
    
    class ClipboardDataObject : IDataObject, IDisposable
    {
        private ClipboardImpl? _clipboard;
        private List<string>? _formats;

        public ClipboardDataObject(IAvnClipboard clipboard)
        {
            _clipboard = new ClipboardImpl(clipboard);
        }

        public void Dispose()
        {
            _clipboard?.Dispose();
            _clipboard = null;
        }

        private ClipboardImpl Clipboard
            => _clipboard ?? throw new ObjectDisposedException(nameof(ClipboardDataObject));

        private List<string> Formats => _formats ??= Clipboard.GetFormats().ToList();

        public IEnumerable<string> GetDataFormats() => Formats;

        public bool Contains(string dataFormat) => Formats.Contains(dataFormat);

        public object? Get(string dataFormat) => Clipboard.GetDataAsync(dataFormat).GetAwaiter().GetResult();

        public Task SetFromDataObjectAsync(IDataObject dataObject) => Clipboard.SetDataObjectAsync(dataObject);
    }
}
