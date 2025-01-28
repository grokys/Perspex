using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml.CompiledBindingsTests;

public partial class TestRootControl : UserControl
{
    public TestRootControl()
    {
        InitializeComponent();
    }

    internal void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        TestChildControlInstance = this.Get<TestChildControl>("TestChildControlInstance");
    }

    public TestChildControl TestChildControlInstance { get; private set; }
}
