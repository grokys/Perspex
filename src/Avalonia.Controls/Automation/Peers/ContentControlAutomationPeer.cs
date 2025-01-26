using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class ContentControlAutomationPeer : ControlAutomationPeer
    {
        protected ContentControlAutomationPeer(ContentControl owner)
            : base(owner) 
        { 
        }

        public new ContentControl Owner => (ContentControl)base.Owner;

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

        protected override string? GetNameCore()
        {
            Control? childControl = Owner.Presenter?.Child;
            AutomationPeer? childPeer = childControl is null ? null :
                CreatePeerForElement(childControl);
            return base.GetNameCore() ?? (childControl as TextBlock)?.Text ?? 
                childPeer?.GetName() ?? Owner.Content?.ToString();
        }

        protected override string? GetHelpTextCore()
        {
            Control? childControl = Owner.Presenter?.Child;
            AutomationPeer? childPeer = childControl is null ? null :
                CreatePeerForElement(childControl);
            return base.GetHelpTextCore() ?? 
                childPeer?.GetHelpText();
        }

        protected override bool IsContentElementCore() => false;
        protected override bool IsControlElementCore() => false;
    }
}
