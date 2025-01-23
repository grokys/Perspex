using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Provider;
using Android.Webkit;
using Avalonia.Logging;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Java.Lang;
using AndroidUri = Android.Net.Uri;
using Exception = System.Exception;
using JavaFile = Java.IO.File;

namespace Avalonia.Android.Platform.Storage;

internal abstract class AndroidStorageItem : IStorageBookmarkItem
{
    private Activity? _activity;
    private readonly bool _needsExternalFilesPermission;
    private readonly AndroidStorageFolder? _parent;
    private readonly AndroidUri? _permissionRoot;

    protected AndroidStorageItem(Activity activity, AndroidUri uri, bool needsExternalFilesPermission, AndroidStorageFolder? parent = null, AndroidUri? permissionRoot = null)
    {
        _activity = activity;
        _needsExternalFilesPermission = needsExternalFilesPermission;
        _parent = parent;
        _permissionRoot = permissionRoot ?? parent?.Uri ?? Uri;
        Uri = uri;
    }

    internal AndroidUri Uri { get; set; }
    
    protected Activity Activity => _activity ?? throw new ObjectDisposedException(nameof(AndroidStorageItem));

    public virtual string Name => GetColumnValue(Activity, Uri, DocumentsContract.Document.ColumnDisplayName)
                          ?? GetColumnValue(Activity, Uri, MediaStore.IMediaColumns.DisplayName)
                          ?? Uri.PathSegments?.LastOrDefault()?.Split("/", StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;

    public Uri Path => new(Uri.ToString()!);

    public bool CanBookmark => true;

    public async Task<string?> SaveBookmarkAsync()
    {
        if (!await EnsureExternalFilesPermission(false))
        {
            return null;
        }

        Activity.ContentResolver?.TakePersistableUriPermission(Uri, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);

        return StorageBookmarkHelper.EncodeBookmark(AndroidStorageProvider.AndroidKey, Uri.ToString()!);
    }

    public async Task ReleaseBookmarkAsync()
    {
        if (!await EnsureExternalFilesPermission(false))
        {
            return;
        }

        Activity.ContentResolver?.ReleasePersistableUriPermission(Uri, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
    }
    
    public abstract Task<StorageItemProperties> GetBasicPropertiesAsync();

    protected static string? GetColumnValue(Context context, AndroidUri contentUri, string column, string? selection = null, string[]? selectionArgs = null)
    {
        try
        {
            var projection = new[] { column };
            using var cursor = context.ContentResolver!.Query(contentUri, projection, selection, selectionArgs, null);
            if (cursor?.MoveToFirst() == true)
            {
                var columnIndex = cursor.GetColumnIndex(column);
                if (columnIndex != -1)
                    return cursor.GetString(columnIndex);
            }
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?.Log(null, "File metadata reader failed: '{Exception}'", ex);
        }

        return null;
    }

    public async Task<IStorageFolder?> GetParentAsync()
    {
        if (!await EnsureExternalFilesPermission(false))
        {
            return null;
        }

        if(_parent != null)
        {
            return _parent;
        }

        using var javaFile = new JavaFile(Uri.Path!);

        // Java file represents files AND directories. Don't be confused.
        if (javaFile.ParentFile is {} parentFile
            && AndroidUri.FromFile(parentFile) is {} androidUri)
        {
            return new AndroidStorageFolder(Activity, androidUri, false);
        }

        return null;
    }

    protected async Task<bool> EnsureExternalFilesPermission(bool write)
    {
        if (!_needsExternalFilesPermission)
        {
            return true;
        }

        return await _activity!.CheckPermission(Manifest.Permission.ReadExternalStorage);
    }
    
    public void Dispose()
    {
        _activity = null;
    }
    
    internal AndroidUri? PermissionRoot => _permissionRoot;

    public abstract Task DeleteAsync();

    public abstract Task<IStorageItem?> MoveAsync(IStorageFolder destination);

    public static IStorageItem CreateItem(Activity activity, AndroidUri uri)
    {
        var mimeType = GetColumnValue(activity, uri, DocumentsContract.Document.ColumnMimeType);
        if (mimeType == DocumentsContract.Document.MimeTypeDir)
        {
            return new AndroidStorageFolder(activity, uri, false);
        }
        else
        {
            return new AndroidStorageFile(activity, uri);
        }
    }
}

internal class AndroidStorageFolder : AndroidStorageItem, IStorageBookmarkFolder
{
    public AndroidStorageFolder(Activity activity, AndroidUri uri, bool needsExternalFilesPermission, AndroidStorageFolder? parent = null, AndroidUri? permissionRoot = null) : base(activity, uri, needsExternalFilesPermission, parent, permissionRoot)
    {
    }

    public Task<IStorageFile?> CreateFileAsync(string name)
    {
        var mimeType = MimeTypeMap.Singleton?.GetMimeTypeFromExtension(MimeTypeMap.GetFileExtensionFromUrl(name)) ?? "application/octet-stream";
        var newFile = DocumentsContract.CreateDocument(Activity.ContentResolver!, Uri, mimeType, name);
        if(newFile == null)
        {
            return Task.FromResult<IStorageFile?>(null);
        }

        return Task.FromResult<IStorageFile?>(new AndroidStorageFile(Activity, newFile, this));
    }

    public Task<IStorageFolder?> CreateFolderAsync(string name)
    {
        var newFolder = DocumentsContract.CreateDocument(Activity.ContentResolver!, Uri, DocumentsContract.Document.MimeTypeDir, name);
        if (newFolder == null)
        {
            return Task.FromResult<IStorageFolder?>(null);
        }

        return Task.FromResult<IStorageFolder?>(new AndroidStorageFolder(Activity, newFolder, false, this, PermissionRoot));
    }

    public override async Task DeleteAsync()
    {
        if (!await EnsureExternalFilesPermission(false))
        {
            return;
        }

        if (Activity != null)
        {
            await DeleteContents(this);
        }

        async Task DeleteContents(AndroidStorageFolder storageFolder)
        {
            await foreach (var file in storageFolder.GetItemsAsync())
            {
                if(file is AndroidStorageFolder folder)
                {
                    await DeleteContents(folder);
                }
                else if(file is AndroidStorageFile storageFile)
                {
                    await storageFile.DeleteAsync();
                }
            }

            DocumentsContract.DeleteDocument(Activity.ContentResolver!, storageFolder.Uri);
        }
    }

    public override Task<StorageItemProperties> GetBasicPropertiesAsync()
    {
        return Task.FromResult(new StorageItemProperties());
    }

    public async IAsyncEnumerable<IStorageItem> GetItemsAsync()
    {
        if (!await EnsureExternalFilesPermission(false))
        {
            yield break;
        }

        var contentResolver = Activity.ContentResolver;
        if (contentResolver == null)
        {
            yield break;
        }

        var root = PermissionRoot ?? Uri;
        var folderId = root != Uri ? DocumentsContract.GetDocumentId(Uri) : DocumentsContract.GetTreeDocumentId(Uri);
        var childrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(root, folderId);

        var projection = new[]
        {
            DocumentsContract.Document.ColumnDocumentId,
            DocumentsContract.Document.ColumnMimeType
        };
        if (childrenUri != null)
        {
            using var cursor = contentResolver.Query(childrenUri, projection, null, null, null);

            if (cursor != null)
                while (cursor.MoveToNext())
                {
                    var mime = cursor.GetString(1);
                    var id = cursor.GetString(0);

                    bool isDirectory = mime == DocumentsContract.Document.MimeTypeDir;
                    var uri = DocumentsContract.BuildDocumentUriUsingTree(root, id);

                    if (uri == null)
                    {
                        continue;
                    }
                    yield return isDirectory ? new AndroidStorageFolder(Activity, uri, false, this, root) :
                        new AndroidStorageFile(Activity, uri, this, root);
                }
        }
    }

    public override async Task<IStorageItem?> MoveAsync(IStorageFolder destination)
    {
        if (Activity != null)
        {
            return await MoveRecursively(this, (AndroidStorageFolder)destination);
        }

        return null;

        static async Task<AndroidStorageFolder?> MoveRecursively(AndroidStorageFolder storageFolder, AndroidStorageFolder destination)
        {
            if (await destination.CreateFolderAsync(storageFolder.Name) is not AndroidStorageFolder newDestination)
            {
                return null;
            }

            destination = newDestination;

            await foreach (var file in storageFolder.GetItemsAsync())
            {
                if (file is AndroidStorageFolder folder)
                {
                    await MoveRecursively(folder, destination);
                }
                else if (file is AndroidStorageFile)
                {
                    await file.MoveAsync(destination);
                }
            }

            await storageFolder.DeleteAsync();

            return destination;
        }
    }

    private async Task<IStorageItem?> GetItemAsync(string name, bool isDirectory)
    {
        if (!await EnsureExternalFilesPermission(false))
        {
            return null;
        }

        var contentResolver = Activity.ContentResolver;
        if (contentResolver == null)
        {
            return null;
        }

        var root = PermissionRoot ?? Uri;
        var folderId = root != Uri ? DocumentsContract.GetDocumentId(Uri) : DocumentsContract.GetTreeDocumentId(Uri);
        var childrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(root, folderId);

        var projection = new[]
        {
            DocumentsContract.Document.ColumnDocumentId,
            DocumentsContract.Document.ColumnMimeType,
            DocumentsContract.Document.ColumnDisplayName
        };

        if (childrenUri != null)
        {
            using var cursor = contentResolver.Query(childrenUri, projection, null, null, null);
            if (cursor != null)
            {
                while (cursor.MoveToNext())
                {
                    var id = cursor.GetString(0);
                    var mime = cursor.GetString(1);

                    var fileName = cursor.GetString(2);
                    if (fileName != name)
                    {
                        continue;
                    }                   

                    bool mineDirectory = mime == DocumentsContract.Document.MimeTypeDir;
                    if (isDirectory != mineDirectory)
                    {
                        return null;
                    }

                    var uri = DocumentsContract.BuildDocumentUriUsingTree(root, id);
                    if (uri == null)
                    {
                        return null;
                    }

                    return isDirectory ? new AndroidStorageFolder(Activity, uri, false, this, root) :
                        new AndroidStorageFile(Activity, uri, this, root);
                }
            }
        }

        return null;
    }

    public async Task<IStorageFolder?> GetFolderAsync(string name)
    {
        var folder = await GetItemAsync(name, true);
        return (IStorageFolder?)folder;
    }

    public async Task<IStorageFile?> GetFileAsync(string name)
    {
        var file = await GetItemAsync(name, false);
        return (IStorageFile?)file;
    }
}

internal sealed class WellKnownAndroidStorageFolder : AndroidStorageFolder
{
    public WellKnownAndroidStorageFolder(Activity activity, string identifier, AndroidUri uri, bool needsExternalFilesPermission)
        : base(activity, uri, needsExternalFilesPermission)
    {
        Name = identifier;
    }

    public override string Name { get; }
}

internal sealed class AndroidStorageFile : AndroidStorageItem, IStorageBookmarkFile
{
    public AndroidStorageFile(Activity activity, AndroidUri uri, AndroidStorageFolder? parent = null, AndroidUri? permissionRoot = null) : base(activity, uri, false, parent, permissionRoot)
    {
    }

    public Task<Stream> OpenReadAsync() => Task.FromResult(OpenContentStream(Activity, Uri, false)
        ?? throw new InvalidOperationException("Failed to open content stream"));

    public Task<Stream> OpenWriteAsync() => Task.FromResult(OpenContentStream(Activity, Uri, true)
        ?? throw new InvalidOperationException("Failed to open content stream"));

    private Stream? OpenContentStream(Context context, AndroidUri uri, bool isOutput)
    {
        var isVirtual = IsVirtualFile(context, uri);
        if (isVirtual)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?.Log(this, "Content URI was virtual: '{Uri}'", uri);
            return GetVirtualFileStream(context, uri, isOutput);
        }

        return isOutput
            ? context.ContentResolver?.OpenOutputStream(uri)
            : context.ContentResolver?.OpenInputStream(uri);
    }

    private bool IsVirtualFile(Context context, AndroidUri uri)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(24))
            return false;

