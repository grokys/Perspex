// The documentation and flag names in this file are initially taken from
// xdg_shell wayland protocol this API is designed after
// therefore, I'm including the license from wayland-protocols repo

/* 
Copyright © 2008-2013 Kristian Høgsberg
Copyright © 2010-2013 Intel Corporation
Copyright © 2013      Rafael Antognolli
Copyright © 2013      Jasper St. Pierre
Copyright © 2014      Jonas Ådahl
Copyright © 2014      Jason Ekstrand
Copyright © 2014-2015 Collabora, Ltd.
Copyright © 2015      Red Hat Inc.

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (including the next
paragraph) shall be included in all copies or substantial portions of the
Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

---

The above is the version of the MIT "Expat" License used by X.org:

    https://cgit.freedesktop.org/xorg/xserver/tree/COPYING
    
    
Adjustments for Avalonia needs:
Copyright © 2019 Nikita Tsukanov
    
    
*/

using System;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Diagnostics;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Avalonia.Media;

namespace Avalonia.Controls.Primitives.PopupPositioning
{
    /// <summary>
    /// Provides positioning parameters to <see cref="IPopupPositioner"/>.
    /// </summary>
    /// <remarks>
    /// The IPopupPositioner provides a collection of rules for the placement of a popup relative
    /// to its parent. Rules can be defined to ensure the popup remains within the visible area's
    /// borders, and to specify how the popup changes its position, such as sliding along an axis,
    /// or flipping around a rectangle. These positioner-created rules are constrained by the
    /// requirement that a popup must intersect with or be at least partially adjacent to its parent
    /// surface.
    /// </remarks>
    [Unstable(ObsoletionMessages.MayBeRemovedInAvalonia12)]
    public record struct PopupPositionerParameters
    {
        private PopupGravity _gravity;
        private PopupAnchor _anchor;

        /// <summary>
        /// Set the size of the popup that is to be positioned with the positioner object, in device-
        /// independent pixels.
        /// </summary>
        public Size Size { get; set; }

        /// <summary>
        /// Specifies the anchor rectangle within the parent that the popup will be placed relative
        /// to, in device-independent pixels.
        /// </summary>
        /// <remarks>
        /// The rectangle is relative to the parent geometry and may not extend outside the window
        /// geometry of the popup's parent.
        /// </remarks>
        public Rect AnchorRectangle { get; set; }

        /// <summary>
        /// Defines the anchor point for the anchor rectangle.
        /// </summary>
        /// <remarks>
        /// The specified anchor is used derive an anchor point that the popup will be positioned
        /// relative to. If a corner anchor is set (e.g. 'TopLeft' or 'BottomRight'), the anchor
        /// point will be at the specified corner; otherwise, the derived anchor point will be
        /// centered on the specified edge, or in the center of the anchor rectangle if no edge is
        /// specified.
        /// </remarks>
        public PopupAnchor Anchor
        {
            get => _anchor;
            set
            {
                PopupPositioningEdgeHelper.ValidateEdge(value);
                _anchor = value;
            }
        }

        /// <summary>
        /// Defines in what direction a popup should be positioned, relative to the anchor point of
        /// the parent.
        /// </summary>
        /// <remarks>
        /// If a corner gravity is specified (e.g. 'BottomRight' or 'TopLeft'), then the popup will
        /// be placed towards the specified gravity; otherwise, the popup will be centered over the
        /// anchor point on any axis that had no gravity specified.
        /// </remarks>
        public PopupGravity Gravity
        {
            get => _gravity;
            set
            {
                PopupPositioningEdgeHelper.ValidateGravity(value);
                _gravity = value;
            }
        }

        /// <summary>
        /// Specify how the popup should be positioned if the originally intended position caused
        /// the popup to be constrained.
        /// </summary>
        /// <remarks>
        /// Adjusts the popup position if the intended position caused the popup to be constrained;
        /// meaning at least partially outside positioning boundaries set by the positioner. The
        /// adjustment is set by constructing a bitmask describing the adjustment to be made when
        /// the popup is constrained on that axis.
        /// 
        /// If no bit for one axis is set, the positioner will assume that the child surface should
        /// not change its position on that axis when constrained.
        /// 
        /// If more than one bit for one axis is set, the order of how adjustments are applied is
        /// specified in the corresponding adjustment descriptions.
        /// 
        /// The default adjustment is none.
        /// </remarks>
        public PopupPositionerConstraintAdjustment ConstraintAdjustment { get; set; }

