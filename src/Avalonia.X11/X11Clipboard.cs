using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using static Avalonia.X11.XLib;
namespace Avalonia.X11
{
    internal class X11Clipboard : IClipboard
    {
        private readonly X11Info _x11;
        private IDataObject? _storedDataObject;
        private IntPtr _handle;
        private TaskCompletionSource<bool>? _storeAtomTcs;
        private TaskCompletionSource<IntPtr[]?>? _requestedFormatsTcs;
        private TaskCompletionSource<object?>? _requestedDataTcs;
        private readonly IntPtr[] _textAtoms;
        private readonly IntPtr _avaloniaSaveTargetsAtom;

        public X11Clipboard(AvaloniaX11Platform platform)
        {
            _x11 = platform.Info;
            _handle = CreateEventWindow(platform, OnEvent);
            _avaloniaSaveTargetsAtom = XInternAtom(_x11.Display, "AVALONIA_SAVE_TARGETS_PROPERTY_ATOM", false);
            _textAtoms = new[]
            {
                _x11.Atoms.XA_STRING,
                _x11.Atoms.OEMTEXT,
                _x11.Atoms.UTF8_STRING,
                _x11.Atoms.UTF16_STRING
            }.Where(a => a != IntPtr.Zero).ToArray();
        }

        private bool IsStringAtom(IntPtr atom)
        {
            return _textAtoms.Contains(atom);
        }

        private Encoding? GetStringEncoding(IntPtr atom)
        {
            return (atom == _x11.Atoms.XA_STRING
                    || atom == _x11.Atoms.OEMTEXT)
                ? Encoding.ASCII
                : atom == _x11.Atoms.UTF8_STRING
                    ? Encoding.UTF8
                    : atom == _x11.Atoms.UTF16_STRING
                        ? Encoding.Unicode
                        : null;
        }
        
