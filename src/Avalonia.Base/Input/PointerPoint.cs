using Avalonia.Input.Raw;

namespace Avalonia.Input
{
    /// <summary>
    /// Provides basic properties for the input pointer associated with a single mouse, pen/stylus, or touch contact.
    /// </summary>
    public struct PointerPoint
    {
        public PointerPoint(IPointer pointer, Point position, PointerPointProperties properties)
        {
            Pointer = pointer;
            Position = position;
            Properties = properties;
        }

        /// <summary>
        /// Gets specific pointer generated by input device.
        /// </summary>
        public IPointer Pointer { get; }

        /// <summary>
        /// Gets extended information about the input pointer.
        /// </summary>
        public PointerPointProperties Properties { get; }

        /// <summary>
        /// Gets the location of the pointer input in client coordinates.
        /// </summary>
        public Point Position { get; }
    }

    /// <summary>
    /// Provides extended properties for a PointerPoint object.
    /// </summary>
    public record PointerPointProperties
    {
        /// <summary>
        /// Gets a value that indicates whether the pointer input was triggered by the primary action mode of an input device.
        /// </summary>
        public bool IsLeftButtonPressed { get; }

        /// <summary>
        /// Gets a value that indicates whether the pointer input was triggered by the tertiary action mode of an input device.
        /// </summary>
        public bool IsMiddleButtonPressed { get; }

        /// <summary>
        /// Gets a value that indicates whether the pointer input was triggered by the secondary action mode (if supported) of an input device.
        /// </summary>
        public bool IsRightButtonPressed { get; }

        /// <summary>
        /// Gets a value that indicates whether the pointer input was triggered by the first extended mouse button (XButton1).
        /// </summary>
        public bool IsXButton1Pressed { get; }

        /// <summary>
        /// Gets a value that indicates whether the pointer input was triggered by the second extended mouse button (XButton2).
        /// </summary>
        public bool IsXButton2Pressed { get; }

        /// <summary>
        /// Gets a value that indicates whether the barrel button of the pen/stylus device is pressed.
        /// </summary>
        public bool IsBarrelButtonPressed { get; }

        /// <summary>
        /// Gets a value that indicates whether the input is from a pen eraser.
        /// </summary>
        public bool IsEraser { get; }

        /// <summary>
        /// Gets a value that indicates whether the digitizer pen is inverted.
        /// </summary>
        public bool IsInverted { get; }

        /// <summary>
        /// Gets the clockwise rotation in degrees of a pen device around its own major axis (such as when the user spins the pen in their fingers).
        /// </summary>
        /// <returns>
        /// A value between 0.0 and 359.0 in degrees of rotation. The default value is 0.0.
        /// </returns>
        public float Twist { get; }

        /// <summary>
        /// Gets a value that indicates the force that the pointer device (typically a pen/stylus) exerts on the surface of the digitizer.
        /// </summary>
        /// <returns>
        /// A value from 0 to 1.0. The default value is 0.5.
        /// </returns>
        public float Pressure { get; } = 0.5f;

        /// <summary>
        /// Gets the plane angle between the Y-Z plane and the plane that contains the Y axis and the axis of the input device (typically a pen/stylus).
        /// </summary>
        /// <returns>
        /// The value is 0.0 when the finger or pen is perpendicular to the digitizer surface, between 0.0 and 90.0 when tilted to the right of perpendicular, and between 0.0 and -90.0 when tilted to the left of perpendicular. The default value is 0.0.
        /// </returns>
        public float XTilt { get; }

        /// <summary>
        /// Gets the plane angle between the X-Z plane and the plane that contains the X axis and the axis of the input device (typically a pen/stylus).
        /// </summary>
        /// <returns>
        /// The value is 0.0 when the finger or pen is perpendicular to the digitizer surface, between 0.0 and 90.0 when tilted towards the user, and between 0.0 and -90.0 when tilted away from the user. The default value is 0.0.
        /// </returns>
        public float YTilt { get; }

        /// <summary>
        /// Gets the kind of pointer state change.
        /// </summary>
        public PointerUpdateKind PointerUpdateKind { get; } = PointerUpdateKind.LeftButtonPressed;

        public PointerPointProperties()
        {
        }

