using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Avalonia.Android.Automation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Java.Lang;

namespace Avalonia.Android
{
    internal class AvaloniaAccessHelper : ExploreByTouchHelper
    {
        private const string AUTOMATION_PROVIDER_NAMESPACE = "Avalonia.Automation.Provider";

        private static readonly IReadOnlyDictionary<string, NodeInfoProviderInitializer>
            s_providerTypeInitializers = new Dictionary<string, NodeInfoProviderInitializer>()
            {
                { typeof(IExpandCollapseProvider).FullName!, (owner, peer, id) => new ExpandCollapseNodeInfoProvider(owner, peer, id) },
                { typeof(IInvokeProvider).FullName!, (owner, peer, id) => new InvokeNodeInfoProvider(owner, peer, id) },
                { typeof(IRangeValueProvider).FullName!, (owner, peer, id) => new RangeValueNodeInfoProvider(owner, peer, id) },
                { typeof(IScrollProvider).FullName!, (owner, peer, id) => new ScrollNodeInfoProvider(owner, peer, id) },
                { typeof(ISelectionItemProvider).FullName!, (owner, peer, id) => new SelectionItemNodeInfoProvider(owner, peer, id) },
                { typeof(IToggleProvider).FullName!, (owner, peer, id) => new ToggleNodeInfoProvider(owner, peer, id) },
                { typeof(IValueProvider).FullName!, (owner, peer, id) => new ValueNodeInfoProvider(owner, peer, id) },
            };

        private readonly Dictionary<int, AutomationPeer> _peers;
        private readonly Dictionary<AutomationPeer, int> _peerIds;

        private readonly Dictionary<AutomationPeer, HashSet<INodeInfoProvider>> _peerNodeInfoProviders;

        private readonly AvaloniaView _view;

        public AvaloniaAccessHelper(AvaloniaView view) : base(view)
        {
            _peers = [];
            _peerIds = [];
            _peerNodeInfoProviders = [];

            AutomationPeer rootPeer = ControlAutomationPeer.CreatePeerForElement(view.TopLevel!);
            GetOrCreateNodeInfoProvidersFromPeer(rootPeer, out int _);

            _view = view;
        }

        private HashSet<INodeInfoProvider>? GetNodeInfoProvidersFromVirtualViewId(int virtualViewId)
        {
            if (_peers.TryGetValue(virtualViewId, out AutomationPeer? peer) &&
                _peerNodeInfoProviders.TryGetValue(peer, out HashSet<INodeInfoProvider>? nodeInfoProviders))
            {
                return nodeInfoProviders;
            }
            else
            {
                return null;
            }
        }

        private HashSet<INodeInfoProvider> GetOrCreateNodeInfoProvidersFromPeer(AutomationPeer peer, out int virtualViewId)
        {
            int peerViewId;
            if (_peerNodeInfoProviders.TryGetValue(peer, out HashSet<INodeInfoProvider>? nodeInfoProviders))
            {
                peerViewId = _peerIds[peer];
            }
            else
            {
                peerViewId = _peerNodeInfoProviders.Count;
                _peers.Add(peerViewId, peer);
                _peerIds.Add(peer, peerViewId);

                nodeInfoProviders = new();
                _peerNodeInfoProviders.Add(peer, nodeInfoProviders);

                peer.ChildrenChanged += (s, ev) => InvalidateVirtualView(peerViewId,
                    AccessibilityEventCompat.ContentChangeTypeSubtree);
                peer.PropertyChanged += (s, ev) =>
                {
                    if (ev.Property == AutomationElementIdentifiers.NameProperty)
                    {
                        InvalidateVirtualView(peerViewId, AccessibilityEventCompat.ContentChangeTypeText);
                    }
                    else if (ev.Property == AutomationElementIdentifiers.HelpTextProperty)
                    {
                        InvalidateVirtualView(peerViewId, AccessibilityEventCompat.ContentChangeTypeContentDescription);
                    }
                    else if (ev.Property == AutomationElementIdentifiers.BoundingRectangleProperty || 
                        ev.Property == AutomationElementIdentifiers.ClassNameProperty)
                    {
                        InvalidateVirtualView(peerViewId);
                    }
                };

                Type peerType = peer.GetType();
                IEnumerable<Type> providerTypes = peerType.GetInterfaces()
                    .Where(x => x.Namespace!.StartsWith(AUTOMATION_PROVIDER_NAMESPACE));
                foreach (Type providerType in providerTypes)
                {
                    if (s_providerTypeInitializers.TryGetValue(providerType.FullName!, out NodeInfoProviderInitializer? ctor))
                    {
                        INodeInfoProvider nodeInfoProvider = ctor(this, peer, peerViewId);
                        nodeInfoProviders.Add(nodeInfoProvider);
                    }
                }
            }

            virtualViewId = peerViewId;
            return nodeInfoProviders;
        }

