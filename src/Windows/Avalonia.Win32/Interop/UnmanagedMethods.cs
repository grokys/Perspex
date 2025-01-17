#nullable enable

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;
using MicroCom.Runtime;

// ReSharper disable InconsistentNaming
#pragma warning disable 169, 649

namespace Avalonia.Win32.Interop
{
    [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Using Win32 naming for consistency.")]
    [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1307:AccessibleFieldsMustBeginWithUpperCaseLetter", Justification = "Using Win32 naming for consistency.")]
    [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "Using Win32 naming for consistency.")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements must be documented", Justification = "Look in Win32 docs.")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items must be documented", Justification = "Look in Win32 docs.")]
    internal unsafe static class UnmanagedMethods
    {
        public const int CW_USEDEFAULT = unchecked((int)0x80000000);

        public delegate void TimerProc(IntPtr hWnd, uint uMsg, IntPtr nIDEvent, uint dwTime);

        public delegate void TimeCallback(uint uTimerID, uint uMsg, UIntPtr dwUser, UIntPtr dw1, UIntPtr dw2);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void WaitOrTimerCallback(IntPtr lpParameter, bool timerOrWaitFired);

        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public static readonly IntPtr DPI_AWARENESS_CONTEXT_UNAWARE = new IntPtr(-1);
        public static readonly IntPtr DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = new IntPtr(-2);
        public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE = new IntPtr(-3);
        public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        public enum Cursor
        {
            IDC_ARROW = 32512,
            IDC_IBEAM = 32513,
            IDC_WAIT = 32514,
            IDC_CROSS = 32515,
            IDC_UPARROW = 32516,
            IDC_SIZE = 32640,
            IDC_ICON = 32641,
            IDC_SIZENWSE = 32642,
            IDC_SIZENESW = 32643,
            IDC_SIZEWE = 32644,
            IDC_SIZENS = 32645,
            IDC_SIZEALL = 32646,
            IDC_NO = 32648,
            IDC_HAND = 32649,
            IDC_APPSTARTING = 32650,
            IDC_HELP = 32651
        }

        public enum MouseActivate : int
        {
            MA_ACTIVATE = 1,
            MA_ACTIVATEANDEAT = 2,
            MA_NOACTIVATE = 3,
            MA_NOACTIVATEANDEAT = 4
        }

        [Flags]
        public enum SetWindowPosFlags : uint
        {
            SWP_ASYNCWINDOWPOS = 0x4000,
            SWP_DEFERERASE = 0x2000,
            SWP_DRAWFRAME = 0x0020,
            SWP_FRAMECHANGED = 0x0020,
            SWP_HIDEWINDOW = 0x0080,
            SWP_NOACTIVATE = 0x0010,
            SWP_NOCOPYBITS = 0x0100,
            SWP_NOMOVE = 0x0002,
            SWP_NOOWNERZORDER = 0x0200,
            SWP_NOREDRAW = 0x0008,
            SWP_NOREPOSITION = 0x0200,
            SWP_NOSENDCHANGING = 0x0400,
            SWP_NOSIZE = 0x0001,
            SWP_NOZORDER = 0x0004,
            SWP_SHOWWINDOW = 0x0040,

            SWP_RESIZE = SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOZORDER
        }

        public static class WindowPosZOrder
        {
            public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
            public static readonly IntPtr HWND_TOP = new IntPtr(0);
            public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
            public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        }

        public enum SizeCommand
        {
            Restored,
            Minimized,
            Maximized,
            MaxShow,
            MaxHide,
        }

        public enum ShowWindowCommand
        {
            /// <summary>
            /// Hides the window and activates another window.
            /// </summary>
            Hide = 0,
            /// <summary>
            /// Activates and displays a window. If the window is minimized, maximized, or arranged, the system restores it to its original
            /// size and position. An application should specify this flag when displaying the window for the first time.
            /// </summary>
            Normal = 1,
            /// <summary>
            /// Activates the window and displays it as a minimized window.
            /// </summary>
            ShowMinimized = 2,
            /// <summary>
            /// Activates the window and displays it as a maximized window.
            /// </summary>
            Maximize = 3,
            /// <inheritdoc cref="Maximize"/>
            ShowMaximized = Maximize,
            /// <summary>
            /// Displays a window in its most recent size and position. This value is similar to <see cref="Normal"/>, except that the window is not activated.
            /// </summary>
            ShowNoActivate = 4,
            /// <summary>
            /// Activates the window and displays it in its current size and position.
            /// </summary>
            Show = 5,
            /// <summary>
            /// Minimizes the specified window and activates the next top-level window in the Z order.
            /// </summary>
            Minimize = 6,
            /// <summary>
            /// Displays the window as a minimized window. This value is similar to <see cref="ShowMinimized"/>, except the window is not activated.
            /// </summary>
            ShowMinNoActive = 7,
            /// <summary>
            /// Displays the window in its current size and position. This value is similar to <see cref="Show"/>, except that the window is not activated.
            /// </summary>
            ShowNA = 8,
            /// <summary>
            /// Activates and displays the window. If the window is minimized, maximized, or arranged, the system restores it to its original size and position.
            /// An application should specify this flag when restoring a minimized window.
            /// </summary>
            Restore = 9,
            /// <summary>
            /// Sets the show state based on the <see cref="ShowWindowCommand"/> value specified in the STARTUPINFO structure passed to the CreateProcess function
            /// by the program that started the application.
            /// </summary>
            ShowDefault = 10,
            /// <summary>
            /// Minimizes a window, even if the thread that owns the window is not responding. This flag should only be used when minimizing windows from a different thread.
            /// </summary>
            ForceMinimize = 11
        }

        public enum SystemMetric
        {
            SM_CXSCREEN = 0,  // 0x00
            SM_CYSCREEN = 1,  // 0x01
            SM_CXVSCROLL = 2,  // 0x02
            SM_CYHSCROLL = 3,  // 0x03
            SM_CYCAPTION = 4,  // 0x04
            SM_CXBORDER = 5,  // 0x05
            SM_CYBORDER = 6,  // 0x06
            SM_CXDLGFRAME = 7,  // 0x07
            SM_CXFIXEDFRAME = 7,  // 0x07
            SM_CYDLGFRAME = 8,  // 0x08
            SM_CYFIXEDFRAME = 8,  // 0x08
            SM_CYVTHUMB = 9,  // 0x09
            SM_CXHTHUMB = 10, // 0x0A
            SM_CXICON = 11, // 0x0B
            SM_CYICON = 12, // 0x0C
            SM_CXCURSOR = 13, // 0x0D
            SM_CYCURSOR = 14, // 0x0E
            SM_CYMENU = 15, // 0x0F
            SM_CXFULLSCREEN = 16, // 0x10
            SM_CYFULLSCREEN = 17, // 0x11
            SM_CYKANJIWINDOW = 18, // 0x12
            SM_MOUSEPRESENT = 19, // 0x13
            SM_CYVSCROLL = 20, // 0x14
            SM_CXHSCROLL = 21, // 0x15
            SM_DEBUG = 22, // 0x16
            SM_SWAPBUTTON = 23, // 0x17
            SM_CXMIN = 28, // 0x1C
            SM_CYMIN = 29, // 0x1D
            SM_CXSIZE = 30, // 0x1E
            SM_CYSIZE = 31, // 0x1F
            SM_CXSIZEFRAME = 32, // 0x20
            SM_CXFRAME = 32, // 0x20
            SM_CYSIZEFRAME = 33, // 0x21
            SM_CYFRAME = 33, // 0x21
            SM_CXMINTRACK = 34, // 0x22
            SM_CYMINTRACK = 35, // 0x23
            SM_CXDOUBLECLK = 36, // 0x24
            SM_CYDOUBLECLK = 37, // 0x25
            SM_CXICONSPACING = 38, // 0x26
            SM_CYICONSPACING = 39, // 0x27
            SM_MENUDROPALIGNMENT = 40, // 0x28
            SM_PENWINDOWS = 41, // 0x29
            SM_DBCSENABLED = 42, // 0x2A
            SM_CMOUSEBUTTONS = 43, // 0x2B
            SM_SECURE = 44, // 0x2C
            SM_CXEDGE = 45, // 0x2D
            SM_CYEDGE = 46, // 0x2E
            SM_CXMINSPACING = 47, // 0x2F
            SM_CYMINSPACING = 48, // 0x30
            SM_CXSMICON = 49, // 0x31
            SM_CYSMICON = 50, // 0x32
            SM_CYSMCAPTION = 51, // 0x33
            SM_CXSMSIZE = 52, // 0x34
            SM_CYSMSIZE = 53, // 0x35
            SM_CXMENUSIZE = 54, // 0x36
            SM_CYMENUSIZE = 55, // 0x37
            SM_ARRANGE = 56, // 0x38
            SM_CXMINIMIZED = 57, // 0x39
            SM_CYMINIMIZED = 58, // 0x3A
            SM_CXMAXTRACK = 59, // 0x3B
            SM_CYMAXTRACK = 60, // 0x3C
            SM_CXMAXIMIZED = 61, // 0x3D
            SM_CYMAXIMIZED = 62, // 0x3E
            SM_NETWORK = 63, // 0x3F
            SM_CLEANBOOT = 67, // 0x43
            SM_CXDRAG = 68, // 0x44
            SM_CYDRAG = 69, // 0x45
            SM_SHOWSOUNDS = 70, // 0x46
            SM_CXMENUCHECK = 71, // 0x47
            SM_CYMENUCHECK = 72, // 0x48
            SM_SLOWMACHINE = 73, // 0x49
            SM_MIDEASTENABLED = 74, // 0x4A
            SM_MOUSEWHEELPRESENT = 75, // 0x4B
            SM_XVIRTUALSCREEN = 76, // 0x4C
            SM_YVIRTUALSCREEN = 77, // 0x4D
            SM_CXVIRTUALSCREEN = 78, // 0x4E
            SM_CYVIRTUALSCREEN = 79, // 0x4F
            SM_CMONITORS = 80, // 0x50
            SM_SAMEDISPLAYFORMAT = 81, // 0x51
            SM_IMMENABLED = 82, // 0x52
            SM_CXFOCUSBORDER = 83, // 0x53
            SM_CYFOCUSBORDER = 84, // 0x54
            SM_TABLETPC = 86, // 0x56
            SM_MEDIACENTER = 87, // 0x57
            SM_STARTER = 88, // 0x58
            SM_SERVERR2 = 89, // 0x59
            SM_MOUSEHORIZONTALWHEELPRESENT = 91, // 0x5B
            SM_CXPADDEDBORDER = 92, // 0x5C
            SM_DIGITIZER = 94, // 0x5E
            SM_MAXIMUMTOUCHES = 95, // 0x5F

