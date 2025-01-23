using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Utilities;

namespace Avalonia.Platform.Storage.FileIO;

internal sealed class BclStorageFolder(DirectoryInfo directoryInfo)
    : BclStorageItem(directoryInfo), IStorageBookmarkFolder
{
    public IAsyncEnumerable<IStorageItem> GetItemsAsync() => GetItemsCore(directoryInfo)
        .Select(WrapFileSystemInfo)
        .Where(f => f is not null)
        .AsAsyncEnumerable()!;

    public Task<IStorageFile?> CreateFileAsync(string name) => Task.FromResult(
        (IStorageFile?)WrapFileSystemInfo(CreateFileCore(directoryInfo, name)));

    public Task<IStorageFolder?> CreateFolderAsync(string name) => Task.FromResult(
        (IStorageFolder?)WrapFileSystemInfo(CreateFolderCore(directoryInfo, name)));

    public Task<IStorageFolder?> GetFolderAsync(string name) => Task.FromResult(
        (IStorageFolder?)WrapFileSystemInfo(GetFolderCore(directoryInfo, name)));

    public Task<IStorageFile?> GetFileAsync(string name) => Task.FromResult(
       (IStorageFile?)WrapFileSystemInfo(GetFileCore(directoryInfo, name)));
}
