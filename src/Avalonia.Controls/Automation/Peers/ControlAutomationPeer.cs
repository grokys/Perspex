using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Automation.Peers
{
    /// <summary>
    /// An automation peer which represents a <see cref="Control"/> element.
    /// </summary>
    public class ControlAutomationPeer : AutomationPeer
    {
        private IReadOnlyList<AutomationPeer>? _children;
        private bool _childrenValid;
        private AutomationPeer? _parent;
        private bool _parentValid;

        public ControlAutomationPeer(Control owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Initialize();
        }

        public Control Owner { get; }

        public AutomationPeer GetOrCreate(Control element)
        {
            if (element == Owner)
                return this;
            return CreatePeerForElement(element);
        }

        /// <summary>
        /// Gets the <see cref="AutomationPeer"/> for a <see cref="Control"/>, creating it if
        /// necessary.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The automation peer.</returns>
        /// <remarks>
        /// Despite the name (which comes from the analogous WPF API), this method does not create
        /// a new peer if one already exists: instead it returns the existing peer.
        /// </remarks>
        public static AutomationPeer CreatePeerForElement(Control element)
        {
            return element.GetOrCreateAutomationPeer();
        }

        /// <summary>
        /// Gets an existing <see cref="AutomationPeer"/> for a <see cref="Control"/>.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The automation peer if already created; otherwise null.</returns>
        /// <remarks>
        /// To ensure that a peer is created, use <see cref="CreatePeerForElement(Control)"/>.
        /// </remarks>
        public static AutomationPeer? FromElement(Control element) => element.GetAutomationPeer();

        protected override void BringIntoViewCore() => Owner.BringIntoView();

        protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore()
        {
            var children = _children ?? Array.Empty<AutomationPeer>();

            if (_childrenValid)
                return children;

            var newChildren = GetChildrenCore() ?? Array.Empty<AutomationPeer>();

            foreach (var peer in children.Except(newChildren))
                peer.TrySetParent(null);
            foreach (var peer in newChildren)
                peer.TrySetParent(this);

            _childrenValid = true;
            return _children = newChildren;
        }

        protected virtual IReadOnlyList<AutomationPeer>? GetChildrenCore()
        {
            var children = Owner.VisualChildren;

            if (children.Count == 0)
                return null;

            var result = new List<AutomationPeer>();

            foreach (var child in children)
            {
                if (child is Control c)
                {
                    var peer = GetOrCreate(c);
                    if (c.IsVisible)
                        result.Add(peer);
                }
            }

            return result;
        }

        protected override AutomationPeer? GetLabeledByCore()
        {
            var label = AutomationProperties.GetLabeledBy(Owner);
            return label is Control c ? GetOrCreate(c) : null;
        }

        protected override string? GetNameCore()
        {
            var result = AutomationProperties.GetName(Owner);

            if (string.IsNullOrWhiteSpace(result) && GetLabeledBy() is AutomationPeer labeledBy)
            {
                result = labeledBy.GetName();
            }

            return result;
        }
        protected override string? GetHelpTextCore()
        {          
            var result = AutomationProperties.GetHelpText(Owner);

            if (string.IsNullOrWhiteSpace(result))
            {
                result = ToolTip.GetTip(Owner) as string;
            }

            return result;          
        }
        protected override AutomationPeer? GetParentCore()
        {
            EnsureConnected();
            return _parent;
        }

        protected override AutomationPeer? GetVisualRootCore()
        {
            if (Owner.GetVisualRoot() is Control c)
                return CreatePeerForElement(c);
            return null;
        }

        /// <summary>
        /// Invalidates the peer's children and causes a re-read from <see cref="GetChildrenCore"/>.
        /// </summary>
        protected void InvalidateChildren()
        {
            _childrenValid = false;
            RaiseChildrenChangedEvent();
        }

        /// <summary>
        /// Invalidates the peer's parent.
        /// </summary>
        protected void InvalidateParent()
        {
            _parent = null;
            _parentValid = false;
        }

        protected override bool ShowContextMenuCore()
        {
            var c = Owner;

            while (c is object)
            {
                if (c.ContextMenu is object)
                {
                    c.ContextMenu.Open(c);
                    return true;
                }

                c = c.Parent as Control;
            }

            return false;
        }

        protected internal override bool TrySetParent(AutomationPeer? parent)
        {
            _parent = parent;
            return true;
        }

        protected override string? GetAcceleratorKeyCore() => AutomationProperties.GetAcceleratorKey(Owner);
        protected override string? GetAccessKeyCore() => AutomationProperties.GetAccessKey(Owner);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
        protected override string? GetAutomationIdCore() => AutomationProperties.GetAutomationId(Owner) ?? Owner.Name;
        protected override Rect GetBoundingRectangleCore() => GetBounds(Owner);
        protected override string GetClassNameCore() => Owner.GetType().Name;
        protected override bool HasKeyboardFocusCore() => Owner.IsFocused;
        protected override bool IsContentElementCore() => true;
        protected override bool IsControlElementCore() => true;
        protected override bool IsEnabledCore() => Owner.IsEffectivelyEnabled;
        protected override bool IsKeyboardFocusableCore() => Owner.Focusable;
        protected override void SetFocusCore() => Owner.Focus();

        protected override AutomationControlType GetControlTypeOverrideCore()
        {
            return AutomationProperties.GetControlTypeOverride(Owner) ?? GetAutomationControlTypeCore();
        }

        protected override bool IsContentElementOverrideCore()
        {
            var view = AutomationProperties.GetAccessibilityView(Owner);
            return view == AccessibilityView.Default ? IsContentElementCore() : view >= AccessibilityView.Content;
        }

        protected override bool IsControlElementOverrideCore()
        {
            var view = AutomationProperties.GetAccessibilityView(Owner);
            return view == AccessibilityView.Default ? IsControlElementCore() : view >= AccessibilityView.Control;
        }

        protected override bool IsOffscreenCore()
        {
            return AutomationProperties.GetIsOffscreenBehavior(Owner) switch
            {
                IsOffscreenBehavior.Onscreen => false,
                IsOffscreenBehavior.Offscreen => true,
                IsOffscreenBehavior.FromClip => Owner.GetTransformedBounds() is not { } bounds ||
                    MathUtilities.IsZero(bounds.Clip.Width) ||
                    MathUtilities.IsZero(bounds.Clip.Height),
                _ => !Owner.IsEffectivelyVisible,
            };
        }

        private static Rect GetBounds(Control control)
        {
            var root = control.GetVisualRoot();

            if (root is not Visual rootVisual)
                return default;

            var transform = control.TransformToVisual(rootVisual);

            if (!transform.HasValue)
                return default;

            return new Rect(control.Bounds.Size).TransformToAABB(transform.Value);
        }

        private void Initialize()
        {
            Owner.PropertyChanged += OwnerPropertyChanged;
            var visualChildren = Owner.VisualChildren;
            visualChildren.CollectionChanged += VisualChildrenChanged;
        }

        private void VisualChildrenChanged(object? sender, EventArgs e) => InvalidateChildren();

        private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Visual.IsVisibleProperty)
            {
                var parent = Owner.GetVisualParent();
                if (parent is Control c)
                    (GetOrCreate(c) as ControlAutomationPeer)?.InvalidateChildren();
            }
            else if (e.Property == Visual.BoundsProperty || 
                     e.Property == Visual.RenderTransformProperty ||
                     e.Property == Visual.RenderTransformOriginProperty)
            {
                RaisePropertyChangedEvent(
                    AutomationElementIdentifiers.BoundingRectangleProperty,
                    null,
                    GetBounds(Owner));
            }
            else if (e.Property == Visual.VisualParentProperty)
            {
                InvalidateParent();
            }
        }


        private void EnsureConnected()
        {
            if (!_parentValid)
            {
                var parent = Owner.GetVisualParent();

                while (parent is object)
                {
                    if (parent is Control c)
                    {
                        var parentPeer = GetOrCreate(c);
                        parentPeer.GetChildren();
                    }

                    parent = parent.GetVisualParent();
                }

                _parentValid = true;
            }
        }
    }
}

