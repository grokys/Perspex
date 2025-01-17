using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml.CompiledBindingsTests;

public partial class TestWindow : Window
{
    public TestWindow()
    {
        InitializeComponent();
    }

    internal void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        TestRootControlInstance = this.Get<TestRootControl>("TestRootControlInstance");
    }

    public TestRootControl TestRootControlInstance { get; private set; }
}
