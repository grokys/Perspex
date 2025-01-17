using Android.OS;
using AndroidX.Core.View.Accessibility;

namespace Avalonia.Android.Automation
{
    public interface INodeInfoProvider
    {
        int VirtualViewId { get; }

        bool PerformNodeAction(int action, Bundle? arguments);

        void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo);
    }
}
