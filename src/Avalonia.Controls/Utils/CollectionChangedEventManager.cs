using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Controls.Utils
{
    internal interface ICollectionChangedListener
    {
        void PreChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e);
        void Changed(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e);
        void PostChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e);
    }

    internal class CollectionChangedEventManager
    {
        public static CollectionChangedEventManager Instance { get; } = new CollectionChangedEventManager();

        private ConditionalWeakTable<INotifyCollectionChanged, Entry> _entries =
            new ConditionalWeakTable<INotifyCollectionChanged, Entry>();

        private CollectionChangedEventManager()
        {
        }

        public void AddListener(INotifyCollectionChanged collection, ICollectionChangedListener listener)
        {
            collection = collection ?? throw new ArgumentNullException(nameof(collection));
            listener = listener ?? throw new ArgumentNullException(nameof(listener));
            Dispatcher.UIThread.VerifyAccess();

            if (!_entries.TryGetValue(collection, out var entry))
            {
                entry = new Entry(collection);
                _entries.Add(collection, entry);
            }

            foreach (var l in entry.Listeners)
            {
                if (l.TryGetTarget(out var target) && target == listener)
                {
                    throw new InvalidOperationException(
                        "Collection listener already added for this collection/listener combination.");
                }
            }

            entry.Listeners.Add(new WeakReference<ICollectionChangedListener>(listener));
        }

        public void RemoveListener(INotifyCollectionChanged collection, ICollectionChangedListener listener)
        {
            collection = collection ?? throw new ArgumentNullException(nameof(collection));
            listener = listener ?? throw new ArgumentNullException(nameof(listener));
            Dispatcher.UIThread.VerifyAccess();

            if (_entries.TryGetValue(collection, out var entry))
            {
                var listeners = entry.Listeners;

                for (var i = 0; i < listeners.Count; ++i)
                {
                    if (listeners[i].TryGetTarget(out var target) && target == listener)
                    {
                        listeners.RemoveAt(i);

                        if (listeners.Count == 0)
                        {
                            entry.Dispose();
                            _entries.Remove(collection);
                        }

                        return;
                    }
                }
            }

            throw new InvalidOperationException(
                "Collection listener not registered for this collection/listener combination.");
        }

        private class Entry : IWeakEventSubscriber<NotifyCollectionChangedEventArgs>, IDisposable
        {
            private INotifyCollectionChanged _collection;

            public Entry(INotifyCollectionChanged collection)
            {
                _collection = collection;
                Listeners = new List<WeakReference<ICollectionChangedListener>>();
                WeakEvents.CollectionChanged.Subscribe(_collection, this);
            }

            public List<WeakReference<ICollectionChangedListener>> Listeners { get; }

            public void Dispose()
            {
                WeakEvents.CollectionChanged.Unsubscribe(_collection, this);
            }

            void IWeakEventSubscriber<NotifyCollectionChangedEventArgs>.
                OnEvent(object? notifyCollectionChanged, WeakEvent ev, NotifyCollectionChangedEventArgs e)
            {
                var listeners = Listeners.ToArray();

                foreach (var l in listeners)
                {
                    if (l.TryGetTarget(out var target))
                    {
                        target.PreChanged(_collection, e);
                    }
                }

                foreach (var l in listeners)
                {
                    if (l.TryGetTarget(out var target))
                    {
                        target.Changed(_collection, e);
                    }
                }

                foreach (var l in listeners)
                {
                    if (l.TryGetTarget(out var target))
                    {
                        target.PostChanged(_collection, e);
                    }
                }
            }
        }
    }
}
