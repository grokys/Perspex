using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Browser.Interop;

internal static partial class StorageHelper
{
    [JSImport("Caniuse.hasNativeFilePicker", AvaloniaModule.MainModuleName)]
    public static partial bool HasNativeFilePicker();

    [JSImport("StorageProvider.selectFolderDialog", AvaloniaModule.StorageModuleName)]
    public static partial Task<JSObject?> SelectFolderDialog(JSObject? startIn, bool preferPolyfill);

    [JSImport("StorageProvider.openFileDialog", AvaloniaModule.StorageModuleName)]
    public static partial Task<JSObject?> OpenFileDialog(JSObject? startIn, bool multiple,
        [JSMarshalAs<JSType.Array<JSType.Any>>] object[]? types, bool excludeAcceptAllOption, bool preferPolyfill);

    [JSImport("StorageProvider.saveFileDialog", AvaloniaModule.StorageModuleName)]
    public static partial Task<JSObject?> SaveFileDialog(JSObject? startIn, string? suggestedName,
        [JSMarshalAs<JSType.Array<JSType.Any>>] object[]? types, bool excludeAcceptAllOption, bool preferPolyfill);

    [JSImport("StorageItem.createWellKnownDirectory", AvaloniaModule.StorageModuleName)]
    public static partial JSObject CreateWellKnownDirectory(string wellKnownDirectory);
    
    [JSImport("StorageProvider.openBookmark", AvaloniaModule.StorageModuleName)]
    public static partial Task<JSObject?> OpenBookmark(string key);

    [JSImport("StorageItem.saveBookmark", AvaloniaModule.StorageModuleName)]
    public static partial Task<string?> SaveBookmark(JSObject item);

    [JSImport("StorageItem.deleteBookmark", AvaloniaModule.StorageModuleName)]
    public static partial Task DeleteBookmark(JSObject item);

    [JSImport("StorageItem.getProperties", AvaloniaModule.StorageModuleName)]
    public static partial Task<JSObject?> GetProperties(JSObject item);

    [JSImport("StorageItem.openWrite", AvaloniaModule.StorageModuleName)]
    public static partial Task<JSObject> OpenWrite(JSObject item);

    [JSImport("StorageItem.openRead", AvaloniaModule.StorageModuleName)]
    public static partial Task<JSObject> OpenRead(JSObject item);
    
    [JSImport("StorageItem.createFromHandle", AvaloniaModule.StorageModuleName)]
    public static partial JSObject? StorageItemFromHandle(JSObject handle);

    [JSImport("StorageItem.getItemsIterator", AvaloniaModule.StorageModuleName)]
    [return: JSMarshalAs<JSType.Object>]
    public static partial JSObject? GetItemsIterator(JSObject item);

    [JSImport("StorageItems.itemsArray", AvaloniaModule.StorageModuleName)]
    public static partial JSObject[] ItemsArray(JSObject item);
    
    [JSImport("StorageItems.filesToItemsArray", AvaloniaModule.StorageModuleName)]
    public static partial JSObject[] FilesToItemsArray(JSObject item);

    [JSImport("StorageProvider.createAcceptType", AvaloniaModule.StorageModuleName)]
    public static partial JSObject CreateAcceptType(string description, string[] mimeTypes, string[]? extensions);

    [JSImport("StorageProvider.deleteAsync", AvaloniaModule.StorageModuleName)]
    public static partial Task DeleteAsync(JSObject fileHandle);
    
    [JSImport("StorageProvider.moveAsync", AvaloniaModule.StorageModuleName)]
    public static partial Task<JSObject?> MoveAsync(JSObject fileHandle, JSObject destinationFolder);
    
    [JSImport("StorageProvider.createFile", AvaloniaModule.StorageModuleName)]
    public static partial Task<JSObject?> CreateFile(JSObject folderHandle, string name);
    
    [JSImport("StorageProvider.createFolder", AvaloniaModule.StorageModuleName)]
    public static partial Task<JSObject?> CreateFolder(JSObject folderHandle, string name);

    [JSImport("StorageItem.getFile", AvaloniaModule.StorageModuleName)]
    public static partial Task<JSObject?> GetFile(JSObject folderHandle, string name);

    [JSImport("StorageItem.getFolder", AvaloniaModule.StorageModuleName)]
    public static partial Task<JSObject?> GetFolder(JSObject folderHandle, string name);
}
