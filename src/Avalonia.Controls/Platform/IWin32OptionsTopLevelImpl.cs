using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Metadata;
using Avalonia.Platform;
using static Avalonia.Controls.Win32Properties;

namespace Avalonia.Controls.Platform
{
    [PrivateApi]
    public interface IWin32OptionsTopLevelImpl : ITopLevelImpl
    {
        /// <summary>
        /// Gets or sets a callback to set the window styles. 
        /// </summary>
        public CustomWindowStylesCallback? WindowStylesCallback { get; set; }

        /// <summary>
        /// Gets or sets a custom callback for the window's WndProc
        /// </summary>
        public CustomWndProcHookCallback? WndProcHookCallback { get; set; }

        /// <summary>
        /// Sets hints that configure the shape of window corners.
        /// </summary>
        void SetWindowCornerHints(Win32WindowCornerHints hints);
    }
}