            SM_REMOTESESSION = 0x1000, // 0x1000
            SM_SHUTTINGDOWN = 0x2000, // 0x2000
            SM_REMOTECONTROL = 0x2001, // 0x2001

            SM_CONVERTABLESLATEMODE = 0x2003,
            SM_SYSTEMDOCKED = 0x2004,
        }

        [Flags]
        public enum ModifierKeys
        {
            MK_NONE    = 0x0000,

            MK_LBUTTON = 0x0001,
            MK_RBUTTON = 0x0002,

            MK_SHIFT   = 0x0004,
            MK_CONTROL = 0x0008,

            MK_MBUTTON  = 0x0010,
            MK_ALT      = 0x0020,
            MK_XBUTTON1 = 0x0020,
            MK_XBUTTON2 = 0x0040
        }

        public enum VirtualKeyStates : int
        {
            VK_LBUTTON = 0x01,
            VK_RBUTTON = 0x02,
            VK_CANCEL = 0x03,
            VK_MBUTTON = 0x04,
            VK_XBUTTON1 = 0x05,
            VK_XBUTTON2 = 0x06,
            VK_BACK = 0x08,
            VK_TAB = 0x09,
            VK_CLEAR = 0x0C,
            VK_RETURN = 0x0D,
            VK_SHIFT = 0x10,
            VK_CONTROL = 0x11,
            VK_MENU = 0x12,
            VK_PAUSE = 0x13,
            VK_CAPITAL = 0x14,
            VK_KANA = 0x15,
            VK_HANGEUL = 0x15,
            VK_HANGUL = 0x15,
            VK_JUNJA = 0x17,
            VK_FINAL = 0x18,
            VK_HANJA = 0x19,
            VK_KANJI = 0x19,
            VK_ESCAPE = 0x1B,
            VK_CONVERT = 0x1C,
            VK_NONCONVERT = 0x1D,
            VK_ACCEPT = 0x1E,
            VK_MODECHANGE = 0x1F,
            VK_SPACE = 0x20,
            VK_PRIOR = 0x21,
            VK_NEXT = 0x22,
            VK_END = 0x23,
            VK_HOME = 0x24,
            VK_LEFT = 0x25,
            VK_UP = 0x26,
            VK_RIGHT = 0x27,
            VK_DOWN = 0x28,
            VK_SELECT = 0x29,
            VK_PRINT = 0x2A,
            VK_EXECUTE = 0x2B,
            VK_SNAPSHOT = 0x2C,
            VK_INSERT = 0x2D,
            VK_DELETE = 0x2E,
            VK_HELP = 0x2F,
            VK_LWIN = 0x5B,
            VK_RWIN = 0x5C,
            VK_APPS = 0x5D,
            VK_SLEEP = 0x5F,
            VK_NUMPAD0 = 0x60,
            VK_NUMPAD1 = 0x61,
            VK_NUMPAD2 = 0x62,
            VK_NUMPAD3 = 0x63,
            VK_NUMPAD4 = 0x64,
            VK_NUMPAD5 = 0x65,
            VK_NUMPAD6 = 0x66,
            VK_NUMPAD7 = 0x67,
            VK_NUMPAD8 = 0x68,
            VK_NUMPAD9 = 0x69,
            VK_MULTIPLY = 0x6A,
            VK_ADD = 0x6B,
            VK_SEPARATOR = 0x6C,
            VK_SUBTRACT = 0x6D,
            VK_DECIMAL = 0x6E,
            VK_DIVIDE = 0x6F,
            VK_F1 = 0x70,
            VK_F2 = 0x71,
            VK_F3 = 0x72,
            VK_F4 = 0x73,
            VK_F5 = 0x74,
            VK_F6 = 0x75,
            VK_F7 = 0x76,
            VK_F8 = 0x77,
            VK_F9 = 0x78,
            VK_F10 = 0x79,
            VK_F11 = 0x7A,
            VK_F12 = 0x7B,
            VK_F13 = 0x7C,
            VK_F14 = 0x7D,
            VK_F15 = 0x7E,
            VK_F16 = 0x7F,
            VK_F17 = 0x80,
            VK_F18 = 0x81,
            VK_F19 = 0x82,
            VK_F20 = 0x83,
            VK_F21 = 0x84,
            VK_F22 = 0x85,
            VK_F23 = 0x86,
            VK_F24 = 0x87,
            VK_NUMLOCK = 0x90,
            VK_SCROLL = 0x91,
            VK_OEM_NEC_EQUAL = 0x92,
            VK_OEM_FJ_JISHO = 0x92,
            VK_OEM_FJ_MASSHOU = 0x93,
            VK_OEM_FJ_TOUROKU = 0x94,
            VK_OEM_FJ_LOYA = 0x95,
            VK_OEM_FJ_ROYA = 0x96,
            VK_LSHIFT = 0xA0,
            VK_RSHIFT = 0xA1,
            VK_LCONTROL = 0xA2,
            VK_RCONTROL = 0xA3,
            VK_LMENU = 0xA4,
            VK_RMENU = 0xA5,
            VK_BROWSER_BACK = 0xA6,
            VK_BROWSER_FORWARD = 0xA7,
            VK_BROWSER_REFRESH = 0xA8,
            VK_BROWSER_STOP = 0xA9,
            VK_BROWSER_SEARCH = 0xAA,
            VK_BROWSER_FAVORITES = 0xAB,
            VK_BROWSER_HOME = 0xAC,
            VK_VOLUME_MUTE = 0xAD,
            VK_VOLUME_DOWN = 0xAE,
            VK_VOLUME_UP = 0xAF,
            VK_MEDIA_NEXT_TRACK = 0xB0,
            VK_MEDIA_PREV_TRACK = 0xB1,
            VK_MEDIA_STOP = 0xB2,
            VK_MEDIA_PLAY_PAUSE = 0xB3,
            VK_LAUNCH_MAIL = 0xB4,
            VK_LAUNCH_MEDIA_SELECT = 0xB5,
            VK_LAUNCH_APP1 = 0xB6,
            VK_LAUNCH_APP2 = 0xB7,
            VK_OEM_1 = 0xBA,
            VK_OEM_PLUS = 0xBB,
            VK_OEM_COMMA = 0xBC,
            VK_OEM_MINUS = 0xBD,
            VK_OEM_PERIOD = 0xBE,
            VK_OEM_2 = 0xBF,
            VK_OEM_3 = 0xC0,
            VK_ABNT_C1 = 0xC1,
            VK_ABNT_C2 = 0xC2,
            VK_OEM_4 = 0xDB,
            VK_OEM_5 = 0xDC,
            VK_OEM_6 = 0xDD,
            VK_OEM_7 = 0xDE,
            VK_OEM_8 = 0xDF,
            VK_OEM_AX = 0xE1,
            VK_OEM_102 = 0xE2,
            VK_ICO_HELP = 0xE3,
            VK_ICO_00 = 0xE4,
            VK_PROCESSKEY = 0xE5,
            VK_ICO_CLEAR = 0xE6,
            VK_PACKET = 0xE7,
            VK_OEM_RESET = 0xE9,
            VK_OEM_JUMP = 0xEA,
            VK_OEM_PA1 = 0xEB,
            VK_OEM_PA2 = 0xEC,
            VK_OEM_PA3 = 0xED,
            VK_OEM_WSCTRL = 0xEE,
            VK_OEM_CUSEL = 0xEF,
            VK_OEM_ATTN = 0xF0,
            VK_OEM_FINISH = 0xF1,
            VK_OEM_COPY = 0xF2,
            VK_OEM_AUTO = 0xF3,
            VK_OEM_ENLW = 0xF4,
            VK_OEM_BACKTAB = 0xF5,
            VK_ATTN = 0xF6,
            VK_CRSEL = 0xF7,
            VK_EXSEL = 0xF8,
            VK_EREOF = 0xF9,
            VK_PLAY = 0xFA,
            VK_ZOOM = 0xFB,
            VK_NONAME = 0xFC,
            VK_PA1 = 0xFD,
            VK_OEM_CLEAR = 0xFE
        }

        public enum WindowActivate
        {
            WA_INACTIVE,
            WA_ACTIVE,
            WA_CLICKACTIVE,
        }

        public enum HitTestValues
        {
            HTERROR = -2,
            HTTRANSPARENT = -1,
            HTNOWHERE = 0,
            HTCLIENT = 1,
            HTCAPTION = 2,
            HTSYSMENU = 3,
            HTGROWBOX = 4,
            HTMENU = 5,
            HTHSCROLL = 6,
            HTVSCROLL = 7,
            HTMINBUTTON = 8,
            HTMAXBUTTON = 9,
            HTLEFT = 10,
            HTRIGHT = 11,
            HTTOP = 12,
            HTTOPLEFT = 13,
            HTTOPRIGHT = 14,
            HTBOTTOM = 15,
            HTBOTTOMLEFT = 16,
            HTBOTTOMRIGHT = 17,
            HTBORDER = 18,
            HTOBJECT = 19,
            HTCLOSE = 20,
            HTHELP = 21
        }

        [Flags]
        public enum WindowStyles : uint
        {
            WS_BORDER = 0x800000,
            WS_CAPTION = 0xc00000,
            WS_CHILD = 0x40000000,
            WS_CLIPCHILDREN = 0x2000000,
            WS_CLIPSIBLINGS = 0x4000000,
            WS_DISABLED = 0x8000000,
            WS_DLGFRAME = 0x400000,
            WS_GROUP = 0x20000,
            WS_HSCROLL = 0x100000,
            WS_MAXIMIZE = 0x1000000,
            WS_MAXIMIZEBOX = 0x10000,
            WS_MINIMIZE = 0x20000000,
            WS_MINIMIZEBOX = 0x20000,
            WS_OVERLAPPED = 0x0,
            WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
            WS_POPUP = 0x80000000u,
            WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
            WS_SYSMENU = 0x80000,
            WS_TABSTOP = 0x10000,
            WS_THICKFRAME = 0x40000,
            WS_VISIBLE = 0x10000000,
            WS_VSCROLL = 0x200000,
            WS_EX_DLGMODALFRAME = 0x00000001,
            WS_EX_NOPARENTNOTIFY = 0x00000004,
            WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
            WS_EX_TOPMOST = 0x00000008,
            WS_EX_ACCEPTFILES = 0x00000010,
            WS_EX_TRANSPARENT = 0x00000020,
            WS_EX_MDICHILD = 0x00000040,
            WS_EX_TOOLWINDOW = 0x00000080,
            WS_EX_WINDOWEDGE = 0x00000100,
            WS_EX_CLIENTEDGE = 0x00000200,
            WS_EX_CONTEXTHELP = 0x00000400,
            WS_EX_RIGHT = 0x00001000,
            WS_EX_LEFT = 0x00000000,
            WS_EX_RTLREADING = 0x00002000,
            WS_EX_LTRREADING = 0x00000000,
            WS_EX_LEFTSCROLLBAR = 0x00004000,
            WS_EX_RIGHTSCROLLBAR = 0x00000000,
            WS_EX_CONTROLPARENT = 0x00010000,
            WS_EX_STATICEDGE = 0x00020000,
            WS_EX_APPWINDOW = 0x00040000,
            WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,
            WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
            WS_EX_LAYERED = 0x00080000,
            WS_EX_NOINHERITLAYOUT = 0x00100000,
            WS_EX_LAYOUTRTL = 0x00400000,
            WS_EX_COMPOSITED = 0x02000000,
            WS_EX_NOACTIVATE = 0x08000000
        }