        public PointerPointProperties(RawInputModifiers modifiers, PointerUpdateKind kind)
        {
            PointerUpdateKind = kind;

            IsLeftButtonPressed = modifiers.HasAllFlags(RawInputModifiers.LeftMouseButton);
            IsMiddleButtonPressed = modifiers.HasAllFlags(RawInputModifiers.MiddleMouseButton);
            IsRightButtonPressed = modifiers.HasAllFlags(RawInputModifiers.RightMouseButton);
            IsXButton1Pressed = modifiers.HasAllFlags(RawInputModifiers.XButton1MouseButton);
            IsXButton2Pressed = modifiers.HasAllFlags(RawInputModifiers.XButton2MouseButton);
            IsInverted = modifiers.HasAllFlags(RawInputModifiers.PenInverted);
            IsEraser = modifiers.HasAllFlags(RawInputModifiers.PenEraser);
            IsBarrelButtonPressed = modifiers.HasAllFlags(RawInputModifiers.PenBarrelButton);

            // The underlying input source might be reporting the previous state,
            // so make sure that we reflect the current state

            if (kind == PointerUpdateKind.LeftButtonPressed)
                IsLeftButtonPressed = true;
            if (kind == PointerUpdateKind.LeftButtonReleased)
                IsLeftButtonPressed = false;
            if (kind == PointerUpdateKind.MiddleButtonPressed)
                IsMiddleButtonPressed = true;
            if (kind == PointerUpdateKind.MiddleButtonReleased)
                IsMiddleButtonPressed = false;
            if (kind == PointerUpdateKind.RightButtonPressed)
                IsRightButtonPressed = true;
            if (kind == PointerUpdateKind.RightButtonReleased)
                IsRightButtonPressed = false;
            if (kind == PointerUpdateKind.XButton1Pressed)
                IsXButton1Pressed = true;
            if (kind == PointerUpdateKind.XButton1Released)
                IsXButton1Pressed = false;
            if (kind == PointerUpdateKind.XButton2Pressed)
                IsXButton2Pressed = true;
            if (kind == PointerUpdateKind.XButton2Released)
                IsXButton2Pressed = false;
        }

        public PointerPointProperties(RawInputModifiers modifiers, PointerUpdateKind kind,
            float twist, float pressure, float xTilt, float yTilt
            ) : this (modifiers, kind)
        {
            Twist = twist;
            Pressure = pressure;
            XTilt = xTilt;
            YTilt = yTilt;
        }

        internal PointerPointProperties(PointerPointProperties basedOn, RawPointerPoint rawPoint)
        {
            IsLeftButtonPressed = basedOn.IsLeftButtonPressed;
            IsMiddleButtonPressed = basedOn.IsMiddleButtonPressed;
            IsRightButtonPressed = basedOn.IsRightButtonPressed;
            IsXButton1Pressed = basedOn.IsXButton1Pressed;
            IsXButton2Pressed = basedOn.IsXButton2Pressed;
            IsInverted = basedOn.IsInverted;
            IsEraser = basedOn.IsEraser;
            IsBarrelButtonPressed = basedOn.IsBarrelButtonPressed;

            Twist = rawPoint.Twist;
            Pressure = rawPoint.Pressure;
            XTilt = rawPoint.XTilt;
            YTilt = rawPoint.YTilt;
        }

        public static PointerPointProperties None { get; } = new PointerPointProperties();
    }

    public enum PointerUpdateKind
    {
        LeftButtonPressed,
        MiddleButtonPressed,
        RightButtonPressed,
        XButton1Pressed,
        XButton2Pressed,
        LeftButtonReleased,
        MiddleButtonReleased,
        RightButtonReleased,
        XButton1Released,
        XButton2Released,
        Other
    }

    public static class PointerUpdateKindExtensions
    {
        public static MouseButton GetMouseButton(this PointerUpdateKind kind)
        {
            if (kind == PointerUpdateKind.LeftButtonPressed || kind == PointerUpdateKind.LeftButtonReleased)
                return MouseButton.Left;
            if (kind == PointerUpdateKind.MiddleButtonPressed || kind == PointerUpdateKind.MiddleButtonReleased)
                return MouseButton.Middle;
            if (kind == PointerUpdateKind.RightButtonPressed || kind == PointerUpdateKind.RightButtonReleased)
                return MouseButton.Right;
            if (kind == PointerUpdateKind.XButton1Pressed || kind == PointerUpdateKind.XButton1Released)
                return MouseButton.XButton1;
            if (kind == PointerUpdateKind.XButton2Pressed || kind == PointerUpdateKind.XButton2Released)
                return MouseButton.XButton2;
            return MouseButton.None;
        }
    }
}
