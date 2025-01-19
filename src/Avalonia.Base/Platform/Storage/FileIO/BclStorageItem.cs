using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace Avalonia.Platform.Storage.FileIO;

internal abstract class BclStorageItem(FileSystemInfo fileSystemInfo) : IStorageBookmarkItem, IStorageItemWithFileSystemInfo
{
    public FileSystemInfo FileSystemInfo { get; } = fileSystemInfo switch
    {
        null => throw new ArgumentNullException(nameof(fileSystemInfo)),
        DirectoryInfo { Exists: false } => throw new ArgumentException("Directory must exist", nameof(fileSystemInfo)),
        _ => fileSystemInfo
    };

    public string Name => FileSystemInfo.Name;

    public bool CanBookmark => true;

    public Uri Path => GetPathCore(FileSystemInfo);

    public Task<StorageItemProperties> GetBasicPropertiesAsync()
    {
        return Task.FromResult(GetBasicPropertiesAsyncCore(FileSystemInfo));
    }

    public Task<IStorageFolder?> GetParentAsync() => Task.FromResult(
        (IStorageFolder?)WrapFileSystemInfo(GetParentCore(FileSystemInfo)));

    public Task DeleteAsync()
    {
        DeleteCore(FileSystemInfo);
        return Task.CompletedTask;
    }

    public Task<IStorageItem?> MoveAsync(IStorageFolder destination) => Task.FromResult(
        WrapFileSystemInfo(MoveCore(FileSystemInfo, destination)));

    public Task<string?> SaveBookmarkAsync()
    {
        var path = FileSystemInfo.FullName;
        return Task.FromResult<string?>(StorageBookmarkHelper.EncodeBclBookmark(path));
    }

    public Task ReleaseBookmarkAsync() => Task.CompletedTask;

    public void Dispose() { }

    [return: NotNullIfNotNull(nameof(fileSystemInfo))]
    protected IStorageItem? WrapFileSystemInfo(FileSystemInfo? fileSystemInfo) => fileSystemInfo switch
    {
        DirectoryInfo directoryInfo => new BclStorageFolder(directoryInfo),
        FileInfo fileInfo => new BclStorageFile(fileInfo),
        _ => null
    };

    internal static void DeleteCore(FileSystemInfo fileSystemInfo) => fileSystemInfo.Delete();

    internal static Uri GetPathCore(FileSystemInfo fileSystemInfo)
    {
        try
        {
            if (fileSystemInfo is DirectoryInfo { Parent: not null } or FileInfo { Directory: not null })
            {
                return StorageProviderHelpers.UriFromFilePath(fileSystemInfo.FullName, fileSystemInfo is DirectoryInfo);
            }
        }
        catch (SecurityException)
        {
        }

        return new Uri(fileSystemInfo.Name, UriKind.Relative);
    }

    internal static StorageItemProperties GetBasicPropertiesAsyncCore(FileSystemInfo fileSystemInfo)
    {
        if (fileSystemInfo.Exists)
        {
            return new StorageItemProperties(
                fileSystemInfo is FileInfo fileInfo ? (ulong)fileInfo.Length : 0,
                fileSystemInfo.CreationTimeUtc,
                fileSystemInfo.LastAccessTimeUtc);
        }

        return new StorageItemProperties();
    }

    internal static DirectoryInfo? GetParentCore(FileSystemInfo fileSystemInfo) => fileSystemInfo switch
    {
        FileInfo { Directory: { } directory } => directory,
        DirectoryInfo { Parent: { } parent } => parent,
        _ => null
    };

    internal static FileSystemInfo? MoveCore(FileSystemInfo fileSystemInfo, IStorageFolder destination)
    {
        if (destination?.TryGetLocalPath() is { } destinationPath)
        {
            var newPath = System.IO.Path.Combine(destinationPath, fileSystemInfo.Name);
            if (fileSystemInfo is DirectoryInfo directoryInfo)
            {
                directoryInfo.MoveTo(newPath);
                return new DirectoryInfo(newPath);
            }

            if (fileSystemInfo is FileInfo fileInfo)
            {
                fileInfo.MoveTo(newPath);
                return new FileInfo(newPath);
            }
        }

        return null;
    }

    internal static FileStream OpenReadCore(FileInfo fileInfo) => fileInfo.OpenRead();

    internal static FileStream OpenWriteCore(FileInfo fileInfo) =>
        new(fileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.Write);

    internal static IEnumerable<FileSystemInfo> GetItemsCore(DirectoryInfo directoryInfo) => directoryInfo
        .EnumerateDirectories()
        .OfType<FileSystemInfo>()
        .Concat(directoryInfo.EnumerateFiles());

    internal static FileSystemInfo? GetFolderCore(DirectoryInfo directoryInfo, string name)
    {
        var path = System.IO.Path.Combine(directoryInfo.FullName, name);
        if (Directory.Exists(path))
        {
            return new DirectoryInfo(path);
        }

        return null;
    }

    internal static FileSystemInfo? GetFileCore(DirectoryInfo directoryInfo, string name)
    {
        var path = System.IO.Path.Combine(directoryInfo.FullName, name);
        if (File.Exists(path))
        {
            return new FileInfo(path);
        }

        return null;
    }

    internal static FileInfo CreateFileCore(DirectoryInfo directoryInfo, string name)
    {
        var fileName = System.IO.Path.Combine(directoryInfo.FullName, name);
        var newFile = new FileInfo(fileName);

        using var stream = newFile.Create();
        return newFile;
    }

    internal static DirectoryInfo CreateFolderCore(DirectoryInfo directoryInfo, string name) =>
        directoryInfo.CreateSubdirectory(name);
}
