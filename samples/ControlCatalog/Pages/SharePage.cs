using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using static System.Net.Mime.MediaTypeNames;

namespace ControlCatalog.Pages
{
    public class SharePage : UserControl
    {
        private IReadOnlyList<IStorageFile> _files;

        public SharePage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            this.Get<Button>("ShareText").Click += (o, e) =>
            {
                var text = this.Get<TextBox>("ShareBox").Text ?? string.Empty;

                var share = (VisualRoot as TopLevel).Share;

                share.Share(text);
            };

            this.Get<Button>("OpenFile").Click += async delegate
            {
                var storageProvider = (VisualRoot as TopLevel).StorageProvider;

                var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
                {
                    Title = "Open file",
                    AllowMultiple = this.Get<CheckBox>("SelectMultiple").IsChecked.Value,
                });

                _files = result;

                string list = string.Empty;

                foreach ( var file in _files )
                {
                    list += file.Name + "\n";   
                }

                this.Get<TextBlock>("FileText").Text = list;
            };

            this.Get<Button>("ShareFile").Click += (o, e) =>
            {
                if(_files != null && _files.Count> 0)
                {
                    var share = (VisualRoot as TopLevel).Share;

                    share.Share(_files.ToList());
                }
            };
        }
    }
}
