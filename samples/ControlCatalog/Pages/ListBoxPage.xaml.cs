using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class ListBoxPage : UserControl
    {
        private readonly TextBox _searchBox;

        public ListBoxPage()
        {
            InitializeComponent();
            _searchBox = this.Get<TextBox>("SearchBox");

            DataContext = new ListBoxPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void FilterItem(object? sender, FunctionItemFilter.FilterEventArgs e)
        {
            if (string.IsNullOrEmpty(_searchBox.Text))
            {
                e.Accept = true;
            }
            else
            {
                var item = (ItemModel)e.Item!;
                e.Accept = item.IsFavorite || item.ID.ToString().Contains(_searchBox.Text!);
            }
        }
    }
}