        /// <summary>
        /// Specify the popup position offset relative to the position of the
        /// anchor on the anchor rectangle and the anchor on the popup.
        /// </summary>
        /// <remarks>
        /// For example if the anchor of the anchor rectangle is at (x, y), the popup has the
        /// gravity bottom|right, and the offset is (ox, oy), the calculated surface position will
        /// be (x + ox, y + oy). The offset position of the surface is the one used for constraint
        /// testing. See set_constraint_adjustment.
        /// 
        /// An example use case is placing a popup menu on top of a user interface element, while
        /// aligning the user interface element of the parent surface with some user interface
        /// element placed somewhere in the popup.
        /// </remarks>
        public Point Offset { get; set; }
    }

    /// <summary>
    /// Defines how a popup position will be adjusted if the unadjusted position would result in
    /// the popup being partly constrained.
    /// </summary>
    /// <remarks>
    /// Whether a popup is considered 'constrained' is left to the positioner to determine. For
    /// example, the popup may be partly outside the target platform defined 'work area', thus
    /// necessitating the popup's position be adjusted until it is entirely inside the work area.
    /// </remarks>
    [Flags]
    public enum PopupPositionerConstraintAdjustment
    {
        /// <summary>
        /// Don't alter the surface position even if it is constrained on some
        /// axis, for example partially outside the edge of an output.
        /// </summary>
        None = 0,

        /// <summary>
        /// Slide the surface along the x axis until it is no longer constrained.
        /// </summary>
        /// <remarks>
        /// First try to slide towards the direction of the gravity on the x axis until either the
        /// edge in the opposite direction of the gravity is unconstrained or the edge in the
        /// direction of the gravity is constrained.
        ///
        /// Then try to slide towards the opposite direction of the gravity on the x axis until
        /// either the edge in the direction of the gravity is unconstrained or the edge in the
        /// opposite direction of the gravity is constrained.
        /// </remarks>
        SlideX = 1,

        /// <summary>
        /// Slide the surface along the y axis until it is no longer constrained.
        /// </summary>
        /// <remarks>
        /// First try to slide towards the direction of the gravity on the y axis until either the
        /// edge in the opposite direction of the gravity is unconstrained or the edge in the
        /// direction of the gravity is constrained.
        /// 
        /// Then try to slide towards the opposite direction of the gravity on the y axis until
        /// either the edge in the direction of the gravity is unconstrained or the edge in the
        /// opposite direction of the gravity is constrained.
        /// </remarks>
        SlideY = 2,

        /// <summary>
        /// Invert the anchor and gravity on the x axis if the surface is constrained on the x axis.
        /// </summary>
        /// <remarks>
        /// For example, if the left edge of the surface is constrained, the gravity is 'left' and
        /// the anchor is 'left', change the gravity to 'right' and the anchor to 'right'.
        /// 
        /// If the adjusted position also ends up being constrained, the resulting position of the
        /// FlipX adjustment will be the one before the adjustment.
        /// </remarks>
        FlipX = 4,

        /// <summary>
        /// Invert the anchor and gravity on the y axis if the surface is constrained on the y axis.
        /// </summary>
        /// <remarks>
        /// For example, if the bottom edge of the surface is constrained, the gravity is 'bottom'
        /// and the anchor is 'bottom', change the gravity to 'top' and the anchor to 'top'.
        /// 
        /// The adjusted position is calculated given the original anchor rectangle and offset, but
        /// with the new flipped anchor and gravity values.
        /// 
        /// If the adjusted position also ends up being constrained, the resulting position of the
        /// FlipY adjustment will be the one before the adjustment.
        /// </remarks>
        FlipY = 8,