        private unsafe void OnEvent(ref XEvent ev)
        {
            if (ev.type == XEventName.SelectionClear)
            {   
                _storeAtomTcs?.TrySetResult(true);
                return;
            }

            if (ev.type == XEventName.SelectionRequest)
            {       
                var sel = ev.SelectionRequestEvent;
                var resp = new XEvent
                {
                    SelectionEvent =
                    {
                        type = XEventName.SelectionNotify,
                        send_event = 1,
                        display = _x11.Display,
                        selection = sel.selection,
                        target = sel.target,
                        requestor = sel.requestor,
                        time = sel.time,
                        property = IntPtr.Zero
                    }
                };
                if (sel.selection == _x11.Atoms.CLIPBOARD)
                {
                    resp.SelectionEvent.property = WriteTargetToProperty(sel.target, sel.requestor, sel.property);
                }
                
                XSendEvent(_x11.Display, sel.requestor, false, new IntPtr((int)EventMask.NoEventMask), ref resp);
            }

            IntPtr WriteTargetToProperty(IntPtr target, IntPtr window, IntPtr property)
            {
                Encoding? textEnc;
                if (target == _x11.Atoms.TARGETS)
                {
                    var atoms = ConvertDataObject(_storedDataObject);
                    XChangeProperty(_x11.Display, window, property,
                        _x11.Atoms.XA_ATOM, 32, PropertyMode.Replace, atoms, atoms.Length);
                    return property;
                }
                else if(target == _x11.Atoms.SAVE_TARGETS && _x11.Atoms.SAVE_TARGETS != IntPtr.Zero)
                {
                    return property;
                }
                else if ((textEnc = GetStringEncoding(target)) != null 
                         && _storedDataObject?.Contains(DataFormats.Text) == true)
                {
                    var text = _storedDataObject.GetText();
                    if(text == null)
                        return IntPtr.Zero;
                    var data = textEnc.GetBytes(text);
                    fixed (void* pdata = data)
                        XChangeProperty(_x11.Display, window, property, target, 8,
                            PropertyMode.Replace,
                            pdata, data.Length);
                    return property;
                }
                else if (target == _x11.Atoms.MULTIPLE && _x11.Atoms.MULTIPLE != IntPtr.Zero)
                {
                    XGetWindowProperty(_x11.Display, window, property, IntPtr.Zero, new IntPtr(0x7fffffff), false,
                        _x11.Atoms.ATOM_PAIR, out _, out var actualFormat, out var nitems, out _, out var prop);
                    if (nitems == IntPtr.Zero)
                        return IntPtr.Zero;
                    if (actualFormat == 32)
                    {
                        var data = (IntPtr*)prop.ToPointer();
                        for (var c = 0; c < nitems.ToInt32(); c += 2)
                        {
                            var subTarget = data[c];
                            var subProp = data[c + 1];
                            var converted = WriteTargetToProperty(subTarget, window, subProp);
                            data[c + 1] = converted;
                        }

                        XChangeProperty(_x11.Display, window, property, _x11.Atoms.ATOM_PAIR, 32, PropertyMode.Replace,
                            prop.ToPointer(), nitems.ToInt32());
                    }

                    XFree(prop);

                    return property;
                }
                else if(_x11.Atoms.GetAtomName(target) is { } atomName && _storedDataObject?.Contains(atomName) == true)
                {
                    var objValue = _storedDataObject.Get(atomName);
                    
                    if(!(objValue is byte[] bytes))
                    {
                        if (objValue is string s)
                            bytes = Encoding.UTF8.GetBytes(s);
                        else
                            return IntPtr.Zero;
                    }

                    XChangeProperty(_x11.Display, window, property, target, 8,
                        PropertyMode.Replace,
                        bytes, bytes.Length);
                    return property;
                }
                else
                    return IntPtr.Zero;
            }

            if (ev.type == XEventName.SelectionNotify && ev.SelectionEvent.selection == _x11.Atoms.CLIPBOARD)
            {
                var sel = ev.SelectionEvent;
                if (sel.property == IntPtr.Zero)
                {
                    _requestedFormatsTcs?.TrySetResult(null);
                    _requestedDataTcs?.TrySetResult(null);
                }
                XGetWindowProperty(_x11.Display, _handle, sel.property, IntPtr.Zero, new IntPtr (0x7fffffff), true, (IntPtr)Atom.AnyPropertyType,
                    out var actualTypeAtom, out var actualFormat, out var nitems, out var bytes_after, out var prop);
                Encoding? textEnc;
                if (nitems == IntPtr.Zero)
                {
                    _requestedFormatsTcs?.TrySetResult(null);
                    _requestedDataTcs?.TrySetResult(null);
                }
                else
                {
                    if (sel.property == _x11.Atoms.TARGETS)
                    {
                        if (actualFormat != 32)
                            _requestedFormatsTcs?.TrySetResult(null);
                        else
                        {
                            var formats = new IntPtr[nitems.ToInt32()];
                            Marshal.Copy(prop, formats, 0, formats.Length);
                            _requestedFormatsTcs?.TrySetResult(formats);
                        }
                    }
                    else if ((textEnc = GetStringEncoding(actualTypeAtom)) != null)
                    {
                        var text = textEnc.GetString((byte*)prop.ToPointer(), nitems.ToInt32());
                        _requestedDataTcs?.TrySetResult(text);
                    }
                    else
                    {
                        if (actualTypeAtom == _x11.Atoms.INCR)
                        {
                            // TODO: Actually implement that monstrosity
                            _requestedDataTcs?.TrySetResult(null);
                        }
                        else
                        {
                            var data = new byte[(int)nitems * (actualFormat / 8)];
                            Marshal.Copy(prop, data, 0, data.Length);
                            _requestedDataTcs?.TrySetResult(data);
                        }
                    }
                }

                XFree(prop);
            }
        }

        private Task<IntPtr[]?> SendFormatRequest()
        {
            if (_requestedFormatsTcs == null || _requestedFormatsTcs.Task.IsCompleted)
                _requestedFormatsTcs = new TaskCompletionSource<IntPtr[]?>();
            XConvertSelection(_x11.Display, _x11.Atoms.CLIPBOARD, _x11.Atoms.TARGETS, _x11.Atoms.TARGETS, _handle,
                IntPtr.Zero);
            return _requestedFormatsTcs.Task;
        }

        private Task<object?> SendDataRequest(IntPtr format)
        {
            if (_requestedDataTcs == null || _requestedDataTcs.Task.IsCompleted)
                _requestedDataTcs = new TaskCompletionSource<object?>();
            XConvertSelection(_x11.Display, _x11.Atoms.CLIPBOARD, format, format, _handle, IntPtr.Zero);
            return _requestedDataTcs.Task;
        }

