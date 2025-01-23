import { avaloniaDb, fileBookmarksStore } from "./indexedDb";
import { FileSystemFileHandle, FileSystemDirectoryHandle, FileSystemWritableFileStream } from "native-file-system-adapter";
import { Caniuse } from "../avalonia";

export class StorageItem {
    private constructor(
        public handle?: FileSystemFileHandle | FileSystemDirectoryHandle,
        private readonly file?: File,
        private readonly bookmarkId?: string,
        public wellKnownType?: WellKnownDirectory
    ) {
    }

    public get name(): string {
        if (this.handle) {
            return this.handle.name;
        }
        if (this.file) {
            return this.file.name;
        }
        return this.wellKnownType ?? "";
    }

    public get kind(): "file" | "directory" {
        if (this.handle) {
            return this.handle.kind;
        }
        if (this.file) {
            return "file";
        }
        return "directory";
    }

    public static createFromHandle(handle: FileSystemFileHandle | FileSystemDirectoryHandle, bookmarkId?: string) {
        return new StorageItem(handle, undefined, bookmarkId, undefined);
    }

    public static createFromFile(file: File) {
        return new StorageItem(undefined, file, undefined, undefined);
    }

    public static createWellKnownDirectory(type: WellKnownDirectory) {
        return new StorageItem(undefined, undefined, undefined, type);
    }

    public static async openRead(item: StorageItem): Promise<Blob> {
        if (item.file) {
            return item.file;
        }

        if (!item.handle || item.kind !== "file") {
            throw new Error("StorageItem is not a file");
        }

        await item.verityPermissions("read");

        const file = await (item.handle as FileSystemFileHandle).getFile();
        return file;
    }

    public static async openWrite(item: StorageItem): Promise<FileSystemWritableFileStream> {
        if (!item.handle || item.kind !== "file") {
            throw new Error("StorageItem is not a writeable file");
        }

        await item.verityPermissions("readwrite");

        return await (item.handle as FileSystemFileHandle).createWritable({ keepExistingData: true });
    }

    public static async getProperties(item: StorageItem): Promise<{ Size: number; LastModified: number; Type: string } | null> {
        // getFile can fail with an exception depending if we use polyfill with a save file dialog or not.
        try {
            const file = item.handle && "getFile" in item.handle
                ? await item.handle.getFile()
                : item.file;

            if (!file) {
                return null;
            }

            return {
                Size: file.size,
                LastModified: file.lastModified,
                Type: file.type
            };
        } catch {
            return null;
        }
    }

    public static getItemsIterator(item: StorageItem): any | null {
        if (item.kind !== "directory" || !item.handle) {
            return null;
        }

        return (item.handle as any).entries();
    }

    public static async createFile(item: StorageItem, name: string): Promise<any | null> {
        if (item.kind !== "directory" || !item.handle) {
            throw new TypeError("Unable to create item in the requested directory");
        }

        await item.verityPermissions("readwrite");

        return await ((item.handle as any).getFileHandle(name, { create: true }) as Promise<any>);
    }

    public static async getFile(item: StorageItem, name: string): Promise<any | null> {
        if (item.kind !== "directory" || !item.handle) {
            return null;
        }

        await item.verityPermissions("read");

        return await ((item.handle as any).getFileHandle(name) as Promise<any>);
    }

    public static async createFolder(item: StorageItem, name: string): Promise<any | null> {
        if (item.kind !== "directory" || !item.handle) {
            throw new TypeError("Unable to create item in the requested directory");
        }

        await item.verityPermissions("readwrite");

        return await ((item.handle as any).getDirectoryHandle(name, { create: true }) as Promise<any>);
    }

    public static async getFolder(item: StorageItem, name: string): Promise<any | null> {
        if (item.kind !== "directory" || !item.handle) {
            return null;
        }

        await item.verityPermissions("read");

        return await ((item.handle as any).getDirectoryHandle(name) as Promise<any>);
    }

    public static async deleteAsync(item: StorageItem): Promise<any | null> {
        if (!item.handle) {
            return null;
        }

        await item.verityPermissions("readwrite");

        return await ((item.handle as any).remove({ recursive: true }) as Promise<any>);
    }

    public static async moveAsync(item: StorageItem, destination: StorageItem): Promise<any | null> {
        if (!item.handle) {
            return null;
        }
        if (destination.kind !== "directory" || !destination.handle) {
            throw new TypeError("Unable to move item to the requested directory");
        }

        await item.verityPermissions("readwrite");

        return await ((item.handle as any).move(destination /*, newName */) as Promise<any>);
    }

    private async verityPermissions(mode: "read" | "readwrite"): Promise<void | never> {
        if (!this.handle) {
            return;
        }

        // If we are using polyfill, let it decide permissions by itself, we can't request anything in this case.
        if (!Caniuse.hasNativeFilePicker()) {
            return;
        }

        if (await this.handle.queryPermission({ mode }) === "granted") {
            return;
        }

        if (await this.handle.requestPermission({ mode }) === "denied") {
            throw new Error("Permissions denied");
        }
    }

    public static async saveBookmark(item: StorageItem): Promise<string | null> {
        // If file was previously bookmarked, just return old one.
        if (item.bookmarkId) {
            return item.bookmarkId;
        }

        // Bookmarks are not supported with polyfill.
        if (!item.handle || !Caniuse.hasNativeFilePicker()) {
            return null;
        }

        const connection = await avaloniaDb.connect();
        try {
            const key = await connection.put(fileBookmarksStore, item.handle, item.generateBookmarkId());
            return key as string;
        } finally {
            connection.close();
        }
    }

    public static async deleteBookmark(item: StorageItem): Promise<void> {
        if (!item.bookmarkId || !Caniuse.hasNativeFilePicker()) {
            return;
        }

        const connection = await avaloniaDb.connect();
        try {
            await connection.delete(fileBookmarksStore, item.bookmarkId);
        } finally {
            connection.close();
        }
    }

    private generateBookmarkId(): string {
        return Date.now().toString(36) + Math.random().toString(36).substring(2);
    }
}

export class StorageItems {
    constructor(private readonly items: StorageItem[]) { }

    public static itemsArray(instance: StorageItems): StorageItem[] {
        return instance.items;
    }

    public static filesToItemsArray(files: File[]): StorageItem[] {
        if (!files) {
            return [];
        }

        const retItems = [];
        for (let i = 0; i < files.length; i++) {
            retItems[i] = StorageItem.createFromFile(files[i]);
        }
        return retItems;
    }
}
