using System.Collections;
using System.ComponentModel;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    public class MenuFlyout : PopupFlyoutBase
    {
        public MenuFlyout()
        {
            Items = new ItemCollection(this);
        }

        /// <summary>
        /// Defines the <see cref="ItemsSource"/> property
        /// </summary>
        public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
            AvaloniaProperty.Register<MenuFlyout, IEnumerable?>(
                nameof(ItemsSource));

        /// <summary>
        /// Defines the <see cref="ItemTemplate"/> property
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
            AvaloniaProperty.Register<MenuFlyout, IDataTemplate?>(nameof(ItemTemplate));

        /// <summary>
        /// Defines the <see cref="ItemContainerTheme"/> property.
        /// </summary>
        public static readonly StyledProperty<ControlTheme?> ItemContainerThemeProperty =
            ItemsControl.ItemContainerThemeProperty.AddOwner<MenuFlyout>();

        /// <summary>
        /// Defines the <see cref="FlyoutPresenterTheme"/> property.
        /// </summary>
        public static readonly StyledProperty<ControlTheme?> FlyoutPresenterThemeProperty =
            Flyout.FlyoutPresenterThemeProperty.AddOwner<MenuFlyout>();
        
        public Classes FlyoutPresenterClasses => _classes ??= new Classes();

        [Content]
        public ItemCollection Items { get; }

        /// <summary>
        /// Gets or sets the items of the MenuFlyout
        /// </summary>
        public IEnumerable? ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the template used for the items
        /// </summary>
        public IDataTemplate? ItemTemplate
        {
            get => GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="ControlTheme"/> that is applied to the container element generated for each item.
        /// </summary>
        public ControlTheme? ItemContainerTheme
        {
            get => GetValue(ItemContainerThemeProperty);
            set => SetValue(ItemContainerThemeProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="ControlTheme"/> that is applied to the container element generated for the flyout presenter.
        /// </summary>
        public ControlTheme? FlyoutPresenterTheme
        {
            get => GetValue(FlyoutPresenterThemeProperty); 
            set => SetValue(FlyoutPresenterThemeProperty, value);
        }
        
        private Classes? _classes;

        protected override Control CreatePresenter()
        {
            return new MenuFlyoutPresenter
            {
                ItemsSource = Items,
                [!ItemsControl.ItemTemplateProperty] = this[!ItemTemplateProperty],
                [!ItemsControl.ItemContainerThemeProperty] = this[!ItemContainerThemeProperty],
            };
        }

        protected override void OnOpening(CancelEventArgs args)
        {
            if (Popup.Child is { } presenter)
            {
                if (_classes != null)
                {
                    SetPresenterClasses(presenter, FlyoutPresenterClasses);
                }

                if (FlyoutPresenterTheme is { } theme)
                {
                    presenter.SetValue(Control.ThemeProperty, theme);
                }
            }

            base.OnOpening(args);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ItemsSourceProperty)
                Items.SetItemsSource(change.GetNewValue<IEnumerable?>());
        }
    }
}
