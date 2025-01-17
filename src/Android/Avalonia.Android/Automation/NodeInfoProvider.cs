using System;
using Android.OS;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Avalonia.Automation;
using Avalonia.Automation.Peers;

namespace Avalonia.Android.Automation
{
    internal delegate INodeInfoProvider NodeInfoProviderInitializer(ExploreByTouchHelper owner, AutomationPeer peer, int virtualViewId);

    internal abstract class NodeInfoProvider<T> : INodeInfoProvider
    {
        private readonly ExploreByTouchHelper _owner;

        private readonly AutomationPeer _peer;

        public int VirtualViewId { get; }

        public NodeInfoProvider(ExploreByTouchHelper owner, AutomationPeer peer, int virtualViewId)
        {
            _owner = owner;
            _peer = peer;
            VirtualViewId = virtualViewId;

            _peer.PropertyChanged += PeerPropertyChanged;
        }

        protected void InvalidateSelf()
        {
            _owner.InvalidateVirtualView(VirtualViewId);
        }

        protected void InvalidateSelf(int changeTypes)
        {
            _owner.InvalidateVirtualView(VirtualViewId, changeTypes);
        }

        protected virtual void PeerPropertyChanged(object? sender, AutomationPropertyChangedEventArgs e) { }

        public T GetProvider() => _peer.GetProvider<T>() ??
            throw new InvalidOperationException($"Peer instance does not implement {nameof(T)}.");

        public abstract bool PerformNodeAction(int action, Bundle? arguments);

        public abstract void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo);
    }
}
