using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    public partial class AvaloniaView : IUIAccessibilityContainer
    {
        private readonly AutomationPeerWrapper _accessWrapper;
        
        [Export("accessibilityContainerType")]
        public UIAccessibilityContainerType AccessibilityContainerType 
        {
            get => _accessWrapper.AccessibilityContainerType;
            set => _accessWrapper.AccessibilityContainerType = value;
        }

        [Export("accessibilityElements")]
        public NSObject? AccessibilityElements 
        { 
            get => _accessWrapper.AccessibilityElements;
            set => _accessWrapper.AccessibilityElements = value;
        }

        [Export("accessibilityElementCount")]
        public nint AccessibilityElementCount() => 
            _accessWrapper.AccessibilityElementCount();

        [Export("accessibilityElementAtIndex:")]
        public NSObject? GetAccessibilityElementAt(nint index) => 
            _accessWrapper.GetAccessibilityElementAt(index);

        [Export("indexOfAccessibilityElement:")]
        public nint GetIndexOfAccessibilityElement(NSObject element) => 
            _accessWrapper.GetIndexOfAccessibilityElement(element);
    }
}
