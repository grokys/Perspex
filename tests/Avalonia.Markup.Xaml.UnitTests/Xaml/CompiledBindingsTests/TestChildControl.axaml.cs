using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml.CompiledBindingsTests;

public partial class TestChildControl : UserControl
{
    public TestChildControl()
    {
        InitializeComponent();
    }

    internal void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        TestListBox = this.Get<ListBox>("TestListBox");
    }

    public ListBox TestListBox { get; private set; }
}
