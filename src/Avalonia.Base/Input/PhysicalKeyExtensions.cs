namespace Avalonia.Input;

/// <summary>
/// Contains extension methods related to <see cref="PhysicalKey"/>.
/// </summary>
public static class PhysicalKeyExtensions
{
    /// <summary>
    /// Maps a physical key to a corresponding key, if possible, on a QWERTY keyboard.
    /// </summary>
    /// <param name="physicalKey">the physical key to map.</param>
    /// <returns>The key corresponding to <paramref name="physicalKey"/>, or <see cref="Key.None"/>.</returns>
    public static Key ToQwertyKey(this PhysicalKey physicalKey)
        => physicalKey switch
        {
            PhysicalKey.None => Key.None,

            // Writing System Keys
            PhysicalKey.Backquote => Key.Oem3,
            PhysicalKey.Backslash => Key.Oem5,
            PhysicalKey.BracketLeft => Key.Oem4,
            PhysicalKey.BracketRight => Key.Oem6,
            PhysicalKey.Comma => Key.OemComma,
            PhysicalKey.Digit0 => Key.D0,
            PhysicalKey.Digit1 => Key.D1,
            PhysicalKey.Digit2 => Key.D2,
            PhysicalKey.Digit3 => Key.D3,
            PhysicalKey.Digit4 => Key.D4,
            PhysicalKey.Digit5 => Key.D5,
            PhysicalKey.Digit6 => Key.D6,
            PhysicalKey.Digit7 => Key.D7,
            PhysicalKey.Digit8 => Key.D8,
            PhysicalKey.Digit9 => Key.D9,
            PhysicalKey.Equal => Key.OemMinus,
            PhysicalKey.IntlBackslash => Key.Oem102,
            PhysicalKey.IntlRo => Key.Oem102,
            PhysicalKey.IntlYen => Key.Oem5,
            PhysicalKey.A => Key.A,
            PhysicalKey.B => Key.B,
            PhysicalKey.C => Key.C,
            PhysicalKey.D => Key.D,
            PhysicalKey.E => Key.E,
            PhysicalKey.F => Key.F,
            PhysicalKey.G => Key.G,
            PhysicalKey.H => Key.H,
            PhysicalKey.I => Key.I,
            PhysicalKey.J => Key.J,
            PhysicalKey.K => Key.K,
            PhysicalKey.L => Key.L,
            PhysicalKey.M => Key.M,
            PhysicalKey.N => Key.N,
            PhysicalKey.O => Key.O,
            PhysicalKey.P => Key.P,
            PhysicalKey.Q => Key.Q,
            PhysicalKey.R => Key.R,
            PhysicalKey.S => Key.S,
            PhysicalKey.T => Key.T,
            PhysicalKey.U => Key.U,
            PhysicalKey.V => Key.V,
            PhysicalKey.W => Key.W,
            PhysicalKey.X => Key.X,
            PhysicalKey.Y => Key.Y,
            PhysicalKey.Z => Key.Z,
            PhysicalKey.Minus => Key.OemMinus,
            PhysicalKey.Period => Key.OemPeriod,
            PhysicalKey.Quote => Key.Oem7,
            PhysicalKey.Semicolon => Key.Oem1,
            PhysicalKey.Slash => Key.Oem2,

            // Functional Keys
            PhysicalKey.AltLeft => Key.LeftAlt,
            PhysicalKey.AltRight => Key.RightAlt,
            PhysicalKey.Backspace => Key.Back,
            PhysicalKey.CapsLock => Key.CapsLock,
            PhysicalKey.ContextMenu => Key.Apps,
            PhysicalKey.ControlLeft => Key.LeftCtrl,
            PhysicalKey.ControlRight => Key.RightCtrl,
            PhysicalKey.Enter => Key.Enter,
            PhysicalKey.MetaLeft => Key.LWin,
            PhysicalKey.MetaRight => Key.RWin,
            PhysicalKey.ShiftLeft => Key.LeftShift,
            PhysicalKey.ShiftRight => Key.RightShift,
            PhysicalKey.Space => Key.Space,
            PhysicalKey.Tab => Key.Tab,
            PhysicalKey.Convert => Key.ImeConvert,
            PhysicalKey.KanaMode => Key.KanaMode,
            PhysicalKey.Lang1 => Key.HangulMode,
            PhysicalKey.Lang2 => Key.HanjaMode,
            PhysicalKey.Lang3 => Key.DbeKatakana,
            PhysicalKey.Lang4 => Key.DbeHiragana,
            PhysicalKey.Lang5 => Key.OemAuto,
            PhysicalKey.NonConvert => Key.ImeNonConvert,

            // Control Pad Section
            PhysicalKey.Delete => Key.Delete,
            PhysicalKey.End => Key.End,
            PhysicalKey.Help => Key.Help,
            PhysicalKey.Home => Key.Home,
            PhysicalKey.Insert => Key.Insert,
            PhysicalKey.PageDown => Key.PageDown,
            PhysicalKey.PageUp => Key.PageUp,

            // Arrow Pad Section
            PhysicalKey.ArrowDown => Key.Down,
            PhysicalKey.ArrowLeft => Key.Left,
            PhysicalKey.ArrowRight => Key.Right,
            PhysicalKey.ArrowUp => Key.Up,

            // Numpad Section
            PhysicalKey.NumLock => Key.NumLock,
            PhysicalKey.NumPad0 => Key.NumPad0,
            PhysicalKey.NumPad1 => Key.NumPad1,
            PhysicalKey.NumPad2 => Key.NumPad2,
            PhysicalKey.NumPad3 => Key.NumPad3,
            PhysicalKey.NumPad4 => Key.NumPad4,
            PhysicalKey.NumPad5 => Key.NumPad5,
            PhysicalKey.NumPad6 => Key.NumPad6,
            PhysicalKey.NumPad7 => Key.NumPad7,
            PhysicalKey.NumPad8 => Key.NumPad8,
            PhysicalKey.NumPad9 => Key.NumPad9,
            PhysicalKey.NumPadAdd => Key.Add,
            PhysicalKey.NumPadClear => Key.Clear,
            PhysicalKey.NumPadComma => Key.AbntC2,
            PhysicalKey.NumPadDecimal => Key.Decimal,
            PhysicalKey.NumPadDivide => Key.Divide,
            PhysicalKey.NumPadEnter => Key.Enter,
            PhysicalKey.NumPadEqual => Key.OemPlus,
            PhysicalKey.NumPadMultiply => Key.Multiply,
            PhysicalKey.NumPadParenLeft => Key.Oem4,
            PhysicalKey.NumPadParenRight => Key.Oem6,
            PhysicalKey.NumPadSubtract => Key.Subtract,

            // Function Section
            PhysicalKey.Escape => Key.Escape,
            PhysicalKey.F1 => Key.F1,
            PhysicalKey.F2 => Key.F2,
            PhysicalKey.F3 => Key.F3,
            PhysicalKey.F4 => Key.F4,
            PhysicalKey.F5 => Key.F5,
            PhysicalKey.F6 => Key.F6,
            PhysicalKey.F7 => Key.F7,
            PhysicalKey.F8 => Key.F8,
            PhysicalKey.F9 => Key.F9,
            PhysicalKey.F10 => Key.F10,
            PhysicalKey.F11 => Key.F11,
            PhysicalKey.F12 => Key.F12,
            PhysicalKey.F13 => Key.F13,
            PhysicalKey.F14 => Key.F14,
            PhysicalKey.F15 => Key.F15,
            PhysicalKey.F16 => Key.F16,
            PhysicalKey.F17 => Key.F17,
            PhysicalKey.F18 => Key.F18,
            PhysicalKey.F19 => Key.F19,
            PhysicalKey.F20 => Key.F20,
            PhysicalKey.F21 => Key.F21,
            PhysicalKey.F22 => Key.F22,
            PhysicalKey.F23 => Key.F23,
            PhysicalKey.F24 => Key.F24,
            PhysicalKey.PrintScreen => Key.PrintScreen,
            PhysicalKey.ScrollLock => Key.Scroll,
            PhysicalKey.Pause => Key.Pause,

            // Media Keys
            PhysicalKey.BrowserBack => Key.BrowserBack,
            PhysicalKey.BrowserFavorites => Key.BrowserFavorites,
            PhysicalKey.BrowserForward => Key.BrowserForward,
            PhysicalKey.BrowserHome => Key.BrowserHome,
            PhysicalKey.BrowserRefresh => Key.BrowserRefresh,
            PhysicalKey.BrowserSearch => Key.BrowserSearch,
            PhysicalKey.BrowserStop => Key.BrowserStop,
            PhysicalKey.Eject => Key.None,
            PhysicalKey.LaunchApp1 => Key.LaunchApplication1,
            PhysicalKey.LaunchApp2 => Key.LaunchApplication2,
            PhysicalKey.LaunchMail => Key.LaunchMail,
            PhysicalKey.MediaPlayPause => Key.MediaPlayPause,
            PhysicalKey.MediaSelect => Key.SelectMedia,
            PhysicalKey.MediaStop => Key.MediaStop,
            PhysicalKey.MediaTrackNext => Key.MediaNextTrack,
            PhysicalKey.MediaTrackPrevious => Key.MediaPreviousTrack,
            PhysicalKey.Power => Key.None,
            PhysicalKey.Sleep => Key.Sleep,
            PhysicalKey.AudioVolumeDown => Key.VolumeDown,
            PhysicalKey.AudioVolumeMute => Key.VolumeMute,
            PhysicalKey.AudioVolumeUp => Key.VolumeUp,
            PhysicalKey.WakeUp => Key.None,

            // Legacy Keys
            PhysicalKey.Again => Key.None,
            PhysicalKey.Copy => Key.OemCopy,
            PhysicalKey.Cut => Key.None,
            PhysicalKey.Find => Key.None,
            PhysicalKey.Open => Key.None,
            PhysicalKey.Paste => Key.None,
            PhysicalKey.Props => Key.None,
            PhysicalKey.Select => Key.Select,
            PhysicalKey.Undo => Key.None,

            _ => Key.None
        };
}
