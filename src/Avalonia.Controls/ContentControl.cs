using System.Text;
using Avalonia.Collections;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Displays <see cref="Content"/> according to an <see cref="IDataTemplate"/>.
    /// </summary>
    [TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
    public class ContentControl : TemplatedControl, IContentControl, IContentPresenterHost
    {
        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> ContentProperty =
            AvaloniaProperty.Register<ContentControl, object?>(nameof(Content));

        /// <summary>
        /// Defines the <see cref="ContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
            AvaloniaProperty.Register<ContentControl, IDataTemplate?>(nameof(ContentTemplate));

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            AvaloniaProperty.Register<ContentControl, HorizontalAlignment>(nameof(HorizontalContentAlignment));

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            AvaloniaProperty.Register<ContentControl, VerticalAlignment>(nameof(VerticalContentAlignment));

        static ContentControl()
        {
            TemplateProperty.OverrideDefaultValue<ContentControl>(new FuncControlTemplate((_, ns) => new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                [~BackgroundProperty] = new TemplateBinding(BackgroundProperty),
                [~BackgroundSizingProperty] = new TemplateBinding(BackgroundSizingProperty),
                [~BorderBrushProperty] = new TemplateBinding(BorderBrushProperty),
                [~BorderThicknessProperty] = new TemplateBinding(BorderThicknessProperty),
                [~CornerRadiusProperty] = new TemplateBinding(CornerRadiusProperty),
                [~ContentTemplateProperty] = new TemplateBinding(ContentTemplateProperty),
                [~ContentProperty] = new TemplateBinding(ContentProperty),
                [~PaddingProperty] = new TemplateBinding(PaddingProperty),
                [~VerticalContentAlignmentProperty] = new TemplateBinding(VerticalContentAlignmentProperty),
                [~HorizontalContentAlignmentProperty] = new TemplateBinding(HorizontalContentAlignmentProperty)
            }.RegisterInNameScope(ns)));
        }

        /// <summary>
        /// Gets or sets the content to display.
        /// </summary>
        [Content]
        [DependsOn(nameof(ContentTemplate))]
        public object? Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to display the content of the control.
        /// </summary>
        public IDataTemplate? ContentTemplate
        {
            get => GetValue(ContentTemplateProperty);
            set => SetValue(ContentTemplateProperty, value);
        }

        /// <summary>
        /// Gets the presenter from the control's template.
        /// </summary>
        public ContentPresenter? Presenter
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the control.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get => GetValue(HorizontalContentAlignmentProperty);
            set => SetValue(HorizontalContentAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the control.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get => GetValue(VerticalContentAlignmentProperty);
            set => SetValue(VerticalContentAlignmentProperty, value);
        }

        /// <inheritdoc/>
        IAvaloniaList<ILogical> IContentPresenterHost.LogicalChildren => LogicalChildren;

        /// <inheritdoc/>
        bool IContentPresenterHost.RegisterContentPresenter(ContentPresenter presenter)
        {
            return RegisterContentPresenter(presenter);
        }
        
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ContentProperty)
            {
                ContentChanged(change);
            }
        }

        /// <summary>
        /// Called when an <see cref="ContentPresenter"/> is registered with the control.
        /// </summary>
        /// <param name="presenter">The presenter.</param>
        protected virtual bool RegisterContentPresenter(ContentPresenter presenter)
        {
            if (presenter.Name == "PART_ContentPresenter")
            {
                Presenter = presenter;
                return true;
            }

            return false;
        }

        private void ContentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue is ILogical oldChild)
            {
                LogicalChildren.Remove(oldChild);
            }

            if (e.NewValue is ILogical newChild)
            {
                LogicalChildren.Add(newChild);
            }
        }

        internal override void BuildDebugDisplay(StringBuilder builder, bool includeContent)
        {
            base.BuildDebugDisplay(builder, includeContent);

            if (includeContent)
            {
                DebugDisplayHelper.AppendOptionalValue(builder, nameof(Content), Content, includeContent);
            }
        }
    }
}
