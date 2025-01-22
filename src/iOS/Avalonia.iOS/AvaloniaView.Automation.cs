using System;
using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    public partial class AvaloniaView : IUIAccessibilityContainer
    {
        private readonly AutomationPeerWrapper _accessWrapper;

        private readonly List<AutomationPeer> _childrenList;
        private readonly Dictionary<AutomationPeer, AutomationPeerWrapper> _childrenMap;

        [Export("accessibilityContainerType")]
        public UIAccessibilityContainerType AccessibilityContainerType { get; set; }

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
            int indexOf = _childrenList.IndexOf((AutomationPeerWrapper)element);
            return indexOf < 0 ? NSRange.NotFound : indexOf;
        }

        internal void UpdateChildren(AutomationPeer peer)
        {
            foreach (AutomationPeer child in peer.GetChildren())
            {
                if (!_childrenMap.ContainsKey(child))
                {
                    AutomationPeerWrapper wrapper = new (this, child);

                    _childrenList.Add(child);
                    _childrenMap.Add(child, wrapper);
                }
            }
        }
    }
}
