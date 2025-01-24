using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    internal class AutomationPeerWrapper : UIAccessibilityElement
    {
        private static readonly IReadOnlyDictionary<AutomationProperty, Action<AutomationPeerWrapper>> s_propertySetters =
            new Dictionary<AutomationProperty, Action<AutomationPeerWrapper>>()
            {
                { AutomationElementIdentifiers.NameProperty, UpdateName },
                { AutomationElementIdentifiers.HelpTextProperty, UpdateHelpText },
                { AutomationElementIdentifiers.BoundingRectangleProperty, UpdateBoundingRectangle },

                { RangeValuePatternIdentifiers.IsReadOnlyProperty, UpdateIsReadOnly },
                { RangeValuePatternIdentifiers.ValueProperty, UpdateValue },

                { ValuePatternIdentifiers.IsReadOnlyProperty, UpdateIsReadOnly },
                { ValuePatternIdentifiers.ValueProperty, UpdateValue },
            };

        private readonly AvaloniaView _view;
        private readonly AutomationPeer _peer;

        public AutomationPeerWrapper(AvaloniaView view, AutomationPeer? peer = null) : base(view)
        {
            _view = view;
            _peer = peer ?? ControlAutomationPeer.CreatePeerForElement(view.TopLevel);

            _peer.PropertyChanged += PeerPropertyChanged;
            _peer.ChildrenChanged += PeerChildrenChanged;

            AccessibilityContainer = _view;
            AccessibilityIdentifier = _peer.GetAutomationId();
        }

        private static void UpdateName(AutomationPeerWrapper self)
        {
            AutomationPeer peer = self;
            self.AccessibilityLabel = peer.GetName();
        }

        private static void UpdateHelpText(AutomationPeerWrapper self)
        {
            AutomationPeer peer = self;
            self.AccessibilityHint = peer.GetHelpText();
        }

        private static void UpdateBoundingRectangle(AutomationPeerWrapper self)
        {
            AutomationPeer peer = self;
            Rect bounds = peer.GetBoundingRectangle();
            PixelRect screenRect = new PixelRect(
                self._view.TopLevel.PointToScreen(bounds.TopLeft),
                self._view.TopLevel.PointToScreen(bounds.BottomRight)
                );
            self.AccessibilityFrame = new CGRect(
                screenRect.X, screenRect.Y,
                screenRect.Width, screenRect.Height
                );
        }

        private static void UpdateIsReadOnly(AutomationPeerWrapper self)
        {
            AutomationPeer peer = self;
            self.AccessibilityRespondsToUserInteraction =
                peer.GetProvider<IValueProvider>()?.IsReadOnly ??
                peer.GetProvider<IRangeValueProvider>()?.IsReadOnly ??
                peer.IsEnabled();
        }

        private static void UpdateValue(AutomationPeerWrapper self)
        {
            AutomationPeer peer = self;
            self.AccessibilityValue = 
                peer.GetProvider<IRangeValueProvider>()?.Value.ToString("0.##") ?? 
                peer.GetProvider<IValueProvider>()?.Value;
            UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement, self);
        }

        private void PeerChildrenChanged(object? sender, EventArgs e)
        {
            _view.UpdateChildren(_peer);
            UIAccessibility.PostNotification(UIAccessibilityPostNotification.ScreenChanged, this);
        }

        private void PeerPropertyChanged(object? sender, AutomationPropertyChangedEventArgs e) => UpdateProperties(e.Property);

        private void UpdateProperties(params AutomationProperty[] properties)
        {
            Action<AutomationPeerWrapper>? setter =
                Delegate.Combine(properties
                .Where(s_propertySetters.ContainsKey)
                .Select(x => s_propertySetters[x])
                .Distinct()
                .ToArray()) as Action<AutomationPeerWrapper>;
            setter?.Invoke(this);
        }

        public void UpdateProperties()
        {
            bool canFocusAtAll = _peer.IsContentElement() && !_peer.IsOffscreen();
            IsAccessibilityElement = canFocusAtAll;
            AccessibilityRespondsToUserInteraction =
                canFocusAtAll && _peer.IsKeyboardFocusable();

            UpdateProperties(s_propertySetters.Keys.ToArray());
        }

        public void UpdateTraits()
        {
            UIAccessibilityTrait traits = UIAccessibilityTrait.None;

            switch (_peer.GetAutomationControlType())
            {
                case AutomationControlType.Button:
                    traits |= UIAccessibilityTrait.Button;
                    break;
                case AutomationControlType.Header:
                    traits |= UIAccessibilityTrait.Header;
                    break;
                case AutomationControlType.Hyperlink:
                    traits |= UIAccessibilityTrait.Link;
                    break;
                case AutomationControlType.Image:
                    traits |= UIAccessibilityTrait.Image;
                    break;
            }

            if (_peer.GetProvider<IRangeValueProvider>()?.IsReadOnly == false)
            {
                traits |= UIAccessibilityTrait.Adjustable;
            }

            if (_peer.GetProvider<ISelectionItemProvider>()?.IsSelected == true)
            {
                traits |= UIAccessibilityTrait.Selected;
            }

            if (_peer.GetProvider<IValueProvider>()?.IsReadOnly == false)
            {
                traits |= UIAccessibilityTrait.UpdatesFrequently;
            }

            if (_peer.IsEnabled() == false)
            {
                traits |= UIAccessibilityTrait.NotEnabled;
            }

            AccessibilityTraits = (ulong)traits;
        }

        [Export("accessibilityActivate")]
        public bool AccessibilityActivate() 
        {
            IToggleProvider? toggleProvider = _peer.GetProvider<IToggleProvider>();
            IInvokeProvider? invokeProvider = _peer.GetProvider<IInvokeProvider>();
            if (toggleProvider is not null)
            {
                toggleProvider.Toggle();
                return true;
            }
            else if (invokeProvider is not null)
            {
                invokeProvider.Invoke();
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool AccessibilityElementIsFocused()
        {
            base.AccessibilityElementIsFocused();
            return _peer.HasKeyboardFocus();
        }

        public override void AccessibilityElementDidBecomeFocused()
        {
            base.AccessibilityElementDidBecomeFocused();
            _peer.SetFocus();
        }

        public override void AccessibilityDecrement()
        {
            base.AccessibilityDecrement();
            IRangeValueProvider? provider = _peer.GetProvider<IRangeValueProvider>();
            if (provider is not null)
            {
                double value = provider.Value;
                provider.SetValue(value - provider.SmallChange);
            }
        }

        public override void AccessibilityIncrement()
        {
            base.AccessibilityIncrement();
            IRangeValueProvider? provider = _peer.GetProvider<IRangeValueProvider>();
            if (provider is not null)
            {
                double value = provider.Value;
                provider.SetValue(value + provider.SmallChange);
            }
        }

        public override bool AccessibilityScroll(UIAccessibilityScrollDirection direction)
        {
            base.AccessibilityScroll(direction);
            IScrollProvider? scrollProvider = _peer.GetProvider<IScrollProvider>();
            if (scrollProvider is not null)
            {
                bool didScroll;
                ScrollAmount verticalAmount, horizontalAmount;
                switch (direction)
                {
                    case UIAccessibilityScrollDirection.Up:
                        verticalAmount = ScrollAmount.SmallIncrement;
                        horizontalAmount = ScrollAmount.NoAmount;
                        didScroll = true;
                        break;
                    case UIAccessibilityScrollDirection.Down:
                        verticalAmount = ScrollAmount.SmallDecrement;
                        horizontalAmount = ScrollAmount.NoAmount;
                        didScroll = true;
                        break;
                    case UIAccessibilityScrollDirection.Left:
                        verticalAmount = ScrollAmount.NoAmount;
                        horizontalAmount = ScrollAmount.SmallIncrement;
                        didScroll = true;
                        break;
                    case UIAccessibilityScrollDirection.Right:
                        verticalAmount = ScrollAmount.NoAmount;
                        horizontalAmount = ScrollAmount.SmallDecrement;
                        didScroll = true;
                        break;
                    default:
                        verticalAmount = ScrollAmount.NoAmount;
                        horizontalAmount = ScrollAmount.NoAmount;
                        didScroll = false;
                        break;
                }

                scrollProvider.Scroll(verticalAmount, horizontalAmount);
                if (didScroll)
                {
                    UIAccessibility.PostNotification(UIAccessibilityPostNotification.PageScrolled, this);
                    return true;
                }
            }
            return false;
        }

        public static implicit operator AutomationPeer(AutomationPeerWrapper instance)
        {
            return instance._peer;
        }
    }
}
