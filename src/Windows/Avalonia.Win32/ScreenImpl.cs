﻿// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using static Avalonia.Win32.Interop.UnmanagedMethods;

#if NETSTANDARD
using Win32Exception = Avalonia.Win32.NetStandard.AvaloniaWin32Exception;
#endif

namespace Avalonia.Win32
{
    public class ScreenImpl : IScreenImpl
    {
        public int ScreenCount
        {
            get => GetSystemMetrics(SystemMetric.SM_CMONITORS);
        }

        private Screen[] _allScreens;
        public Screen[] AllScreens
        {
            get
            {
                if (_allScreens == null)
                {
                    int index = 0;
                    Screen[] screens = new Screen[ScreenCount];
                    EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr data) =>
                                                                  {
                                                                      MONITORINFO monitorInfo = new MONITORINFO();
                                                                      if (GetMonitorInfo(monitor, monitorInfo))
                                                                      {
                                                                          RECT bounds = monitorInfo.rcMonitor;
                                                                          RECT workingArea = monitorInfo.rcWork;
                                                                          Rect avaloniaBounds = new Rect(bounds.left, bounds.top, bounds.right, bounds.bottom);
                                                                          Rect avaloniaWorkArea =
                                                                              new Rect(workingArea.left, workingArea.top, workingArea.right,
                                                                                       workingArea.bottom);
                                                                          screens[index] =
                                                                              new WinScreen(avaloniaBounds, avaloniaWorkArea, monitorInfo.dwFlags == 1,
                                                                                            monitor);
                                                                          index++;
                                                                      }
                                                                      return true;
                                                                  }, IntPtr.Zero);
                    _allScreens = screens;
                }
                return _allScreens;
            }
        }

        public void InvalidateScreensCache()
        {
            _allScreens = null;
        }
        
        public Screen PrimaryScreen
        {
            get
            {
                for (int i = 0; i < AllScreens.Length; i++)
                {
                    if (AllScreens[i].Primary)
                        return AllScreens[i];
                }

                return null;
            }
        }
    }
}