        /// <summary>
        /// Horizontally resize the surface
        /// </summary>
        /// <remarks>
        /// Resize the surface horizontally so that it is completely unconstrained.
        /// </remarks>
        ResizeX = 16,

        /// <summary>
        /// Vertically resize the surface
        /// </summary>
        /// <remarks>
        /// Resize the surface vertically so that it is completely unconstrained.
        /// </remarks>
        ResizeY = 32,

        All = SlideX|SlideY|FlipX|FlipY|ResizeX|ResizeY
    }

    static class PopupPositioningEdgeHelper
    {
        public static void ValidateEdge(this PopupAnchor edge)
        {
            if (edge.HasAllFlags(PopupAnchor.Left | PopupAnchor.Right) ||
                edge.HasAllFlags(PopupAnchor.Top | PopupAnchor.Bottom))
                throw new ArgumentException("Opposite edges specified");
        }

        public static void ValidateGravity(this PopupGravity gravity)
        {
            ValidateEdge((PopupAnchor)gravity);
        }

        public static PopupAnchor Flip(this PopupAnchor edge)
        {
            if (edge.HasAnyFlag(PopupAnchor.HorizontalMask))
                edge ^= PopupAnchor.HorizontalMask;

            if (edge.HasAnyFlag(PopupAnchor.VerticalMask))
                edge ^= PopupAnchor.VerticalMask;

            return edge;
        }

        public static PopupAnchor FlipX(this PopupAnchor edge)
        {
            if (edge.HasAnyFlag(PopupAnchor.HorizontalMask))
                edge ^= PopupAnchor.HorizontalMask;
            return edge;
        }
        
        public static PopupAnchor FlipY(this PopupAnchor edge)
        {
            if (edge.HasAnyFlag(PopupAnchor.VerticalMask))
                edge ^= PopupAnchor.VerticalMask;
            return edge;
        }

        public static PopupGravity FlipX(this PopupGravity gravity)
        {
            return (PopupGravity)FlipX((PopupAnchor)gravity);
        }

        public static PopupGravity FlipY(this PopupGravity gravity)
        {
            return (PopupGravity)FlipY((PopupAnchor)gravity);
        }
    }

    /// <summary>
    /// Defines the edges around an anchor rectangle on which a popup will open.
    /// </summary>
    [Flags]
    public enum PopupAnchor
    {
        /// <summary>
        /// The center of the anchor rectangle.
        /// </summary>
        None,

        /// <summary>
        /// The top edge of the anchor rectangle.
        /// </summary>
        Top = 1,

        /// <summary>
        /// The bottom edge of the anchor rectangle.
        /// </summary>
        Bottom = 2,

        /// <summary>
        /// The left edge of the anchor rectangle.
        /// </summary>
        Left = 4,

        /// <summary>
        /// The right edge of the anchor rectangle.
        /// </summary>
        Right = 8,

        /// <summary>
        /// The top-left corner of the anchor rectangle.
        /// </summary>
        TopLeft = Top | Left,

        /// <summary>
        /// The top-right corner of the anchor rectangle.
        /// </summary>
        TopRight = Top | Right,

        /// <summary>
        /// The bottom-left corner of the anchor rectangle.
        /// </summary>
        BottomLeft = Bottom | Left,

        /// <summary>
        /// The bottom-right corner of the anchor rectangle.
        /// </summary>
        BottomRight = Bottom | Right,

        /// <summary>
        /// A mask for the vertical component flags.
        /// </summary>
        VerticalMask = Top | Bottom,

        /// <summary>
        /// A mask for the horizontal component flags.
        /// </summary>
        HorizontalMask = Left | Right,

        /// <summary>
        /// A mask for all flags.
        /// </summary>
        AllMask = VerticalMask|HorizontalMask
    }

    /// <summary>
    /// Defines the direction in which a popup will open.
    /// </summary>
    [Flags]
    public enum PopupGravity
    {
        /// <summary>
        /// The popup will be centered over the anchor edge.
        /// </summary>
        None,

        /// <summary>
        /// The popup will be positioned above the anchor edge
        /// </summary>
        Top = 1,

        /// <summary>
        /// The popup will be positioned below the anchor edge
        /// </summary>
        Bottom = 2,

