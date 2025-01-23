using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Browser.Interop;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Browser.Storage;

internal class BrowserStorageProvider : IStorageProvider
{
    internal static ReadOnlySpan<byte> BrowserBookmarkKey => "browser"u8;
    internal const string PickerCancelMessage = "The user aborted a request";
    internal const string NoPermissionsMessage = "Permissions denied";
    internal const string FileFolderNotFoundMessage = "A requested file or directory could not be found";
    internal const string TypeMissmatchMessage = "The path supplied exists, but was not an entry of requested type";

    public bool CanOpen => true;
    public bool CanSave => true;
    public bool CanPickFolder => true;

    private bool PreferPolyfill =>
        AvaloniaLocator.Current.GetService<BrowserPlatformOptions>()?.PreferFileDialogPolyfill ?? false;
    
    public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        await AvaloniaModule.ImportStorage();
        var startIn = (options.SuggestedStartLocation as JSStorageItem)?.FileHandle;

        var (types, excludeAll) = ConvertFileTypes(options.FileTypeFilter);

        try
        {
            using var items = await StorageHelper.OpenFileDialog(startIn, options.AllowMultiple, types, excludeAll, PreferPolyfill);
            if (items is null)
            {
                return Array.Empty<IStorageFile>();
            }

            var itemsArray = StorageHelper.ItemsArray(items);
            return itemsArray.Select(item => new JSStorageFile(item)).ToArray();
        }
        catch (JSException ex) when (ex.Message.Contains(PickerCancelMessage, StringComparison.Ordinal))
        {
            return Array.Empty<IStorageFile>();
        }
        finally
        {
            if (types is not null)
            {
                foreach (var type in types)
                {
                    type.Dispose();
                }
            }
        }
    }

    public async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        await AvaloniaModule.ImportStorage();
        var startIn = (options.SuggestedStartLocation as JSStorageItem)?.FileHandle;

        var (types, excludeAll) = ConvertFileTypes(options.FileTypeChoices);

        try
        {
            var suggestedName =
                StorageProviderHelpers.NameWithExtension(options.SuggestedFileName, options.DefaultExtension, null);
            var item = await StorageHelper.SaveFileDialog(startIn, suggestedName, types, excludeAll, PreferPolyfill);
            return item is not null ? new JSStorageFile(item) : null;
        }
        catch (JSException ex) when (ex.Message.Contains(PickerCancelMessage, StringComparison.Ordinal))
        {
            return null;
        }
        finally
        {
            if (types is not null)
            {
                foreach (var type in types)
                {
                    type.Dispose();
                }
            }
        }
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        await AvaloniaModule.ImportStorage();
        var startIn = (options.SuggestedStartLocation as JSStorageItem)?.FileHandle;

        try
        {
            var item = await StorageHelper.SelectFolderDialog(startIn, PreferPolyfill);
            return item is not null ? new[] { new JSStorageFolder(item) } : Array.Empty<IStorageFolder>();
        }
        catch (JSException ex) when (ex.Message.Contains(PickerCancelMessage, StringComparison.Ordinal))
        {
            return Array.Empty<IStorageFolder>();
        }
    }

    public async Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark)
    {
        return await DecodeBookmark(bookmark) as IStorageBookmarkFile;
    }
    
    public async Task<IStorageBookmarkFolder?> OpenFolderBookmarkAsync(string bookmark)
    {
        return await DecodeBookmark(bookmark) as IStorageBookmarkFolder;
    }

    public Task<IStorageFile?> TryGetFileFromPathAsync(Uri filePath)
    {
        return Task.FromResult<IStorageFile?>(null);
    }

    public Task<IStorageFolder?> TryGetFolderFromPathAsync(Uri folderPath)
    {
        return Task.FromResult<IStorageFolder?>(null);
    }

    public async Task<IStorageFolder?> TryGetWellKnownFolderAsync(WellKnownFolder wellKnownFolder)
    {
        await AvaloniaModule.ImportStorage();
        var directory = StorageHelper.CreateWellKnownDirectory(wellKnownFolder switch
        {
            WellKnownFolder.Desktop => "desktop",
            WellKnownFolder.Documents => "documents",
            WellKnownFolder.Downloads => "downloads",
            WellKnownFolder.Music => "music",
            WellKnownFolder.Pictures => "pictures",
            WellKnownFolder.Videos => "videos",
            _ => throw new ArgumentOutOfRangeException(nameof(wellKnownFolder), wellKnownFolder, null)
        });

        return new JSStorageFolder(directory);
    }

    private static (JSObject[]? types, bool excludeAllOption) ConvertFileTypes(IEnumerable<FilePickerFileType>? input)
    {
        var types = input?
            .Where(t => t.MimeTypes?.Any() == true && t != FilePickerFileTypes.All)
            .Select(t => StorageHelper.CreateAcceptType(t.Name, t.MimeTypes!.ToArray(), t.TryGetExtensions()?.Select(e => "." + e).ToArray()))
            .ToArray();
        if (types?.Length == 0)
        {
            types = null;
        }

        var includeAll = input?.Contains(FilePickerFileTypes.All) == true || types is null;

        return (types, !includeAll);
    }

    private async Task<IStorageBookmarkItem?> DecodeBookmark(string bookmark)
    {
        await AvaloniaModule.ImportStorage();
        var item = StorageBookmarkHelper.TryDecodeBookmark(BrowserBookmarkKey, bookmark, out var bytes) switch
        {
            StorageBookmarkHelper.DecodeResult.Success => await StorageHelper.OpenBookmark(Encoding.UTF8.GetString(bytes!)),
            // Attempt to decode 11.0 browser bookmarks
            StorageBookmarkHelper.DecodeResult.InvalidFormat => await StorageHelper.OpenBookmark(bookmark),
            _ => null
        };
        return item?.GetPropertyAsString("kind") switch
        {
            "directory" => new JSStorageFolder(item),
            "file" => new JSStorageFile(item),
            _ => null
        };
    }
}

