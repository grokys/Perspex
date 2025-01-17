using Android.OS;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Android.Automation
{
    internal class ToggleNodeInfoProvider : NodeInfoProvider<IToggleProvider>
    {
        public ToggleNodeInfoProvider(ExploreByTouchHelper owner, AutomationPeer peer, int virtualViewId) : 
            base(owner, peer, virtualViewId)
        {
        }

        public override bool PerformNodeAction(int action, Bundle? arguments)
        {
            IToggleProvider provider = GetProvider();
            switch (action)
            {
                case AccessibilityNodeInfoCompat.ActionClick:
                    provider.Toggle();
                    return true;
                default:
                    return false;
            }
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionClick);
            nodeInfo.Clickable = true;

            IToggleProvider provider = GetProvider();
            nodeInfo.Checked = provider.ToggleState == ToggleState.On;
            nodeInfo.Checkable = true;
        }
    }
}
