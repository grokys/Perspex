using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests
{
    public class VirtualizingStackPanelTests : ScopedTestBase
    {
        private static FuncDataTemplate<ItemWithHeight> CanvasWithHeightTemplate = new((_, _) =>
            new Canvas
            {
                Width = 100,
                [!Layoutable.HeightProperty] = new Binding("Height"),
            });

        private static FuncDataTemplate<ItemWithWidth> CanvasWithWidthTemplate = new((_, _) =>
            new Canvas
            {
                Height = 100,
                [!Layoutable.WidthProperty] = new Binding("Width"),
            });

        [Fact]
        public void Creates_Initial_Items()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            Assert.Equal(1000, scroll.Extent.Height);

            AssertRealizedItems(target, itemsControl, 0, 10);
        }

        [Fact]
        public void Initializes_Initial_Control_Items()
        {
            using var app = App();
            var items = Enumerable.Range(0, 100).Select(x => new Button { Width = 25, Height = 10 });
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: null);

            Assert.Equal(1000, scroll.Extent.Height);

            AssertRealizedControlItems<Button>(target, itemsControl, 0, 10);
        }

        [Fact]
        public void Creates_Reassigned_Items()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget(items: Array.Empty<object>());

            Assert.Empty(itemsControl.GetRealizedContainers());

            itemsControl.ItemsSource = new[] { "foo", "bar" };
            Layout(target);

            AssertRealizedItems(target, itemsControl, 0, 2);
        }

        [Fact]
        public void Scrolls_Down_One_Item()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            scroll.Offset = new Vector(0, 10);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 1, 10);
        }

        [Fact]
        public void Scrolls_Down_More_Than_A_Page()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 20, 10);
        }

        [Fact]
        public void Scrolls_Down_To_Index()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            target.ScrollIntoView(20);

            AssertRealizedItems(target, itemsControl, 11, 10);
        }

        [Fact]
        public void Scrolls_Up_To_Index()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            scroll.ScrollToEnd();
            Layout(target);

            Assert.Equal(90, target.FirstRealizedIndex);

            target.ScrollIntoView(20);

            AssertRealizedItems(target, itemsControl, 20, 10);
        }

        [Fact]
        public void Scrolling_Up_To_Index_Does_Not_Create_A_Page_Of_Unrealized_Elements()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            scroll.ScrollToEnd();
            Layout(target);
            target.ScrollIntoView(20);

            Assert.Equal(11, target.Children.Count);
        }

        [Fact]
        public void Creates_Elements_On_Item_Insert_1()
        {
            using var app = App();
            var (target, _, itemsControl) = CreateTarget();
            var items = (IList)itemsControl.ItemsSource!;

            Assert.Equal(10, target.GetRealizedElements().Count);

            items.Insert(0, "new");

            Assert.Equal(11, target.GetRealizedElements().Count);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Blank space inserted in realized elements and subsequent indexes updated.
            Assert.Equal(new[] { -1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, indexes);

            var elements = target.GetRealizedElements().ToList();
            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout an element for the new element is created.
            Assert.Equal(Enumerable.Range(0, 10), indexes);

            // But apart from the new element and the removed last element, all existing elements
            // should be the same.
            elements[0] = target.GetRealizedElements().ElementAt(0);
            elements.RemoveAt(elements.Count - 1);
            Assert.Equal(elements, target.GetRealizedElements());
        }

        [Fact]
        public void Creates_Elements_On_Item_Insert_2()
        {
            using var app = App();
            var (target, _, itemsControl) = CreateTarget();
            var items = (IList)itemsControl.ItemsSource!;

            Assert.Equal(10, target.GetRealizedElements().Count);

            items.Insert(2, "new");

            Assert.Equal(11, target.GetRealizedElements().Count);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Blank space inserted in realized elements and subsequent indexes updated.
            Assert.Equal(new[] { 0, 1, -1, 3, 4, 5, 6, 7, 8, 9, 10 }, indexes);

            var elements = target.GetRealizedElements().ToList();
            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout an element for the new element is created.
            Assert.Equal(Enumerable.Range(0, 10), indexes);

            // But apart from the new element and the removed last element, all existing elements
            // should be the same.
            elements[2] = target.GetRealizedElements().ElementAt(2);
            elements.RemoveAt(elements.Count - 1);
            Assert.Equal(elements, target.GetRealizedElements());
        }

        [Fact]
        public void Updates_Elements_On_Item_Moved()
        {
            // Arrange

            using var app = App();

            var actualItems = new AvaloniaList<string>(Enumerable
                .Range(0, 100)
                .Select(x => $"Item {x}"));

            var (target, _, itemsControl) = CreateTarget(items: actualItems);

            var expectedRealizedElementContents = new[] { 1, 2, 0, 3, 4, 5, 6, 7, 8, 9 }
                .Select(x => $"Item {x}");

            // Act

            actualItems.Move(0, 2);
            Layout(target);

            // Assert

            var actualRealizedElementContents = target
                .GetRealizedElements()
                .Cast<ContentPresenter>()
                .Select(x => x.Content);

            Assert.Equivalent(expectedRealizedElementContents, actualRealizedElementContents);
        }

        [Fact]
        public void Updates_Elements_On_Item_Range_Moved()
        {
            // Arrange

            using var app = App();

            var actualItems = new AvaloniaList<string>(Enumerable
                .Range(0, 100)
                .Select(x => $"Item {x}"));

            var (target, _, itemsControl) = CreateTarget(items: actualItems);

            var expectedRealizedElementContents = new[] { 2, 0, 1, 3, 4, 5, 6, 7, 8, 9 }
                .Select(x => $"Item {x}");

            // Act

            actualItems.MoveRange(0, 2, 3);
            Layout(target);

            // Assert

            var actualRealizedElementContents = target
                .GetRealizedElements()
                .Cast<ContentPresenter>()
                .Select(x => x.Content);

            Assert.Equivalent(expectedRealizedElementContents, actualRealizedElementContents);
        }

        [Fact]
        public void Updates_Elements_On_Item_Remove()
        {
            using var app = App();
            var (target, _, itemsControl) = CreateTarget();
            var items = (IList)itemsControl.ItemsSource!;

            Assert.Equal(10, target.GetRealizedElements().Count);

            var toRecycle = target.GetRealizedElements().ElementAt(2);
            items.RemoveAt(2);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Item removed from realized elements and subsequent row indexes updated.
            Assert.Equal(Enumerable.Range(0, 9), indexes);

            var elements = target.GetRealizedElements().ToList();
            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout an element for the newly visible last row is created and indexes updated.
            Assert.Equal(Enumerable.Range(0, 10), indexes);

            // And the removed row should now have been recycled as the last row.
            elements.Add(toRecycle);
            Assert.Equal(elements, target.GetRealizedElements());
        }

        [Fact]
        public void Updates_Elements_On_Item_Replace()
        {
            using var app = App();
            var (target, _, itemsControl) = CreateTarget();
            var items = (ObservableCollection<string>)itemsControl.ItemsSource!;

            Assert.Equal(10, target.GetRealizedElements().Count);

            var toReplace = target.GetRealizedElements().ElementAt(2);
            items[2] = "new";

            // Container being replaced should have been recycled.
            Assert.DoesNotContain(toReplace, target.GetRealizedElements());
            Assert.False(toReplace!.IsVisible);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Item removed from realized elements at old position and space inserted at new position.
            Assert.Equal(new[] { 0, 1, -1, 3, 4, 5, 6, 7, 8, 9 }, indexes);

            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout the missing container should have been created.
            Assert.Equal(Enumerable.Range(0, 10), indexes);
        }

        [Fact]
        public void Updates_Elements_On_Item_Move()
        {
            using var app = App();
            var (target, _, itemsControl) = CreateTarget();
            var items = (ObservableCollection<string>)itemsControl.ItemsSource!;

            Assert.Equal(10, target.GetRealizedElements().Count);

            var toMove = target.GetRealizedElements().ElementAt(2);
            items.Move(2, 6);

            // Container being moved should have been recycled.
            Assert.DoesNotContain(toMove, target.GetRealizedElements());
            Assert.False(toMove!.IsVisible);

            var indexes = GetRealizedIndexes(target, itemsControl);

            // Item removed from realized elements at old position and space inserted at new position.
            Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, -1, 7, 8, 9 }, indexes);

            Layout(target);

            indexes = GetRealizedIndexes(target, itemsControl);

            // After layout the missing container should have been created.
            Assert.Equal(Enumerable.Range(0, 10), indexes);
        }

        [Fact]
        public void Removes_Control_Items_From_Panel_On_Item_Remove()
        {
            using var app = App();
            var items = new ObservableCollection<Button>(Enumerable.Range(0, 100).Select(x => new Button { Width = 25, Height = 10 }));
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: null);

            Assert.Equal(1000, scroll.Extent.Height);

            var removed = items[1];
            items.RemoveAt(1);

            Assert.Null(removed.Parent);
            Assert.Null(removed.VisualParent);
        }

        [Fact]
        public void Does_Not_Recycle_Focused_Element()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            var focused = target.GetRealizedElements().First()!;
            focused.Focusable = true;
            focused.Focus();
            Assert.True(target.GetRealizedElements().First()!.IsKeyboardFocusWithin);

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            Assert.All(target.GetRealizedElements(), x => Assert.False(x!.IsKeyboardFocusWithin));
        }

        [Fact]
        public void Removing_Item_Of_Focused_Element_Clears_Focus()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();
            var items = (IList)itemsControl.ItemsSource!;

            var focused = target.GetRealizedElements().First()!;
            focused.Focusable = true;
            focused.Focus();
            Assert.True(focused.IsKeyboardFocusWithin);
            Assert.Equal(focused, KeyboardNavigation.GetTabOnceActiveElement(itemsControl));

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            items.RemoveAt(0);

            Assert.All(target.GetRealizedElements(), x => Assert.False(x!.IsKeyboardFocusWithin));
            Assert.All(target.GetRealizedElements(), x => Assert.NotSame(focused, x));
        }

        [Fact]
        public void Scrolling_Back_To_Focused_Element_Uses_Correct_Element()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            var focused = target.GetRealizedElements().First()!;
            focused.Focusable = true;
            focused.Focus();
            Assert.True(focused.IsKeyboardFocusWithin);

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            scroll.Offset = new Vector(0, 0);
            Layout(target);

            Assert.Same(focused, target.GetRealizedElements().First());
        }

        [Fact]
        public void Focusing_Another_Element_Recycles_Original_Focus_Element()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();

            var originalFocused = target.GetRealizedElements().First()!;
            originalFocused.Focusable = true;
            originalFocused.Focus();

            scroll.Offset = new Vector(0, 500);
            Layout(target);

            var newFocused = target.GetRealizedElements().First()!;
            newFocused.Focusable = true;
            newFocused.Focus();

            Assert.False(originalFocused.IsVisible);
        }

        [Fact]
        public void Focused_Element_Losing_Focus_Does_Not_Reset_Selection()
        {
            using var app = App();
            var (target, scroll, listBox) = CreateTarget<ListBox>(
                styles: new[]
                {
                    new Style(x => x.OfType<ListBoxItem>())
                    {
                        Setters =
                        {
                            new Setter(ListBoxItem.TemplateProperty, ListBoxItemTemplate()),
                        }
                    }
                });

            listBox.SelectedIndex = 0;

            var selectedContainer = target.GetRealizedElements().First()!;
            selectedContainer.Focusable = true;
            selectedContainer.Focus();

            scroll.Offset = new Vector(0, 500);
            Layout(target);

            var newFocused = target.GetRealizedElements().First()!;
            newFocused.Focusable = true;
            newFocused.Focus();

            Assert.Equal(0, listBox.SelectedIndex);
        }

        [Fact]
        public void Removing_Range_When_Scrolled_To_End_Updates_Viewport()
        {
            using var app = App();
            var items = new AvaloniaList<string>(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
            var (target, scroll, itemsControl) = CreateTarget(items: items);

            scroll.Offset = new Vector(0, 900);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 90, 10);

            items.RemoveRange(0, 80);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 10, 10);
            Assert.Equal(new Vector(0, 100), scroll.Offset);
        }

        [Fact]
        public void Removing_Range_To_Have_Less_Than_A_Page_Of_Items_When_Scrolled_To_End_Updates_Viewport()
        {
            using var app = App();
            var items = new AvaloniaList<string>(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
            var (target, scroll, itemsControl) = CreateTarget(items: items);

            scroll.Offset = new Vector(0, 900);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 90, 10);

            items.RemoveRange(0, 95);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 0, 5);
            Assert.Equal(new Vector(0, 0), scroll.Offset);
        }

        [Fact]
        public void Resetting_Collection_To_Have_Less_Items_When_Scrolled_To_End_Updates_Viewport()
        {
            using var app = App();
            var items = new ResettingCollection(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
            var (target, scroll, itemsControl) = CreateTarget(items: items);

            scroll.Offset = new Vector(0, 900);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 90, 10);

            items.Reset(Enumerable.Range(0, 20).Select(x => $"Item {x}"));
            Layout(target);

            AssertRealizedItems(target, itemsControl, 10, 10);
            Assert.Equal(new Vector(0, 100), scroll.Offset);
        }

        [Fact]
        public void Resetting_Collection_To_Have_Less_Than_A_Page_Of_Items_When_Scrolled_To_End_Updates_Viewport()
        {
            using var app = App();
            var items = new ResettingCollection(Enumerable.Range(0, 100).Select(x => $"Item {x}"));
            var (target, scroll, itemsControl) = CreateTarget(items: items);

            scroll.Offset = new Vector(0, 900);
            Layout(target);

            AssertRealizedItems(target, itemsControl, 90, 10);

            items.Reset(Enumerable.Range(0, 5).Select(x => $"Item {x}"));
            Layout(target);

            AssertRealizedItems(target, itemsControl, 0, 5);
            Assert.Equal(new Vector(0, 0), scroll.Offset);
        }

        [Fact]
        public void NthChild_Selector_Works()
        {
            using var app = App();

            var style = new Style(x => x.OfType<ContentPresenter>().NthChild(5, 0))
            {
                Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.Red) },
            };

            var (target, _, _) = CreateTarget(styles: new[] { style });
            var realized = target.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();

            Assert.Equal(10, realized.Count);

            for (var i = 0; i < 10; ++i)
            {
                var container = realized[i];
                var index = target.IndexFromContainer(container);
                var expectedBackground = (i == 4 || i == 9) ? Brushes.Red : null;

                Assert.Equal(i, index);
                Assert.Equal(expectedBackground, container.Background);
            }
        }

        // https://github.com/AvaloniaUI/Avalonia/issues/12838
        [Fact]
        public void NthChild_Selector_Works_For_ItemTemplate_Children()
        {
            using var app = App();

            var style = new Style(x => x.OfType<ContentPresenter>().NthChild(5, 0).Child().OfType<Canvas>())
            {
                Setters = { new Setter(Panel.BackgroundProperty, Brushes.Red) },
            };

            var (target, _, _) = CreateTarget(styles: new[] { style });
            var realized = target.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();

            Assert.Equal(10, realized.Count);

            for (var i = 0; i < 10; ++i)
            {
                var container = realized[i];
                var index = target.IndexFromContainer(container);
                var expectedBackground = (i == 4 || i == 9) ? Brushes.Red : null;

                Assert.Equal(i, index);
                Assert.Equal(expectedBackground, ((Canvas)container.Child!).Background);
            }
        }

        [Fact]
        public void NthLastChild_Selector_Works()
        {
            using var app = App();

            var style = new Style(x => x.OfType<ContentPresenter>().NthLastChild(5, 0))
            {
                Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.Red) },
            };

            var (target, _, _) = CreateTarget(styles: new[] { style });
            var realized = target.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();

            Assert.Equal(10, realized.Count);

            for (var i = 0; i < 10; ++i)
            {
                var container = realized[i];
                var index = target.IndexFromContainer(container);
                var expectedBackground = (i == 0 || i == 5) ? Brushes.Red : null;

                Assert.Equal(i, index);
                Assert.Equal(expectedBackground, container.Background);
            }
        }

        // https://github.com/AvaloniaUI/Avalonia/issues/12838
        [Fact]
        public void NthLastChild_Selector_Works_For_ItemTemplate_Children()
        {
            using var app = App();

            var style = new Style(x => x.OfType<ContentPresenter>().NthLastChild(5, 0).Child().OfType<Canvas>())
            {
                Setters = { new Setter(Panel.BackgroundProperty, Brushes.Red) },
            };

            var (target, _, _) = CreateTarget(styles: new[] { style });
            var realized = target.GetRealizedContainers()!.Cast<ContentPresenter>().ToList();

            Assert.Equal(10, realized.Count);

            for (var i = 0; i < 10; ++i)
            {
                var container = realized[i];
                var index = target.IndexFromContainer(container);
                var expectedBackground = (i == 0 || i == 5) ? Brushes.Red : null;

                Assert.Equal(i, index);
                Assert.Equal(expectedBackground, ((Canvas)container.Child!).Background);
            }
        }

        [Fact]
        public void ContainerPrepared_Is_Raised_When_Scrolling()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();
            var raised = 0;

            itemsControl.ContainerPrepared += (s, e) => ++raised;

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            Assert.Equal(10, raised);
        }

        [Fact]
        public void ContainerClearing_Is_Raised_When_Scrolling()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();
            var raised = 0;

            itemsControl.ContainerClearing += (s, e) => ++raised;

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            Assert.Equal(10, raised);
        }

        [Fact]
        public void ContainerIndexChanged_Is_Raised_On_Insert()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();
            var items = (IList)itemsControl.ItemsSource!;
            var raised = 0;
            var index = 1;

            itemsControl.ContainerIndexChanged += (s, e) =>
            {
                ++raised;
                Assert.Equal(index, e.OldIndex);
                Assert.Equal(++index, e.NewIndex);
            };

            items.Insert(index, "new");

            Assert.Equal(9, raised);
        }

        [Fact]
        public void ContainerIndexChanged_Is_Raised_When_Item_Inserted_Before_Realized_Elements()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();
            var items = (IList)itemsControl.ItemsSource!;
            var raised = 0;
            var index = 20;

            itemsControl.ContainerIndexChanged += (s, e) =>
            {
                ++raised;
                Assert.Equal(index, e.OldIndex);
                Assert.Equal(++index, e.NewIndex);
            };

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            items.Insert(10, "new");

            Assert.Equal(10, raised);
        }

        [Fact]
        public void ContainerIndexChanged_Is_Raised_On_Remove()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();
            var items = (IList)itemsControl.ItemsSource!;
            var raised = 0;
            var index = 1;

            itemsControl.ContainerIndexChanged += (s, e) =>
            {
                ++raised;
                Assert.Equal(index + 1, e.OldIndex);
                Assert.Equal(index++, e.NewIndex);
            };

            items.RemoveAt(index);

            Assert.Equal(8, raised);
        }

        [Fact]
        public void ContainerIndexChanged_Is_Raised_When_Item_Removed_Before_Realized_Elements()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();
            var items = (IList)itemsControl.ItemsSource!;
            var raised = 0;
            var index = 20;

            itemsControl.ContainerIndexChanged += (s, e) =>
            {
                Assert.Equal(index, e.OldIndex);
                Assert.Equal(index - 1, e.NewIndex);
                ++index;
                ++raised;
            };

            scroll.Offset = new Vector(0, 200);
            Layout(target);

            items.RemoveAt(10);

            Assert.Equal(10, raised);
        }

        [Fact]
        public void Fires_Correct_Container_Lifecycle_Events_On_Replace()
        {
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();
            var items = (IList)itemsControl.ItemsSource!;
            var events = new List<string>();

            itemsControl.ContainerPrepared += (s, e) => events.Add($"Prepared #{e.Container.GetHashCode()} = {e.Index}");
            itemsControl.ContainerClearing += (s, e) => events.Add($"Clearing #{e.Container.GetHashCode()}");
            itemsControl.ContainerIndexChanged += (s, e) => events.Add($"IndexChanged #{e.Container.GetHashCode()} {e.OldIndex} -> {e.NewIndex}");

            var toReplace = target.GetRealizedElements().ElementAt(2)!;
            items[2] = "New Item";

            Assert.Equal(
                new[] { $"Clearing #{toReplace.GetHashCode()}" },
                events);
            events.Clear();

            itemsControl.UpdateLayout();

            Assert.Equal(
                new[] { $"Prepared #{toReplace.GetHashCode()} = 2" },
                events);
            events.Clear();
        }

        [Fact]
        public void Scrolling_Down_With_Larger_Element_Does_Not_Cause_Jump_And_Arrives_At_End()
        {
            using var app = App();

            var items = Enumerable.Range(0, 1000).Select(x => new ItemWithHeight(x)).ToList();
            items[20].Height = 200;

            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            var index = target.FirstRealizedIndex;

            // Scroll down to the larger element.
            while (target.LastRealizedIndex < items.Count - 1)
            {
                scroll.LineDown();
                Layout(target);

                Assert.True(
                    target.FirstRealizedIndex >= index,
                    $"{target.FirstRealizedIndex} is not greater or equal to {index}");

                if (scroll.Offset.Y + scroll.Viewport.Height == scroll.Extent.Height)
                    Assert.Equal(items.Count - 1, target.LastRealizedIndex);

                index = target.FirstRealizedIndex;
            }
        }

        [Fact]
        public void Scrolling_Up_To_Larger_Element_Does_Not_Cause_Jump()
        {
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x)).ToList();
            items[20].Height = 200;

            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            // Scroll past the larger element.
            scroll.Offset = new Vector(0, 600);
            Layout(target);

            // Precondition checks
            Assert.True(target.FirstRealizedIndex > 20);

            var index = target.FirstRealizedIndex;

            // Scroll up to the top.
            while (scroll.Offset.Y > 0)
            {
                scroll.LineUp();
                Layout(target);

                Assert.True(target.FirstRealizedIndex <= index, $"{target.FirstRealizedIndex} is not less than {index}");
                index = target.FirstRealizedIndex;
            }
        }

        [Fact]
        public void Scrolling_Up_To_Smaller_Element_Does_Not_Cause_Jump()
        {
            using var app = App();

            var items = Enumerable.Range(0, 100).Select(x => new ItemWithHeight(x, 30)).ToList();
            items[20].Height = 25;

            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            // Scroll past the larger element.
            scroll.Offset = new Vector(0, 25 * items[0].Height);
            Layout(target);

            // Precondition checks
            Assert.True(target.FirstRealizedIndex > 20);

            var index = target.FirstRealizedIndex;

            // Scroll up to the top.
            while (scroll.Offset.Y > 0)
            {
                scroll.Offset = scroll.Offset - new Vector(0, 5);
                Layout(target);

                Assert.True(
                    target.FirstRealizedIndex <= index,
                    $"{target.FirstRealizedIndex} is not less than {index}");
                Assert.True(
                    index - target.FirstRealizedIndex <= 1,
                    $"FirstIndex changed from {index} to {target.FirstRealizedIndex}");

                index = target.FirstRealizedIndex;
            }
        }

        [Fact]
        public void Does_Not_Throw_When_Estimating_Viewport_With_Ancestor_Margin()
        {
            // Issue #11272
            using var app = App();
            var (_, _, itemsControl) = CreateUnrootedTarget<ItemsControl>();
            var container = new Decorator { Margin = new Thickness(100) };
            var root = new TestRoot(true, container);

            root.LayoutManager.ExecuteInitialLayoutPass();

            container.Child = itemsControl;

            root.LayoutManager.ExecuteLayoutPass();
        }

        [Fact]
        public void Supports_Null_Recycle_Key_When_Scrolling()
        {
            using var app = App();
            var (_, scroll, itemsControl) = CreateUnrootedTarget<NonRecyclingItemsControl>();
            var root = CreateRoot(itemsControl);

            root.LayoutManager.ExecuteInitialLayoutPass();

            var firstItem = itemsControl.ContainerFromIndex(0)!;
            scroll.Offset = new(0, 20);

            Layout(itemsControl);

            Assert.Null(firstItem.Parent);
            Assert.Null(firstItem.VisualParent);
            Assert.DoesNotContain(firstItem, itemsControl.ItemsPanelRoot!.Children);
        }

        [Fact]
        public void Supports_Null_Recycle_Key_When_Clearing_Items()
        {
            using var app = App();
            var (_, _, itemsControl) = CreateUnrootedTarget<NonRecyclingItemsControl>();
            var root = CreateRoot(itemsControl);

            root.LayoutManager.ExecuteInitialLayoutPass();

            var firstItem = itemsControl.ContainerFromIndex(0)!;
            itemsControl.ItemsSource = null;

            Layout(itemsControl);

            Assert.Null(firstItem.Parent);
            Assert.Null(firstItem.VisualParent);
            Assert.Empty(itemsControl.ItemsPanelRoot!.Children);
        }

        [Fact]
        public void ScrollIntoView_On_Effectively_Invisible_Panel_Does_Not_Create_Ghost_Elements()
        {
            var items = new[] { "foo", "bar", "baz" };
            var (target, _, itemsControl) = CreateUnrootedTarget<ItemsControl>(items: items);
            var container = new Decorator { Margin = new Thickness(100), Child = itemsControl };
            var root = new TestRoot(true, container);

            root.LayoutManager.ExecuteInitialLayoutPass();

            // Clear the items and do a layout to recycle all elements.
            itemsControl.ItemsSource = null;
            root.LayoutManager.ExecuteLayoutPass();

            // Should have no realized elements and 3 unrealized elements.
            Assert.Equal(0, target.GetRealizedElements().Count);
            Assert.Equal(3, target.Children.Count);

            // Make the panel effectively invisible and set items.
            container.IsVisible = false;
            itemsControl.ItemsSource = items;

            // Try to scroll into view while effectively invisible.
            target.ScrollIntoView(0);

            // Make the panel visible and layout.
            container.IsVisible = true;
            root.LayoutManager.ExecuteLayoutPass();

            // Should have 3 realized elements and no unrealized elements.
            Assert.Equal(3, target.GetRealizedElements().Count);
            Assert.Equal(3, target.Children.Count);
        }

        // https://github.com/AvaloniaUI/Avalonia/issues/10968
        [Fact]
        public void Does_Not_Realize_Items_If_Self_Outside_Viewport()
        {
            using var app = App();
            var (panel, _, itemsControl) = CreateUnrootedTarget<ItemsControl>();
            itemsControl.Margin = new Thickness(0.0, 200.0, 0.0, 0.0);

            var scrollContentPresenter = new ScrollContentPresenter
            {
                Width = 100,
                Height = 100,
                Content = itemsControl
            };

            var root = CreateRoot(scrollContentPresenter);
            root.LayoutManager.ExecuteInitialLayoutPass();
            Assert.Equal(1, panel.VisualChildren.Count);

            scrollContentPresenter.Content = null;
            root.LayoutManager.ExecuteLayoutPass();

            scrollContentPresenter.Content = itemsControl;
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(1, panel.VisualChildren.Count);
        }

        [Fact]
        public void Alternating_Backgrounds_Should_Be_Correct_After_Scrolling()
        {
            // Issue #12381.
            static void AssertColors(VirtualizingStackPanel target)
            {
                var containers = target.GetRealizedContainers()!
                    .Cast<ListBoxItem>()
                    .ToList();

                for (var i = target.FirstRealizedIndex; i <= target.LastRealizedIndex; i++)
                {
                    var container = Assert.IsType<ListBoxItem>(target.ContainerFromIndex(i));
                    var expectedBackground = i % 2 == 0 ? Colors.Green : Colors.Red;
                    var brush = Assert.IsAssignableFrom<ISolidColorBrush>(container.Background);

                    Assert.Equal(expectedBackground, brush.Color);
                }
            }

            using var app = App();
            var styles = new[]
            {
                new Style(x => x.OfType<ListBoxItem>())
                {
                    Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.White) },
                },
                new Style(x => x.OfType<ListBoxItem>().NthChild(2, 1))
                {
                    Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.Green) },
                },
                new Style(x => x.OfType<ListBoxItem>().NthChild(2, 0))
                {
                    Setters = { new Setter(ListBoxItem.BackgroundProperty, Brushes.Red) },
                },
            };
            var (target, scroll, itemsControl) = CreateUnrootedTarget<ListBox>();

            // We need to display an odd number of items to reproduce the issue.
            var root = CreateRoot(itemsControl, clientSize: new(100, 90), styles: styles);
            root.LayoutManager.ExecuteInitialLayoutPass();

            var containers = target.GetRealizedContainers()!
                .Cast<ListBoxItem>()
                .ToList();

            Assert.Equal(0, target.FirstRealizedIndex);
            Assert.Equal(8, target.LastRealizedIndex);
            AssertColors(target);

            scroll.Offset = new Vector(0, 10);
            target.UpdateLayout();

            Assert.Equal(1, target.FirstRealizedIndex);
            Assert.Equal(9, target.LastRealizedIndex);
            AssertColors(target);
        }

        [Fact]
        public void Inserting_Item_Before_Viewport_Preserves_FirstRealizedIndex()
        {
            // Issue #12744
            using var app = App();
            var (target, scroll, itemsControl) = CreateTarget();
            var items = (IList)itemsControl.ItemsSource!;

            // Scroll down 20 items.
            scroll.Offset = new Vector(0, 200);
            target.UpdateLayout();
            Assert.Equal(20, target.FirstRealizedIndex);

            // Insert an item at the beginning.
            items.Insert(0, "New Item");
            target.UpdateLayout();

            // The first realized index should still be 20 as the scroll should be unchanged.
            Assert.Equal(20, target.FirstRealizedIndex);
            Assert.Equal(new(0, 200), scroll.Offset);
        }

        [Fact]
        public void Can_Bind_Item_IsVisible()
        {
            using var app = App();
            var style = CreateIsVisibleBindingStyle();
            var items = Enumerable.Range(0, 100).Select(x => new ItemWithIsVisible(x)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(items: items, styles: new[] { style });
            var container = target.ContainerFromIndex(2)!;

            Assert.True(container.IsVisible);
            Assert.Equal(20, container.Bounds.Top);

            items[2].IsVisible = false;
            Layout(target);

            Assert.False(container.IsVisible);

            // Next container should be in correct position.
            Assert.Equal(20, target.ContainerFromIndex(3)!.Bounds.Top);
        }

        [Fact]
        public void IsVisible_Binding_Persists_After_Scrolling()
        {
            using var app = App();
            var style = CreateIsVisibleBindingStyle();
            var items = Enumerable.Range(0, 100).Select(x => new ItemWithIsVisible(x)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(items: items, styles: new[] { style });
            var container = target.ContainerFromIndex(2)!;

            Assert.True(container.IsVisible);
            Assert.Equal(20, container.Bounds.Top);

            items[2].IsVisible = false;
            scroll.Offset = new Vector(0, 200);
            Layout(target);

            scroll.Offset = new Vector(0, 0);
            Layout(target);

            container = target.ContainerFromIndex(2)!;
            Assert.False(container.IsVisible);
        }

        [Fact]
        public void Recycling_A_Hidden_Control_Shows_It()
        {
            using var app = App();
            var style = CreateIsVisibleBindingStyle();
            var items = Enumerable.Range(0, 3).Select(x => new ItemWithIsVisible(x)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(items: items, styles: new[] { style });
            var container = target.ContainerFromIndex(2)!;

            Assert.True(container.IsVisible);
            Assert.Equal(20, container.Bounds.Top);

            items[2].IsVisible = false;
            Layout(target);

            Assert.False(container.IsVisible);

            items.RemoveAt(2);
            items.Add(new ItemWithIsVisible(3));
            Layout(target);

            Assert.True(container.IsVisible);
        }

        [Fact]
        public void ScrollIntoView_With_TargetRect_Outside_Viewport_Should_Scroll_To_Item()
        {
            using var app = App();
            var items = Enumerable.Range(0, 101).Select(x => new ItemWithHeight(x, x * 100 + 1));
            var itemTemplate = new FuncDataTemplate<ItemWithHeight>((x, _) =>
                new Border
                {
                    Height = 10,
                    [!Layoutable.WidthProperty] = new Binding("Height"),
                });
            var (target, scroll, itemsControl) = CreateTarget(
                items: items,
                itemTemplate: itemTemplate,
                styles: new[]
                {
                    new Style(x => x.OfType<ScrollViewer>())
                    {
                        Setters =
                        {
                            new Setter(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Visible),
                        }
                    }
                });
            itemsControl.ContainerPrepared += (_, ev) =>
            {
                ev.Container.AddHandler(Control.RequestBringIntoViewEvent, (_, e) =>
                {
                    var dataContext = (ItemWithHeight)e.TargetObject!.DataContext!;
                    e.TargetRect = new Rect(dataContext.Height - 50, 0, 50, 10);
                });
            };

            target.ScrollIntoView(100);

            Assert.Equal(9901, scroll.Offset.X);
        }

        [Fact]
        public void ScrollIntoView_Correctly_Scrolls_Down_To_A_Page_Of_Smaller_Items()
        {
            using var app = App();

            // First 10 items have height of 20, next 10 have height of 10.
            var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, ((29 - x) / 10) * 10));
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            // Scroll the last item into view.
            target.ScrollIntoView(19);

            // At the time of the scroll, the average item height is 20, so the requested item
            // should be placed at 380 (19 * 20) which therefore results in an extent of 390 to
            // accommodate the item height of 10. This is obviously not a perfect answer, but
            // it's the best we can do without knowing the actual item heights.
            var container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(19));
            Assert.Equal(new Rect(0, 380, 100, 10), container.Bounds);
            Assert.Equal(new Size(100, 100), scroll.Viewport);
            Assert.Equal(new Size(100, 390), scroll.Extent);
            Assert.Equal(new Vector(0, 290), scroll.Offset);

            // Items 10-19 should be visible.
            AssertRealizedItems(target, itemsControl, 10, 10);
        }

        [Fact]
        public void ScrollIntoView_Correctly_Scrolls_Down_To_A_Page_Of_Larger_Items()
        {
            using var app = App();

            // First 10 items have height of 10, next 10 have height of 20.
            var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, ((x / 10) + 1) * 10));
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            // Scroll the last item into view.
            target.ScrollIntoView(19);

            // At the time of the scroll, the average item height is 10, so the requested item
            // should be placed at 190 (19 * 10) which therefore results in an extent of 210 to
            // accommodate the item height of 20. This is obviously not a perfect answer, but
            // it's the best we can do without knowing the actual item heights.
            var container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(19));
            Assert.Equal(new Rect(0, 190, 100, 20), container.Bounds);
            Assert.Equal(new Size(100, 100), scroll.Viewport);
            Assert.Equal(new Size(100, 210), scroll.Extent);
            Assert.Equal(new Vector(0, 110), scroll.Offset);

            // Items 15-19 should be visible.
            AssertRealizedItems(target, itemsControl, 15, 5);
        }

        [Fact]
        public void ScrollIntoView_Correctly_Scrolls_Right_To_A_Page_Of_Smaller_Items()
        {
            using var app = App();

            // First 10 items have width of 20, next 10 have width of 10.
            var items = Enumerable.Range(0, 20).Select(x => new ItemWithWidth(x, ((29 - x) / 10) * 10));
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithWidthTemplate, orientation: Orientation.Horizontal);

            // Scroll the last item into view.
            target.ScrollIntoView(19);

            // At the time of the scroll, the average item width is 20, so the requested item
            // should be placed at 380 (19 * 20) which therefore results in an extent of 390 to
            // accommodate the item width of 10. This is obviously not a perfect answer, but
            // it's the best we can do without knowing the actual item widths.
            var container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(19));
            Assert.Equal(new Rect(380, 0, 10, 100), container.Bounds);
            Assert.Equal(new Size(100, 100), scroll.Viewport);
            Assert.Equal(new Size(390, 100), scroll.Extent);
            Assert.Equal(new Vector(290, 0), scroll.Offset);

            // Items 10-19 should be visible.
            AssertRealizedItems(target, itemsControl, 10, 10);
        }

        [Fact]
        public void ScrollIntoView_Correctly_Scrolls_Right_To_A_Page_Of_Larger_Items()
        {
            using var app = App();

            // First 10 items have width of 10, next 10 have width of 20.
            var items = Enumerable.Range(0, 20).Select(x => new ItemWithWidth(x, ((x / 10) + 1) * 10));
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithWidthTemplate, orientation: Orientation.Horizontal);

            // Scroll the last item into view.
            target.ScrollIntoView(19);

            // At the time of the scroll, the average item width is 10, so the requested item
            // should be placed at 190 (19 * 10) which therefore results in an extent of 210 to
            // accommodate the item width of 20. This is obviously not a perfect answer, but
            // it's the best we can do without knowing the actual item widths.
            var container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(19));
            Assert.Equal(new Rect(190, 0, 20, 100), container.Bounds);
            Assert.Equal(new Size(100, 100), scroll.Viewport);
            Assert.Equal(new Size(210, 100), scroll.Extent);
            Assert.Equal(new Vector(110, 0), scroll.Offset);

            // Items 15-19 should be visible.
            AssertRealizedItems(target, itemsControl, 15, 5);
        }

        [Fact]
        public void Extent_And_Offset_Should_Be_Updated_When_Containers_Resize()
        {
            using var app = App();

            // All containers start off with a height of 50 (2 containers fit in viewport).
            var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, 50)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            // Scroll to the 5th item (containers 4 and 5 should be visible).
            target.ScrollIntoView(5);
            Assert.Equal(4, target.FirstRealizedIndex);
            Assert.Equal(5, target.LastRealizedIndex);

            // The extent should be 500 (10 * 50) and the offset should be 200 (4 * 50).
            var container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(5));
            Assert.Equal(new Rect(0, 250, 100, 50), container.Bounds);
            Assert.Equal(new Size(100, 100), scroll.Viewport);
            Assert.Equal(new Size(100, 1000), scroll.Extent);
            Assert.Equal(new Vector(0, 200), scroll.Offset);

            // Update the height of all items to 25 and run a layout pass.
            foreach (var item in items)
                item.Height = 25;
            target.UpdateLayout();

            // The extent should be updated to reflect the new heights. The offset should be
            // unchanged but the first realized index should be updated to 8 (200 / 25).
            Assert.Equal(new Size(100, 100), scroll.Viewport);
            Assert.Equal(new Size(100, 500), scroll.Extent);
            Assert.Equal(new Vector(0, 200), scroll.Offset);
            Assert.Equal(8, target.FirstRealizedIndex);
            Assert.Equal(11, target.LastRealizedIndex);
        }

        [Fact]
        public void Focused_Container_Is_Positioned_Correctly_when_Container_Size_Change_Causes_It_To_Be_Moved_Out_Of_Visible_Viewport()
        {
            using var app = App();

            // All containers start off with a height of 50 (2 containers fit in viewport).
            var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, 50)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            // Scroll to the 5th item (containers 4 and 5 should be visible).
            target.ScrollIntoView(5);
            Assert.Equal(4, target.FirstRealizedIndex);
            Assert.Equal(5, target.LastRealizedIndex);

            // Focus the 5th item.
            var container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(5));
            container.Focusable = true;
            container.Focus();

            // Update the height of all items to 25 and run a layout pass.
            foreach (var item in items)
                item.Height = 25;
            target.UpdateLayout();

            // The focused container should now be outside the realized range.
            Assert.Equal(8, target.FirstRealizedIndex);
            Assert.Equal(11, target.LastRealizedIndex);

            // The container should still exist and be positioned outside the visible viewport.
            container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(5));
            Assert.Equal(new Rect(0, 125, 100, 25), container.Bounds);
        }

        [Fact]
        public void Focused_Container_Is_Positioned_Correctly_when_Container_Size_Change_Causes_It_To_Be_Moved_Into_Visible_Viewport()
        {
            using var app = App();

            // All containers start off with a height of 25 (4 containers fit in viewport).
            var items = Enumerable.Range(0, 20).Select(x => new ItemWithHeight(x, 25)).ToList();
            var (target, scroll, itemsControl) = CreateTarget(items: items, itemTemplate: CanvasWithHeightTemplate);

            // Scroll to the 5th item (containers 4-7 should be visible).
            target.ScrollIntoView(7);
            Assert.Equal(4, target.FirstRealizedIndex);
            Assert.Equal(7, target.LastRealizedIndex);

            // Focus the 7th item.
            var container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(7));
            container.Focusable = true;
            container.Focus();

            // Scroll up to the 3rd item (containers 3-6 should still be visible).
            target.ScrollIntoView(3);
            Assert.Equal(3, target.FirstRealizedIndex);
            Assert.Equal(6, target.LastRealizedIndex);

            // Update the height of all items to 20 and run a layout pass.
            foreach (var item in items)
                item.Height = 20;
            target.UpdateLayout();

            // The focused container should now be inside the realized range.
            Assert.Equal(3, target.FirstRealizedIndex);
            Assert.Equal(7, target.LastRealizedIndex);

            // The container should be positioned correctly.
            container = Assert.IsType<ContentPresenter>(target.ContainerFromIndex(7));
            Assert.Equal(new Rect(0, 140, 100, 20), container.Bounds);
        }

        [Fact]
        public void Lays_Out_Children_Vertically_With_Spacing()
        {
            using var app = App();
            var itemTemplate = new FuncDataTemplate<int>((x, _) => new Canvas { Width = 100, Height = x });
            var (target, scroll, itemsControl) = CreateTarget([20, 30, 50], itemTemplate);

            target.Spacing = 10;

            // TODO for some reason Spacing setting doesn't trigger Measure and Arrange by itself even when AffectsMeasure/AffectsArrange is called in the static constructor of VirtualizingStackPanel
            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(100, 120), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 100, 20), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 30, 100, 30), target.Children[1].Bounds);
            Assert.Equal(new Rect(0, 70, 100, 50), target.Children[2].Bounds);
        }

        [Fact]
        public void Lays_Out_Children_Horizontally_With_Spacing()
        {
            using var app = App();
            var itemTemplate = new FuncDataTemplate<int>((x, _) => new Canvas { Width = x, Height = 100 });
            var (target, scroll, itemsControl) = CreateTarget([20, 30, 50], itemTemplate, orientation: Orientation.Horizontal);

            target.Orientation = Orientation.Horizontal;
            target.Spacing = 10;

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Size(120, 100), target.Bounds.Size);
            Assert.Equal(new Rect(0, 0, 20, 100), target.Children[0].Bounds);
            Assert.Equal(new Rect(30, 0, 30, 100), target.Children[1].Bounds);
            Assert.Equal(new Rect(70, 0, 50, 100), target.Children[2].Bounds);
        }

        private static IReadOnlyList<int> GetRealizedIndexes(VirtualizingStackPanel target, ItemsControl itemsControl)
        {
            return target.GetRealizedElements()
                .Select(x => x is null ? -1 : itemsControl.IndexFromContainer((Control)x))
                .ToList();
        }

        private static void AssertRealizedItems(
            VirtualizingStackPanel target,
            ItemsControl itemsControl,
            int firstIndex,
            int count)
        {
            Assert.All(target.GetRealizedContainers()!, x => Assert.Same(target, x.VisualParent));
            Assert.All(target.GetRealizedContainers()!, x => Assert.Same(itemsControl, x.Parent));

            var childIndexes = target.GetRealizedContainers()!
                .Select(x => itemsControl.IndexFromContainer(x))
                .Where(x => x >= 0)
                .OrderBy(x => x)
                .ToList();
            Assert.Equal(Enumerable.Range(firstIndex, count), childIndexes);

            var visibleChildren = target.Children
                .Where(x => x.IsVisible)
                .ToList();
            Assert.Equal(count, visibleChildren.Count);
        }

        private static void AssertRealizedControlItems<TContainer>(
            VirtualizingStackPanel target,
            ItemsControl itemsControl,
            int firstIndex,
            int count)
        {
            Assert.All(target.GetRealizedContainers()!, x => Assert.IsType<TContainer>(x));
            Assert.All(target.GetRealizedContainers()!, x => Assert.Same(target, x.VisualParent));
            Assert.All(target.GetRealizedContainers()!, x => Assert.Same(itemsControl, x.Parent));

            var childIndexes = target.GetRealizedContainers()!
                .Select(x => itemsControl.IndexFromContainer(x))
                .Where(x => x >= 0)
                .OrderBy(x => x)
                .ToList();
            Assert.Equal(Enumerable.Range(firstIndex, count), childIndexes);
        }

        private static (VirtualizingStackPanel, ScrollViewer, ItemsControl) CreateTarget(
            IEnumerable<object>? items = null,
            Optional<IDataTemplate?> itemTemplate = default,
            IEnumerable<Style>? styles = null,
            Orientation orientation = Orientation.Vertical)
        {
            return CreateTarget<ItemsControl>(
                items: items,
                itemTemplate: itemTemplate,
                styles: styles,
                orientation: orientation);
        }

        private static (VirtualizingStackPanel, ScrollViewer, T) CreateTarget<T>(
            IEnumerable<object>? items = null,
            Optional<IDataTemplate?> itemTemplate = default,
            IEnumerable<Style>? styles = null,
            Orientation orientation = Orientation.Vertical)
                where T : ItemsControl, new()
        {
            var (target, scroll, itemsControl) = CreateUnrootedTarget<T>(items, itemTemplate, orientation);
            var root = CreateRoot(itemsControl, styles: styles);

            root.LayoutManager.ExecuteInitialLayoutPass();

            return (target, scroll, itemsControl);
        }

        private static (VirtualizingStackPanel, ScrollViewer, T) CreateUnrootedTarget<T>(
            IEnumerable<object>? items = null,
            Optional<IDataTemplate?> itemTemplate = default,
            Orientation orientation = Orientation.Vertical)
                where T : ItemsControl, new()
        {
            var target = new VirtualizingStackPanel
            {
                Orientation = orientation,
            };

            items ??= new ObservableCollection<string>(Enumerable.Range(0, 100).Select(x => $"Item {x}"));

            var presenter = new ItemsPresenter
            {
                [~ItemsPresenter.ItemsPanelProperty] = new TemplateBinding(ItemsPresenter.ItemsPanelProperty),
            };

            var scroll = new ScrollViewer
            {
                Name = "PART_ScrollViewer",
                Content = presenter,
            };

            if (orientation == Orientation.Horizontal)
            {
                scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }

            scroll.Template = ScrollViewerTemplate();

            var itemsControl = new T
            {
                ItemsSource = items,
                Template = new FuncControlTemplate<T>((_, ns) => scroll.RegisterInNameScope(ns)),
                ItemsPanel = new FuncTemplate<Panel?>(() => target),
                ItemTemplate = itemTemplate.GetValueOrDefault(DefaultItemTemplate()),
            };

            return (target, scroll, itemsControl);
        }

        private static TestRoot CreateRoot(
            Control? child,
            Size? clientSize = null,
            IEnumerable<Style>? styles = null)
        {
            var root = new TestRoot(true, child);
            root.ClientSize = clientSize ?? new(100, 100);

            if (styles is not null)
                root.Styles.AddRange(styles);

            return root;
        }

        private static Style CreateIsVisibleBindingStyle()
        {
            return new Style(x => x.OfType<ContentPresenter>())
            {
                Setters =
                {
                    new Setter(Visual.IsVisibleProperty, new Binding("IsVisible")),
                }
            };
        }

        private static IDataTemplate DefaultItemTemplate()
        {
            return new FuncDataTemplate<object>((x, _) => new Canvas { Width = 100, Height = 10 });
        }

        private static void Layout(Control target)
        {
            var root = (ILayoutRoot?)target.GetVisualRoot();
            root?.LayoutManager.ExecuteLayoutPass();
        }

        private static IControlTemplate ListBoxItemTemplate()
        {
            return new FuncControlTemplate<ListBoxItem>((x, ns) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    Width = 100,
                    Height = 10,
                }.RegisterInNameScope(ns));
        }

        private static IControlTemplate ScrollViewerTemplate()
        {
            return new FuncControlTemplate<ScrollViewer>((x, ns) =>
                new ScrollContentPresenter
                {
                    Name = "PART_ScrollContentPresenter",
                }.RegisterInNameScope(ns));
        }

        private static IDisposable App() => UnitTestApplication.Start(TestServices.RealFocus);

        private class ItemWithHeight : NotifyingBase
        {
            private double _height;

            public ItemWithHeight(int index, double height = 10)
            {
                Caption = $"Item {index}";
                Height = height;
            }

            public string Caption { get; set; }

            public double Height
            {
                get => _height;
                set => SetField(ref _height, value);
            }
        }

        private class ItemWithWidth : NotifyingBase
        {
            private double _width;

            public ItemWithWidth(int index, double width = 10)
            {
                Caption = $"Item {index}";
                Width = width;
            }

            public string Caption { get; set; }

            public double Width
            {
                get => _width;
                set => SetField(ref _width, value);
            }
        }

        private class ItemWithIsVisible : NotifyingBase
        {
            private bool _isVisible = true;

            public ItemWithIsVisible(int index)
            {
                Caption = $"Item {index}";
            }

            public string Caption { get; set; }

            public bool IsVisible
            {
                get => _isVisible;
                set => SetField(ref _isVisible, value);
            }
        }

        private class ResettingCollection : List<string>, INotifyCollectionChanged
        {
            public ResettingCollection(IEnumerable<string> items)
            {
                AddRange(items);
            }

            public void Reset(IEnumerable<string> items)
            {
                Clear();
                AddRange(items);
                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            public event NotifyCollectionChangedEventHandler? CollectionChanged;
        }

        private class NonRecyclingItemsControl : ItemsControl
        {
            protected override Type StyleKeyOverride => typeof(ItemsControl);

            protected internal override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
            {
                recycleKey = null;
                return true;
            }
        }
    }
}