internal abstract class JSStorageItem : IStorageBookmarkItem
{
    internal JSObject? _fileHandle;

    protected JSStorageItem(JSObject fileHandle)
    {
        _fileHandle = fileHandle ?? throw new ArgumentNullException(nameof(fileHandle));
    }

    internal JSObject FileHandle => _fileHandle ?? throw new ObjectDisposedException(nameof(JSStorageItem));

    public string Name => FileHandle.GetPropertyAsString("name") ?? string.Empty;
    public Uri Path => new Uri(Name, UriKind.Relative);

    public async Task<StorageItemProperties> GetBasicPropertiesAsync()
    {
        using var properties = await StorageHelper.GetProperties(FileHandle);
        var size = (long?)properties?.GetPropertyAsDouble("Size");
        var lastModified = (long?)properties?.GetPropertyAsDouble("LastModified");

        return new StorageItemProperties(
            (ulong?)size,
            dateCreated: null,
            dateModified: lastModified > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(lastModified.Value) : null);
    }

    public bool CanBookmark => StorageHelper.HasNativeFilePicker();

    public async Task<string?> SaveBookmarkAsync()
    {
        if (!CanBookmark)
        {
            return null;
        }

        var nativeBookmark = await StorageHelper.SaveBookmark(FileHandle);
        return nativeBookmark is null ? null
            : StorageBookmarkHelper.EncodeBookmark(BrowserStorageProvider.BrowserBookmarkKey, nativeBookmark);
    }

    public Task<IStorageFolder?> GetParentAsync()
    {
        return Task.FromResult<IStorageFolder?>(null);
    }

    public Task DeleteAsync()
    {
        return StorageHelper.DeleteAsync(FileHandle);
    }

    public async Task<IStorageItem?> MoveAsync(IStorageFolder destination)
    {
        if (destination is not JSStorageFolder folder)
        {
            throw new InvalidOperationException("Destination folder must be initialized the StorageProvider API.");
        }

        var storageItem = await StorageHelper.MoveAsync(FileHandle, folder.FileHandle);
        if (storageItem is null)
        {
            return null;
        }

        var kind = storageItem.GetPropertyAsString("kind");
        return kind switch
        {
            "directory" => new JSStorageFolder(storageItem),
            "file" => new JSStorageFile(storageItem),
            _ => this
        };
    }

    public Task ReleaseBookmarkAsync()
    {
        if (!CanBookmark)
        {
            return Task.CompletedTask;
        }

        return StorageHelper.DeleteBookmark(FileHandle);
    }

    public void Dispose()
    {
        _fileHandle?.Dispose();
        _fileHandle = null;
    }
}

internal class JSStorageFile : JSStorageItem, IStorageBookmarkFile
{
    public JSStorageFile(JSObject fileHandle) : base(fileHandle)
    {
    }

