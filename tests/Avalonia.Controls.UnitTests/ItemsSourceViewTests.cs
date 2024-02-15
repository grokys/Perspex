using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Avalonia.Collections;
using Avalonia.Diagnostics;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ItemsSourceViewTests
    {
        [Fact]
        public void Only_Subscribes_To_Source_CollectionChanged_When_CollectionChanged_Subscribed()
        {
            var source = new AvaloniaList<string>();
            var target = ItemsSourceView.GetOrCreate(source);
            var debug = (INotifyCollectionChangedDebug)source;

            Assert.Null(debug.GetCollectionChangedSubscribers());

            void Handler(object sender, NotifyCollectionChangedEventArgs e) { }
            target.CollectionChanged += Handler;

            Assert.NotNull(debug.GetCollectionChangedSubscribers());
            Assert.Equal(1, debug.GetCollectionChangedSubscribers().Length);

            target.CollectionChanged -= Handler;

            Assert.Null(debug.GetCollectionChangedSubscribers());
        }

        [Fact]
        public void Cannot_Create_ItemsSourceView_With_Collection_That_Implements_INCC_But_Not_List()
        {
            var source = new InvalidCollection();
            Assert.Throws<ArgumentException>(() => ItemsSourceView.GetOrCreate(source));
        }

        [Fact]
        public void Reassigning_Source_Unsubscribes_From_Previous_Source()
        {
            var source = new AvaloniaList<string>();
            var target = new ReassignableItemsSourceView(source);
            var debug = (INotifyCollectionChangedDebug)source;

            target.CollectionChanged += (s, e) => { };

            Assert.Equal(1, debug.GetCollectionChangedSubscribers().Length);

            target.SetSource(new string[0]);

            Assert.Null(debug.GetCollectionChangedSubscribers());
        }

        [Fact]
        public void Reassigning_Source_Subscribes_To_New_Source()
        {
            var source = new AvaloniaList<string>();
            var target = new ReassignableItemsSourceView(new string[0]);
            var debug = (INotifyCollectionChangedDebug)source;

            target.CollectionChanged += (s, e) => { };
            target.SetSource(source);

            Assert.Equal(1, debug.GetCollectionChangedSubscribers().Length);
        }

        [Fact]
        public void Filtered_View_Adds_New_Items()
        {
            var source = new AvaloniaList<string>() { "foo", "bar" };
            var target = ItemsSourceView.GetOrCreate(source);

            var collectionChangeEvents = new List<NotifyCollectionChangedEventArgs>();

            target.CollectionChanged += (s, e) => collectionChangeEvents.Add(e);

            target.Filters.Add(new FunctionItemFilter { Filter = (s, e) => e.Accept = !Equals(bool.FalseString, e.Item) });
            
            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChangeEvents[0].Action);
            collectionChangeEvents.Clear();

            source.Add(bool.FalseString);

            Assert.Empty(collectionChangeEvents);

            Assert.Equal(new int[] { 0, 1, -1 }, ItemsSourceView.GetDiagnosticItemMap(target));

            source.InsertRange(1, new[] { bool.TrueString, bool.TrueString });

            Assert.Equal(new int[] { 0, 1, 2, 3, -1 }, ItemsSourceView.GetDiagnosticItemMap(target));

            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Add, collectionChangeEvents[0].Action);
            Assert.Equal(1, collectionChangeEvents[0].NewStartingIndex);
            Assert.Equal(2, collectionChangeEvents[0].NewItems.Count);
            Assert.Equal(bool.TrueString, collectionChangeEvents[0].NewItems[0]);

            Assert.Equal(4, target.Count);
            Assert.Equal(bool.TrueString, target[1]);
            Assert.Equal(bool.TrueString, target[2]);

            source.Add(bool.TrueString);

            Assert.Equal(new int[] { 0, 1, 2, 3, -1, 4 }, ItemsSourceView.GetDiagnosticItemMap(target));

            Assert.Equal(5, target.Count);
            Assert.Equal(bool.TrueString, target[^1]);
        }

        [Fact]
        public void Filtered_View_Removes_Old_Items()
        {            
            var source = new AvaloniaList<string>() { "foo", "bar", bool.TrueString, bool.FalseString, bool.TrueString, "end" };
            var target = ItemsSourceView.GetOrCreate(source);

            var collectionChangeEvents = new List<NotifyCollectionChangedEventArgs>();

            target.CollectionChanged += (s, e) => collectionChangeEvents.Add(e);

            target.Filters.Add(new FunctionItemFilter { Filter = (s, e) => e.Accept = !Equals(bool.FalseString, e.Item) });

            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChangeEvents[0].Action);
            collectionChangeEvents.Clear();

            Assert.Equal(new int[] { 0, 1, 2, -1, 3, 4 }, ItemsSourceView.GetDiagnosticItemMap(target));

            source.RemoveAt(4);

            Assert.Equal(4, target.Count);
            Assert.Equal(new int[] { 0, 1, 2, -1, 3 }, ItemsSourceView.GetDiagnosticItemMap(target));

            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Remove, collectionChangeEvents[0].Action);
            Assert.Equal(3, collectionChangeEvents[0].OldStartingIndex);
            Assert.Equal(1, collectionChangeEvents[0].OldItems.Count);
            Assert.Equal(bool.TrueString, collectionChangeEvents[0].OldItems[0]);
            collectionChangeEvents.Clear();

            source.RemoveAt(3);
            Assert.Empty(collectionChangeEvents);
            Assert.Equal(4, target.Count);

            Assert.Equal(new int[] { 0, 1, 2, 3 }, ItemsSourceView.GetDiagnosticItemMap(target));
        }

        [Fact]
        public void Filtered_View_Resets_When_Source_Cleared()
        {            
            var source = new AvaloniaList<string>() { "foo", "bar" };
            var target = ItemsSourceView.GetOrCreate(source);

            var collectionChangeEvents = new List<NotifyCollectionChangedEventArgs>();

            target.CollectionChanged += (s, e) => collectionChangeEvents.Add(e);

            target.Filters.Add(new FunctionItemFilter { Filter = (s, e) => e.Accept = !Equals(bool.FalseString, e.Item) });
            
            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChangeEvents[0].Action);
            collectionChangeEvents.Clear();

            source.Clear();

            Assert.Equal(0, target.Count);
            Assert.Equal(Array.Empty<int>(), ItemsSourceView.GetDiagnosticItemMap(target));
            Assert.Equal(1, collectionChangeEvents.Count);
            Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChangeEvents[0].Action);
        }

        private class InvalidCollection : INotifyCollectionChanged, IEnumerable<string>
        {
            public event NotifyCollectionChangedEventHandler CollectionChanged { add { } remove { } }

            public IEnumerator<string> GetEnumerator()
            {
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                yield break;
            }
        }

        private class ReassignableItemsSourceView : ItemsSourceView
        {
            public ReassignableItemsSourceView(IEnumerable source)
                : base(null, source)
            {
            }

            public new void SetSource(IEnumerable source) => base.SetSource(source);
        }
    }
}
