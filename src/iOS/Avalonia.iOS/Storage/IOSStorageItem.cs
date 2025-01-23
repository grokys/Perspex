using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Reactive;
using Foundation;

using UIKit;

namespace Avalonia.iOS.Storage;

internal abstract class IOSStorageItem : IStorageBookmarkItem
{
    private readonly string _filePath;

    protected IOSStorageItem(NSUrl url, NSUrl? securityScopedAncestorUrl = null)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        SecurityScopedAncestorUrl = securityScopedAncestorUrl ?? url;

        using (var doc = new UIDocument(url))
        {
            _filePath = doc.FileUrl?.Path ?? url.FilePathUrl?.Path ?? string.Empty;
            Name = doc.LocalizedName 
                ?? System.IO.Path.GetFileName(_filePath) 
                ?? url.FilePathUrl?.LastPathComponent
                ?? string.Empty;
        }
    }
    
    public static IStorageItem CreateItem(NSUrl url, NSUrl? securityScopedAncestorUrl = null)
    {
        return url.HasDirectoryPath ?
            new IOSStorageFolder(url, securityScopedAncestorUrl) :
            new IOSStorageFile(url, securityScopedAncestorUrl);
    }

    internal NSUrl Url { get; }
    // Calling StartAccessingSecurityScopedResource on items retrieved from, or created in a folder
    // fails, because only folders directly opened via StorageProvider.OpenFolderPickerAsync have
    // security-scoped NSUrls. This property stores and exposes that ancestor's Url, so we can have
    // recursive access to an opened folder.
    internal NSUrl SecurityScopedAncestorUrl { get; }
    internal string FilePath => _filePath;

    public bool CanBookmark => true;

    public string Name { get; }
    public Uri Path => Url!;

    public Task<StorageItemProperties> GetBasicPropertiesAsync()
    {
        var attributes = NSFileManager.DefaultManager.GetAttributes(_filePath, out var error);
        if (error is not null)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.IOSPlatform)?.
                Log(this, "GetBasicPropertiesAsync returned an error: {ErrorCode} {ErrorMessage}", error.Code, error.LocalizedFailureReason);
        }

        var properties = attributes is null ?
            new StorageItemProperties() :
            new StorageItemProperties(
                attributes.Size,
                attributes.CreationDate is { } creationDate ? (DateTime)creationDate : null,
                attributes.ModificationDate is { } modificationDate ? (DateTime)modificationDate : null);

        return Task.FromResult(properties);
    }

    public Task<IStorageFolder?> GetParentAsync()
    {
        return Task.FromResult<IStorageFolder?>(new IOSStorageFolder(Url.RemoveLastPathComponent(), SecurityScopedAncestorUrl));
    }

    public Task DeleteAsync()
    {
        try
        {
            SecurityScopedAncestorUrl.StartAccessingSecurityScopedResource();

            return NSFileManager.DefaultManager.Remove(Url, out var error)
                ? Task.CompletedTask
                : Task.FromException(new NSErrorException(error));
        }
        finally
        {
            SecurityScopedAncestorUrl.StopAccessingSecurityScopedResource();
        }
    }

    public Task<IStorageItem?> MoveAsync(IStorageFolder destination)
    {
        if (destination is not IOSStorageFolder folder)
        {
            throw new InvalidOperationException("Destination folder must be initialized the StorageProvider API.");
        }

        try
        {
            SecurityScopedAncestorUrl.StartAccessingSecurityScopedResource();
            folder.SecurityScopedAncestorUrl.StartAccessingSecurityScopedResource();

            var isDir = this is IStorageFolder;
            var newPath = new NSUrl(System.IO.Path.Combine(folder.FilePath, Name), isDir);

            if (NSFileManager.DefaultManager.Move(Url, newPath, out var error))
            {
                return Task.FromResult<IStorageItem?>(isDir
                    ? new IOSStorageFolder(newPath)
                    : new IOSStorageFile(newPath));
            }

            if (error is not null)
            {
                throw new NSErrorException(error);
            }

            return Task.FromResult<IStorageItem?>(null);
        }
        finally
        {
            SecurityScopedAncestorUrl.StopAccessingSecurityScopedResource();
            folder.SecurityScopedAncestorUrl.StopAccessingSecurityScopedResource();
        }
    }

    public Task ReleaseBookmarkAsync()
    {
        // no-op
        return Task.CompletedTask;
    }

    public unsafe Task<string?> SaveBookmarkAsync()
    {
        try
        {
            if (!SecurityScopedAncestorUrl.StartAccessingSecurityScopedResource())
            {
                return Task.FromResult<string?>(null);
            }

            using var newBookmark = Url.CreateBookmarkData(NSUrlBookmarkCreationOptions.SuitableForBookmarkFile, [], null, out var bookmarkError);
            if (bookmarkError is not null)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.IOSPlatform)?.
                    Log(this, "SaveBookmark returned an error: {ErrorCode} {ErrorMessage}", bookmarkError.Code, bookmarkError.LocalizedFailureReason);
                return Task.FromResult<string?>(null);
            }

            var bytes = new Span<byte>((void*)newBookmark.Bytes, (int)newBookmark.Length);
            return Task.FromResult<string?>(
                StorageBookmarkHelper.EncodeBookmark(IOSStorageProvider.PlatformKey, bytes));
        }
        finally
        {
            SecurityScopedAncestorUrl.StopAccessingSecurityScopedResource();
        }
    }

    public void Dispose()
    {
    }
}

