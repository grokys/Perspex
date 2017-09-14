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

        private Screen[] allScreens;
        public Screen[] AllScreens
        {
            get
            {
                if (allScreens == null)
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
                    allScreens = screens;
                }
                return allScreens;
            }
        }

        public void InvalidateScreensCache()
        {
            allScreens = null;
        }
        
        public Screen PrimaryScreen
        {
            get
            {
                for (var i = 0; i < allScreens.Length; i++)
                {
                    if (allScreens[i].Primary)
                        return allScreens[i];
                }

                return null;
            }
        }
    }
}