        [Flags]
        public enum ClassStyles : uint
        {
            CS_VREDRAW = 0x0001,
            CS_HREDRAW = 0x0002,
            CS_DBLCLKS = 0x0008,
            CS_OWNDC = 0x0020,
            CS_CLASSDC = 0x0040,
            CS_PARENTDC = 0x0080,
            CS_NOCLOSE = 0x0200,
            CS_SAVEBITS = 0x0800,
            CS_BYTEALIGNCLIENT = 0x1000,
            CS_BYTEALIGNWINDOW = 0x2000,
            CS_GLOBALCLASS = 0x4000,
            CS_IME = 0x00010000,
            CS_DROPSHADOW = 0x00020000
        }

        [Flags]
        public enum PointerDeviceChangeFlags
        {
            PDC_ARRIVAL = 0x001,
            PDC_REMOVAL = 0x002,
            PDC_ORIENTATION_0 = 0x004,
            PDC_ORIENTATION_90 = 0x008,
            PDC_ORIENTATION_180 = 0x010,
            PDC_ORIENTATION_270 = 0x020,
            PDC_MODE_DEFAULT = 0x040,
            PDC_MODE_CENTERED = 0x080,
            PDC_MAPPING_CHANGE = 0x100,
            PDC_RESOLUTION = 0x200,
            PDC_ORIGIN = 0x400,
            PDC_MODE_ASPECTRATIOPRESERVED = 0x800
        }

        public enum PointerInputType
        {
            PT_NONE = 0x00000000,
            PT_POINTER = 0x00000001,
            PT_TOUCH = 0x00000002,
            PT_PEN = 0x00000003,
            PT_MOUSE = 0x00000004,
            PT_TOUCHPAD = 0x00000005
        }

        public enum WindowsMessage : uint
        {
            WM_NULL = 0x0000,
            WM_CREATE = 0x0001,
            WM_DESTROY = 0x0002,
            WM_MOVE = 0x0003,
            WM_SIZE = 0x0005,
            WM_ACTIVATE = 0x0006,
            WM_SETFOCUS = 0x0007,
            WM_KILLFOCUS = 0x0008,
            WM_ENABLE = 0x000A,
            WM_SETREDRAW = 0x000B,
            WM_SETTEXT = 0x000C,
            WM_GETTEXT = 0x000D,
            WM_GETTEXTLENGTH = 0x000E,
            WM_PAINT = 0x000F,
            WM_CLOSE = 0x0010,
            WM_QUERYENDSESSION = 0x0011,
            WM_QUERYOPEN = 0x0013,
            WM_ENDSESSION = 0x0016,
            WM_QUIT = 0x0012,
            WM_ERASEBKGND = 0x0014,
            WM_SYSCOLORCHANGE = 0x0015,
            WM_SHOWWINDOW = 0x0018,
            WM_WININICHANGE = 0x001A,
            WM_SETTINGCHANGE = WM_WININICHANGE,
            WM_DEVMODECHANGE = 0x001B,
            WM_ACTIVATEAPP = 0x001C,
            WM_FONTCHANGE = 0x001D,
            WM_TIMECHANGE = 0x001E,
            WM_CANCELMODE = 0x001F,
            WM_SETCURSOR = 0x0020,
            WM_MOUSEACTIVATE = 0x0021,
            WM_CHILDACTIVATE = 0x0022,
            WM_QUEUESYNC = 0x0023,
            WM_GETMINMAXINFO = 0x0024,
            WM_PAINTICON = 0x0026,
            WM_ICONERASEBKGND = 0x0027,
            WM_NEXTDLGCTL = 0x0028,
            WM_SPOOLERSTATUS = 0x002A,
            WM_DRAWITEM = 0x002B,
            WM_MEASUREITEM = 0x002C,
            WM_DELETEITEM = 0x002D,
            WM_VKEYTOITEM = 0x002E,
            WM_CHARTOITEM = 0x002F,
            WM_SETFONT = 0x0030,
            WM_GETFONT = 0x0031,
            WM_SETHOTKEY = 0x0032,
            WM_GETHOTKEY = 0x0033,
            WM_QUERYDRAGICON = 0x0037,
            WM_COMPAREITEM = 0x0039,
            WM_GETOBJECT = 0x003D,
            WM_COMPACTING = 0x0041,
            WM_WINDOWPOSCHANGING = 0x0046,
            WM_WINDOWPOSCHANGED = 0x0047,
            WM_COPYDATA = 0x004A,
            WM_CANCELJOURNAL = 0x004B,
            WM_NOTIFY = 0x004E,
            WM_INPUTLANGCHANGEREQUEST = 0x0050,
            WM_INPUTLANGCHANGE = 0x0051,
            WM_TCARD = 0x0052,
            WM_HELP = 0x0053,
            WM_USERCHANGED = 0x0054,
            WM_NOTIFYFORMAT = 0x0055,
            WM_CONTEXTMENU = 0x007B,
            WM_STYLECHANGING = 0x007C,
            WM_STYLECHANGED = 0x007D,
            WM_DISPLAYCHANGE = 0x007E,
            WM_GETICON = 0x007F,
            WM_SETICON = 0x0080,
            WM_NCCREATE = 0x0081,
            WM_NCDESTROY = 0x0082,
            WM_NCCALCSIZE = 0x0083,
            WM_NCHITTEST = 0x0084,
            WM_NCPAINT = 0x0085,
            WM_NCACTIVATE = 0x0086,
            WM_GETDLGCODE = 0x0087,
            WM_SYNCPAINT = 0x0088,
            WM_NCMOUSEMOVE = 0x00A0,
            WM_NCLBUTTONDOWN = 0x00A1,
            WM_NCLBUTTONUP = 0x00A2,
            WM_NCLBUTTONDBLCLK = 0x00A3,
            WM_NCRBUTTONDOWN = 0x00A4,
            WM_NCRBUTTONUP = 0x00A5,
            WM_NCRBUTTONDBLCLK = 0x00A6,
            WM_NCMBUTTONDOWN = 0x00A7,
            WM_NCMBUTTONUP = 0x00A8,
            WM_NCMBUTTONDBLCLK = 0x00A9,
            WM_NCXBUTTONDOWN = 0x00AB,
            WM_NCXBUTTONUP = 0x00AC,
            WM_NCXBUTTONDBLCLK = 0x00AD,
            WM_INPUT_DEVICE_CHANGE = 0x00FE,
            WM_INPUT = 0x00FF,
            WM_KEYFIRST = 0x0100,
            WM_KEYDOWN = 0x0100,
            WM_KEYUP = 0x0101,
            WM_CHAR = 0x0102,
            WM_DEADCHAR = 0x0103,
            WM_SYSKEYDOWN = 0x0104,
            WM_SYSKEYUP = 0x0105,
            WM_SYSCHAR = 0x0106,
            WM_SYSDEADCHAR = 0x0107,
            WM_UNICHAR = 0x0109,
            WM_KEYLAST = 0x0109,
            WM_IME_STARTCOMPOSITION = 0x010D,
            WM_IME_ENDCOMPOSITION = 0x010E,
            WM_IME_COMPOSITION = 0x010F,
            WM_IME_KEYLAST = 0x010F,
            WM_INITDIALOG = 0x0110,
            WM_COMMAND = 0x0111,
            WM_SYSCOMMAND = 0x0112,
            WM_TIMER = 0x0113,
            WM_HSCROLL = 0x0114,
            WM_VSCROLL = 0x0115,
            WM_INITMENU = 0x0116,
            WM_INITMENUPOPUP = 0x0117,
            WM_MENUSELECT = 0x011F,
            WM_MENUCHAR = 0x0120,
            WM_ENTERIDLE = 0x0121,
            WM_MENURBUTTONUP = 0x0122,
            WM_MENUDRAG = 0x0123,
            WM_MENUGETOBJECT = 0x0124,
            WM_UNINITMENUPOPUP = 0x0125,
            WM_MENUCOMMAND = 0x0126,
            WM_CHANGEUISTATE = 0x0127,
            WM_UPDATEUISTATE = 0x0128,
            WM_QUERYUISTATE = 0x0129,
            WM_CTLCOLORMSGBOX = 0x0132,
            WM_CTLCOLOREDIT = 0x0133,
            WM_CTLCOLORLISTBOX = 0x0134,
            WM_CTLCOLORBTN = 0x0135,
            WM_CTLCOLORDLG = 0x0136,
            WM_CTLCOLORSCROLLBAR = 0x0137,
            WM_CTLCOLORSTATIC = 0x0138,
            WM_MOUSEFIRST = 0x0200,
            WM_MOUSEMOVE = 0x0200,
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_LBUTTONDBLCLK = 0x0203,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205,
            WM_RBUTTONDBLCLK = 0x0206,
            WM_MBUTTONDOWN = 0x0207,
            WM_MBUTTONUP = 0x0208,
            WM_MBUTTONDBLCLK = 0x0209,
            WM_MOUSEWHEEL = 0x020A,
            WM_XBUTTONDOWN = 0x020B,
            WM_XBUTTONUP = 0x020C,
            WM_XBUTTONDBLCLK = 0x020D,
            WM_MOUSEHWHEEL = 0x020E,
            WM_MOUSELAST = 0x020E,
            WM_PARENTNOTIFY = 0x0210,
            WM_ENTERMENULOOP = 0x0211,
            WM_EXITMENULOOP = 0x0212,
            WM_NEXTMENU = 0x0213,
            WM_SIZING = 0x0214,
            WM_CAPTURECHANGED = 0x0215,
            WM_MOVING = 0x0216,
            WM_POWERBROADCAST = 0x0218,
            WM_DEVICECHANGE = 0x0219,
            WM_MDICREATE = 0x0220,
            WM_MDIDESTROY = 0x0221,
            WM_MDIACTIVATE = 0x0222,
            WM_MDIRESTORE = 0x0223,
            WM_MDINEXT = 0x0224,
            WM_MDIMAXIMIZE = 0x0225,
            WM_MDITILE = 0x0226,
            WM_MDICASCADE = 0x0227,
            WM_MDIICONARRANGE = 0x0228,
            WM_MDIGETACTIVE = 0x0229,
            WM_MDISETMENU = 0x0230,
            WM_ENTERSIZEMOVE = 0x0231,
            WM_EXITSIZEMOVE = 0x0232,
            WM_DROPFILES = 0x0233,
            WM_MDIREFRESHMENU = 0x0234,