    public async Task<Stream> OpenReadAsync()
    {
        try
        {
            var blob = await StorageHelper.OpenRead(FileHandle);
            return new BlobReadableStream(blob);
        }
        catch (JSException ex) when (ex.Message == BrowserStorageProvider.NoPermissionsMessage)
        {
            throw new UnauthorizedAccessException("User denied permissions to open the file", ex);
        }
    }

    public async Task<Stream> OpenWriteAsync()
    {
        try
        {
            using var properties = await StorageHelper.GetProperties(FileHandle);
            var streamWriter = await StorageHelper.OpenWrite(FileHandle);
            var size = (long?)properties?.GetPropertyAsDouble("Size") ?? 0;

            return new WriteableStream(streamWriter, size);
        }
        catch (JSException ex) when (ex.Message == BrowserStorageProvider.NoPermissionsMessage)
        {
            throw new UnauthorizedAccessException("User denied permissions to open the file", ex);
        }
    }
}

internal class JSStorageFolder : JSStorageItem, IStorageBookmarkFolder
{
    public JSStorageFolder(JSObject fileHandle) : base(fileHandle)
    {
    }

    public async IAsyncEnumerable<IStorageItem> GetItemsAsync()
    {
        using var itemsIterator = StorageHelper.GetItemsIterator(FileHandle);
        if (itemsIterator is null)
        {
            yield break;
        }

        while (true)
        {
            var nextResult = await itemsIterator.CallMethodObjectAsync("next");
            if (nextResult is null)
            {
                yield break;
            }

            var isDone = nextResult.GetPropertyAsBoolean("done");
            if (isDone)
            {
                yield break;
            }

            var valArray = nextResult.GetPropertyAsJSObject("value");
            var storageItem = valArray?.GetArrayItem(1); // 0 - item name, 1 - item instance
            if (storageItem is null)
            {
                yield break;
            }

            var kind = storageItem.GetPropertyAsString("kind");
            var item = StorageHelper.StorageItemFromHandle(storageItem)!;
            switch (kind)
            {
                case "directory":
                    yield return new JSStorageFolder(item);
                    break;
                case "file":
                    yield return new JSStorageFile(item);
                    break;
            }
        }
    }

    public async Task<IStorageFile?> CreateFileAsync(string name)
    {
        try
        {
            var storageFile = await StorageHelper.CreateFile(FileHandle, name);
            if (storageFile is null)
            {
                return null;
            }

            return new JSStorageFile(storageFile);
        }
        catch (JSException ex) when (ex.Message == BrowserStorageProvider.NoPermissionsMessage)
        {
            throw new UnauthorizedAccessException("User denied permissions to open the file", ex);
        }
    }

    public async Task<IStorageFolder?> CreateFolderAsync(string name)
    {
        try
        {
            var storageFile = await StorageHelper.CreateFolder(FileHandle, name);
            if (storageFile is null)
            {
                return null;
            }

            return new JSStorageFolder(storageFile);
        }
        catch (JSException ex) when (ex.Message == BrowserStorageProvider.NoPermissionsMessage)
        {
            throw new UnauthorizedAccessException("User denied permissions to open the file", ex);
        }
    }

    public async Task<IStorageFolder?> GetFolderAsync(string name)
    {
        try
        {
            var storageFile = await StorageHelper.GetFolder(FileHandle, name);
            if (storageFile is null)
            {
                return null;
            }

            return new JSStorageFolder(storageFile);
        }
        catch (JSException ex) when (ShouldSupressErrorOnFileAccess(ex))
        {
            return null;
        }
    }

    public async Task<IStorageFile?> GetFileAsync(string name)
    {
        try
        {
            var storageFile = await StorageHelper.GetFile(FileHandle, name);
            if (storageFile is null)
            {
                return null;
            }

            return new JSStorageFile(storageFile);
        }
        catch (JSException ex) when (ShouldSupressErrorOnFileAccess(ex))
        {
            return null;
        }
    }

    private static bool ShouldSupressErrorOnFileAccess(JSException ex) =>
        ex.Message == BrowserStorageProvider.NoPermissionsMessage ||
        ex.Message.Contains(BrowserStorageProvider.TypeMissmatchMessage, StringComparison.Ordinal) ||
        ex.Message.Contains(BrowserStorageProvider.FileFolderNotFoundMessage, StringComparison.Ordinal);
}
