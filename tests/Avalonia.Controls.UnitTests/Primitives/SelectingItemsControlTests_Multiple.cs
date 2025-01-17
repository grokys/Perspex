using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class SelectingItemsControlTests_Multiple
    {
        [Fact]
        public void Setting_SelectedIndex_Should_Add_To_SelectedItems()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

            target.SelectedIndex = 1;

            Assert.Equal(new[] { "bar" }, target.SelectedItems.Cast<object>().ToList());
        }

        [Fact]
        public void Adding_SelectedItems_Should_Set_SelectedIndex()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

            target.SelectedItems.Add("bar");

            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void Assigning_Single_SelectedItems_Should_Set_SelectedIndex()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

            target.SelectedItems = new AvaloniaList<object>("bar");

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(new[] { "bar" }, target.SelectedItems);
            Assert.Equal(new[] { 1 }, SelectedContainers(target));
        }

        [Fact]
        public void Assigning_Multiple_SelectedItems_Should_Set_SelectedIndex()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

            target.SelectedItems = new AvaloniaList<string>("foo", "bar", "baz");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { "foo", "bar", "baz" }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));
        }

        [Fact]
        public void Selected_Items_Should_Be_Marked_When_Panel_Created_After_SelectedItems_Is_Set()
        {
            // Issue #2565.
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" }, performLayout: false);

            Assert.Null(target.ItemsPanelRoot);
            target.SelectedItems = new AvaloniaList<string>("foo", "bar", "baz");

            var root = Assert.IsType<TestRoot>(target.GetVisualRoot());
            root.LayoutManager.ExecuteInitialLayoutPass();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { "foo", "bar", "baz" }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));
        }

        [Fact]
        public void Reassigning_SelectedItems_Should_Clear_Selection()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

            target.SelectedItems.Add("bar");
            target.SelectedItems = new AvaloniaList<object>();

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Adding_First_SelectedItem_Should_Raise_SelectedIndex_SelectedItem_Changed()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar" });
            var indexRaised = false;
            var itemRaised = false;

            target.PropertyChanged += (s, e) =>
            {
                indexRaised |= e.Property.Name == "SelectedIndex" &&
                    (int)e.OldValue! == -1 &&
                    (int)e.NewValue! == 1;
                itemRaised |= e.Property.Name == "SelectedItem" &&
                    (string?)e.OldValue == null &&
                    (string?)e.NewValue == "bar";
            };

            target.SelectedItems.Add("bar");

            Assert.True(indexRaised);
            Assert.True(itemRaised);
        }

        [Fact]
        public void Adding_Subsequent_SelectedItems_Should_Not_Raise_SelectedIndex_SelectedItem_Changed()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

            target.SelectedItems.Add("foo");

            bool raised = false;
            target.PropertyChanged += (s, e) =>
                raised |= e.Property.Name == "SelectedIndex" ||
                          e.Property.Name == "SelectedItem";

            target.SelectedItems.Add("bar");

            Assert.False(raised);
        }

        [Fact]
        public void Removing_Last_SelectedItem_Should_Raise_SelectedIndex_Changed()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

            target.SelectedItems.Add("foo");

            bool raised = false;
            target.PropertyChanged += (s, e) =>
                raised |= e.Property.Name == "SelectedIndex" &&
                          (int)e.OldValue! == 0 &&
                          (int)e.NewValue! == -1;

            target.SelectedItems.RemoveAt(0);

            Assert.True(raised);
        }

        [Fact]
        public void Adding_SelectedItems_Should_Set_Item_IsSelected()
        {
            using var app = Start();
            var items = new[]
            {
                new ListBoxItem(),
                new ListBoxItem(),
                new ListBoxItem(),
            };

            var target = CreateTarget(items: items);

            target.SelectedItems.Add(target.Items[0]);
            target.SelectedItems.Add(target.Items[1]);

            Assert.True(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
            Assert.False(items[2].IsSelected);
        }

        [Fact]
        public void Assigning_SelectedItems_Should_Set_Item_IsSelected()
        {
            using var app = Start();
            var items = new[]
            {
                new ListBoxItem(),
                new ListBoxItem(),
                new ListBoxItem(),
            };

            var target = CreateTarget(items: items);

            target.SelectedItems = new AvaloniaList<object> { items[0], items[1] };

            Assert.True(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
            Assert.False(items[2].IsSelected);
        }

        [Fact]
        public void Removing_SelectedItems_Should_Clear_Item_IsSelected()
        {
            using var app = Start();
            var items = new[]
            {
                new ListBoxItem(),
                new ListBoxItem(),
                new ListBoxItem(),
            };

            var target = CreateTarget(items: items);

            target.SelectedItems.Add(items[0]);
            target.SelectedItems.Add(items[1]);
            target.SelectedItems.Remove(items[1]);

            Assert.True(items[0].IsSelected);
            Assert.False(items[1].IsSelected);
        }

        [Fact]
        public void Reassigning_SelectedItems_Should_Not_Clear_Item_IsSelected()
        {
            using var app = Start();
            var items = new[]
            {
                new ListBoxItem(),
                new ListBoxItem(),
                new ListBoxItem(),
            };

            var target = CreateTarget(items: items);

            target.SelectedItems.Add(target.Items[0]);
            target.SelectedItems.Add(target.Items[1]);
            target.SelectedItems = new AvaloniaList<object> { items[0], items[1] };

            Assert.True(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
            Assert.False(items[2].IsSelected);
        }

        [Fact]
        public void Setting_SelectedIndex_Should_Unmark_Previously_Selected_Containers()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

            target.SelectedItems.Add("foo");
            target.SelectedItems.Add("bar");

            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));

            target.SelectedIndex = 2;

            Assert.Equal(new[] { 2 }, SelectedContainers(target));
        }

        [Fact]
        public void Range_Select_Should_Select_Range()
        {
            using var app = Start();
            var items = new[]
            {
                "foo",
                "bar",
                "baz",
                "qux",
                "qiz",
                "lol",
            };

            var target = CreateTarget(items: items);

            target.SelectedIndex = 1;
            target.SelectRange(3);

            Assert.Equal(new[] { "bar", "baz", "qux" }, target.SelectedItems.Cast<object>().ToList());
        }

        [Fact]
        public void Range_Select_Backwards_Should_Select_Range()
        {
            using var app = Start();
            var items = new[]
            {
                "foo",
                "bar",
                "baz",
                "qux",
                "qiz",
                "lol",
            };

            var target = CreateTarget(items: items);

            target.SelectedIndex = 3;
            target.SelectRange(1);

            Assert.Equal(new[] { "qux", "bar", "baz" }, target.SelectedItems.Cast<object>().ToList());
        }

        [Fact]
        public void Second_Range_Select_Backwards_Should_Select_From_Original_Selection()
        {
            using var app = Start();
            var items = new[]
            {
                "foo",
                "bar",
                "baz",
                "qux",
                "qiz",
                "lol",
            };

            var target = CreateTarget(items: items);

            target.SelectedIndex = 2;
            target.SelectRange(5);
            target.SelectRange(4);

            Assert.Equal(new[] { "baz", "qux", "qiz" }, target.SelectedItems.Cast<object>().ToList());
        }

        [Fact]
        public void Setting_SelectedIndex_After_Range_Should_Unmark_Previously_Selected_Containers()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz", "qux" });

            target.SelectRange(2);

            Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));

            target.SelectedIndex = 3;

            Assert.Equal(new[] { 3 }, SelectedContainers(target));
        }

        [Fact]
        public void Toggling_Selection_After_Range_Should_Work()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz", "foo", "bar", "baz" });

            target.SelectRange(3);

            Assert.Equal(new[] { 0, 1, 2, 3 }, SelectedContainers(target));

            target.Toggle(4);

            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, SelectedContainers(target));
        }

        [Fact]
        public void Suprious_SelectedIndex_Changes_Should_Not_Be_Triggered()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

            var selectedIndexes = new List<int>();
            target.GetObservable(TestSelector.SelectedIndexProperty).Subscribe(x => selectedIndexes.Add(x));

            target.SelectedItems = new AvaloniaList<object> { "bar", "baz" };
            target.SelectedItem = "foo";

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { -1, 1, 0 }, selectedIndexes);
        }

        [Fact]
        public void Can_Set_SelectedIndex_To_Another_Selected_Item()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

            target.SelectedItems.Add("foo");
            target.SelectedItems.Add("bar");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { "foo", "bar" }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));

            var raised = false;
            target.SelectionChanged += (s, e) =>
            {
                raised = true;
                Assert.Empty(e.AddedItems);
                Assert.Equal(new[] { "foo" }, e.RemovedItems);
            };

            target.SelectedIndex = 1;

            Assert.True(raised);
            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(new[] { "bar" }, target.SelectedItems);
            Assert.Equal(new[] { 1 }, SelectedContainers(target));
        }

        /// <summary>
        /// Tests a problem discovered with ListBox with selection.
        /// </summary>
        /// <remarks>
        /// - Items is bound to DataContext first, followed by say SelectedIndex
        /// - When the ListBox is removed from the visual tree, DataContext becomes null (as it's
        ///   inherited)
        /// - This changes Items to null, which changes SelectedIndex to null as there are no
        ///   longer any items
        /// - However, the news that DataContext is now null hasn't yet reached the SelectedItems
        ///   binding and so the unselection is sent back to the ViewModel
        /// 
        /// This is a similar problem to that tested by XamlBindingTest.Should_Not_Write_To_Old_DataContext.
        /// However, that tests a general property binding problem: here we are writing directly
        /// to the SelectedItems collection - not via a binding - so it's something that the 
        /// binding system cannot solve. Instead we solve it by not clearing SelectedItems when
        /// DataContext is in the process of changing.
        /// </remarks>
        [Fact]
        public void Should_Not_Write_SelectedItems_To_Old_DataContext()
        {
            using var app = Start();
            var vm = new OldDataContextViewModel();
            var target = CreateTarget();

            var itemsBinding = new Binding
            {
                Path = "Items",
                Mode = BindingMode.OneWay,
            };

            var selectedItemsBinding = new Binding
            {
                Path = "SelectedItems",
                Mode = BindingMode.OneWay,
            };

            // Bind ItemsSource and SelectedItems to the VM.
            target.Bind(TestSelector.ItemsSourceProperty, itemsBinding);
            target.Bind(TestSelector.SelectedItemsProperty, selectedItemsBinding);

            // Set DataContext and SelectedIndex
            target.DataContext = vm;
            target.SelectedIndex = 1;

            // Make sure SelectedItems are written back to VM.
            Assert.Equal(new[] { "bar" }, vm.SelectedItems);

            // Clear DataContext and ensure that SelectedItems is still set in the VM.
            target.DataContext = null;
            Assert.Equal(new[] { "bar" }, vm.SelectedItems);

            // Ensure target's SelectedItems is now clear.
            Assert.Empty(target.SelectedItems);
        }

        /// <summary>
        /// See <see cref="Should_Not_Write_SelectedItems_To_Old_DataContext"/>.
        /// </summary>
        [Fact]
        public void Should_Not_Write_SelectionModel_To_Old_DataContext()
        {
            using var app = Start();
            var vm = new OldDataContextViewModel();
            var target = CreateTarget();

            var itemsBinding = new Binding
            {
                Path = "Items",
                Mode = BindingMode.OneWay,
            };

            var selectionBinding = new Binding
            {
                Path = "Selection",
                Mode = BindingMode.OneWay,
            };

            // Bind ItemsSource and Selection to the VM.
            target.Bind(TestSelector.ItemsSourceProperty, itemsBinding);
            target.Bind(TestSelector.SelectionProperty, selectionBinding);

            // Set DataContext and SelectedIndex
            target.DataContext = vm;
            target.SelectedIndex = 1;

            // Make sure selection is written to selection model
            Assert.Equal(1, vm.Selection.SelectedIndex);

            // Clear DataContext and ensure that selection is still set in model.
            target.DataContext = null;
            Assert.Equal(1, vm.Selection.SelectedIndex);

            // Ensure target's SelectedItems is now clear.
            Assert.Empty(target.SelectedItems);
        }

        [Fact]
        public void Unbound_SelectedItems_Should_Be_Cleared_When_DataContext_Cleared()
        {
            using var app = Start();
            var data = new
            {
                Items = new[] { "foo", "bar", "baz" },
            };

            var target = CreateTarget(dataContext: data);
            var itemsBinding = new Binding { Path = "Items" };
            target.Bind(TestSelector.ItemsSourceProperty, itemsBinding);

            Assert.Same(data.Items, target.ItemsSource);

            target.SelectedItems.Add("bar");
            target.DataContext = null;

            Assert.Empty(target.SelectedItems);
        }

        [Fact]
        public void Adding_To_SelectedItems_Should_Raise_SelectionChanged()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });
            var called = false;

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { "bar" }, e.AddedItems.Cast<object>().ToList());
                Assert.Empty(e.RemovedItems);
                called = true;
            };

            target.SelectedItems.Add("bar");

            Assert.True(called);
        }

        [Fact]
        public void Removing_From_SelectedItems_Should_Raise_SelectionChanged()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });
            var called = false;

            target.SelectedItem = "bar";
            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { "bar" }, e.RemovedItems.Cast<object>().ToList());
                Assert.Empty(e.AddedItems);
                called = true;
            };

            target.SelectedItems.Remove("bar");

            Assert.True(called);
        }

        [Fact]
        public void Assigning_SelectedItems_Should_Raise_SelectionChanged()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

            target.SelectedItem = "bar";

            var called = false;

            target.SelectionChanged += (s, e) =>
            {
                Assert.Equal(new[] { "foo", "baz" }, e.AddedItems.Cast<object>());
                Assert.Equal(new[] { "bar" }, e.RemovedItems.Cast<object>());
                called = true;
            };

            target.SelectedItems = new AvaloniaList<object>("foo", "baz");

            Assert.True(called);
        }

        [Fact]
        public void SelectAll_Sets_SelectedIndex_And_SelectedItem()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

            target.SelectAll();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }

        [Fact]
        public void SelectAll_Raises_SelectionChanged_Event()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

            SelectionChangedEventArgs? receivedArgs = null;

            target.SelectionChanged += (_, args) => receivedArgs = args;

            target.SelectAll();

            Assert.NotNull(receivedArgs);
            Assert.Equal(target.ItemsSource, receivedArgs.AddedItems);
            Assert.Empty(receivedArgs.RemovedItems);
        }

        [Fact]
        public void UnselectAll_Clears_SelectedIndex_And_SelectedItem()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });

            target.SelectedIndex = 0;
            target.UnselectAll();

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Equal(null, target.SelectedItem);
        }

        [Fact]
        public void SelectAll_Handles_Duplicate_Items()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz", "foo", "bar", "baz" });

            target.SelectAll();

            Assert.Equal(new[] { "foo", "bar", "baz", "foo", "bar", "baz" }, target.SelectedItems);
        }

        [Fact]
        public void Adding_Item_Before_SelectedItems_Should_Update_Selection()
        {
            using var app = Start();
            var items = new ObservableCollection<string> { "foo", "bar", "baz" };
            var target = CreateTarget(itemsSource: items);

            target.SelectAll();
            items.Insert(0, "qux");
            Layout(target);

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
            Assert.Equal(new[] { "foo", "bar", "baz" }, target.SelectedItems);
            Assert.Equal(new[] { 1, 2, 3 }, SelectedContainers(target));
        }

        [Fact]
        public void Removing_Item_Before_SelectedItem_Should_Update_Selection()
        {
            using var app = Start();
            var items = new ObservableCollection<string> { "foo", "bar", "baz" };
            var target = CreateTarget(itemsSource: items);

            target.SelectedIndex = 1;
            target.SelectRange(2);

            Assert.Equal(new[] { "bar", "baz" }, target.SelectedItems);

            items.RemoveAt(0);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("bar", target.SelectedItem);
            Assert.Equal(new[] { "bar", "baz" }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));
        }

        [Fact]
        public void Removing_SelectedItem_With_Multiple_Selection_Active_Should_Update_Selection()
        {
            using var app = Start();
            var items = new ObservableCollection<string> { "foo", "bar", "baz" };
            var target = CreateTarget(itemsSource: items);

            target.SelectAll();
            items.RemoveAt(0);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("bar", target.SelectedItem);
            Assert.Equal(new[] { "bar", "baz" }, target.SelectedItems);
            Assert.Equal(new[] { 0, 1 }, SelectedContainers(target));
        }

        [Fact]
        public void Replacing_Selected_Item_Should_Update_SelectedItems()
        {
            using var app = Start();
            var items = new ObservableCollection<string> { "foo", "bar", "baz" };
            var target = CreateTarget(itemsSource: items);

            target.SelectAll();
            items[1] = "qux";

            Assert.Equal(new[] { "foo", "baz" }, target.SelectedItems);
        }

        [Fact]
        public void Adding_Selected_ItemContainers_Should_Update_Selection()
        {
            using var app = Start();
            var items = new[]
            {
                new TestContainer(),
                new TestContainer(),
            };

            var target = CreateTarget(items: items);

            target.Items.Add(new TestContainer { IsSelected = true });
            target.Items.Add(new TestContainer { IsSelected = true });

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal(target.Items[2], target.SelectedItem);
            Assert.Equal(new[] { target.Items[2], target.Items[3] }, target.SelectedItems);
        }

        [Fact]
        public void Adding_To_Selection_Should_Set_SelectedIndex()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

            target.SelectedItems.Add("bar");

            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void Assigning_Null_To_Selection_Should_Create_New_SelectionModel()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar" });
            var oldSelection = target.Selection;

            target.Selection = null!;

            Assert.NotNull(target.Selection);
            Assert.NotSame(oldSelection, target.Selection);
        }

        [Fact]
        public void Assigning_SelectionModel_With_Different_Source_To_Selection_Should_Fail()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar" });
            var selection = new SelectionModel<string> { Source = new[] { "baz" } };

            Assert.Throws<ArgumentException>(() => target.Selection = selection);
        }

        [Fact]
        public void Assigning_SelectionModel_With_Null_Source_To_Selection_Should_Set_Source()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar" });
            var selection = new SelectionModel<string>();

            target.Selection = selection;

            Assert.Equal(target.ItemsSource, selection.Source);
        }

        [Fact]
        public void Assigning_Single_Selected_Item_To_Selection_Should_Set_SelectedIndex()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar" });
            var selection = new SelectionModel<string> { SingleSelect = false };

            selection.Select(1);
            target.Selection = selection;

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(new[] { "bar" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 1 }, SelectedContainers(target));
        }

        [Fact]
        public void Assigning_Multiple_Selected_Items_To_Selection_Should_Set_SelectedIndex()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar", "baz" });
            var selection = new SelectionModel<string> { SingleSelect = false };

            selection.SelectRange(0, 2);
            target.Selection = selection;

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(new[] { "foo", "bar", "baz" }, target.Selection.SelectedItems);
            Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));
        }

        [Fact]
        public void Reassigning_Selection_Should_Clear_Selection()
        {
            using var app = Start();
            var target = CreateTarget(itemsSource: new[] { "foo", "bar" });

            target.Selection.Select(1);
            target.Selection = new SelectionModel<string>();

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Assigning_Selection_Should_Set_Item_IsSelected()
        {
            using var app = Start();
            var items = new[]
            {
                new ListBoxItem(),
                new ListBoxItem(),
                new ListBoxItem(),
            };

            var target = CreateTarget(items: items);
            var selection = new SelectionModel<object> { SingleSelect = false };

            selection.SelectRange(0, 1);
            target.Selection = selection;

            Assert.True(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
            Assert.False(items[2].IsSelected);
        }

        [Fact]
        public void Assigning_Selection_Should_Raise_SelectionChanged()
        {
            using var app = Start();
            var items = new[] { "foo", "bar", "baz" };
            var target = CreateTarget(itemsSource: items);
            var raised = 0;

            target.SelectedItem = "bar";

            target.SelectionChanged += (s, e) =>
            {
                if (raised == 0)
                {
                    Assert.Empty(e.AddedItems.Cast<object>());
                    Assert.Equal(new[] { "bar" }, e.RemovedItems.Cast<object>());
                }
                else
                {
                    Assert.Equal(new[] { "foo", "baz" }, e.AddedItems.Cast<object>());
                    Assert.Empty(e.RemovedItems.Cast<object>());
                }

                ++raised;
            };

            var selection = new SelectionModel<string> { Source = items, SingleSelect = false };
            selection.Select(0);
            selection.Select(2);
            target.Selection = selection;

            Assert.Equal(2, raised);
        }

        [Fact]
        public void Can_Bind_Initial_Selected_State_Via_ItemContainerTheme()
        {
            using var app = Start();
            var items = new ItemViewModel[] { new("Item 0", true), new("Item 1", false), new("Item 2", true) };
            var itemTheme = new ControlTheme(typeof(ContentPresenter))
            {
                Setters =
                {
                    new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected")),
                }
            };

            var target = CreateTarget(itemsSource: items, itemContainerTheme: itemTheme);

            Assert.Equal(new[] { 0, 2 }, SelectedContainers(target));
            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(items[0], target.SelectedItem);
            Assert.Equal(new[] { 0, 2 }, target.Selection.SelectedIndexes);
            Assert.Equal(new[] { items[0], items[2] }, target.Selection.SelectedItems);
        }

        [Fact]
        public void Can_Bind_Initial_Selected_State_Via_Style()
        {
            using var app = Start();
            var items = new ItemViewModel[] { new("Item 0", true), new("Item 1", false), new("Item 2", true) };
            var style = new Style(x => x.OfType<ContentPresenter>())
            {
                Setters =
                {
                    new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected")),
                }
            };

            var target = CreateTarget(itemsSource: items, styles: new[] { style });

            Assert.Equal(new[] { 0, 2 }, SelectedContainers(target));
            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(items[0], target.SelectedItem);
            Assert.Equal(new[] { 0, 2 }, target.Selection.SelectedIndexes);
            Assert.Equal(new[] { items[0], items[2] }, target.Selection.SelectedItems);
        }

        [Fact]
        public void Selection_State_Is_Updated_Via_IsSelected_Binding()
        {
            using var app = Start();
            var items = new ItemViewModel[] { new("Item 0", true), new("Item 1", false), new("Item 2", true) };
            var itemTheme = new ControlTheme(typeof(TestContainer))
            {
                BasedOn = CreateTestContainerTheme(),
                Setters =
                {
                    new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected")),
                }
            };

            // For the container selection state to be communicated back to the SelectingItemsControl
            // we need a container which raises the SelectingItemsControl.IsSelectedChangedEvent when
            // the IsSelected property changes.
            var target = CreateTarget<TestSelectorWithContainers>(
                itemsSource: items,
                itemContainerTheme: itemTheme);

            items[1].IsSelected = true;

            Assert.Equal(new[] { 0, 1, 2 }, SelectedContainers(target));
            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(items[0], target.SelectedItem);
            Assert.Equal(new[] { 0, 1, 2 }, target.Selection.SelectedIndexes);
            Assert.Equal(new[] { items[0], items[1], items[2] }, target.Selection.SelectedItems);

            items[0].IsSelected = false;

            Assert.Equal(new[] { 1, 2 }, SelectedContainers(target));
            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(items[1], target.SelectedItem);
            Assert.Equal(new[] { 1, 2 }, target.Selection.SelectedIndexes);
            Assert.Equal(new[] { items[1], items[2] }, target.Selection.SelectedItems);
        }

        [Fact]
        public void Selection_State_Is_Written_Back_To_Item_Via_IsSelected_Binding()
        {
            using var app = Start();
            var items = new ItemViewModel[] { new("Item 0", true), new("Item 1", false), new("Item 2", true) };
            var itemTheme = new ControlTheme(typeof(ContentPresenter))
            {
                Setters =
                {
                    new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected")),
                }
            };

            var target = CreateTarget(itemsSource: items, itemContainerTheme: itemTheme);
            var container0 = Assert.IsAssignableFrom<Control>(target.ContainerFromIndex(0));
            var container1 = Assert.IsAssignableFrom<Control>(target.ContainerFromIndex(1));

            SelectingItemsControl.SetIsSelected(container1, true);

            Assert.True(items[1].IsSelected);

            SelectingItemsControl.SetIsSelected(container0, false);

            Assert.False(items[0].IsSelected);
        }

        [Fact]
        public void Selection_Is_Updated_On_Container_Realization_With_IsSelected_Binding()
        {
            using var app = Start();
            var items = Enumerable.Range(0, 100).Select(x => new ItemViewModel($"Item {x}", false)).ToList();
            items[0].IsSelected = true;
            items[15].IsSelected = true;

            var itemTheme = new ControlTheme(typeof(ContentPresenter))
            {
                Setters =
                {
                    new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected")),
                    new Setter(Control.HeightProperty, 100.0),
                }
            };

            // Create a SelectingItemsControl with a virtualizing stack panel.
            var target = CreateTarget(itemsSource: items, itemContainerTheme: itemTheme, virtualizing: true);
            var panel = Assert.IsType<VirtualizingStackPanel>(target.ItemsPanelRoot);
            var scroll = panel.FindAncestorOfType<ScrollViewer>()!;

            // The SelectingItemsControl does not yet know anything about item 15's selection state.
            Assert.Equal(new[] { 0 }, SelectedContainers(target));
            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(items[0], target.SelectedItem);
            Assert.Equal(new[] { 0 }, target.Selection.SelectedIndexes);
            Assert.Equal(new[] { items[0] }, target.Selection.SelectedItems);

            // Scroll item 15 into view.
            scroll.Offset = new(0, 1000);
            Layout(target);

            Assert.Equal(10, panel.FirstRealizedIndex);
            Assert.Equal(19, panel.LastRealizedIndex);

            // The final selection should be in place.
            Assert.True(items[0].IsSelected);
            Assert.True(items[15].IsSelected);
            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(items[0], target.SelectedItem);
            Assert.Equal(new[] { 0, 15 }, target.Selection.SelectedIndexes);
            Assert.Equal(new[] { items[0], items[15] }, target.Selection.SelectedItems);

            // Although item 0 is selected, it's not realized.
            Assert.Equal(new[] { 15 }, SelectedContainers(target));
        }

        [Fact]
        public void Can_Change_Selection_For_Containers_Outside_Of_Viewport()
        {
            // Issue #11119
            using var app = Start();
            var items = Enumerable.Range(0, 100).Select(x => new TestContainer 
            { 
                Content = $"Item {x}",
                Height = 100,
            }).ToList();

            // Create a SelectingItemsControl with a virtualizing stack panel.
            var target = CreateTarget(itemsSource: items, virtualizing: true);
            target.AutoScrollToSelectedItem = false;

            var panel = Assert.IsType<VirtualizingStackPanel>(target.ItemsPanelRoot);
            var scroll = panel.FindAncestorOfType<ScrollViewer>()!;

            // Select item 1.
            target.SelectedIndex = 1;

            // Scroll item 1 and 2 out of view.
            scroll.Offset = new(0, 1000);
            Layout(target);

            Assert.Equal(10, panel.FirstRealizedIndex);
            Assert.Equal(19, panel.LastRealizedIndex);

            // Select item 2 now that items 1 and 2 are both unrealized.
            target.SelectedIndex = 2;

            // The selection should be updated.
            Assert.Empty(SelectedContainers(target));
            Assert.Equal(2, target.SelectedIndex);
            Assert.Same(items[2], target.SelectedItem);
            Assert.Equal(new[] { 2 }, target.Selection.SelectedIndexes);
            Assert.Equal(new[] { items[2] }, target.Selection.SelectedItems);

            // Scroll selected item back into view.
            scroll.Offset = new(0, 0);

            target.PropertyChanged += (s, e) =>
            {
                if (e.Property == SelectingItemsControl.SelectedIndexProperty)
                {
                }
            };

            Layout(target);

            // The selection should be preserved.
            Assert.Equal(new[] { 2 }, SelectedContainers(target));
            Assert.Equal(2, target.SelectedIndex);
            Assert.Same(items[2], target.SelectedItem);
            Assert.Equal(new[] { 2 }, target.Selection.SelectedIndexes);
            Assert.Equal(new[] { items[2] }, target.Selection.SelectedItems);
        }

        [Fact]
        public void Selection_Is_Not_Cleared_On_Recycling_Containers()
        {
            using var app = Start();
            var items = Enumerable.Range(0, 100).Select(x => new ItemViewModel($"Item {x}", false)).ToList();

            // Create a SelectingItemsControl that creates containers that raise IsSelectedChanged,
            // with a virtualizing stack panel.
            var target = CreateTarget<TestSelectorWithContainers>(
                itemsSource: items, 
                virtualizing: true);
            target.AutoScrollToSelectedItem = false;

            var panel = Assert.IsType<VirtualizingStackPanel>(target.ItemsPanelRoot);
            var scroll = panel.FindAncestorOfType<ScrollViewer>()!;

            // Select item 1.
            target.SelectedIndex = 1;

            // Scroll item 1 out of view.
            scroll.Offset = new(0, 1000);
            Layout(target);

            Assert.Equal(10, panel.FirstRealizedIndex);
            Assert.Equal(19, panel.LastRealizedIndex);

            // The selection should be preserved.
            Assert.Equal(new[] { 1 }, SelectedContainers(target));
            Assert.Equal(1, target.SelectedIndex);
            Assert.Same(items[1], target.SelectedItem);
            Assert.Equal(new[] { 1 }, target.Selection.SelectedIndexes);
            Assert.Equal(new[] { items[1] }, target.Selection.SelectedItems);
        }

        [Fact]
        public void Selection_State_Change_On_Unrealized_Item_Is_Respected_With_IsSelected_Binding()
        {
            using var app = Start();
            var items = Enumerable.Range(0, 100).Select(x => new ItemViewModel($"Item {x}", false)).ToList();
            var itemTheme = new ControlTheme(typeof(ContentPresenter))
            {
                Setters =
                {
                    new Setter(SelectingItemsControl.IsSelectedProperty, new Binding("IsSelected")),
                    new Setter(Control.HeightProperty, 100.0),
                }
            };

            // Create a SelectingItemsControl with a virtualizing stack panel.
            var target = CreateTarget(itemsSource: items, itemContainerTheme: itemTheme, virtualizing: true);
            var panel = Assert.IsType<VirtualizingStackPanel>(target.ItemsPanelRoot);
            var scroll = panel.FindAncestorOfType<ScrollViewer>()!;

            // Scroll item 1 out of view.
            scroll.Offset = new(0, 1000);
            Layout(target);

            Assert.Equal(10, panel.FirstRealizedIndex);
            Assert.Equal(19, panel.LastRealizedIndex);

            // Select item 1 now it's unrealized.
            items[1].IsSelected = true;

            // The SelectingItemsControl does not yet know anything about the selection change.
            Assert.Empty(SelectedContainers(target));
            Assert.Equal(-1, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
            Assert.Empty(target.Selection.SelectedIndexes);
            Assert.Empty(target.Selection.SelectedItems);

            // Scroll item 1 back into view.
            scroll.Offset = new(0, 0);
            Layout(target);

            // The item and container should be marked as selected.
            Assert.True(items[1].IsSelected);
            Assert.Equal(new[] { 1 }, SelectedContainers(target));
            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(items[1], target.SelectedItem);
            Assert.Equal(new[] { 1 }, target.Selection.SelectedIndexes);
            Assert.Equal(new[] { items[1] }, target.Selection.SelectedItems);
        }

        private static IEnumerable<int> SelectedContainers(SelectingItemsControl target)
        {
            Assert.NotNull(target.ItemsPanel);

            return target.ItemsPanelRoot!.Children
                .Select(x => SelectingItemsControl.GetIsSelected(x) ? target.IndexFromContainer(x) : -1)
                .Where(x => x != -1);
        }

        private static TestSelector CreateTarget(
            object? dataContext = null,
            IList? items = null,
            IList? itemsSource = null,
            ControlTheme? itemContainerTheme = null,
            IDataTemplate? itemTemplate = null,
            IEnumerable<Style>? styles = null,
            bool performLayout = true,
            bool virtualizing = false)
        {
            return CreateTarget<TestSelector>(
                dataContext:  dataContext,
                items: items,
                itemsSource: itemsSource,
                itemContainerTheme: itemContainerTheme,
                itemTemplate: itemTemplate,
                styles: styles,
                performLayout: performLayout,
                virtualizing: virtualizing);
        }

        private static T CreateTarget<T>(
            object? dataContext = null,
            IList? items = null,
            IList? itemsSource = null,
            ControlTheme? itemContainerTheme = null,
            IDataTemplate? itemTemplate = null,
            IEnumerable<Style>? styles = null,
            bool performLayout = true,
            bool virtualizing = false)
                where T : TestSelector, new()
        {
            var target = new T
            {
                DataContext = dataContext,
                ItemContainerTheme = itemContainerTheme,
                ItemTemplate = itemTemplate,
                ItemsSource = itemsSource,
                SelectionMode = SelectionMode.Multiple,
            };

            if (items is not null)
            {
                foreach (var item in items)
                    target.Items.Add(item);
            }

            if (virtualizing)
                target.ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel());

            var root = CreateRoot(target);

            if (styles is not null)
            {
                foreach (var style in styles)
                    root.Styles.Add(style);
            }

            if (performLayout)
                root.LayoutManager.ExecuteInitialLayoutPass();

            return target;
        }

        private static TestRoot CreateRoot(Control child)
        {
            return new TestRoot
            {
                Resources =
                {
                    { typeof(TestSelector), CreateTestSelectorControlTheme() },
                    { typeof(TestContainer), CreateTestContainerTheme() },
                    { typeof(ScrollViewer), CreateScrollViewerTheme() },
                },
                Child = child,
            };
        }

        private static ControlTheme CreateTestSelectorControlTheme()
        {
            return new ControlTheme(typeof(TestSelector))
            {
                Setters =
                {
                    new Setter(TreeView.TemplateProperty, CreateTestSelectorTemplate()),
                },
            };
        }

        private static FuncControlTemplate CreateTestSelectorTemplate()
        {
            return new FuncControlTemplate<ItemsControl>((parent, scope) =>
            {
                return new Border
                {
                    Background = new Media.SolidColorBrush(0xffffffff),
                    Child = new ScrollViewer
                    {
                        Name = "PART_ScrollViewer",
                        Content = new ItemsPresenter
                        {
                            Name = "PART_ItemsPresenter",
                            [~ItemsPresenter.ItemsPanelProperty] = parent[~ItemsControl.ItemsPanelProperty],
                        }.RegisterInNameScope(scope)
                    }.RegisterInNameScope(scope)
                };
            });
        }

        private static ControlTheme CreateTestContainerTheme()
        {
            return new ControlTheme(typeof(TestContainer))
            {
                Setters =
                {
                    new Setter(TestContainer.TemplateProperty, CreateTestContainerTemplate()),
                    new Setter(TestContainer.HeightProperty, 100.0),
                },
            };
        }

        private static FuncControlTemplate CreateTestContainerTemplate()
        {
            return new FuncControlTemplate<TestContainer>((parent, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = parent[!TestContainer.ContentProperty],
                    [!ContentPresenter.ContentTemplateProperty] = parent[!TestContainer.ContentTemplateProperty],
                }.RegisterInNameScope(scope));
        }

        private static ControlTheme CreateScrollViewerTheme()
        {
            return new ControlTheme(typeof(ScrollViewer))
            {
                Setters =
                {
                    new Setter(TreeView.TemplateProperty, CreateScrollViewerTemplate()),
                },
            };
        }

        private static FuncControlTemplate CreateScrollViewerTemplate()
        {
            return new FuncControlTemplate<ScrollViewer>((parent, scope) =>
                new Panel
                {
                    Children =
                    {
                        new ScrollContentPresenter
                        {
                            Name = "PART_ContentPresenter",
                        }.RegisterInNameScope(scope),
                        new ScrollBar
                        {
                            Name = "verticalScrollBar",
                        }
                    }
                });
        }

        private static void Layout(Control c)
        {
            (c.GetVisualRoot() as ILayoutRoot)?.LayoutManager.ExecuteLayoutPass();
        }

        public static IDisposable Start()
        {
            return UnitTestApplication.Start(
                TestServices.MockThreadingInterface.With(
                    focusManager: new FocusManager(),
                    fontManagerImpl: new HeadlessFontManagerStub(),
                    keyboardDevice: () => new KeyboardDevice(),
                    keyboardNavigation: () => new KeyboardNavigationHandler(),
                    inputManager: new InputManager(),
                    renderInterface: new HeadlessPlatformRenderInterface(),
                    textShaperImpl: new HeadlessTextShaperStub()));
        }

        private class TestSelector : SelectingItemsControl
        {
            public static readonly new AvaloniaProperty<IList?> SelectedItemsProperty =
                SelectingItemsControl.SelectedItemsProperty;
            public static readonly new DirectProperty<SelectingItemsControl, ISelectionModel> SelectionProperty =
                SelectingItemsControl.SelectionProperty;

            public TestSelector()
            {
                SelectionMode = SelectionMode.Multiple;
            }

            public new IList SelectedItems
            {
                get { return base.SelectedItems!; }
                set { base.SelectedItems = value; }
            }

            public new ISelectionModel Selection
            {
                get => base.Selection;
                set => base.Selection = value;
            }

            public new SelectionMode SelectionMode
            {
                get { return base.SelectionMode; }
                set { base.SelectionMode = value; }
            }

            public void SelectAll() => Selection.SelectAll();
            public void UnselectAll() => Selection.Clear();
            public void SelectRange(int index) => UpdateSelection(index, true, true);
            public void Toggle(int index) => UpdateSelection(index, true, false, true);
        }

        private class TestSelectorWithContainers : TestSelector
        {
            protected override Type StyleKeyOverride => typeof(TestSelector);

            protected internal override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
            {
                return new TestContainer();
            }

            protected internal override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
            {
                return NeedsContainer<TestContainer>(item, out recycleKey);
            }
        }

        private class TestContainer : ContentControl, ISelectable
        {
            public static readonly StyledProperty<bool> IsSelectedProperty =
                SelectingItemsControl.IsSelectedProperty.AddOwner<TestContainer>();

            static TestContainer()
            {
                SelectableMixin.Attach<TestContainer>(SelectingItemsControl.IsSelectedProperty);
            }

            public bool IsSelected
            {
                get => GetValue(IsSelectedProperty);
                set => SetValue(IsSelectedProperty, value);
            }
        }

        private class ItemViewModel : NotifyingBase
        {
            private bool _isSelected;

            public ItemViewModel(string value, bool isSelected = false)
            {
                Value = value;
                _isSelected = isSelected;
            }

            public string Value { get; set; }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected != value)
                    {
                        _isSelected = value;
                        RaisePropertyChanged();
                    }
                }
            }

            public override string ToString() => Value;
        }

        private class OldDataContextViewModel
        {
            public OldDataContextViewModel()
            {
                Items = new List<string> { "foo", "bar" };
                SelectedItems = new List<string>();
                Selection = new SelectionModel<string>();
            }

            public List<string> Items { get; }
            public List<string> SelectedItems { get; }
            public SelectionModel<string> Selection { get; }
        }
    }
}
