using System;
using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    public partial class AvaloniaView : IUIAccessibilityContainer
    {   
        private readonly List<AutomationPeer> _childrenList = new();
        private readonly Dictionary<AutomationPeer, AutomationPeerWrapper> _childrenMap = new();

        [Export("accessibilityContainerType")]
        public UIAccessibilityContainerType AccessibilityContainerType { get; set; } = 
            UIAccessibilityContainerType.SemanticGroup;

        [Export("accessibilityElementCount")]
        public nint AccessibilityElementCount() 
        {
            UpdateChildren(_accessWrapper);
            return _childrenList.Count;
        }

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
            int indexOf = _childrenList.IndexOf((AutomationPeerWrapper)element);
            return indexOf < 0 ? NSRange.NotFound : indexOf;
        }

        internal void UpdateChildren(AutomationPeer peer)
        {
            foreach (AutomationPeer child in peer.GetChildren())
            {
                if ((child.GetName().Length == 0 &&
                    !child.IsKeyboardFocusable()) ||
                    child.IsOffscreen())
                {
                    _childrenList.Remove(child);
                    _childrenMap.Remove(child);
                }
                else if (!_childrenMap.TryGetValue(child, out AutomationPeerWrapper? wrapper))
                {
                    _childrenList.Add(child);
                    _childrenMap.Add(child, new(this, child));
                }
                else
                {
                    wrapper.UpdateProperties();
                    wrapper.UpdateTraits();
                }
                UpdateChildren(child);
            }
        }
    }
}