        protected override int GetVirtualViewAt(float x, float y)
        {
            Point p = _view.TopLevelImpl.PointToClient(new PixelPoint((int)x, (int)y));
            IEmbeddedRootProvider? embeddedRootProvider = _peers[0].GetProvider<IEmbeddedRootProvider>();
            AutomationPeer? peer = embeddedRootProvider?.GetPeerFromPoint(p);
            if (peer is not null)
            {
                GetOrCreateNodeInfoProvidersFromPeer(peer, out int virtualViewId);
                return virtualViewId == 0 ? InvalidId : virtualViewId;
            }
            else
            {
                peer = embeddedRootProvider?.GetFocus();
                return peer is null ? InvalidId : _peerIds[peer];
            }
        }

        protected override void GetVisibleVirtualViews(IList<Integer>? virtualViewIds)
        {
            if (virtualViewIds is null)
            {
                return;
            }

            foreach (AutomationPeer peer in _peers[0].GetChildren())
            {
                GetOrCreateNodeInfoProvidersFromPeer(peer, out int virtualViewId);
                virtualViewIds.Add(Integer.ValueOf(virtualViewId));
            }
        }

        protected override bool OnPerformActionForVirtualView(int virtualViewId, int action, Bundle? arguments)
        {
            return (GetNodeInfoProvidersFromVirtualViewId(virtualViewId) ?? [])
                .Select(x => x.PerformNodeAction(action, arguments))
                .Aggregate(false, (a, b) => a | b);
        }

        protected override void OnPopulateNodeForVirtualView(int virtualViewId, AccessibilityNodeInfoCompat nodeInfo)
        {
            if (!_peers.TryGetValue(virtualViewId, out AutomationPeer? peer))
            {
                return; // BAIL!! No work to be done
            }

            // UI logical structure
            foreach (AutomationPeer child in peer.GetChildren())
            {
                GetOrCreateNodeInfoProvidersFromPeer(child, out int childId);
                nodeInfo.AddChild(_view, childId);
            }

            // UI labels
            AutomationPeer? labeledBy = peer.GetLabeledBy();
            if (labeledBy is not null)
            {
                GetOrCreateNodeInfoProvidersFromPeer(labeledBy, out int labeledById);
                nodeInfo.SetLabeledBy(_view, labeledById);
            }

            // UI debug metadata
            nodeInfo.ClassName = peer.GetClassName();
            nodeInfo.UniqueId = peer.GetAutomationId();

            // Common control state
            nodeInfo.Enabled = peer.IsEnabled();

            // Control focus state
            bool canFocusAtAll = peer.IsContentElement() && !peer.IsOffscreen();
            nodeInfo.ScreenReaderFocusable = canFocusAtAll;
            nodeInfo.Focusable = canFocusAtAll && peer.IsKeyboardFocusable();

            nodeInfo.AccessibilityFocused = peer.HasKeyboardFocus();
            nodeInfo.Focused = peer.HasKeyboardFocus();

            // On-screen bounds
            Rect bounds = peer.GetBoundingRectangle();
            PixelRect screenRect = new PixelRect(
                _view.TopLevelImpl.PointToScreen(bounds.TopLeft),
                _view.TopLevelImpl.PointToScreen(bounds.BottomRight)
                );
            nodeInfo.SetBoundsInParent(new(
                screenRect.X, screenRect.Y,
                screenRect.Right, screenRect.Bottom
                ));

            // UI provider specifics
            foreach (INodeInfoProvider nodeInfoProvider in _peerNodeInfoProviders[peer])
            {
                nodeInfoProvider.PopulateNodeInfo(nodeInfo);
            }

            // Control text contents
            nodeInfo.Text ??= peer.GetName();
            nodeInfo.ContentDescription ??= peer.GetHelpText();
        }
    }
}
