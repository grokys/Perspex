using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for controls which decorate a single child control.
    /// </summary>
    public class Decorator : Control
    {
        /// <summary>
        /// Defines the <see cref="Child"/> property.
        /// </summary>
        public static readonly StyledProperty<Control?> ChildProperty =
            AvaloniaProperty.Register<Decorator, Control?>(nameof(Child));

        /// <summary>
        /// Defines the <see cref="Padding"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> PaddingProperty =
            AvaloniaProperty.Register<Decorator, Thickness>(nameof(Padding));

        /// <summary>
        /// Initializes static members of the <see cref="Decorator"/> class.
        /// </summary>
        static Decorator()
        {
            AffectsMeasure<Decorator>(ChildProperty, PaddingProperty);
            ChildProperty.Changed.AddClassHandler<Decorator>((x, e) => x.ChildChanged(e));
        }

        /// <summary>
        /// Gets or sets the decorated control.
        /// </summary>
        [Content]
        public Control? Child
        {
            get => GetValue(ChildProperty);
            set => SetValue(ChildProperty, value);
        }

        /// <summary>
        /// Gets or sets the padding to place around the <see cref="Child"/> control.
        /// </summary>
        public Thickness Padding
        {
            get => GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            return LayoutHelper.MeasureChild(Child, availableSize, Padding);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            return LayoutHelper.ArrangeChild(Child, finalSize, Padding);
        }

        /// <summary>
        /// Called when the <see cref="Child"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ChildChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldChild = (Control?)e.OldValue;
            var newChild = (Control?)e.NewValue;

            if (oldChild != null)
            {
                ((ISetLogicalParent)oldChild).SetParent(null);
                LogicalChildren.Clear();
                VisualChildren.Remove(oldChild);
            }

            if (newChild != null)
            {
                ((ISetLogicalParent)newChild).SetParent(this);
                VisualChildren.Add(newChild);
                LogicalChildren.Add(newChild);
            }
        }
    }
}
