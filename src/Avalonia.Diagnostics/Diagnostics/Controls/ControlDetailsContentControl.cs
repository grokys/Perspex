using System;
using Avalonia.Controls;

namespace Avalonia.Diagnostics.Controls
{
    internal class ControlDetailsContentControl : ContentControl
    {
        protected override Type StyleKeyOverride => typeof(ContentControl);

        internal override bool AddContentToLogicalTree => false;
    }
}
