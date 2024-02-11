using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class ListBoxPage : UserControl
    {
        private readonly ListBox _listBox;
        private readonly TextBox _searchBox;

        public ListBoxPage()
        {
            InitializeComponent();
            DataContext = new ListBoxPageViewModel();

            _listBox = this.Get<ListBox>("ListBox");

            _searchBox = this.Get<TextBox>("SearchBox");
            _searchBox.TextChanged += OnSearchTextChanged;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            _listBox.Items.Filter = string.IsNullOrEmpty(_searchBox.Text) ? null : FilterItem;
        }

        private void FilterItem(object? sender, ItemSourceViewFilterEventArgs e)
        {
            e.Accept = ((ItemModel)e.Item!).ToString().Contains(_searchBox.Text!);
        }
    }
}
