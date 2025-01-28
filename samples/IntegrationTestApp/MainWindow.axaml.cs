using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using IntegrationTestApp.Models;
using IntegrationTestApp.Pages;
using IntegrationTestApp.ViewModels;

namespace IntegrationTestApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // Set name in code behind, so source generator will ignore it.
            Name = "MainWindow";

            InitializeComponent();

            var viewModel = new MainWindowViewModel(CreatePages());
            InitializeViewMenu(viewModel.Pages);

            DataContext = viewModel;
            AppOverlayPopups.Text = Program.OverlayPopups ? "Overlay Popups" : "Native Popups";
            PositionChanged += OnPositionChanged;
        }

        private MainWindowViewModel? ViewModel => (MainWindowViewModel?)DataContext;

        private void InitializeViewMenu(IEnumerable<Page> pages)
        {
            var viewMenu = (NativeMenuItem?)NativeMenu.GetMenu(this)?.Items[1];

            foreach (var page in pages)
            {
                var menuItem = new NativeMenuItem
                {
                    Header = (string?)page.Name,
                    ToolTip = $"Tip:{(string?)page.Name}",
                    ToggleType = NativeMenuItemToggleType.Radio,
                };

                menuItem.Click += (_, _) =>
                {
                    if (ViewModel is { } viewModel)
                        viewModel.SelectedPage = page;
                };

                viewMenu?.Menu?.Items.Add(menuItem);
            }
        }

        private void Pager_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (Pager.SelectedItem is Page page)
                PagerContent.Child = page.CreateContent();
        }

        private void OnPositionChanged(object? sender, PixelPointEventArgs e)
        {
            // HACK: Toggling the window decorations can cause the window to be moved off screen, 
            // causing test failures. Until this bug is fixed, detect this and move the window
            // to the screen origin. See #11411.
            if (Screens.ScreenFromWindow(this) is { } screen)
            {
                var bounds = new PixelRect(
                    e.Point,
                    PixelSize.FromSize(ClientSize, DesktopScaling));

                if (!screen.WorkingArea.Contains(bounds))
                    Position = screen.WorkingArea.Position;
            }
        }

        private static IEnumerable<Page> CreatePages()
        {
            return
            [
                new("Automation", () => new AutomationPage()),
                new("Button", () => new ButtonPage()),
                new("CheckBox", () => new CheckBoxPage()),
                new("ComboBox", () => new ComboBoxPage()),
                new("ContextMenu", () => new ContextMenuPage()),
                new("DesktopPage", () => new DesktopPage()),
                new("Embedding", () => new EmbeddingPage()),
                new("Gestures", () => new GesturesPage()),
                new("ListBox", () => new ListBoxPage()),
                new("Menu", () => new MenuPage()),
                new("Pointer", () => new PointerPage()),
                new("RadioButton", () => new RadioButtonPage()),
                new("Screens", () => new ScreensPage()),
                new("ScrollBar", () => new ScrollBarPage()),
                new("Slider", () => new SliderPage()),
                new("Window Decorations", () => new WindowDecorationsPage()),
                new("Window", () => new WindowPage()),
                new("Popups", () => new PopupsPage()),
            ];
        }
    }
}
