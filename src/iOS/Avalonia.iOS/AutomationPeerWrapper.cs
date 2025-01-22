using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    public class AutomationPeerWrapper : UIAccessibilityElement
    {
        private static readonly IReadOnlyDictionary<AutomationProperty, Action<AutomationPeerWrapper>> s_propertySetters =
            new Dictionary<AutomationProperty, Action<AutomationPeerWrapper>>()
            {
                { AutomationElementIdentifiers.NameProperty, UpdateName },
                { AutomationElementIdentifiers.HelpTextProperty, UpdateHelpText },
                { AutomationElementIdentifiers.BoundingRectangleProperty, UpdateBoundingRectangle },
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

        public void UpdateProperties()
        {
            UpdateProperties(s_propertySetters.Keys.ToArray());
        }

        public void UpdateProperties(params AutomationProperty[] properties)
        {
            AccessibilityRespondsToUserInteraction = _peer.IsEnabled();

            foreach (AutomationProperty property in properties
                .Where(s_propertySetters.ContainsKey))
            {
                s_propertySetters[property](this);
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
                self._view.TopLevel.PointToScreen(bounds.TopLeft),
                self._view.TopLevel.PointToScreen(bounds.BottomRight)
                );
            self.AccessibilityFrame = new CoreGraphics.CGRect(
                screenRect.X, screenRect.Y,
                screenRect.Width, screenRect.Height
                );
        }

        private void PeerChildrenChanged(object? sender, EventArgs e) => _view.UpdateChildren(_peer);

        private void PeerPropertyChanged(object? sender, AutomationPropertyChangedEventArgs e) => UpdateProperties(e.Property);

        public static implicit operator AutomationPeer(AutomationPeerWrapper instance) 
        {
            return instance._peer;
        }
    }
}