            WM_POINTERDEVICECHANGE = 0x0238,
            WM_POINTERDEVICEINRANGE = 0x239,
            WM_POINTERDEVICEOUTOFRANGE = 0x23A,
            WM_NCPOINTERUPDATE = 0x0241,
            WM_NCPOINTERDOWN = 0x0242,
            WM_NCPOINTERUP = 0x0243,
            WM_POINTERUPDATE = 0x0245,
            WM_POINTERDOWN = 0x0246,
            WM_POINTERUP = 0x0247,
            WM_POINTERENTER = 0x0249,
            WM_POINTERLEAVE = 0x024A,
            WM_POINTERACTIVATE = 0x024B,
            WM_POINTERCAPTURECHANGED = 0x024C,
            WM_TOUCHHITTESTING = 0x024D,
            WM_POINTERWHEEL = 0x024E,
            WM_POINTERHWHEEL = 0x024F,
            DM_POINTERHITTEST = 0x0250,

            WM_IME_SETCONTEXT = 0x0281,
            WM_IME_NOTIFY = 0x0282,
            WM_IME_CONTROL = 0x0283,
            WM_IME_COMPOSITIONFULL = 0x0284,
            WM_IME_SELECT = 0x0285,
            WM_IME_CHAR = 0x0286,
            WM_IME_REQUEST = 0x0288,
            WM_IME_KEYDOWN = 0x0290,
            WM_IME_KEYUP = 0x0291,
            WM_MOUSEHOVER = 0x02A1,
            WM_MOUSELEAVE = 0x02A3,
            WM_NCMOUSEHOVER = 0x02A0,
            WM_NCMOUSELEAVE = 0x02A2,
            WM_WTSSESSION_CHANGE = 0x02B1,
            WM_TABLET_FIRST = 0x02c0,
            WM_TABLET_LAST = 0x02df,
            WM_DPICHANGED = 0x02E0,
            WM_CUT = 0x0300,
            WM_COPY = 0x0301,
            WM_PASTE = 0x0302,
            WM_CLEAR = 0x0303,
            WM_UNDO = 0x0304,
            WM_RENDERFORMAT = 0x0305,
            WM_RENDERALLFORMATS = 0x0306,
            WM_DESTROYCLIPBOARD = 0x0307,
            WM_DRAWCLIPBOARD = 0x0308,
            WM_PAINTCLIPBOARD = 0x0309,
            WM_VSCROLLCLIPBOARD = 0x030A,
            WM_SIZECLIPBOARD = 0x030B,
            WM_ASKCBFORMATNAME = 0x030C,
            WM_CHANGECBCHAIN = 0x030D,
            WM_HSCROLLCLIPBOARD = 0x030E,
            WM_QUERYNEWPALETTE = 0x030F,
            WM_PALETTEISCHANGING = 0x0310,
            WM_PALETTECHANGED = 0x0311,
            WM_HOTKEY = 0x0312,
            WM_PRINT = 0x0317,
            WM_PRINTCLIENT = 0x0318,
            WM_APPCOMMAND = 0x0319,
            WM_THEMECHANGED = 0x031A,
            WM_CLIPBOARDUPDATE = 0x031D,
            WM_DWMCOMPOSITIONCHANGED = 0x031E,
            WM_DWMNCRENDERINGCHANGED = 0x031F,
            WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320,
            WM_DWMWINDOWMAXIMIZEDCHANGE = 0x0321,
            WM_GETTITLEBARINFOEX = 0x033F,
            WM_HANDHELDFIRST = 0x0358,
            WM_HANDHELDLAST = 0x035F,
            WM_AFXFIRST = 0x0360,
            WM_AFXLAST = 0x037F,
            WM_PENWINFIRST = 0x0380,
            WM_PENWINLAST = 0x038F,
            WM_TOUCH = 0x0240,
            WM_APP = 0x8000,
            WM_USER = 0x0400,

            WM_DISPATCH_WORK_ITEM = WM_USER,
        }

        public enum DwmWindowAttribute : uint
        {
            DWMWA_NCRENDERING_ENABLED = 1,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS,
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_EXCLUDED_FROM_PEEK,
            DWMWA_CLOAK,
            DWMWA_CLOAKED,
            DWMWA_FREEZE_REPRESENTATION,
            DWMWA_PASSIVE_UPDATE_MODE,
            DWMWA_USE_HOSTBACKDROPBRUSH,
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            DWMWA_WINDOW_CORNER_PREFERENCE = 33,
            DWMWA_BORDER_COLOR,
            DWMWA_CAPTION_COLOR,
            DWMWA_TEXT_COLOR,
            DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,
            DWMWA_LAST
        };

        public enum DwmWindowCornerPreference : uint
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND,
            DWMWCP_ROUND,
            DWMWCP_ROUNDSMALL
        }

        public enum MapVirtualKeyMapTypes : uint
        {
            MAPVK_VK_TO_VSC = 0x00,
            MAPVK_VSC_TO_VK = 0x01,
            MAPVK_VK_TO_CHAR = 0x02,
            MAPVK_VSC_TO_VK_EX = 0x03,
        }

        public enum BitmapCompressionMode : uint
        {
            BI_RGB = 0,
            BI_RLE8 = 1,
            BI_RLE4 = 2,
            BI_BITFIELDS = 3,
            BI_JPEG = 4,
            BI_PNG = 5
        }

        public enum DIBColorTable
        {
            DIB_RGB_COLORS = 0,     /* color table in RGBs */
            DIB_PAL_COLORS          /* color table in palette indices */
        }

        public enum WindowLongParam
        {
            GWL_WNDPROC = -4,
            GWL_HINSTANCE = -6,
            GWL_HWNDPARENT = -8,
            GWL_ID = -12,
            GWL_STYLE = -16,
            GWL_EXSTYLE = -20,
            GWL_USERDATA = -21
        }

        public enum MenuCharParam
        {
            MNC_IGNORE = 0,
            MNC_CLOSE = 1,
            MNC_EXECUTE = 2,
            MNC_SELECT = 3
        }

        public enum SysCommands
        {
            SC_SIZE = 0xF000,
            SC_MOVE = 0xF010,
            SC_MINIMIZE = 0xF020,
            SC_MAXIMIZE = 0xF030,
            SC_NEXTWINDOW = 0xF040,
            SC_PREVWINDOW = 0xF050,
            SC_CLOSE = 0xF060,
            SC_VSCROLL = 0xF070,
            SC_HSCROLL = 0xF080,
            SC_MOUSEMENU = 0xF090,
            SC_KEYMENU = 0xF100,
            SC_ARRANGE = 0xF110,
            SC_RESTORE = 0xF120,
            SC_TASKLIST = 0xF130,
            SC_SCREENSAVE = 0xF140,
            SC_HOTKEY = 0xF150,
            SC_DEFAULT = 0xF160,
            SC_MONITORPOWER = 0xF170,
            SC_CONTEXTHELP = 0xF180,
            SC_SEPARATOR = 0xF00F,
            SCF_ISSECURE = 0x00000001,
        }

        [Flags]
        public enum PointerFlags
        {
            POINTER_FLAG_NONE = 0x00000000,
            POINTER_FLAG_NEW = 0x00000001,
            POINTER_FLAG_INRANGE = 0x00000002,
            POINTER_FLAG_INCONTACT = 0x00000004,
            POINTER_FLAG_FIRSTBUTTON = 0x00000010,
            POINTER_FLAG_SECONDBUTTON = 0x00000020,
            POINTER_FLAG_THIRDBUTTON = 0x00000040,
            POINTER_FLAG_FOURTHBUTTON = 0x00000080,
            POINTER_FLAG_FIFTHBUTTON = 0x00000100,
            POINTER_FLAG_PRIMARY = 0x00002000,
            POINTER_FLAG_CONFIDENCE = 0x00000400,
            POINTER_FLAG_CANCELED = 0x00000800,
            POINTER_FLAG_DOWN = 0x00010000,
            POINTER_FLAG_UPDATE = 0x00020000,
            POINTER_FLAG_UP = 0x00040000,
            POINTER_FLAG_WHEEL = 0x00080000,
            POINTER_FLAG_HWHEEL = 0x00100000,
            POINTER_FLAG_CAPTURECHANGED = 0x00200000,
            POINTER_FLAG_HASTRANSFORM = 0x00400000
        }

        public enum PointerButtonChangeType : ulong
        {
            POINTER_CHANGE_NONE,
            POINTER_CHANGE_FIRSTBUTTON_DOWN,
            POINTER_CHANGE_FIRSTBUTTON_UP,
            POINTER_CHANGE_SECONDBUTTON_DOWN,
            POINTER_CHANGE_SECONDBUTTON_UP,
            POINTER_CHANGE_THIRDBUTTON_DOWN,
            POINTER_CHANGE_THIRDBUTTON_UP,
            POINTER_CHANGE_FOURTHBUTTON_DOWN,
            POINTER_CHANGE_FOURTHBUTTON_UP,
            POINTER_CHANGE_FIFTHBUTTON_DOWN,
            POINTER_CHANGE_FIFTHBUTTON_UP
        }

        [Flags]
        public enum PenFlags
        {
            PEN_FLAGS_NONE = 0x00000000,
            PEN_FLAGS_BARREL = 0x00000001,
            PEN_FLAGS_INVERTED = 0x00000002,
            PEN_FLAGS_ERASER = 0x00000004,
        }

        [Flags]
        public enum PenMask
        {
            PEN_MASK_NONE = 0x00000000,
            PEN_MASK_PRESSURE = 0x00000001,
            PEN_MASK_ROTATION = 0x00000002,
            PEN_MASK_TILT_X = 0x00000004,
            PEN_MASK_TILT_Y = 0x00000008
        }

        [Flags]
        public enum TouchFlags
        {
            TOUCH_FLAG_NONE = 0x00000000
        }

