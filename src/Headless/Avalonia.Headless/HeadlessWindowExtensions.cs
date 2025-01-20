using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Headless;

/// <summary>
/// Set of extension methods to simplify usage of Avalonia.Headless platform.
/// </summary>
public static class HeadlessWindowExtensions
{
    /// <summary>
    /// Triggers a renderer timer tick and captures last rendered frame.
    /// </summary>
    /// <returns>Bitmap with last rendered frame. Null, if nothing was rendered.</returns>
    public static WriteableBitmap? CaptureRenderedFrame(this TopLevel topLevel)
    {
        Dispatcher.UIThread.PulseRenderFrames(1);
        return topLevel.GetLastRenderedFrame();
    }

    /// <summary>
    /// Reads last rendered frame.
    /// Note, in order to trigger rendering timer, call <see cref="AvaloniaHeadlessPlatform.ForceRenderTimerTick"/> method.  
    /// </summary>
    /// <returns>Bitmap with last rendered frame. Null, if nothing was rendered.</returns>
    public static WriteableBitmap? GetLastRenderedFrame(this TopLevel topLevel)
    {
        if (AvaloniaLocator.Current.GetService<IPlatformRenderInterface>() is HeadlessPlatformRenderInterface)
        {
            throw new NotSupportedException(
                "To capture a rendered frame, make sure that headless application was initialized with '.UseSkia()' and disabled 'UseHeadlessDrawing' in the 'AvaloniaHeadlessPlatformOptions'.");
        }

        return GetImpl(topLevel).GetLastRenderedFrame();
    }

    /// <summary>
    /// Simulates a keyboard press on the headless window/toplevel.
    /// </summary>
    [Obsolete("Use the overload that takes a physical key and key symbol instead, or KeyPressQwerty alternatively.")]
    public static void KeyPress(this TopLevel topLevel, Key key, RawInputModifiers modifiers) =>
        KeyPress(topLevel, key, modifiers, PhysicalKey.None, null);

    /// <summary>
    /// Simulates keyboard press on the headless window/toplevel.
    /// </summary>
    public static void KeyPress(this TopLevel topLevel, Key key, RawInputModifiers modifiers, PhysicalKey physicalKey,
        string? keySymbol) =>
        RunJobsOnImpl(topLevel, w => w.KeyPress(key, modifiers, physicalKey, keySymbol));

    /// <summary>
    /// Simulates keyboard press on the headless window/toplevel, as if typed on a QWERTY keyboard.
    /// </summary>
    public static void KeyPressQwerty(this TopLevel topLevel, PhysicalKey physicalKey, RawInputModifiers modifiers) =>
        RunJobsOnImpl(topLevel, w => w.KeyPress(physicalKey.ToQwertyKey(), modifiers, physicalKey, physicalKey.ToQwertyKeySymbol()));

    /// <summary>
    /// Simulates a keyboard release on the headless window/toplevel.
    /// </summary>
    [Obsolete("Use the overload that takes a physical key and key symbol instead, or KeyReleaseQwerty alternatively.")]
    public static void KeyRelease(this TopLevel topLevel, Key key, RawInputModifiers modifiers) =>
        KeyRelease(topLevel, key, modifiers, PhysicalKey.None, null);

    /// <summary>
    /// Simulates keyboard release on the headless window/toplevel.
    /// </summary>
    public static void KeyRelease(this TopLevel topLevel, Key key, RawInputModifiers modifiers, PhysicalKey physicalKey,
        string? keySymbol) =>
        RunJobsOnImpl(topLevel, w => w.KeyRelease(key, modifiers, physicalKey, keySymbol));

    /// <summary>
    /// Simulates keyboard release on the headless window/toplevel, as if typed on a QWERTY keyboard.
    /// </summary>
    public static void KeyReleaseQwerty(this TopLevel topLevel, PhysicalKey physicalKey, RawInputModifiers modifiers) =>
        RunJobsOnImpl(topLevel, w => w.KeyRelease(physicalKey.ToQwertyKey(), modifiers, physicalKey, physicalKey.ToQwertyKeySymbol()));

    /// <summary>
    /// Simulates a text input event on the headless window/toplevel.
    /// </summary>
    /// <remarks>
    /// This event is independent of KeyPress and KeyRelease. If you need to simulate text input to a TextBox or a similar control, please use KeyTextInput.
    /// </remarks>
    public static void KeyTextInput(this TopLevel topLevel, string text) =>
        RunJobsOnImpl(topLevel, w => w.TextInput(text));

    /// <summary>
    /// Simulates a mouse down on the headless window/toplevel.
    /// </summary>
    /// <remarks>
    /// In the headless platform, there is a single mouse pointer. There are no helper methods for touch or pen input.
    /// </remarks>
    public static void MouseDown(this TopLevel topLevel, Point point, MouseButton button,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.MouseDown(point, button, modifiers));

    /// <summary>
    /// Simulates a mouse move on the headless window/toplevel.
    /// </summary>
    public static void MouseMove(this TopLevel topLevel, Point point,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.MouseMove(point, modifiers));

    /// <summary>
    /// Simulates a mouse up on the headless window/toplevel.
    /// </summary>
    public static void MouseUp(this TopLevel topLevel, Point point, MouseButton button,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.MouseUp(point, button, modifiers));

    /// <summary>
    /// Simulates a mouse wheel on the headless window/toplevel.
    /// </summary>
    public static void MouseWheel(this TopLevel topLevel, Point point, Vector delta,
        RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.MouseWheel(point, delta, modifiers));

    /// <summary>
    /// Simulates a drag and drop target event on the headless window/toplevel. This event simulates a user moving files from another app to the current app.
    /// </summary>
    public static void DragDrop(this TopLevel topLevel, Point point, RawDragEventType type, IDataObject data,
        DragDropEffects effects, RawInputModifiers modifiers = RawInputModifiers.None) =>
        RunJobsOnImpl(topLevel, w => w.DragDrop(point, type, data, effects, modifiers));

    private static void RunJobsOnImpl(this TopLevel topLevel, Action<IHeadlessWindow> action)
    {
        // Most of input operations that involve hit test require frame to be rendered.
        Dispatcher.UIThread.PulseRenderFrames(1);
        action(GetImpl(topLevel));
        Dispatcher.UIThread.PulseTime(TimeSpan.FromMilliseconds(1));
    }

    private static IHeadlessWindow GetImpl(this TopLevel topLevel)
    {
        return topLevel.PlatformImpl as IHeadlessWindow ??
               throw new InvalidOperationException("TopLevel must be a headless window.");
    }
}
