using System;
using Avalonia.Base.UnitTests.Layout;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class ContainerTests
    {
        [Fact]
        public void Container_Cannot_Be_Added_To_Style_Children()
        {
            var target = new ContainerQuery();
            var style = new Style();

            Assert.Throws<InvalidOperationException>(() => style.Children.Add(target));
        }

        [Fact]
        public void Container_Width_Queries_Matches()
        {
            using var app = UnitTestApplication.Start();
            var root = new LayoutTestRoot()
            {
                ClientSize = new Size(400, 400)
            };
            var containerQuery1 = new ContainerQuery(x => new WidthQuery(x, QueryComparisonOperator.LessThanOrEquals, 500));
            containerQuery1.Children.Add(new Style(x => x.Is<Border>())
            {
                Setters = { new Setter(Control.WidthProperty, 200.0) }
            });            
            var containerQuery2 = new ContainerQuery(x => new WidthQuery(x, QueryComparisonOperator.GreaterThan, 500));
            containerQuery2.Children.Add(new Style(x => x.Is<Border>())
            {
                Setters = { new Setter(Control.WidthProperty, 500.0) }
            });
            root.Styles.Add(containerQuery1);
            root.Styles.Add(containerQuery2);
            var child = new Border()
            {
                Name = "Child",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };
            var border = new Border()
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                Child = child,
                Name = "Parent"
            };
            Container.SetSizing(border, Avalonia.Layout.ContainerSizing.Width);

            root.Child = border;

            root.LayoutManager.ExecuteInitialLayoutPass();
            Assert.Equal(child.Width, 200.0);

            root.ClientSize = new Size(600, 600);
            root.InvalidateMeasure();

            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(child.Width, 500.0);
        }

        [Fact]
        public void Container_Height_Queries_Matches()
        {
            using var app = UnitTestApplication.Start();
            var root = new LayoutTestRoot()
            {
                ClientSize = new Size(400, 400)
            };
            var containerQuery1 = new ContainerQuery(x => new HeightQuery(x, QueryComparisonOperator.LessThanOrEquals, 500));
            containerQuery1.Children.Add(new Style(x => x.Is<Border>())
            {
                Setters = { new Setter(Control.HeightProperty, 200.0) }
            });
            var containerQuery2 = new ContainerQuery(x => new HeightQuery(x, QueryComparisonOperator.GreaterThan, 500));
            containerQuery2.Children.Add(new Style(x => x.Is<Border>())
            {
                Setters = { new Setter(Control.HeightProperty, 500.0) }
            });
            root.Styles.Add(containerQuery1);
            root.Styles.Add(containerQuery2);
            var child = new Border()
            {
                Name = "Child",
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
            };
            var border = new Border()
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                Child = child,
                Name = "Parent"
            };
            Container.SetSizing(border, Avalonia.Layout.ContainerSizing.Height);

            root.Child = border;

            root.LayoutManager.ExecuteInitialLayoutPass();
            Assert.Equal(child.Height, 200.0);

            root.ClientSize = new Size(600, 600);
            root.InvalidateMeasure();

            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(child.Height, 500.0);
        }

        [Fact]
        public void Container_Queries_Matches_Name()
        {
            using var app = UnitTestApplication.Start();
            var root = new LayoutTestRoot()
            {
                ClientSize = new Size(600, 600)
            };
            var containerQuery1 = new ContainerQuery(x => new WidthQuery(x, QueryComparisonOperator.LessThanOrEquals, 500));
            containerQuery1.Children.Add(new Style(x => x.Is<Border>())
            {
                Setters = { new Setter(Control.WidthProperty, 200.0) }
            });
            var containerQuery2 = new ContainerQuery(x => new WidthQuery(x, QueryComparisonOperator.LessThanOrEquals, 500), "TEST");
            containerQuery2.Children.Add(new Style(x => x.Is<Border>())
            {
                Setters = { new Setter(Control.WidthProperty, 300.0) }
            });
            root.Styles.Add(containerQuery2);
            root.Styles.Add(containerQuery1);
            var child = new Border()
            {
                Name = "Child",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };
            var controlInner = new ContentControl()
            {
                Width = 400,
                Height = 400,
                Content = child,
                Name = "Inner"
            };
            Container.SetSizing(controlInner, Avalonia.Layout.ContainerSizing.Width);
            Container.SetName(controlInner, "TEST");
            var border = new Border()
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                Child = controlInner,
                Name = "Parent"
            };
            Container.SetSizing(border, Avalonia.Layout.ContainerSizing.Width);

            root.Child = border;

            root.LayoutManager.ExecuteInitialLayoutPass();

            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(child.Width, 300.0);
        }
    }
}
