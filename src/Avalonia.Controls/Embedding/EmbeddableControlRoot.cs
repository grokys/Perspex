using System;
using System.ComponentModel;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Automation;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.Controls.Embedding
{
    public class EmbeddableControlRoot : TopLevel, IFocusScope, IDisposable
    {
        public EmbeddableControlRoot(ITopLevelImpl impl) : base(impl)
        {
        }

        public EmbeddableControlRoot() : base(PlatformManager.CreateEmbeddableTopLevel())
        {
        }

        protected bool EnforceClientSize { get; set; } = true;

        public void Prepare()
        {
            EnsureInitialized();
            ApplyTemplate();
            LayoutManager.ExecuteInitialLayoutPass();
        }

        public new void StartRendering() => base.StartRendering();

        public new void StopRendering() => base.StopRendering();
        
        private void EnsureInitialized()
        {
            if (!this.IsInitialized)
            {
                var init = (ISupportInitialize)this;
                init.BeginInit();
                init.EndInit();
            }
        }
        
        protected override Size MeasureOverride(Size availableSize)
        {
            if (EnforceClientSize)
                availableSize = PlatformImpl?.ClientSize ?? default(Size);
            var rv = base.MeasureOverride(availableSize);
            if (EnforceClientSize)
                return availableSize;
            return rv;
        }

        protected override Type StyleKeyOverride => typeof(EmbeddableControlRoot);

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new EmbeddableControlRootAutomationPeer(this);
        }

        public void Dispose()
        {
            PlatformImpl?.Dispose();
            LayoutManager?.Dispose();
        }
    }
}