        /// <summary>
        /// The popup will be positioned to the left of the anchor edge
        /// </summary>
        Left = 4,

        /// <summary>
        /// The popup will be positioned to the right of the anchor edge
        /// </summary>
        Right = 8,

        /// <summary>
        /// The popup will be positioned to the top-left of the anchor edge
        /// </summary>
        TopLeft = Top | Left,

        /// <summary>
        /// The popup will be positioned to the top-right of the anchor edge
        /// </summary>
        TopRight = Top | Right,

        /// <summary>
        /// The popup will be positioned to the bottom-left of the anchor edge
        /// </summary>
        BottomLeft = Bottom | Left,

        /// <summary>
        /// The popup will be positioned to the bottom-right of the anchor edge
        /// </summary>
        BottomRight = Bottom | Right,
    }

    /// <summary>
    /// Positions an <see cref="IPopupHost"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="IPopupPositioner"/> is an abstraction of the wayland xdg_positioner spec.
    /// 
    /// The popup positioner implementation is determined by the platform implementation. A default
    /// managed implementation is provided in <see cref="ManagedPopupPositioner"/> for platforms
    /// on which popups can be arbitrarily positioned.
    /// </remarks>
    [NotClientImplementable]
    public interface IPopupPositioner
    {
        /// <summary>
        /// Updates the position of the associated <see cref="IPopupHost"/> according to the
        /// specified parameters.
        /// </summary>
        /// <param name="parameters">The positioning parameters.</param>
        void Update(PopupPositionerParameters parameters);
    }

    internal static class PopupPositionerExtensions
    {
        public static void Update(
            this IPopupPositioner positioner,
            TopLevel topLevel,
            PopupPositionRequest positionRequest,
            Size popupSize,
            FlowDirection flowDirection)
        {
            if (popupSize == default)
            {
                return;
            }

            var parameters = BuildParameters(topLevel, positionRequest, popupSize, flowDirection);
            positioner.Update(parameters);
        }

