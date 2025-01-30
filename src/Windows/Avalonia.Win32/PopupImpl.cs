using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    internal class PopupImpl : WindowImpl, IPopupImpl
    {
        private readonly IWindowBaseImpl? _parent;
        private bool _dropShadowHint = true;
        private Size? _maxAutoSize;


        // This is needed because we are calling virtual methods from constructors
        // One fabulous design decision leads to another, I guess
        [ThreadStatic]
        private static IntPtr s_parentHandle;

        public override void Show(bool activate, bool isDialog)
        {
            // Popups are always shown non-activated.
            UnmanagedMethods.ShowWindow(Handle.Handle, UnmanagedMethods.ShowWindowCommand.ShowNoActivate);

        }

        protected override bool ShouldTakeFocusOnClick => false;

        public override Size MaxAutoSizeHint
        {
            get
            {
                if (_maxAutoSize is null)
                {
                    var screen = base.Screen.ScreenFromHwnd(Hwnd, UnmanagedMethods.MONITOR.MONITOR_DEFAULTTONEAREST);
                    
                    if (screen is not null)
                    {
                        _maxAutoSize = screen.WorkingArea.ToRect(RenderScaling).Size;
                    }
                }

                return _maxAutoSize ?? Size.Infinity;
            }
        }

        protected override IntPtr CreateWindowOverride(ushort atom)
        {
            UnmanagedMethods.WindowStyles style =
                UnmanagedMethods.WindowStyles.WS_POPUP |
                UnmanagedMethods.WindowStyles.WS_CLIPSIBLINGS |
                UnmanagedMethods.WindowStyles.WS_CLIPCHILDREN;

            UnmanagedMethods.WindowStyles exStyle =
                UnmanagedMethods.WindowStyles.WS_EX_TOOLWINDOW;

            var result = UnmanagedMethods.CreateWindowEx(
                (int)exStyle,
                atom,
                null,
                (uint)style,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                s_parentHandle,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
            s_parentHandle = IntPtr.Zero;

            EnableBoxShadow(result, _dropShadowHint);

            return result;
        }

        protected override IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch ((UnmanagedMethods.WindowsMessage)msg)
            {
                case UnmanagedMethods.WindowsMessage.WM_DISPLAYCHANGE:
                    _maxAutoSize = null;
                    goto default;
                case UnmanagedMethods.WindowsMessage.WM_MOUSEACTIVATE:
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        _parent?.Activate();
                    }, DispatcherPriority.Background);
                    return (IntPtr)UnmanagedMethods.MouseActivate.MA_NOACTIVATE;
                default:
                    return base.WndProc(hWnd, msg, wParam, lParam);
            }
        }

        // This is needed because we are calling virtual methods from constructors
        // One fabulous design decision leads to another, I guess
        private static IWindowBaseImpl SaveParentHandle(IWindowBaseImpl parent)
        {
            s_parentHandle = parent.Handle!.Handle;
            return parent;
        }

        // This is needed because we are calling virtual methods from constructors
        // One fabulous design decision leads to another, I guess
        public PopupImpl(IWindowBaseImpl parent) : this(SaveParentHandle(parent), false)
        {

        }

        private PopupImpl(IWindowBaseImpl parent, bool dummy)
        {
            _windowProperties = new WindowProperties
            {
                ShowInTaskbar = false,
                IsResizable = false,
                Decorations = SystemDecorations.None,
            };

            _parent = parent;
            PopupPositioner = new ManagedPopupPositioner(new ManagedPopupPositionerPopupImplHelper(parent, MoveResize));
        }

        private void MoveResize(PixelPoint position, Size size, double scaling)
        {
            Move(position);
            Resize(size, WindowResizeReason.Layout);
            //TODO: We ignore the scaling override for now
        }

        private static void EnableBoxShadow (IntPtr hwnd, bool enabled)
        {
            var classes = (int)UnmanagedMethods.GetClassLongPtr(hwnd, (int)UnmanagedMethods.ClassLongIndex.GCL_STYLE);

            if (enabled)
            {
                classes |= (int)UnmanagedMethods.ClassStyles.CS_DROPSHADOW;
            }
            else
            {
                classes &= ~(int)UnmanagedMethods.ClassStyles.CS_DROPSHADOW;
            }

            UnmanagedMethods.SetClassLong(hwnd, UnmanagedMethods.ClassLongIndex.GCL_STYLE, new IntPtr(classes));
        }

        public void SetWindowManagerAddShadowHint(bool enabled)
        {
            _dropShadowHint = enabled;

            EnableBoxShadow(Handle.Handle, enabled);
        }

        public void TakeFocus()
        {
            var parent = _parent;

            while (parent != null)
            {
                if (parent is PopupImpl pi)
                    parent = pi._parent;
                else
                    break;
            }

            if (parent == null)
                return;

            var focusOwner = UnmanagedMethods.GetFocus();
            if (focusOwner != IntPtr.Zero &&
                UnmanagedMethods.GetAncestor(focusOwner, UnmanagedMethods.GetAncestorFlags.GA_ROOT)
                == parent.Handle?.Handle)
                UnmanagedMethods.SetFocus(parent.Handle.Handle);
        }

        public IPopupPositioner PopupPositioner { get; }
    }
}