        [Flags]
        public enum TouchMask
        {
            TOUCH_MASK_NONE = 0x00000000,
            TOUCH_MASK_CONTACTAREA = 0x00000001,
            TOUCH_MASK_ORIENTATION = 0x00000002,
            TOUCH_MASK_PRESSURE = 0x00000004,
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct POINTER_TOUCH_INFO
        {
            public POINTER_INFO pointerInfo;
            public TouchFlags touchFlags;
            public TouchMask touchMask;
            public int rcContactLeft;
            public int rcContactTop;
            public int rcContactRight;
            public int rcContactBottom;
            public int rcContactRawLeft;
            public int rcContactRawTop;
            public int rcContactRawRight;
            public int rcContactRawBottom;
            public uint orientation;
            public uint pressure;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct POINTER_PEN_INFO
        {
            public POINTER_INFO pointerInfo;
            public PenFlags penFlags;
            public PenMask penMask;
            public uint pressure;
            public uint rotation;
            public int tiltX;
            public int tiltY;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct POINTER_INFO
        {
            public PointerInputType pointerType;
            public uint pointerId;
            public uint frameId;
            public PointerFlags pointerFlags;
            public IntPtr sourceDevice;
            public IntPtr hwndTarget;
            public int ptPixelLocationX;
            public int ptPixelLocationY;
            public int ptHimetricLocationX;
            public int ptHimetricLocationY;
            public int ptPixelLocationRawX;
            public int ptPixelLocationRawY;
            public int ptHimetricLocationRawX;
            public int ptHimetricLocationRawY;
            public uint dwTime;
            public uint historyCount;
            public int inputData;
            public ModifierKeys dwKeyStates;
            public ulong PerformanceCount;
            public PointerButtonChangeType ButtonChangeType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RGBQUAD
        {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;

            public void Init()
            {
                biSize = (uint)sizeof(BITMAPINFOHEADER);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            // C# cannot inlay structs in structs so must expand directly here
            //
            //[StructLayout(LayoutKind.Sequential)]
            //public struct BITMAPINFOHEADER
            //{
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public BitmapCompressionMode biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
            //}

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public uint[] cols;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        public const int SizeOf_BITMAPINFOHEADER = 40;

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int GetMouseMovePointsEx(
            uint cbSize, MOUSEMOVEPOINT* pointsIn,
            MOUSEMOVEPOINT* pointsBufferOut, int nBufPoints, uint resolution);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)] // For GetMouseMovePointsEx
        public struct MOUSEMOVEPOINT
        {
            public int x;                       //Specifies the x-coordinate of the mouse
            public int y;                       //Specifies the x-coordinate of the mouse
            public int time;                    //Specifies the time stamp of the mouse coordinate
            public IntPtr dwExtraInfo;              //Specifies extra information associated with this coordinate.
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool IsMouseInPointerEnabled();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int EnableMouseInPointer(bool enable);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerCursorId(uint pointerId, out uint cursorId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerType(uint pointerId, out PointerInputType pointerType);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerInfo(uint pointerId, out POINTER_INFO pointerInfo);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerInfoHistory(uint pointerId, ref int entriesCount, [MarshalAs(UnmanagedType.LPArray), In, Out] POINTER_INFO[] pointerInfos);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerPenInfo(uint pointerId, out POINTER_PEN_INFO penInfo);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerPenInfoHistory(uint pointerId, ref int entriesCount, [MarshalAs(UnmanagedType.LPArray), In, Out] POINTER_PEN_INFO[] penInfos);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerTouchInfo(uint pointerId, out POINTER_TOUCH_INFO touchInfo);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerDeviceRects(IntPtr device, out RECT pointerDeviceRect, out RECT displayRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerTouchInfoHistory(uint pointerId, ref int entriesCount, [MarshalAs(UnmanagedType.LPArray), In, Out] POINTER_TOUCH_INFO[] touchInfos);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        public static extern int SetDIBitsToDevice(IntPtr hdc, int XDest, int YDest,
            uint dwWidth, uint dwHeight,
            int XSrc, int YSrc,
            uint uStartScan, uint cScanLines,
           IntPtr lpvBits, [In] ref BITMAPINFO lpbmi, uint fuColorUse);

        [DllImport("gdi32.dll", SetLastError = false, ExactSpelling = true)]
        public static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);

        [DllImport("user32.dll")]
        public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool AdjustWindowRectEx(ref RECT lpRect, uint dwStyle, bool bMenu, uint dwExStyle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool AdjustWindowRectExForDpi(ref RECT lpRect, WindowStyles dwStyle, bool bMenu, WindowStyles dwExStyle, uint dpi);

        [DllImport("user32.dll")]
        public static extern IntPtr BeginPaint(IntPtr hwnd, out PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateWindowExW", ExactSpelling = true)]
        public static extern IntPtr CreateWindowEx(
           int dwExStyle,
           uint lpClassName,
           string? lpWindowName,
           uint dwStyle,
           int x,
           int y,
           int nWidth,
           int nHeight,
           IntPtr hWndParent,
           IntPtr hMenu,
           IntPtr hInstance,
           IntPtr lpParam);

        [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public const int SC_MOUSEMOVE = 0xf012;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageW")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "DispatchMessageW")]
        public static extern IntPtr DispatchMessage(ref MSG lpmsg);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        [DllImport("user32.dll")]
        public static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        public static extern uint GetCaretBlinkTime();

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern uint GetDoubleClickTime();

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetKeyboardState(byte* lpKeyState);

        [DllImport("user32.dll", EntryPoint = "MapVirtualKeyW")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", EntryPoint = "GetMessageW", SetLastError = true)]
        public static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        public static extern int GetMessageTime();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetModuleHandleW", ExactSpelling = true)]
        public static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(SystemMetric smIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumChildWindows(IntPtr parentHwnd, EnumWindowsProc enumFunc, IntPtr lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongPtrW", ExactSpelling = true)]
        public static extern uint GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongW", ExactSpelling = true)]
        public static extern uint GetWindowLong32b(IntPtr hWnd, int nIndex);

        public static uint GetWindowLong(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
            {
                return GetWindowLong32b(hWnd, nIndex);
            }
            else
            {
                return GetWindowLongPtr(hWnd, nIndex);
            }
        }

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongW", ExactSpelling = true)]
        private static extern uint SetWindowLong32b(IntPtr hWnd, int nIndex, uint value);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtrW", ExactSpelling = true)]
        private static extern IntPtr SetWindowLong64b(IntPtr hWnd, int nIndex, IntPtr value);

        public static uint SetWindowLong(IntPtr hWnd, int nIndex, uint value)
        {
            if (IntPtr.Size == 4)
            {
                return SetWindowLong32b(hWnd, nIndex, value);
            }
            else
            {
                return (uint)SetWindowLong64b(hWnd, nIndex, new IntPtr(value)).ToInt32();
            }
        }

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr handle)
        {
            if (IntPtr.Size == 4)
            {
                return new IntPtr(SetWindowLong32b(hWnd, nIndex, (uint)handle.ToInt32()));
            }
            else
            {
                return SetWindowLong64b(hWnd, nIndex, handle);
            }
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        public static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool GetUpdateRect(IntPtr hwnd, out RECT lpRect, bool bErase);

        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(IntPtr hWnd, ref RECT lpRect, bool bErase);


        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(IntPtr hWnd, RECT* lpRect, bool bErase);


        [DllImport("user32.dll")]
        public static extern bool ValidateRect(IntPtr hWnd, IntPtr lpRect);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowEnabled(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowUnicode(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool KillTimer(IntPtr hWnd, IntPtr uIDEvent);

        [DllImport("user32.dll", EntryPoint = "LoadCursorW", ExactSpelling = true)]
        public static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);

        [DllImport("user32.dll")]
        public static extern IntPtr CreateIconIndirect([In] ref ICONINFO iconInfo);

        [DllImport("user32.dll")]
        public static extern IntPtr CreateIconFromResourceEx(byte* pbIconBits, uint cbIconBits,
            int fIcon, int dwVersion, int csDesired, int cyDesired, int flags);

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", EntryPoint = "PeekMessageW", ExactSpelling = true)]
        public static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32")]
        public static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "RegisterClassExW")]
        public static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll")]
        public static extern void RegisterTouchWindow(IntPtr hWnd, int flags);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "RegisterWindowMessageW", ExactSpelling = true)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetTimer(IntPtr hWnd, IntPtr nIDEvent, uint uElapse, TimerProc? lpTimerFunc);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SetWindowPosFlags uFlags);
        [DllImport("user32.dll")]
        public static extern bool SetWindowPlacement(IntPtr hWnd, in WINDOWPLACEMENT windowPlacement);
        [DllImport("user32.dll")]
        public static extern bool SetFocus(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();
        [DllImport("user32.dll")]
        public static extern bool SetParent(IntPtr hWnd, IntPtr hWndNewParent);
        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        public enum GetAncestorFlags
        {
            GA_PARENT = 1,
            GA_ROOT = 2,
            GA_ROOTOWNER = 3
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags gaFlags);
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommand nCmdShow);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateTimerQueue();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DeleteTimerQueueEx(IntPtr TimerQueue, IntPtr CompletionEvent);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateTimerQueueTimer(
            out IntPtr phNewTimer,
            IntPtr TimerQueue,
            WaitOrTimerCallback Callback,
            IntPtr Parameter,
            uint DueTime,
            uint Period,
            uint Flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DeleteTimerQueueTimer(IntPtr TimerQueue, IntPtr Timer, IntPtr CompletionEvent);

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern int ToUnicodeEx(
            uint wVirtKey,
            uint wScanCode,
            byte* lpKeyState,
            char* pwszBuff,
            int cchBuff,
            uint wFlags,
            IntPtr dwhkl);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT lpEventTrack);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "UnregisterClassW", ExactSpelling = true)]
        public static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "SetWindowTextW")]
        public static extern bool SetWindowText(IntPtr hwnd, string? lpString);

        public enum ClassLongIndex : int
        {
            GCLP_MENUNAME = -8,
            GCLP_HBRBACKGROUND = -10,
            GCLP_HCURSOR = -12,
            GCLP_HICON = -14,
            GCLP_HMODULE = -16,
            GCL_CBWNDEXTRA = -18,
            GCL_CBCLSEXTRA = -20,
            GCLP_WNDPROC = -24,
            GCL_STYLE = -26,
            GCLP_HICONSM = -34,
            GCW_ATOM = -32
        }

        [DllImport("shell32", CharSet = CharSet.Auto)]
        public static extern int Shell_NotifyIcon(NIM dwMessage, NOTIFYICONDATA lpData);

        [DllImport("shell32", CharSet = CharSet.Auto)]
        public static extern nint SHAppBarMessage(AppBarMessage dwMessage, ref APPBARDATA lpData);

        [DllImport("user32.dll", EntryPoint = "SetClassLongPtrW", ExactSpelling = true)]
        private static extern IntPtr SetClassLong64(IntPtr hWnd, ClassLongIndex nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetClassLongW", ExactSpelling = true)]
        private static extern IntPtr SetClassLong32(IntPtr hWnd, ClassLongIndex nIndex, IntPtr dwNewLong);

        public static IntPtr SetClassLong(IntPtr hWnd, ClassLongIndex nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 4)
            {
                return SetClassLong32(hWnd, nIndex, dwNewLong);
            }

            return SetClassLong64(hWnd, nIndex, dwNewLong);
        }

        public static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size > 4)
                return GetClassLongPtr64(hWnd, nIndex);
            else
                return new IntPtr(GetClassLongPtr32(hWnd, nIndex));
        }

        [DllImport("user32.dll", EntryPoint = "GetClassLongW", ExactSpelling = true)]
        public static extern uint GetClassLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW", ExactSpelling = true)]
        public static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetCursor")]
        internal static extern IntPtr SetCursor(IntPtr hCursor);

        [DllImport("ole32.dll", PreserveSig = true)]
        internal static extern int CoCreateInstance(in Guid clsid,
            IntPtr ignore1, int ignore2, in Guid iid, [Out] out IntPtr pUnkOuter);

        internal static T CreateInstance<T>(in Guid clsid, in Guid iid) where T : IUnknown
        {
            var hresult = CoCreateInstance(in clsid, IntPtr.Zero, 1, in iid, out IntPtr pUnk);
            if (hresult != 0)
            {
                throw new COMException("CreateInstance", hresult);
            }
            using var unk = MicroComRuntime.CreateProxyFor<IUnknown>(pUnk, true);
            return unk.QueryInterface<T>();
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, out IntPtr ppv);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool OpenClipboard(IntPtr hWndOwner);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        public static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        public static extern IntPtr GetClipboardData(ClipboardFormat uFormat);

        [DllImport("user32.dll")]
        public static extern IntPtr SetClipboardData(ClipboardFormat uFormat, IntPtr hMem);

        [DllImport("ole32.dll", PreserveSig = true)]
        public static extern int OleGetClipboard(out IntPtr dataObject);
        [DllImport("ole32.dll", PreserveSig = true)]
        public static extern int OleFlushClipboard();

        [DllImport("ole32.dll", PreserveSig = true)]
        public static extern int OleSetClipboard(IntPtr dataObject);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GlobalLock(IntPtr handle);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern bool GlobalUnlock(IntPtr handle);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GlobalAlloc(int uFlags, int dwBytes);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryW", ExactSpelling = true)]
        public static extern IntPtr LoadLibrary(string fileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryExW", ExactSpelling = true)]
        public static extern IntPtr LoadLibraryEx(string fileName, IntPtr hFile, int flags);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetSaveFileNameW")]
        public static extern bool GetSaveFileName(IntPtr lpofn);

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetOpenFileNameW")]
        public static extern bool GetOpenFileName(IntPtr lpofn);

        [DllImport("comdlg32.dll")]
        public static extern int CommDlgExtendedError();

        public static bool ShCoreAvailable => LoadLibrary("shcore.dll") != IntPtr.Zero;

        [DllImport("shcore.dll")]
        public static extern void SetProcessDpiAwareness(PROCESS_DPI_AWARENESS value);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetProcessDpiAwarenessContext(IntPtr dpiAWarenessContext);

        [DllImport("shcore.dll")]
        public static extern long GetDpiForMonitor(IntPtr hmonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, DEVICECAP nIndex);

        [DllImport("shcore.dll")]
        public static extern void GetScaleFactorForMonitor(IntPtr hMon, out uint pScale);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(POINT pt, MONITOR dwFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromRect(RECT rect, MONITOR dwFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, MONITOR dwFlags);

        [DllImport("user32")]
        public static extern bool GetTouchInputInfo(
            IntPtr hTouchInput,
            uint cInputs,
            TOUCHINPUT* pInputs,
            int cbSize
        );

        [DllImport("user32")]
        public static extern bool CloseTouchInputHandle(IntPtr hTouchInput);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "PostMessageW")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "SendMessageW")]
        public static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("gdi32.dll")]
        public static extern int SetDIBitsToDevice(IntPtr hdc, int XDest, int YDest, uint
                dwWidth, uint dwHeight, int XSrc, int YSrc, uint uStartScan, uint cScanLines,
            IntPtr lpvBits, [In] ref BITMAPINFOHEADER lpbmi, uint fuColorUse);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateDIBSection(IntPtr hDC, ref BITMAPINFOHEADER pBitmapInfo, int un, out IntPtr lplpVoid, IntPtr handle, int dw);
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateBitmap(int width, int height, int planes, int bitCount, IntPtr data);
        [DllImport("gdi32.dll")]
        public static extern int DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern int ChoosePixelFormat(IntPtr hdc, ref PixelFormatDescriptor pfd);