        private static PopupPositionerParameters BuildParameters(
            TopLevel topLevel,
            PopupPositionRequest positionRequest,
            Size popupSize,
            FlowDirection flowDirection)
        {
            PopupPositionerParameters positionerParameters = default;
            positionerParameters.Offset = positionRequest.Offset;
            positionerParameters.Size = popupSize;
            positionerParameters.ConstraintAdjustment = positionRequest.ConstraintAdjustment;
            if (positionRequest.Placement == PlacementMode.Pointer)
            {
                // We need a better way for tracking the last pointer position
                var position = topLevel.PointToClient(topLevel.LastPointerPosition ?? default);

                positionerParameters.AnchorRectangle = new Rect(position, new Size(1, 1));
                positionerParameters.Anchor = PopupAnchor.TopLeft;
                positionerParameters.Gravity = PopupGravity.BottomRight;
            }
            else if (positionRequest.Placement == PlacementMode.Custom)
            {
                if (positionRequest.PlacementCallback is null)
                    throw new InvalidOperationException(
                        "CustomPopupPlacementCallback property must be set, when Placement=PlacementMode.Custom");

                positionerParameters.AnchorRectangle = CalculateAnchorRect(topLevel, positionRequest);

                var customPlacementParameters = new CustomPopupPlacement(
                    popupSize,
                    positionRequest.Target)
                {
                    AnchorRectangle = positionerParameters.AnchorRectangle,
                    Anchor = positionerParameters.Anchor,
                    Gravity = positionerParameters.Gravity,
                    ConstraintAdjustment = positionerParameters.ConstraintAdjustment,
                    Offset = positionerParameters.Offset
                };

                positionRequest.PlacementCallback.Invoke(customPlacementParameters);

                positionerParameters.AnchorRectangle = customPlacementParameters.AnchorRectangle;
                positionerParameters.Anchor = customPlacementParameters.Anchor;
                positionerParameters.Gravity = customPlacementParameters.Gravity;
                positionerParameters.ConstraintAdjustment = customPlacementParameters.ConstraintAdjustment;
                positionerParameters.Offset = customPlacementParameters.Offset;
            }
            else
            {
                positionerParameters.AnchorRectangle = CalculateAnchorRect(topLevel, positionRequest);

                var parameters = positionRequest.Placement switch
                {
                    PlacementMode.Bottom => (PopupAnchor.Bottom, PopupGravity.Bottom),
                    PlacementMode.Right => (PopupAnchor.Right, PopupGravity.Right),
                    PlacementMode.Left => (PopupAnchor.Left, PopupGravity.Left),
                    PlacementMode.Top => (PopupAnchor.Top, PopupGravity.Top),
                    PlacementMode.Center => (PopupAnchor.None, PopupGravity.None),
                    PlacementMode.AnchorAndGravity => (positionRequest.Anchor, positionRequest.Gravity),
                    PlacementMode.TopEdgeAlignedRight => (PopupAnchor.TopRight, PopupGravity.TopLeft),
                    PlacementMode.TopEdgeAlignedLeft => (PopupAnchor.TopLeft, PopupGravity.TopRight),
                    PlacementMode.BottomEdgeAlignedLeft => (PopupAnchor.BottomLeft, PopupGravity.BottomRight),
                    PlacementMode.BottomEdgeAlignedRight => (PopupAnchor.BottomRight, PopupGravity.BottomLeft),
                    PlacementMode.LeftEdgeAlignedTop => (PopupAnchor.TopLeft, PopupGravity.BottomLeft),
                    PlacementMode.LeftEdgeAlignedBottom => (PopupAnchor.BottomLeft, PopupGravity.TopLeft),
                    PlacementMode.RightEdgeAlignedTop => (PopupAnchor.TopRight, PopupGravity.BottomRight),
                    PlacementMode.RightEdgeAlignedBottom => (PopupAnchor.BottomRight, PopupGravity.TopRight),
                    _ => throw new ArgumentOutOfRangeException(nameof(positionRequest.Placement), positionRequest.Placement,
                        "Invalid value for Popup.PlacementMode")
                };
                positionerParameters.Anchor = parameters.Item1;
                positionerParameters.Gravity = parameters.Item2;
            }

            // Invert coordinate system if FlowDirection is RTL
            if (flowDirection == FlowDirection.RightToLeft)
            {
                if ((positionerParameters.Anchor & PopupAnchor.Right) == PopupAnchor.Right)
                {
                    positionerParameters.Anchor ^= PopupAnchor.Right;
                    positionerParameters.Anchor |= PopupAnchor.Left;
                }
                else if ((positionerParameters.Anchor & PopupAnchor.Left) == PopupAnchor.Left)
                {
                    positionerParameters.Anchor ^= PopupAnchor.Left;
                    positionerParameters.Anchor |= PopupAnchor.Right;
                }

                if ((positionerParameters.Gravity & PopupGravity.Right) == PopupGravity.Right)
                {
                    positionerParameters.Gravity ^= PopupGravity.Right;
                    positionerParameters.Gravity |= PopupGravity.Left;
                }
                else if ((positionerParameters.Gravity & PopupGravity.Left) == PopupGravity.Left)
                {
                    positionerParameters.Gravity ^= PopupGravity.Left;
                    positionerParameters.Gravity |= PopupGravity.Right;
                }
            }

            return positionerParameters;
        }

        private static Rect CalculateAnchorRect(TopLevel topLevel, PopupPositionRequest positionRequest)
        {
            var target = positionRequest.Target;
            if (target == null)
                throw new InvalidOperationException("Placement mode is not Pointer and PlacementTarget is null");
            Matrix? matrix = target.TransformToVisual(topLevel);

            if (matrix == null)
            {
                if (target.GetVisualRoot() == null)
                    throw new InvalidOperationException("Target control is not attached to the visual tree");
                throw new InvalidOperationException("Target control is not in the same tree as the popup parent");
            }

            var bounds = new Rect(default, target.Bounds.Size);
            var anchorRect = positionRequest.AnchorRect ?? bounds;
            return anchorRect.Intersect(bounds).TransformToAABB(matrix.Value);
        }
    }

}
