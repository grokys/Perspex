using Android.OS;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Android.Automation
{
    internal class ExpandCollapseNodeInfoProvider : NodeInfoProvider<IExpandCollapseProvider>
    {
        public ExpandCollapseNodeInfoProvider(ExploreByTouchHelper owner, AutomationPeer peer, int virtualViewId) : 
            base(owner, peer, virtualViewId)
        {
        }

        public override bool PerformNodeAction(int action, Bundle? arguments)
        {
            IExpandCollapseProvider provider = GetProvider();
            switch (action)
            {
                case AccessibilityNodeInfoCompat.ActionExpand:
                    provider.Expand();
                    return true;
                case AccessibilityNodeInfoCompat.ActionCollapse:
                    provider.Collapse();
                    return true;
                default:
                    return false;
            }
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionExpand);
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionCollapse);
        }
    }
}