        [DllImport("gdi32.dll")]
        public static extern int DescribePixelFormat(IntPtr hdc, ref PixelFormatDescriptor pfd);

        [DllImport("gdi32.dll")]
        public static extern int SetPixelFormat(IntPtr hdc, int iPixelFormat, ref PixelFormatDescriptor pfd);


        [DllImport("gdi32.dll")]
        public static extern int DescribePixelFormat(IntPtr hdc, int iPixelFormat, int bytes, ref PixelFormatDescriptor pfd);

        [DllImport("gdi32.dll")]
        public static extern bool SwapBuffers(IntPtr hdc);

        [DllImport("opengl32.dll")]
        public static extern IntPtr wglCreateContext(IntPtr hdc);

        [DllImport("opengl32.dll")]
        public static extern bool wglDeleteContext(IntPtr context);


        [DllImport("opengl32.dll", SetLastError = true)]
        public static extern bool wglMakeCurrent(IntPtr hdc, IntPtr context);

        [DllImport("opengl32.dll")]
        public static extern IntPtr wglGetCurrentContext();

        [DllImport("opengl32.dll")]
        public static extern IntPtr wglGetCurrentDC();

        [DllImport("opengl32.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr wglGetProcAddress(string name);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "CreateFileMappingW", ExactSpelling = true)]
        public static extern IntPtr CreateFileMapping(IntPtr hFile,
            IntPtr lpFileMappingAttributes,
            uint flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            string lpName);

        [DllImport("msvcrt.dll", EntryPoint = "memcpy", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CopyMemory(IntPtr dest, IntPtr src, UIntPtr count);

        [DllImport("ole32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern HRESULT RegisterDragDrop(IntPtr hwnd, IntPtr target);

        [DllImport("ole32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern HRESULT RevokeDragDrop(IntPtr hwnd);

        [DllImport("ole32.dll", EntryPoint = "OleInitialize")]
        public static extern HRESULT OleInitialize(IntPtr val);

        [DllImport("ole32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern void ReleaseStgMedium(ref STGMEDIUM medium);

        [DllImport("user32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "GetClipboardFormatNameW", ExactSpelling = true)]
        public static extern int GetClipboardFormatName(int format, StringBuilder lpString, int cchMax);

        [DllImport("user32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "RegisterClipboardFormatW", ExactSpelling = true)]
        public static extern int RegisterClipboardFormat(string format);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GlobalSize(IntPtr hGlobal);

        [DllImport("shell32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "DragQueryFileW", ExactSpelling = true)]
        public static extern int DragQueryFile(IntPtr hDrop, int iFile, StringBuilder? lpszFile, int cch);

        [DllImport("ole32.dll", CharSet = CharSet.Auto, ExactSpelling = true, PreserveSig = false)]
        internal static extern void DoDragDrop(IntPtr dataObject, IntPtr dropSource, int allowedEffects, [Out] out int finalEffect);

        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, void* pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(out bool enabled);

        [DllImport("dwmapi.dll")]
        public static extern void DwmFlush();

        [DllImport("dwmapi.dll")]
        public static extern bool DwmDefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref IntPtr plResult);

        [DllImport("dwmapi.dll", SetLastError = false)]
        public static extern int DwmEnableBlurBehindWindow(IntPtr hwnd, ref DWM_BLURBEHIND blurBehind);

        [Flags]
        public enum LayeredWindowFlags
        {
            LWA_ALPHA = 0x00000002,
            LWA_COLORKEY = 0x00000001,
        }

        [DllImport("user32.dll")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, LayeredWindowFlags dwFlags);

        [Flags]
        public enum DWM_BB
        {
            Enable = 1,
            BlurRegion = 2,
            TransitionMaximized = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DWM_BLURBEHIND
        {
            public DWM_BB dwFlags;
            public bool fEnable;
            public IntPtr hRgnBlur;
            public bool fTransitionOnMaximized;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RTL_OSVERSIONINFOEX
        {
            internal uint dwOSVersionInfoSize;
            internal uint dwMajorVersion;
            internal uint dwMinorVersion;
            internal uint dwBuildNumber;
            internal uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string szCSDVersion;
        }

        [DllImport("ntdll")]
        private static extern int RtlGetVersion(ref RTL_OSVERSIONINFOEX lpVersionInformation);

        internal static Version RtlGetVersion()
        {
            RTL_OSVERSIONINFOEX v = new RTL_OSVERSIONINFOEX();
            v.dwOSVersionInfoSize = (uint)Marshal.SizeOf<RTL_OSVERSIONINFOEX>();
            if (RtlGetVersion(ref v) == 0)
            {
                return new Version((int)v.dwMajorVersion, (int)v.dwMinorVersion, (int)v.dwBuildNumber);
            }
            else
            {
                throw new Exception("RtlGetVersion failed!");
            }
        }

        [DllImport("kernel32", EntryPoint = "WaitForMultipleObjectsEx", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int IntWaitForMultipleObjectsEx(int nCount, IntPtr[] pHandles, bool bWaitAll, int dwMilliseconds, bool bAlertable);

        public const int WAIT_FAILED = unchecked((int)0xFFFFFFFF);

        internal static int WaitForMultipleObjectsEx(int nCount, IntPtr[] pHandles, bool bWaitAll, int dwMilliseconds, bool bAlertable)
        {
            int result = IntWaitForMultipleObjectsEx(nCount, pHandles, bWaitAll, dwMilliseconds, bAlertable);
            if (result == WAIT_FAILED)
            {
                throw new Win32Exception();
            }

            return result;
        }

        [Flags]
        internal enum QueueStatusFlags
        {
            QS_KEY = 0x0001,
            QS_MOUSEMOVE = 0x0002,
            QS_MOUSEBUTTON = 0x0004,
            QS_POSTMESSAGE = 0x0008,
            QS_TIMER = 0x0010,
            QS_PAINT = 0x0020,
            QS_SENDMESSAGE = 0x0040,
            QS_HOTKEY = 0x0080,
            QS_ALLPOSTMESSAGE = 0x0100,
            QS_EVENT = 0x02000,
            QS_MOUSE = QS_MOUSEMOVE | QS_MOUSEBUTTON,
            QS_INPUT = QS_MOUSE | QS_KEY,
            QS_ALLEVENTS = QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY,
            QS_ALLINPUT = QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY | QS_SENDMESSAGE
        }

        [Flags]
        internal enum MsgWaitForMultipleObjectsFlags
        {
            MWMO_WAITALL = 0x0001,
            MWMO_ALERTABLE = 0x0002,
            MWMO_INPUTAVAILABLE = 0x0004
        }

        [DllImport("user32", EntryPoint="MsgWaitForMultipleObjectsEx", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern int IntMsgWaitForMultipleObjectsEx(int nCount, IntPtr[]? pHandles, int dwMilliseconds,
            QueueStatusFlags dwWakeMask, MsgWaitForMultipleObjectsFlags dwFlags);

        internal static int MsgWaitForMultipleObjectsEx(int nCount, IntPtr[]? pHandles, int dwMilliseconds,
            QueueStatusFlags dwWakeMask, MsgWaitForMultipleObjectsFlags dwFlags)
        {
            int result = IntMsgWaitForMultipleObjectsEx(nCount, pHandles, dwMilliseconds, dwWakeMask, dwFlags);
            if(result == -1)
            {
                throw new Win32Exception();
            }

            return result;
        }

        [Flags]
        public enum GCS : uint
        {
            /// <summary>Retrieve or update the attribute of the composition string.</summary>
            GCS_COMPATTR = 0x0010,

            /// <summary>Retrieve or update clause information of the composition string.</summary>
            GCS_COMPCLAUSE = 0x0020,

            /// <summary>Retrieve or update the attributes of the reading string of the current composition.</summary>
            GCS_COMPREADATTR = 0x0002,

            /// <summary>Retrieve or update the clause information of the reading string of the composition string.</summary>
            GCS_COMPREADCLAUSE = 0x0004,

            /// <summary>Retrieve or update the reading string of the current composition.</summary>
            GCS_COMPREADSTR = 0x0001,

            /// <summary>Retrieve or update the current composition string.</summary>
            GCS_COMPSTR = 0x0008,

            /// <summary>Retrieve or update the cursor position in composition string.</summary>
            GCS_CURSORPOS = 0x0080,

            /// <summary>Retrieve or update the starting position of any changes in composition string.</summary>
            GCS_DELTASTART = 0x0100,

            /// <summary>Retrieve or update clause information of the result string.</summary>
            GCS_RESULTCLAUSE = 0x1000,

            /// <summary>Retrieve or update clause information of the reading string.</summary>
            GCS_RESULTREADCLAUSE = 0x0400,

            /// <summary>Retrieve or update the reading string.</summary>
            GCS_RESULTREADSTR = 0x0200,

            /// <summary>Retrieve or update the string of the composition result.</summary>
            GCS_RESULTSTR = 0x0800,
        }

        [DllImport("imm32.dll", SetLastError = true)]
        public static extern IntPtr ImmGetContext(IntPtr hWnd);
        [DllImport("imm32.dll", SetLastError = true)]
        public static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);
        [DllImport("imm32.dll", SetLastError = true)]
        public static extern IntPtr ImmCreateContext();
        [DllImport("imm32.dll")]
        public static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);
        [DllImport("imm32.dll")]
        public static extern bool ImmSetOpenStatus(IntPtr hIMC, bool flag);
        [DllImport("imm32.dll")]
        public static extern bool ImmSetActiveContext(IntPtr hIMC, bool flag);
        [DllImport("imm32.dll")]
        public static extern bool ImmSetStatusWindowPos(IntPtr hIMC, ref POINT lpptPos);
        [DllImport("imm32.dll")]
        public static extern bool ImmIsIME(IntPtr HKL);
        [DllImport("imm32.dll")]
        public static extern bool ImmSetCandidateWindow(IntPtr hIMC, ref CANDIDATEFORM lpCandidate);
        [DllImport("imm32.dll")]
        public static extern bool ImmSetCompositionWindow(IntPtr hIMC, ref COMPOSITIONFORM lpComp);
        [DllImport("imm32.dll")]
        public static extern bool ImmSetCompositionFont(IntPtr hIMC, ref LOGFONT lf);

        [DllImport("imm32.dll", SetLastError = false, CharSet = CharSet.Unicode, EntryPoint = "ImmGetCompositionStringW", ExactSpelling = true)]
        public static extern int ImmGetCompositionString(IntPtr hIMC, GCS dwIndex, [Out, Optional] IntPtr lpBuf, uint dwBufLen);

        public static string? ImmGetCompositionString(IntPtr hIMC, GCS dwIndex)
        {
            int bufferLength = ImmGetCompositionString(hIMC, dwIndex, IntPtr.Zero, 0);

            if (bufferLength > 0)
            {
                var buffer = bufferLength <= 64 ? stackalloc byte[bufferLength] : new byte[bufferLength];

                fixed (byte* bufferPtr = buffer)
                {
                    var result = ImmGetCompositionString(hIMC, dwIndex, (IntPtr)bufferPtr, (uint)bufferLength);
                    if (result >= 0)
                    {
                        return Encoding.Unicode.GetString(bufferPtr, result);
                    }
                }
            }

            return null;
        }

        [DllImport("imm32.dll")]
        public static extern bool ImmNotifyIME(IntPtr hIMC, int dwAction, int dwIndex, int dwValue);
        [DllImport("user32.dll")]
        public static extern bool CreateCaret(IntPtr hwnd, IntPtr hBitmap, int nWidth, int nHeight);
        [DllImport("user32.dll")]
        public static extern bool SetCaretPos(int X, int Y);
        [DllImport("user32.dll")]
        public static extern bool DestroyCaret();
        [DllImport("user32.dll")]
        public static extern IntPtr GetKeyboardLayout(int idThread);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int LCIDToLocaleName(uint Locale, StringBuilder lpName, int cchName, int dwFlags);

        public static uint MAKELCID(uint lgid, uint srtid)
        {
            return (((uint)(ushort)srtid) << 16) |
                   ((ushort)lgid);
        }

        public static ushort PRIMARYLANGID(uint lgid)
        {
            return (ushort)(lgid & 0x3ff);
        }

        public static uint LGID(IntPtr HKL)
        {
            unchecked
            {
                return (uint)((ulong)HKL & 0xffff);
            }
        }

        public const int SORT_DEFAULT = 0;
        public const int LANG_ZH = 0x0004;
        public const int LANG_JA = 0x0011;
        public const int LANG_KO = 0x0012;

        public const int CFS_FORCE_POSITION = 0x0020;
        public const int CFS_CANDIDATEPOS = 0x0040;
        public const int CFS_EXCLUDE = 0x0080;
        public const int CFS_POINT = 0x0002;
        public const int CFS_RECT = 0x0001;

        // lParam for WM_IME_SETCONTEXT
        public const long ISC_SHOWUICANDIDATEWINDOW = 0x00000001;
        public const long ISC_SHOWUICOMPOSITIONWINDOW = 0x80000000;
        public const long ISC_SHOWUIGUIDELINE = 0x40000000;
        public const long ISC_SHOWUIALLCANDIDATEWINDOW = 0x0000000F;
        public const long ISC_SHOWUIALL = 0xC000000F;

        public const int NI_COMPOSITIONSTR = 21;
        public const int CPS_COMPLETE = 1;
        public const int CPS_CONVERT = 2;
        public const int CPS_REVERT = 3;
        public const int CPS_CANCEL = 4;

        [StructLayout(LayoutKind.Sequential)]
        internal struct CANDIDATEFORM
        {
            public int dwIndex;
            public int dwStyle;
            public POINT ptCurrentPos;
            public RECT rcArea;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct COMPOSITIONFORM
        {
            public int dwStyle;
            public POINT ptCurrentPos;
            public RECT rcArea;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct LOGFONT
        {
            public int lfHeight;
            public int lfWidth;
            public int lfEscapement;
            public int lfOrientation;
            public int lfWeight;
            public byte lfItalic;
            public byte lfUnderline;
            public byte lfStrikeOut;
            public byte lfCharSet;
            public byte lfOutPrecision;
            public byte lfClipPrecision;
            public byte lfQuality;
            public byte lfPitchAndFamily;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string lfFaceName;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            // ...
            WCA_ACCENT_POLICY = 19
            // ...
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLIC = 4, //1703 and above
            ACCENT_ENABLE_HOSTBACKDROP = 5,        // RS5 1809
            ACCENT_INVALID_STATE = 6
        }

        internal enum AccentFlags
        {
            DrawLeftBorder = 0x20,
            DrawTopBorder = 0x40,
            DrawRightBorder = 0x80,
            DrawBottomBorder = 0x100,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        public enum MONITOR
        {
            MONITOR_DEFAULTTONULL = 0x00000000,
            MONITOR_DEFAULTTOPRIMARY = 0x00000001,
            MONITOR_DEFAULTTONEAREST = 0x00000002,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MONITORINFOEX
        {
            internal MONITORINFO Base;

            internal __char_32 szDevice;

            public static MONITORINFOEX Create()
            {
                var info = new MONITORINFO();
                info.cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>();
                return new MONITORINFOEX() { Base = info };
            }
        }

        public enum DEVICECAP
        {
            HORZRES = 8,
            BITSPIXEL = 12,
            PLANES = 14,
            DESKTOPHORZRES = 118
        }

        public enum PROCESS_DPI_AWARENESS
        {
            PROCESS_DPI_UNAWARE = 0,
            PROCESS_SYSTEM_DPI_AWARE = 1,
            PROCESS_PER_MONITOR_DPI_AWARE = 2
        }

        public enum MONITOR_DPI_TYPE
        {
            MDT_EFFECTIVE_DPI = 0,
            MDT_ANGULAR_DPI = 1,
            MDT_RAW_DPI = 2,
            MDT_DEFAULT = MDT_EFFECTIVE_DPI
        }

        public enum ClipboardFormat
        {
            /// <summary>
            /// Text format. Each line ends with a carriage return/linefeed (CR-LF) combination. A null character signals the end of the data. Use this format for ANSI text.
            /// </summary>
            CF_TEXT = 1,
            /// <summary>
            /// A handle to a bitmap
            /// </summary>
            CF_BITMAP = 2,
            /// <summary>
            /// A memory object containing a BITMAPINFO structure followed by the bitmap bits.
            /// </summary>
            CF_DIB = 3,
            /// <summary>
            /// Unicode text format. Each line ends with a carriage return/linefeed (CR-LF) combination. A null character signals the end of the data.
            /// </summary>
            CF_UNICODETEXT = 13,
            /// <summary>
            /// A handle to type HDROP that identifies a list of files.
            /// </summary>
            CF_HDROP = 15,
        }

        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }

        public struct POINT
        {
            public int X;
            public int Y;
        }

        public struct SIZE
        {
            public int X;
            public int Y;
        }

        public struct SIZE_F
        {
            public float X;
            public float Y;
        }

        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public int Width => right - left;
            public int Height => bottom - top;
            public RECT(Rect rect)
            {
                left = (int)rect.X;
                top = (int)rect.Y;
                right = (int)(rect.X + rect.Width);
                bottom = (int)(rect.Y + rect.Height);
            }

            public void Offset(POINT pt)
            {
                left += pt.X;
                right += pt.X;
                top += pt.Y;
                bottom += pt.Y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NCCALCSIZE_PARAMS
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public RECT[] rgrc;
            public WINDOWPOS lppos;
        }

        public struct TRACKMOUSEEVENT
        {
            public int cbSize;
            public uint dwFlags;
            public IntPtr hwndTrack;
            public int dwHoverTime;
        }

        [Flags]
        public enum WindowPlacementFlags : uint
        {
            SetMinPosition = 0x0001,
            RestoreToMaximized = 0x0002,
            AsyncWindowPlacement = 0x0004,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            /// <summary>
            /// The length of the structure, in bytes. Before calling the GetWindowPlacement or SetWindowPlacement functions, set this member to sizeof(WINDOWPLACEMENT).
            /// <para>
            /// GetWindowPlacement and SetWindowPlacement fail if this member is not set correctly.
            /// </para>
            /// </summary>
            public int Length;

            /// <summary>
            /// Specifies flags that control the position of the minimized window and the method by which the window is restored.
            /// </summary>
            public WindowPlacementFlags Flags;

            /// <summary>
            /// The current show state of the window.
            /// </summary>
            public ShowWindowCommand ShowCmd;

            /// <summary>
            /// The coordinates of the window's upper-left corner when the window is minimized.
            /// </summary>
            public POINT MinPosition;

            /// <summary>
            /// The coordinates of the window's upper-left corner when the window is maximized.
            /// </summary>
            public POINT MaxPosition;

            /// <summary>
            /// The window's coordinates when the window is in the restored position.
            /// </summary>
            public RECT NormalPosition;

            /// <summary>
            /// Gets the default (empty) value.
            /// </summary>
            public static WINDOWPLACEMENT Default
            {
                get
                {
                    WINDOWPLACEMENT result = new WINDOWPLACEMENT();
                    result.Length = Marshal.SizeOf<WINDOWPLACEMENT>();
                    return result;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WNDCLASSEX
        {
            public int cbSize;
            public int style;
            public WndProc lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOUCHINPUT
        {
            public int X;
            public int Y;
            public IntPtr Source;
            public uint Id;
            public TouchInputFlags Flags;
            public int Mask;
            public uint Time;
            public IntPtr ExtraInfo;
            public int CxContact;
            public int CyContact;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ICONINFO
        {
            public bool IsIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr MaskBitmap;
            public IntPtr ColorBitmap;
        };

        [Flags]
        public enum TouchInputFlags
        {
            /// <summary>
            /// Movement has occurred. Cannot be combined with TOUCHEVENTF_DOWN.
            /// </summary>
            TOUCHEVENTF_MOVE = 0x0001,

            /// <summary>
            /// The corresponding touch point was established through a new contact. Cannot be combined with TOUCHEVENTF_MOVE or TOUCHEVENTF_UP.
            /// </summary>
            TOUCHEVENTF_DOWN = 0x0002,

            /// <summary>
            /// A touch point was removed.
            /// </summary>
            TOUCHEVENTF_UP = 0x0004,

            /// <summary>
            /// A touch point is in range. This flag is used to enable touch hover support on compatible hardware. Applications that do not want support for hover can ignore this flag.
            /// </summary>
            TOUCHEVENTF_INRANGE = 0x0008,

            /// <summary>
            /// Indicates that this TOUCHINPUT structure corresponds to a primary contact point. See the following text for more information on primary touch points.
            /// </summary>
            TOUCHEVENTF_PRIMARY = 0x0010,

            /// <summary>
            /// When received using GetTouchInputInfo, this input was not coalesced.
            /// </summary>
            TOUCHEVENTF_NOCOALESCE = 0x0020,

            /// <summary>
            /// The touch event came from the user's palm.
            /// </summary>
            TOUCHEVENTF_PALM = 0x0080
        }

        [Flags]
        public enum OpenFileNameFlags
        {
            OFN_ALLOWMULTISELECT = 0x00000200,
            OFN_EXPLORER = 0x00080000,
            OFN_HIDEREADONLY = 0x00000004,
            OFN_NOREADONLYRETURN = 0x00008000,
            OFN_OVERWRITEPROMPT = 0x00000002
        }

        public enum HRESULT : uint
        {
            S_FALSE = 0x0001,
            S_OK = 0x0000,
            E_INVALIDARG = 0x80070057,
            E_OUTOFMEMORY = 0x8007000E,
            E_NOTIMPL = 0x80004001,
            E_UNEXPECTED = 0x8000FFFF,
            E_CANCELLED = 0x800704C7,
        }

        public enum Icons
        {
            ICON_SMALL = 0,
            ICON_BIG = 1,
            /// <summary>The small icon, but with the system theme variant rather than the window's own theme. Requested by other processes, e.g. the taskbar and Task Manager.</summary>
            ICON_SMALL2 = 2,
        }

        public static class ShellIds
        {
            public static readonly Guid OpenFileDialog = Guid.Parse("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
            public static readonly Guid SaveFileDialog = Guid.Parse("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B");
            public static readonly Guid IFileDialog = Guid.Parse("42F85136-DB7E-439C-85F1-E4075D135FC8");
            public static readonly Guid IShellItem = Guid.Parse("43826D1E-E718-42EE-BC55-A1E261C37BFE");
            public static readonly Guid TaskBarList = Guid.Parse("56FDF344-FD6D-11D0-958A-006097C9A090");
            public static readonly Guid ITaskBarList2 = Guid.Parse("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf");
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct COMDLG_FILTERSPEC
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszSpec;
        }

        public delegate void MarkFullscreenWindow(IntPtr This, IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fullscreen);
        public delegate void SetOverlayIcon(IntPtr This, IntPtr hWnd, IntPtr hIcon, [MarshalAs(UnmanagedType.LPWStr)] string? pszDescription);
        public delegate HRESULT HrInit(IntPtr This);

        public struct ITaskBarList3VTable
        {
            public IntPtr IUnknown1;
            public IntPtr IUnknown2;
            public IntPtr IUnknown3;
            public IntPtr HrInit;
            public IntPtr AddTab;
            public IntPtr DeleteTab;
            public IntPtr ActivateTab;
            public IntPtr SetActiveAlt;
            public IntPtr MarkFullscreenWindow;
            public IntPtr SetProgressValue;
            public IntPtr SetProgressState;
            public IntPtr RegisterTab;
            public IntPtr UnregisterTab;
            public IntPtr SetTabOrder;
            public IntPtr SetTabActive;
            public IntPtr ThumbBarAddButtons;
            public IntPtr ThumbBarUpdateButtons;
            public IntPtr ThumbBarSetImageList;
            public IntPtr SetOverlayIcon;
            public IntPtr SetThumbnailTooltip;
            public IntPtr SetThumbnailClip;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct APPBARDATA
        {
            private static readonly int s_size = Marshal.SizeOf<APPBARDATA>();

            public int cbSize;
            public nint hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;

            public APPBARDATA()
            {
                cbSize = s_size;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct _DROPFILES
    {
        public Int32 pFiles;
        public Int32 X;
        public Int32 Y;
        public bool fNC;
        public bool fWide;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct STGMEDIUM
    {
        public TYMED tymed;
        public IntPtr unionmember;
        public IntPtr pUnkForRelease;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FORMATETC
    {
        public ushort cfFormat;
        public IntPtr ptd;
        public DVASPECT dwAspect;
        public int lindex;
        public TYMED tymed;
    }

    [Flags]
    internal enum PixelFormatDescriptorFlags : uint
    {
        PFD_DOUBLEBUFFER = 0x00000001,
        PFD_STEREO = 0x00000002,
        PFD_DRAW_TO_WINDOW = 0x00000004,
        PFD_DRAW_TO_BITMAP = 0x00000008,
        PFD_SUPPORT_GDI = 0x00000010,
        PFD_SUPPORT_OPENGL = 0x00000020,
        PFD_GENERIC_FORMAT = 0x00000040,
        PFD_NEED_PALETTE = 0x00000080,
        PFD_NEED_SYSTEM_PALETTE = 0x00000100,
        PFD_SWAP_EXCHANGE = 0x00000200,
        PFD_SWAP_COPY = 0x00000400,
        PFD_SWAP_LAYER_BUFFERS = 0x00000800,
        PFD_GENERIC_ACCELERATED = 0x00001000,
        PFD_SUPPORT_DIRECTDRAW = 0x00002000,
        PFD_DEPTH_DONTCARE = 0x20000000,
        PFD_DOUBLEBUFFER_DONTCARE = 0x40000000,
        PFD_STEREO_DONTCARE = 0x80000000,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PixelFormatDescriptor
    {
        public ushort Size;
        public ushort Version;
        public PixelFormatDescriptorFlags Flags;
        public byte PixelType;
        public byte ColorBits;
        public byte RedBits;
        public byte RedShift;
        public byte GreenBits;
        public byte GreenShift;
        public byte BlueBits;
        public byte BlueShift;
        public byte AlphaBits;
        public byte AlphaShift;
        public byte AccumBits;
        public byte AccumRedBits;
        public byte AccumGreenBits;
        public byte AccumBlueBits;
        public byte AccumAlphaBits;
        public byte DepthBits;
        public byte StencilBits;
        public byte AuxBuffers;
        public byte LayerType;
        private byte Reserved;
        public uint LayerMask;
        public uint VisibleMask;
        public uint DamageMask;
    }

    internal enum NIM : uint
    {
        ADD = 0x00000000,
        MODIFY = 0x00000001,
        DELETE = 0x00000002,
        SETFOCUS = 0x00000003,
        SETVERSION = 0x00000004
    }

    internal enum AppBarMessage : uint
    {
        ABM_GETSTATE = 0x00000004,
        ABM_GETTASKBARPOS = 0x00000005,
    }

    [Flags]
    internal enum NIF : uint
    {
        MESSAGE = 0x00000001,
        ICON = 0x00000002,
        TIP = 0x00000004,
        STATE = 0x00000008,
        INFO = 0x00000010,
        GUID = 0x00000020,
        REALTIME = 0x00000040,
        SHOWTIP = 0x00000080
    }

    [Flags]
    internal enum NIIF : uint
    {
        NONE = 0x00000000,
        INFO = 0x00000001,
        WARNING = 0x00000002,
        ERROR = 0x00000003,
        USER = 0x00000004,
        ICON_MASK = 0x0000000F,
        NOSOUND = 0x00000010,
        LARGE_ICON = 0x00000020,
        RESPECT_QUIET_TIME = 0x00000080
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal class NOTIFYICONDATA
    {
        public int cbSize = Marshal.SizeOf<NOTIFYICONDATA>();
        public IntPtr hWnd;
        public int uID;
        public NIF uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string? szTip;
        public int dwState = 0;
        public int dwStateMask = 0;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string? szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string? szInfoTitle;
        public NIIF dwInfoFlags;
    }
}
