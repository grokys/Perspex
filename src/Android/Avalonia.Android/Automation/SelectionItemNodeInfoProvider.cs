using Android.OS;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Android.Automation
{
    internal class SelectionItemNodeInfoProvider : NodeInfoProvider<ISelectionItemProvider>
    {
        public SelectionItemNodeInfoProvider(ExploreByTouchHelper owner, AutomationPeer peer, int virtualViewId) : 
            base(owner, peer, virtualViewId)
        {
        }

        public override bool PerformNodeAction(int action, Bundle? arguments)
        {
            ISelectionItemProvider provider = GetProvider();
            switch (action)
            {
                case AccessibilityNodeInfoCompat.ActionSelect:
                    provider.Select();
                    return true;
                default:
                    return false;
            }
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionSelect);

            ISelectionItemProvider provider = GetProvider();
            nodeInfo.Selected = provider.IsSelected;
        }
    }
}
