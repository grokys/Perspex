﻿namespace Avalonia.Platform
{
    public interface IScreenImpl
    {
        int screenCount { get; }

        IScreenImpl[] AllScreens { get; }

        IScreenImpl PrimaryScreen { get; }

        string DeviceName { get; }

        Rect Bounds { get; }

        Rect WorkingArea { get; }

        bool Primary { get; }
    }
}