        private bool HasOwner => XGetSelectionOwner(_x11.Display, _x11.Atoms.CLIPBOARD) != IntPtr.Zero;
        
        public async Task<string?> GetTextAsync()
        {
            if (!HasOwner)
                return null;
            var res = await SendFormatRequest();
            var target = _x11.Atoms.UTF8_STRING;
            if (res != null)
            {
                var preferredFormats = new[] {_x11.Atoms.UTF16_STRING, _x11.Atoms.UTF8_STRING, _x11.Atoms.XA_STRING};
                foreach (var pf in preferredFormats)
                    if (res.Contains(pf))
                    {
                        target = pf;
                        break;
                    }
            }

            return (string?)await SendDataRequest(target);
        }


        private IntPtr[] ConvertDataObject(IDataObject? data)
        {
            var atoms = new HashSet<IntPtr> { _x11.Atoms.TARGETS, _x11.Atoms.MULTIPLE };

            if (data is not null)
            {
                foreach (var fmt in data.GetDataFormats())
                {
                    if (fmt == DataFormats.Text)
                        foreach (var ta in _textAtoms)
                            atoms.Add(ta);
                    else
                        atoms.Add(_x11.Atoms.GetAtom(fmt));
                }
            }

            return atoms.ToArray();
        }

        private Task StoreAtomsInClipboardManager(IDataObject data)
        {
            if (_x11.Atoms.CLIPBOARD_MANAGER != IntPtr.Zero && _x11.Atoms.SAVE_TARGETS != IntPtr.Zero)
            {
                var clipboardManager = XGetSelectionOwner(_x11.Display, _x11.Atoms.CLIPBOARD_MANAGER);
                if (clipboardManager != IntPtr.Zero)
                {          
                    if (_storeAtomTcs == null || _storeAtomTcs.Task.IsCompleted)
                        _storeAtomTcs = new TaskCompletionSource<bool>();   
                           
                    var atoms = ConvertDataObject(data);
                    XChangeProperty(_x11.Display, _handle, _avaloniaSaveTargetsAtom, _x11.Atoms.XA_ATOM, 32,
                        PropertyMode.Replace,
                        atoms, atoms.Length);
                    XConvertSelection(_x11.Display, _x11.Atoms.CLIPBOARD_MANAGER, _x11.Atoms.SAVE_TARGETS,
                        _avaloniaSaveTargetsAtom, _handle, IntPtr.Zero);
                    return _storeAtomTcs.Task;
                }
            }
            return Task.CompletedTask;
        }

        public Task SetTextAsync(string? text)
        {
            var data = new DataObject();

            if (text is not null)
                data.Set(DataFormats.Text, text);

            return SetDataObjectAsync(data);
        }

        public Task ClearAsync()
        {
            return SetTextAsync(null);
        }

        public Task SetDataObjectAsync(IDataObject data)
        {
            _storedDataObject = data;
            XSetSelectionOwner(_x11.Display, _x11.Atoms.CLIPBOARD, _handle, IntPtr.Zero);
            return StoreAtomsInClipboardManager(data);
        }

        public async Task<string[]> GetFormatsAsync()
        {
            if (!HasOwner)
                return [];

            var res = await SendFormatRequest();
            if (res == null)
                return [];

            var rv = new List<string>();
            if (_textAtoms.Any(res.Contains))
                rv.Add(DataFormats.Text);

            foreach (var t in res)
            {
                if (_x11.Atoms.GetAtomName(t) is { } atomName)
                    rv.Add(atomName);
            }

            return rv.ToArray();
        }

        public async Task<object?> GetDataAsync(string format)
        {
            if (!HasOwner)
                return null;
            if (format == DataFormats.Text)
                return await GetTextAsync();

            var formatAtom = _x11.Atoms.GetAtom(format);
            var res = await SendFormatRequest();
            if (res is null || !res.Contains(formatAtom))
                return null;
            
            return await SendDataRequest(formatAtom);
        }

        /// <inheritdoc />
        public Task FlushAsync() =>
            Task.CompletedTask;
    }
}
