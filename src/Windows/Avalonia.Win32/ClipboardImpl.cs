using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;

namespace Avalonia.Win32
{
    internal class ClipboardImpl : IClipboard
    {
        private const int OleRetryCount = 10;
        private const int OleRetryDelay = 100;
        /// <summary>
        /// The amount of time in milliseconds to sleep before flushing the clipboard after a set.
        /// </summary>
        /// <remarks>
        /// This is mitigation for clipboard listener issues.
        /// </remarks>
        private const int OleFlushDelay = 10;

        private static async Task<IDisposable> OpenClipboard()
        {
            var i = OleRetryCount;

            while (!UnmanagedMethods.OpenClipboard(IntPtr.Zero))
            {
                if (--i == 0)
                    throw new TimeoutException("Timeout opening clipboard.");
                await Task.Delay(100);
            }

            return Disposable.Create(() => UnmanagedMethods.CloseClipboard());
        }

        public async Task<string?> GetTextAsync()
        {
            using (await OpenClipboard())
            {
                IntPtr hText = UnmanagedMethods.GetClipboardData(UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT);
                if (hText == IntPtr.Zero)
                {
                    return null;
                }

                var pText = UnmanagedMethods.GlobalLock(hText);
                if (pText == IntPtr.Zero)
                {
                    return null;
                }

                var rv = Marshal.PtrToStringUni(pText);
                UnmanagedMethods.GlobalUnlock(hText);
                return rv;
            }
        }

        public async Task SetTextAsync(string? text)
        {
            using (await OpenClipboard())
            {
                UnmanagedMethods.EmptyClipboard();

                if (text is not null)
                {
                    var hGlobal = Marshal.StringToHGlobalUni(text);
                    UnmanagedMethods.SetClipboardData(UnmanagedMethods.ClipboardFormat.CF_UNICODETEXT, hGlobal);
                }
            }
        }

        public async Task ClearAsync()
        {
            using (await OpenClipboard())
            {
                UnmanagedMethods.EmptyClipboard();
            }
        }

        public async Task SetDataObjectAsync(IDataObject data)
        {
            Dispatcher.UIThread.VerifyAccess();
            using var wrapper = new DataObject(data);
            var i = OleRetryCount;

            while (true)
            {
                var ptr = wrapper.GetNativeIntPtr<Win32Com.IDataObject>();
                var hr = UnmanagedMethods.OleSetClipboard(ptr);

                if (hr == 0)
                    break;

                if (--i == 0)
                    Marshal.ThrowExceptionForHR(hr);

                await Task.Delay(OleRetryDelay);
            }
        }

        public async Task<string[]> GetFormatsAsync()
        {
            Dispatcher.UIThread.VerifyAccess();
            var i = OleRetryCount;

            while (true)
            {
                var hr = UnmanagedMethods.OleGetClipboard(out var dataObject);

                if (hr == 0)
                {
                    using var proxy = MicroComRuntime.CreateProxyFor<Win32Com.IDataObject>(dataObject, true);
                    using var wrapper = new OleDataObject(proxy);
                    var formats = wrapper.GetDataFormats().ToArray();
                    return formats;
                }

                if (--i == 0)
                    Marshal.ThrowExceptionForHR(hr);

                await Task.Delay(OleRetryDelay);
            }
        }

        public async Task<object?> GetDataAsync(string format)
        {
            Dispatcher.UIThread.VerifyAccess();
            var i = OleRetryCount;

            while (true)
            {
                var hr = UnmanagedMethods.OleGetClipboard(out var dataObject);

                if (hr == 0)
                {
                    using var proxy = MicroComRuntime.CreateProxyFor<Win32Com.IDataObject>(dataObject, true);
                    using var wrapper = new OleDataObject(proxy);
                    var rv = wrapper.Get(format);
                    return rv;
                }

                if (--i == 0)
                    Marshal.ThrowExceptionForHR(hr);

                await Task.Delay(OleRetryDelay);
            }
        }

        /// <summary>
        /// Permanently renders the contents of the last IDataObject that was set onto the clipboard.
        /// </summary>
        public async Task FlushAsync()
        {
            await Task.Delay(OleFlushDelay);

            // Retry OLE operations several times as mitigation for clipboard locking issues in TS sessions.

            int i = OleRetryCount;

            while (true)
            {
                var hr = UnmanagedMethods.OleFlushClipboard();

                if (hr == 0)
                {
                    break;
                }

                if (--i == 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                await Task.Delay(OleRetryDelay);
            }
        }
    }
}
