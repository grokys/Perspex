#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Utilities;

namespace Avalonia.Native;

internal class StorageItem : IStorageBookmarkItem, IStorageItemWithFileSystemInfo
{
    private readonly StorageProviderApi _storageProviderApi;
    private readonly FileSystemInfo _fileSystemInfo;

    protected StorageItem(StorageProviderApi storageProviderApi, FileSystemInfo fileSystemInfo, Uri uri, Uri scopeOwnerUri)
    {
        _storageProviderApi = storageProviderApi;
        Path = uri;
        _fileSystemInfo = fileSystemInfo;
        ScopeOwnerUri = scopeOwnerUri;
    }

    public string Name => _fileSystemInfo.Name;
    public Uri Path { get; }
    public Uri ScopeOwnerUri { get; }

    public Task<StorageItemProperties> GetBasicPropertiesAsync()
    {
        using var scope = OpenScope();
        return Task.FromResult(
            BclStorageItem.GetBasicPropertiesAsyncCore(_fileSystemInfo));
    }

    public bool CanBookmark => true;
    public FileSystemInfo FileSystemInfo => _fileSystemInfo;

    protected IDisposable? OpenScope()
    {
        return _storageProviderApi.OpenSecurityScope(ScopeOwnerUri.AbsoluteUri);
    }

    public Task<string?> SaveBookmarkAsync()
    {
        using var scope = OpenScope();
        return Task.FromResult(_storageProviderApi.SaveBookmark(Path));
    }

    public Task ReleaseBookmarkAsync()
    {
        _storageProviderApi.ReleaseBookmark(Path);
        return Task.CompletedTask;
    }

    public Task<IStorageFolder?> GetParentAsync()
    {
        using var scope = OpenScope();
        var parent = BclStorageItem.GetParentCore(_fileSystemInfo);
        return Task.FromResult((IStorageFolder?)WrapFileSystemInfo(parent, null));
    }

    public Task DeleteAsync()
    {
        using var scope = OpenScope();
        BclStorageItem.DeleteCore(_fileSystemInfo);
        return Task.CompletedTask;
    }

    public Task<IStorageItem?> MoveAsync(IStorageFolder destination)
    {
        using var destinationScope = (destination as StorageItem)?.OpenScope();
        using var scope = OpenScope();
        var item = WrapFileSystemInfo(BclStorageItem.MoveCore(_fileSystemInfo, destination), null);
        return Task.FromResult(item);
    }

    [return: NotNullIfNotNull(nameof(fileSystemInfo))]
    protected IStorageItem? WrapFileSystemInfo(FileSystemInfo? fileSystemInfo, Uri? scopedOwner)
    {
        if (fileSystemInfo is null) return null;

        // It might not be always correct to assume NSUri from the file path, but that's the best we have here without using native API directly.
        var fileUri = BclStorageItem.GetPathCore(fileSystemInfo);
        return fileSystemInfo switch
        {
            DirectoryInfo directoryInfo => new StorageFolder(
                _storageProviderApi, directoryInfo, fileUri, scopedOwner ?? fileUri),
            FileInfo fileInfo => new StorageFile(
                _storageProviderApi, fileInfo, fileUri, scopedOwner ?? fileUri),
            _ => throw new ArgumentOutOfRangeException(nameof(fileSystemInfo), fileSystemInfo, null)
        };
    }

    public void Dispose()
    {
    }
}

internal class StorageFile(
    StorageProviderApi storageProviderApi, FileInfo fileInfo, Uri uri, Uri scopeOwnerUri)
    : StorageItem(storageProviderApi, fileInfo, uri, scopeOwnerUri), IStorageBookmarkFile
{
    public Task<Stream> OpenReadAsync()
    {
        var scope = OpenScope();
        var innerStream = BclStorageItem.OpenReadCore(fileInfo);
        return Task.FromResult<Stream>(scope is not null ? new SecurityScopedStream(innerStream, scope) : innerStream);
    }

    public Task<Stream> OpenWriteAsync()
    {
        var scope = OpenScope();
        var innerStream = BclStorageItem.OpenWriteCore(fileInfo);
        return Task.FromResult<Stream>(scope is not null ? new SecurityScopedStream(innerStream, scope) : innerStream);
    }
}

internal class StorageFolder(
    StorageProviderApi storageProviderApi, DirectoryInfo directoryInfo, Uri uri, Uri scopeOwnerUri)
    : StorageItem(storageProviderApi, directoryInfo, uri, scopeOwnerUri), IStorageBookmarkFolder
{
    public IAsyncEnumerable<IStorageItem> GetItemsAsync()
    {
        return GetItems().AsAsyncEnumerable();

        IEnumerable<IStorageItem> GetItems()
        {
            using var scope = OpenScope();
            foreach (var item in BclStorageItem.GetItemsCore(directoryInfo))
            {
                yield return WrapFileSystemInfo(item, ScopeOwnerUri);
            }
        }
    }

    public Task<IStorageFile?> CreateFileAsync(string name)
    {
        using var scope = OpenScope();
        var file = BclStorageItem.CreateFileCore(directoryInfo, name);
        return Task.FromResult((IStorageFile?)WrapFileSystemInfo(file, ScopeOwnerUri));
    }

    public Task<IStorageFolder?> CreateFolderAsync(string name)
    {
        using var scope = OpenScope();
        var folder = BclStorageItem.CreateFolderCore(directoryInfo, name);
        return Task.FromResult((IStorageFolder?)WrapFileSystemInfo(folder, ScopeOwnerUri));
    }

    public Task<IStorageFolder?> GetFolderAsync(string name)
    {
        using var scope = OpenScope();
        var item = BclStorageItem.GetFolderCore(directoryInfo, name);
        return Task.FromResult((IStorageFolder?)WrapFileSystemInfo(item, ScopeOwnerUri));
    }

    public Task<IStorageFile?> GetFileAsync(string name)
    {
        using var scope = OpenScope();
        var item = BclStorageItem.GetFileCore(directoryInfo, name);
        return Task.FromResult((IStorageFile?)WrapFileSystemInfo(item, ScopeOwnerUri));
    }
}
