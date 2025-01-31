using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class ComboBoxPageViewModel : ViewModelBase
    {
        private bool _wrapSelection;

        public bool WrapSelection
        {
            get => _wrapSelection;
            set => this.RaiseAndSetIfChanged(ref _wrapSelection, value);
        }

        private string _textValue = string.Empty;
        public string TextValue
        {
            get => _textValue;
            set => this.RaiseAndSetIfChanged(ref _textValue, value);
        }

        private IdAndName? _selectedItem = null;
        public IdAndName? SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        public ObservableCollection<IdAndName> Values { get; set; } = new ObservableCollection<IdAndName>
        {
            new IdAndName(){ Id = "Id 1", Name = "Name 1" },
            new IdAndName(){ Id = "Id 2", Name = "Name 2" },
            new IdAndName(){ Id = "Id 3", Name = "Name 3" },
            new IdAndName(){ Id = "Id 4", Name = "Name 4" },
            new IdAndName(){ Id = "Id 5", Name = "Name 5" },
        };
    }

    public class IdAndName
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }
}
