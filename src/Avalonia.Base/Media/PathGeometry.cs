using System;
using Avalonia.Collections;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Visuals.Platform;

namespace Avalonia.Media
{
    public class PathGeometry : StreamGeometry
    {
        /// <summary>
        /// Defines the <see cref="Figures"/> property.
        /// </summary>
        public static readonly DirectProperty<PathGeometry, PathFigures?> FiguresProperty =
            AvaloniaProperty.RegisterDirect<PathGeometry, PathFigures?>(nameof(Figures), g => g.Figures, (g, f) => g.Figures = f);

        /// <summary>
        /// Defines the <see cref="FillRule"/> property.
        /// </summary>
        public static readonly StyledProperty<FillRule> FillRuleProperty =
                                 AvaloniaProperty.Register<PathGeometry, FillRule>(nameof(FillRule));

        private PathFigures? _figures;
        private IDisposable? _figuresObserver;
        private IDisposable? _figuresPropertiesObserver;

        static PathGeometry()
        {
            FiguresProperty.Changed.AddClassHandler<PathGeometry>((s, e) =>
                s.OnFiguresChanged(e.NewValue as PathFigures));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PathGeometry"/> class.
        /// </summary>
        public PathGeometry()
        {
            Figures = new PathFigures();
        }

        /// <summary>
        /// Parses the specified path data to a <see cref="PathGeometry"/>.
        /// </summary>
        /// <param name="pathData">The s.</param>
        /// <returns></returns>
        public new static PathGeometry Parse(string pathData)
        {
            var pathGeometry = new PathGeometry();

            using (var context = new PathGeometryContext(pathGeometry))
            using (var parser = new PathMarkupParser(context))
            {
                parser.Parse(pathData);
            }

            return pathGeometry;
        }

        /// <summary>
        /// Gets or sets the figures.
        /// </summary>
        /// <value>
        /// The figures.
        /// </value>
        [Content]
        public PathFigures? Figures
        {
            get { return _figures; }
            set { SetAndRaise(FiguresProperty, ref _figures, value); }
        }

        /// <summary>
        /// Gets or sets the fill rule.
        /// </summary>
        /// <value>
        /// The fill rule.
        /// </value>
        public FillRule FillRule
        {
            get { return GetValue(FillRuleProperty); }
            set { SetValue(FillRuleProperty, value); }
        }

        private protected sealed override IGeometryImpl? CreateDefiningGeometry()
        {
            var figures = Figures;

            if (figures is null)
                return null;

            var factory = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
            var geometry = factory.CreateStreamGeometry();

            using (var ctx = new StreamGeometryContext(geometry.Open()))
            {
                ctx.SetFillRule(FillRule);
                foreach (var f in figures)
                {
                    f.ApplyTo(ctx);
                }
            }

            return geometry;
        }

        private void OnFiguresChanged(PathFigures? figures)
        {
            _figuresObserver?.Dispose();
            _figuresPropertiesObserver?.Dispose();

            _figuresObserver = figures?.ForEachItem(
                s =>
                {
                    s.SegmentsInvalidated += InvalidateGeometryFromSegments;
                    InvalidateGeometry();
                },
                s =>
                {
                    s.SegmentsInvalidated -= InvalidateGeometryFromSegments;
                    InvalidateGeometry();
                },
                InvalidateGeometry);
            
            _figuresPropertiesObserver = figures?.TrackItemPropertyChanged(_ => InvalidateGeometry());
 
        }

        private void InvalidateGeometryFromSegments(object? _, EventArgs __)
        {
            InvalidateGeometry();
        }

        public override string ToString()
        {
            var figuresString = _figures is not null ? string.Join(" ", _figures) : string.Empty;
            return FormattableString.Invariant($"{(FillRule != FillRule.EvenOdd ? "F1 " : "")}{figuresString}");
        }
    }
}
