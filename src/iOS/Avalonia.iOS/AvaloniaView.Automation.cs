using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    public partial class AvaloniaView : IUIAccessibilityContainer
    {
        private readonly AutomationPeerWrapper _accessWrapper;

        public nint AccessibilityElementCount() => 
            _accessWrapper.AccessibilityElementCount();

        public NSObject? GetAccessibilityElementAt(nint index) => 
            _accessWrapper.GetAccessibilityElementAt(index);

        public nint GetIndexOfAccessibilityElement(NSObject element) => 
            _accessWrapper.GetIndexOfAccessibilityElement(element);
    }
}