internal sealed class IOSStorageFile : IOSStorageItem, IStorageBookmarkFile
{
    public IOSStorageFile(NSUrl url, NSUrl? securityScopedAncestorUrl = null) : base(url, securityScopedAncestorUrl)
    {
    }

    public Task<Stream> OpenReadAsync()
    {
        return Task.FromResult(CreateStream(FileAccess.Read));
    }

    public Task<Stream> OpenWriteAsync()
    {
        return Task.FromResult(CreateStream(FileAccess.Write));
    }

    private Stream CreateStream(FileAccess fileAccess)
    {
        var document = new UIDocument(Url);
        var path = document.FileUrl.Path!;
        var scopeCreated = SecurityScopedAncestorUrl.StartAccessingSecurityScopedResource();
        var stream = File.Open(path, FileMode.Open, fileAccess);

        return scopeCreated ?
            new SecurityScopedStream(stream, Disposable.Create(() =>
            {
                document.Dispose();
                SecurityScopedAncestorUrl.StopAccessingSecurityScopedResource();
            })) :
            stream;
    }
}

internal sealed class IOSStorageFolder : IOSStorageItem, IStorageBookmarkFolder
{
    public IOSStorageFolder(NSUrl url, NSUrl? securityScopedAncestorUrl = null) : base(url, securityScopedAncestorUrl)
    {
    }

    public IOSStorageFolder(NSUrl url, WellKnownFolder wellKnownFolder) : base(url, null)
    {
        WellKnownFolder = wellKnownFolder;
    }

    public WellKnownFolder? WellKnownFolder { get; }

    public async IAsyncEnumerable<IStorageItem> GetItemsAsync()
    {
        try
        {
            SecurityScopedAncestorUrl.StartAccessingSecurityScopedResource();

            // TODO: find out if it can be lazily enumerated.
            var tcs = new TaskCompletionSource<IReadOnlyList<IStorageItem>>();

            new NSFileCoordinator().CoordinateRead(Url,
                NSFileCoordinatorReadingOptions.WithoutChanges,
                out var error,
                uri =>
                {
                    var content = NSFileManager.DefaultManager.GetDirectoryContent(uri, null, NSDirectoryEnumerationOptions.None, out var error);
                    if (error is not null)
                    {
                        tcs.TrySetException(new NSErrorException(error));
                    }
                    else
                    {
                        var items = content
                            .Select(u => CreateItem(u, SecurityScopedAncestorUrl))
                            .ToArray();
                        tcs.TrySetResult(items);
                    }
                });
        
            if (error is not null)
            {
                throw new NSErrorException(error);
            }

            var items = await tcs.Task;
            foreach (var item in items)
            {
                yield return item;
            }
        }
        finally
        {
            SecurityScopedAncestorUrl.StopAccessingSecurityScopedResource();
        }
    }

    public Task<IStorageFile?> CreateFileAsync(string name)
    {
        try
        {
            if (!SecurityScopedAncestorUrl.StartAccessingSecurityScopedResource())
            {
                return Task.FromResult<IStorageFile?>(null);
            }

            var path = System.IO.Path.Combine(FilePath, name);
            NSFileAttributes? attributes = null;
            if (NSFileManager.DefaultManager.CreateFile(path, new NSData(), attributes))
            {
                return Task.FromResult<IStorageFile?>(new IOSStorageFile(new NSUrl(path, false), SecurityScopedAncestorUrl));
            }

            return Task.FromResult<IStorageFile?>(null);
        }
        finally
        {
            SecurityScopedAncestorUrl.StopAccessingSecurityScopedResource();
        }
    }

    public Task<IStorageFolder?> CreateFolderAsync(string name)
    {
        try
        {
            SecurityScopedAncestorUrl.StartAccessingSecurityScopedResource();

            var path = System.IO.Path.Combine(FilePath, name);
            NSFileAttributes? attributes = null;
            if (NSFileManager.DefaultManager.CreateDirectory(path, false, attributes, out var error))
            {
                return Task.FromResult<IStorageFolder?>(new IOSStorageFolder(new NSUrl(path, true), SecurityScopedAncestorUrl));
            }

            if (error is not null)
            {
                throw new NSErrorException(error);
            }

            return Task.FromResult<IStorageFolder?>(null);
        }
        finally
        {
            SecurityScopedAncestorUrl.StopAccessingSecurityScopedResource();
        }
    }

    private NSUrl? GetItem(string name, bool isDirectory)
    {
        try
        {
            SecurityScopedAncestorUrl.StartAccessingSecurityScopedResource();

            var path = System.IO.Path.Combine(FilePath, name);
            if (NSFileManager.DefaultManager.FileExists(path, ref isDirectory))
            {
                return new NSUrl(path, isDirectory);
            }

            return null;
        }
        finally
        {
            SecurityScopedAncestorUrl.StopAccessingSecurityScopedResource();
        }
    }

    public Task<IStorageFolder?> GetFolderAsync(string name)
    {
        var url = GetItem(name, true);
        return Task.FromResult<IStorageFolder?>(url is null ? null : new IOSStorageFolder(url));
    }

    public Task<IStorageFile?> GetFileAsync(string name)
    {
        var url = GetItem(name, false);
        return Task.FromResult<IStorageFile?>(url is null ? null : new IOSStorageFile(url));
    }
}
