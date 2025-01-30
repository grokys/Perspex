using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class ListBoxPage : UserControl
    {
        public ListBoxPage()
        {
            InitializeComponent();
            DataContext = new ListBoxPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void FilterItem(object? sender, FunctionItemFilter.FilterEventArgs e)
        {
            if (e.FilterState is not string { Length: > 0 } searchText)
            {
                e.Accept = true;
            }
            else
            {
                var item = (ItemModel)e.Item!;
                e.Accept = item.IsFavorite || item.ID.ToString().Contains(searchText);
            }
        }

        private void SelectItemId(object? sender, ComparableSorter.ComparableSelectEventArgs e) => e.Comparable = ((ItemModel)e.Item!).ID;
        private void SelectItemIsFavorite(object? sender, ComparableSorter.ComparableSelectEventArgs e) => e.Comparable = ((ItemModel)e.Item!).IsFavorite;
    }
}
