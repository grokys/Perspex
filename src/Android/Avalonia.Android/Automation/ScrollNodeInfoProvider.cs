using Android.OS;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Android.Automation
{
    internal class ScrollNodeInfoProvider : NodeInfoProvider<IScrollProvider>
    {
        public ScrollNodeInfoProvider(ExploreByTouchHelper owner, AutomationPeer peer, int virtualViewId) : 
            base(owner, peer, virtualViewId)
        {
        }

        public override bool PerformNodeAction(int action, Bundle? arguments)
        {
            IScrollProvider provider = GetProvider();
            switch (action)
            {
                case AccessibilityNodeInfoCompat.ActionScrollForward:
                    if (provider.VerticallyScrollable)
                    {
                        provider.Scroll(ScrollAmount.NoAmount, ScrollAmount.SmallIncrement);
                    }
                    else if(provider.HorizontallyScrollable)
                    {
                        provider.Scroll(ScrollAmount.SmallIncrement, ScrollAmount.NoAmount);
                    }
                    return true;
                case AccessibilityNodeInfoCompat.ActionScrollBackward:
                    if (provider.VerticallyScrollable)
                    {
                        provider.Scroll(ScrollAmount.NoAmount, ScrollAmount.SmallDecrement);
                    }
                    else if (provider.HorizontallyScrollable)
                    {
                        provider.Scroll(ScrollAmount.SmallDecrement, ScrollAmount.NoAmount);
                    }
                    return true;
                default:
                    return false;
            }
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionScrollForward);
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionScrollBackward);
            nodeInfo.Scrollable = true;
        }
    }
}