        if (!DocumentsContract.IsDocumentUri(context, uri))
            return false;

        var value = GetColumnValue(context, uri, DocumentsContract.Document.ColumnFlags);
        if (!string.IsNullOrEmpty(value) && int.TryParse(value, out var flagsInt))
        {
            var flags = (DocumentContractFlags)flagsInt;
            return flags.HasFlag(DocumentContractFlags.VirtualDocument);
        }

        return false;
    }

    private static Stream? GetVirtualFileStream(Context context, AndroidUri uri, bool isOutput)
    {
        var mimeTypes = context.ContentResolver?.GetStreamTypes(uri, FilePickerFileTypes.All.MimeTypes![0]);
        if (mimeTypes?.Length >= 1)
        {
            var mimeType = mimeTypes[0];
            var asset = context.ContentResolver!
                .OpenTypedAssetFileDescriptor(uri, mimeType, null);

            var stream = isOutput
                ? asset?.CreateOutputStream()
                : asset?.CreateInputStream();

            return stream;
        }

        return null;
    }

    public override Task<StorageItemProperties> GetBasicPropertiesAsync()
    {
        ulong? size = null;
        DateTimeOffset? itemDate = null;
        DateTimeOffset? dateModified = null;

        try
        {
            var projection = new[]
            {
                MediaStore.IMediaColumns.Size, MediaStore.IMediaColumns.DateAdded,
                MediaStore.IMediaColumns.DateModified
            };
            using var cursor = Activity.ContentResolver!.Query(Uri, projection, null, null, null);

            if (cursor?.MoveToFirst() == true)
            {
                try
                {
                    var columnIndex = cursor.GetColumnIndex(MediaStore.IMediaColumns.Size);
                    if (columnIndex != -1)
                    {
                        size = (ulong)cursor.GetLong(columnIndex);
                    }
                }
                catch (Exception ex)
                {
                    Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                        .Log(this, "File Size metadata reader failed: '{Exception}'", ex);
                }

                try
                {
                    var columnIndex = cursor.GetColumnIndex(MediaStore.IMediaColumns.DateAdded);
                    if (columnIndex != -1)
                    {
                        var longValue = cursor.GetLong(columnIndex);
                        itemDate = longValue > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(longValue) : null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                        .Log(this, "File DateAdded metadata reader failed: '{Exception}'", ex);
                }

                try
                {
                    var columnIndex = cursor.GetColumnIndex(MediaStore.IMediaColumns.DateModified);
                    if (columnIndex != -1)
                    {
                        var longValue = cursor.GetLong(columnIndex);
                        dateModified = longValue > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(longValue) : null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.TryGet(LogEventLevel.Verbose, LogArea.AndroidPlatform)?
                        .Log(this, "File DateAdded metadata reader failed: '{Exception}'", ex);
                }
            }
        }
        catch (UnsupportedOperationException)
        {
            // It's not possible to get parameters of some files/folders.
        }

        return Task.FromResult(new StorageItemProperties(size, itemDate, dateModified));
    }

    public override async Task DeleteAsync()
    {
        if (!await EnsureExternalFilesPermission(false))
        {
            return;
        }

        if (Activity != null)
        {
            DocumentsContract.DeleteDocument(Activity.ContentResolver!, Uri);
        }
    }

    public override async Task<IStorageItem?> MoveAsync(IStorageFolder destination)
    {
        if (!await EnsureExternalFilesPermission(false))
        {
            return null;
        }

        if (Activity != null && destination is AndroidStorageFolder storageFolder)
        {
            AndroidUri? movedUri = null;

            if (OperatingSystem.IsAndroidVersionAtLeast(24))
            {
                try
                {
                    if (Activity.ContentResolver is { } contentResolver &&
                        storageFolder.Uri is { } targetParentUri &&
                        await GetParentAsync() is AndroidStorageFolder parentFolder)
                    {
                        movedUri = DocumentsContract.MoveDocument(contentResolver, Uri, parentFolder.Uri, targetParentUri);
                    }
                }
                catch (Exception)
                {
                    // There are many reason why DocumentContract will fail to move a file. We fallback to copying below.
                }
            }

            if (movedUri is not null)
            {
                return new AndroidStorageFile(Activity, movedUri, storageFolder);
            }

            return await MoveFileByCopy();
        }

        async Task<AndroidStorageFile?> MoveFileByCopy()
        {
            var newFile = await storageFolder.CreateFileAsync(Name) as AndroidStorageFile;

            try
            {
                if (newFile != null)
                {
                    using var input = await OpenReadAsync();
                    using var output = await newFile.OpenWriteAsync();

                    await input.CopyToAsync(output);

                    await DeleteAsync();

                    return new AndroidStorageFile(Activity, newFile.Uri, storageFolder);
                }
            }
            catch
            {
                newFile?.DeleteAsync();
            }

            return null;
        }

        return null;
    }
}
