using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    public class AutomationPeerWrapper : UIAccessibilityElement, IUIAccessibilityContainer
    {
        private static readonly IReadOnlyDictionary<AutomationProperty, Action<AutomationPeerWrapper>> s_propertySetters =
            new Dictionary<AutomationProperty, Action<AutomationPeerWrapper>>()
            {
                { AutomationElementIdentifiers.NameProperty, UpdateName },
                { AutomationElementIdentifiers.HelpTextProperty, UpdateHelpText },
                { AutomationElementIdentifiers.BoundingRectangleProperty, UpdateBoundingRectangle },
            };

        private readonly AutomationPeer _peer;

        private List<AutomationPeer> _childrenList;
        private Dictionary<AutomationPeer, AutomationPeerWrapper> _childrenMap;

        public new AvaloniaView AccessibilityContainer
        {
            get => (AvaloniaView)base.AccessibilityContainer!;
            set => base.AccessibilityContainer = value;
        }
        
        [Export("accessibilityContainerType")]
        public UIAccessibilityContainerType AccessibilityContainerType { get; set; }

        public AutomationPeerWrapper(AvaloniaView container, AutomationPeer peer) : base(container)
        {
            AccessibilityContainer = container;
            IsAccessibilityElement = true;

            _peer = peer;
            _peer.ChildrenChanged += PeerChildrenChanged;
            _peer.PropertyChanged += PeerPropertyChanged;
            UpdateProperties();

            _childrenList = new();
            _childrenMap = new();
            UpdateChildren();
        }

        [Export("accessibilityElementCount")]
        public nint AccessibilityElementCount() => 
            _childrenList.Count;

        [Export("accessibilityElementAtIndex:")]
        public NSObject? GetAccessibilityElementAt(nint index)
        {
            try
            {
                return _childrenMap[_childrenList[(int)index]];
            }
            catch (KeyNotFoundException) { }
            catch (ArgumentOutOfRangeException) { }

            return null;
        }

        [Export("indexOfAccessibilityElement:")]
        public nint GetIndexOfAccessibilityElement(NSObject element)
        {
            int indexOf = _childrenList.IndexOf(((AutomationPeerWrapper)element)._peer);
            return indexOf < 0 ? NSRange.NotFound : indexOf;
        }

        private void UpdateProperties()
        {
            foreach (AutomationProperty property in s_propertySetters.Keys)
            {
                s_propertySetters[property](this);
            }
        }

        private void UpdateProperties(params AutomationProperty[] properties)
        {
            foreach (AutomationProperty property in properties
                .Where(s_propertySetters.ContainsKey))
            {
                s_propertySetters[property](this);
            }
        }

        private void UpdateChildren()
        {
            foreach (AutomationPeer child in _peer.GetChildren())
            {
                if (child.IsOffscreen())
                {
                    _childrenList.Remove(child);
                    _childrenMap.Remove(child);
                }
                else if (!_childrenMap.ContainsKey(child))
                {
                    _childrenList.Add(child);
                    _childrenMap.Add(child, new AutomationPeerWrapper(AccessibilityContainer, child));
                }
            }
        }

        private static void UpdateName(AutomationPeerWrapper self)
        {
            self.AccessibilityLabel = self._peer.GetName();
        }

        private static void UpdateHelpText(AutomationPeerWrapper self)
        {
            self.AccessibilityHint = self._peer.GetHelpText();
        }

        private static void UpdateBoundingRectangle(AutomationPeerWrapper self)
        {
            Rect bounds = self._peer.GetBoundingRectangle();
            PixelRect screenRect = new PixelRect(
                self.AccessibilityContainer.TopLevel.PointToScreen(bounds.TopLeft),
                self.AccessibilityContainer.TopLevel.PointToScreen(bounds.BottomRight)
                );
            self.AccessibilityFrame = new CoreGraphics.CGRect(
                screenRect.X, screenRect.Y,
                screenRect.Width, screenRect.Height
                );
        }

        private void PeerChildrenChanged(object? sender, EventArgs e) => UpdateChildren();

        private void PeerPropertyChanged(object? sender, AutomationPropertyChangedEventArgs e) => UpdateProperties(e.Property);
    }
}
