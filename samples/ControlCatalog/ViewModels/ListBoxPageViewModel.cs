using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using ControlCatalog.Pages;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class ListBoxPageViewModel : ViewModelBase
    {
        private bool _multiple;
        private bool _toggle;
        private bool _alwaysSelected;
        private bool _autoScrollToSelectedItem = true;
        private bool _wrapSelection;
        private int _counter;
        private IObservable<SelectionMode> _selectionMode;

        public ListBoxPageViewModel()
        {
            Items = new ObservableCollection<ItemModel>(Enumerable.Range(1, 10000).Select(i => GenerateItem()));

            Selection = new SelectionModel<ItemModel>();
            Selection.Select(1);

            _selectionMode = this.WhenAnyValue(
                x => x.Multiple,
                x => x.Toggle,
                x => x.AlwaysSelected,
                (m, t, a) =>
                    (m ? Avalonia.Controls.SelectionMode.Multiple : 0) |
                    (t ? Avalonia.Controls.SelectionMode.Toggle : 0) |
                    (a ? Avalonia.Controls.SelectionMode.AlwaysSelected : 0));

            AddItemCommand = MiniCommand.Create(() =>
                {
                    var item = GenerateItem();
                    Items.Add(item);
                    Selection.Clear();
                    Selection.Select(Items.Count - 1);
                });

            RemoveItemCommand = MiniCommand.Create(() =>
            {
                var items = Selection.SelectedItems.ToList();

                foreach (var item in items)
                {
                    Items.Remove(item!);
                }
            });

            SelectRandomItemCommand = MiniCommand.Create(() =>
            {
                var random = new Random();

                using (Selection.BatchUpdate())
                {
                    Selection.Clear();
                    Selection.Select(random.Next(Items.Count - 1));
                }
            });
        }

        public ObservableCollection<ItemModel> Items { get; }
        public SelectionModel<ItemModel> Selection { get; }
        public IObservable<SelectionMode> SelectionMode => _selectionMode;

        public bool Multiple
        {
            get => _multiple;
            set => this.RaiseAndSetIfChanged(ref _multiple, value);
        }

        public bool Toggle
        {
            get => _toggle;
            set => this.RaiseAndSetIfChanged(ref _toggle, value);
        }

        public bool AlwaysSelected
        {
            get => _alwaysSelected;
            set => this.RaiseAndSetIfChanged(ref _alwaysSelected, value);
        }

        public bool AutoScrollToSelectedItem
        {
            get => _autoScrollToSelectedItem;
            set => this.RaiseAndSetIfChanged(ref _autoScrollToSelectedItem, value);
        }

        public bool WrapSelection
        {
            get => _wrapSelection;
            set => this.RaiseAndSetIfChanged(ref _wrapSelection, value);
        }

        public MiniCommand AddItemCommand { get; }
        public MiniCommand RemoveItemCommand { get; }
        public MiniCommand SelectRandomItemCommand { get; }

        private ItemModel GenerateItem() => new ItemModel(_counter++);
    }

    /// <summary>
    /// An Item model for the <see cref="ListBoxPage"/>
    /// </summary>
    public class ItemModel : INotifyPropertyChanged
    {
        private bool _isFavorite;

        /// <summary>
        /// Creates a new ItemModel with the given ID
        /// </summary>
        /// <param name="id">The ID to display</param>
        public ItemModel(int id)
        {
            ID = id;
        }

        /// <summary>
        /// The ID of this Item
        /// </summary>
        public int ID { get; }

        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                _isFavorite = value;
                PropertyChanged?.Invoke(this, new(nameof(IsFavorite)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public override string ToString()
        {
            return $"Item {ID}";
        }
    }
}
