using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.Utils;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Controls;

public partial class ItemsSourceView : IWeakEventSubscriber<PropertyChangedEventArgs>
{
    private static readonly Lazy<ConditionalWeakTable<NotifyCollectionChangedEventArgs, NotifyCollectionChangedEventArgs?>> s_rewrittenCollectionChangedEvents = new();
    private static readonly ThreadLocal<CompoundSorter> s_compoundSorter = new();

    private static NotifyCollectionChangedEventArgs? GetRewrittenEvent(NotifyCollectionChangedEventArgs e)
    {
        if (s_rewrittenCollectionChangedEvents.IsValueCreated && s_rewrittenCollectionChangedEvents.Value.TryGetValue(e, out var rewritten))
        {
            return rewritten;
        }

        return e;
    }

    private (List<object?> items, List<int> indexMap, HashSet<string> invalidationProperties)? _layersState;

    private readonly Lazy<Dictionary<INotifyPropertyChanged, int>> _propertyChangedSubscriptions = new();
    private Dictionary<INotifyPropertyChanged, int> PropertyChangedSubscriptions => _propertyChangedSubscriptions.Value;

    private int _deferredRefreshDepth;
    /// <summary>
    /// Gets whether there are any undisposed <see cref="DeferredRefreshScope"/> objects for this <see cref="ItemsSourceView"/>.
    /// </summary>
    private bool DeferredRefreshActive => _deferredRefreshDepth != 0;

    internal static int[]? GetDiagnosticItemMap(ItemsSourceView itemsSourceView) => itemsSourceView._layersState?.indexMap.ToArray();

    /// <summary>
    /// Gets a list of the <see cref="ItemFilter"/> objects owned by this <see cref="ItemsSourceView"/>.
    /// </summary>
    public AvaloniaList<ItemFilter> Filters { get; }

    /// <summary>
    /// Gets whether any <see cref="ItemFilter"/> in the <see cref="Filters"/> collection is currently active.
    /// </summary>
    protected bool HasActiveFilters
    {
        get
        {
            for (int i = 0; i < Filters.Count; i++)
            {
                if (Filters[i].IsActive)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Gets a list of the <see cref="ItemSorter"/> objects owned by this <see cref="ItemsSourceView"/>.
    /// </summary>
    public AvaloniaList<ItemSorter> Sorters { get; }

    /// <summary>
    /// Gets whether any <see cref="ItemSorter"/> in the <see cref="Sorters"/> collection is currently active.
    /// </summary>
    protected bool HasActiveSorters
    {
        get
        {
            for (int i = 0; i < Sorters.Count; i++)
            {
                if (Sorters[i].IsActive)
                    return true;
            }
            return false;
        }
    }

    protected bool HasActiveLayers => HasActiveFilters || HasActiveSorters;

    private static void BlockUpdateFromBackgroundThreads()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            throw new InvalidThreadException();
        }
    }

    private static void ValidateLayer(ItemsSourceViewLayer layer)
    {
        Dispatcher.UIThread.VerifyAccess();

        if (layer == null)
        {
            throw new InvalidOperationException($"Cannot add null to this collection.");
        }
    }

    private void OnLayersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var owner = _owner.Target as AvaloniaObject;

        if (e.OldItems != null)
        {
            for (int i = 0; i < e.OldItems.Count; i++)
            {
                var layer = (ItemsSourceViewLayer)e.OldItems[i]!;
                layer.Invalidated -= OnLayerInvalidated;
                if (owner != null)
                {
                    layer.Detach(owner);
                }
            }
        }

        if (e.NewItems != null)
        {
            for (int i = 0; i < e.NewItems.Count; i++)
            {
                var layer = (ItemsSourceViewLayer)e.NewItems[i]!;
                layer.Invalidated += OnLayerInvalidated;
                if (owner != null)
                {
                    layer.Attach(owner);
                }
            }
        }

        Refresh();
    }

    private void OnLayerInvalidated(object? sender, EventArgs e)
    {
        Refresh();
    }

    /// <summary>
    /// While any <see cref="DeferredRefreshScope"/> returned from this method is active, the <see cref="ItemsSourceView"/> will not raise <see cref="INotifyCollectionChanged"/>
    /// events, nor will it evaluate any <see cref="Filters"/> or <see cref="Sorters"/>. When all scopes for a given <see cref="ItemsSourceView"/> have exited, <see cref="Refresh()"/>
    /// will automatically be called on the UI thread.
    /// </summary>
    /// <remarks>
    /// Scopes may be entered or exited from any thread. The <see cref="Source"/> collection can be safely modified from a background thread while a
    /// <see cref="DeferredRefreshScope"/> is active.
    /// </remarks>
    /// <returns>
    /// A disposable object (both <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/>) which represents a deferred refresh scope. When this object is disposed, the scope will exit.
    /// </returns>
    public DeferredRefreshScope EnterDeferredRefreshScope() => new(this);

    /// <summary>
    /// Re-evaluates the current view of the <see cref="Source"/> collection and if necessary raises a <see cref="NotifyCollectionChangedAction.Reset"/> event.
    /// This method executes a full re-evaluation of each active <see cref="ItemsSourceViewLayer"/> object found in <see cref="Filters"/> and <see cref="Sorters"/>.
    /// </summary>
    /// <remarks>
    /// <para>If a <see cref="DeferredRefreshScope"/> is active, calling this method has no effect.</para>
    /// <para>Calling this method is often not necessary, if appropriate values have been provided to <see cref="ItemsSourceViewLayer.State"/>
    /// and/or <see cref="ItemsSourceViewLayer.InvalidationPropertyNames"/> on each layer.</para>
    /// </remarks>
    /// <exception cref="InvalidThreadException">Thrown if the method is called from any thread except <see cref="Dispatcher.UIThread"/>.</exception>
    public void Refresh()
    {
        if (DeferredRefreshActive)
        {
            return;
        }

        Refresh(applyingDeferredUpdates: false, raiseEvents: true);
    }

    private void Refresh(bool applyingDeferredUpdates, bool raiseEvents)
    {
        BlockUpdateFromBackgroundThreads();

        if (!HasActiveLayers)
        {
            if (_layersState == null)
            {
                if (raiseEvents && (_source is not INotifyCollectionChanged || applyingDeferredUpdates))
                    RaiseCollectionChanged(CollectionUtils.ResetEventArgs);
                return;
            }

            RemoveListenerIfNecessary();
            _layersState = null;
        }
        else
        {
            AddListenerIfNecessary();
            _layersState = EvaluateLayers();
        }

        if (raiseEvents)
            RaiseCollectionChanged(CollectionUtils.ResetEventArgs);
    }

    private IDisposable? EnterLayersBatchScope(bool filters = true, bool sorters = true)
    {
        Action? exitAllScopes = null;
        
        for (int i = 0; filters && i < Filters.Count; i++)
        {
            if (Filters[i].IsActive)
            {
                Filters[i].BeginBatchOperation();
                exitAllScopes += Filters[i].EndBatchOperation;
            }
        }

        for (int i = 0; sorters && i < Sorters.Count; i++)
        {
            if (Sorters[i].IsActive)
            {
                Sorters[i].BeginBatchOperation();
                exitAllScopes += Sorters[i].EndBatchOperation;
            }
        }

        return exitAllScopes == null ? null : Disposable.Create(exitAllScopes);
    }

    private (List<object?> items, List<int> indexMap, HashSet<string> invalidationProperties) EvaluateLayers()
    {
        var result = new List<object?>(_source.Count);
        var map = new List<int>(_source.Count);
        var viewIndexToSourceIndex = new List<int>(_source.Count);

        var invalidationProperties = new HashSet<string>();

        using var layerScopes = EnterLayersBatchScope();

        for (int i = 0; i < Filters.Count; i++)
        {
            if (Filters[i].IsActive)
                invalidationProperties.UnionWith(Filters[i].GetInvalidationPropertyNamesEnumerator());
        }
        for (int i = 0; i < Sorters.Count; i++)
        {
            if (Sorters[i].IsActive)
                invalidationProperties.UnionWith(Sorters[i].GetInvalidationPropertyNamesEnumerator());
        }

        Dictionary<INotifyPropertyChanged, int>? newPropertyChangedSubscriptions = null;
        if (invalidationProperties.Count > 0)
        {
            newPropertyChangedSubscriptions = new();
        }

        CompoundSorter? comparer = null;
        if (HasActiveSorters)
        {
            comparer = s_compoundSorter.Value ??= new();
            comparer.Sorters = Sorters;
        }

        try
        {
            int i = 0;
            // use an enumerator so that the IList implementation can manage the iteration, e.g. by throwing
            // an exception should the collection change during enumeration, or by enumerating over a local
            // copy of the collection.
            foreach (var item in _source)
            {
                if (newPropertyChangedSubscriptions != null && item is INotifyPropertyChanged inpc)
                {
                    if (newPropertyChangedSubscriptions.ContainsKey(inpc))
                    {
                        newPropertyChangedSubscriptions[inpc] += 1;
                    }
                    else
                    {
                        newPropertyChangedSubscriptions[inpc] = 1;
                    }
                }

                if (ItemPassesFilters(Filters, item))
                {
                    if (comparer != null)
                    {
                        var index = result.BinarySearch(item, comparer);
                        if (index < 0)
                        {
                            index = ~index;
                        }

                        viewIndexToSourceIndex.Insert(index, i);
                        result.Insert(index, item);
                    }
                    else
                    {
                        viewIndexToSourceIndex.Add(i);
                        result.Add(item);
                    }
                }

                i++;
            }

            map.InsertMany(0, -1, _source.Count);

            for (i = 0; i < viewIndexToSourceIndex.Count; i++)
            {
                map[viewIndexToSourceIndex[i]] = i;
            }

            return (result, map, invalidationProperties);
        }
        finally
        {
            if (comparer != null)
            {
                comparer.Sorters = null;
            }

            if (newPropertyChangedSubscriptions != null)
            {
                foreach (var kvp in newPropertyChangedSubscriptions)
                {
                    if (!PropertyChangedSubscriptions.ContainsKey(kvp.Key))
                    {
                        WeakEvents.ThreadSafePropertyChanged.Subscribe(kvp.Key, this);
                    }

                    PropertyChangedSubscriptions[kvp.Key] = kvp.Value;
                }

                List<INotifyPropertyChanged>? toRemove = null;
                foreach (var inpc in PropertyChangedSubscriptions.Keys)
                {
                    if (!newPropertyChangedSubscriptions.ContainsKey(inpc))
                    {
                        WeakEvents.ThreadSafePropertyChanged.Unsubscribe(inpc, this);
                        (toRemove ??= new()).Add(inpc);
                    }
                }

                if (toRemove != null)
                {
                    for (int i = 0; i < toRemove.Count; i++)
                    {
                        PropertyChangedSubscriptions.Remove(toRemove[i]);
                    }
                }
            }
        }
    }

    private static bool ItemPassesFilters(IList<ItemFilter> itemFilters, object? item)
    {
        for (int i = 0; i < itemFilters.Count; i++)
        {
            if (itemFilters[i].IsActive && !itemFilters[i].FilterItem(item))
            {
                return false;
            }
        }
        return true;
    }

    void IWeakEventSubscriber<PropertyChangedEventArgs>.OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e)
    {
        if (DeferredRefreshActive)
        {
            return;
        }

        BlockUpdateFromBackgroundThreads();

        if (sender is not INotifyPropertyChanged inpc
            || _layersState is not { } layersState
            || e.PropertyName is not { } propertyName
            || !layersState.invalidationProperties.Contains(propertyName))
        {
            return;
        }

        bool? passes = null;
        IDisposable? layerScopes = null;

        try
        {
            for (int sourceIndex = 0; sourceIndex < Source.Count; sourceIndex++)
            {
                if (Source[sourceIndex] != sender)
                {
                    continue;
                }

                // If a collection doesn't raise CollectionChanged events, we aren't able to unsubscribe from stale items.
                // So we can sometimes receive this event from items which are no longer in the collection. Don't execute
                // the filter until we are sure that the item is still present.
                if (passes == null)
                {
                    layerScopes = EnterLayersBatchScope();
                    passes = ItemPassesFilters(Filters, sender);
                }

                switch ((layersState.indexMap[sourceIndex], passes))
                {
                    case (-1, true):
                        {
                            var viewIndex = ViewIndex(sourceIndex, sender);

                            layersState.indexMap[sourceIndex] = viewIndex;
                            ShiftIndexMapOnViewChanged(sourceIndex + 1, 1);

                            layersState.items.Insert(viewIndex, sender);
                            RaiseCollectionChanged(new(NotifyCollectionChangedAction.Add, sender, viewIndex));
                        }
                        break;

                    case (int viewIndex, true) when HasActiveSorters:
                        // To perform a correct binary search, the rest of the list must already be sorted. Since the changed item may
                        // now have a stale index, this means that we have to remove it from the list before identifying the new index.
                        layersState.items.RemoveAt(viewIndex);
                        var newViewIndex = ViewIndex(sourceIndex, sender);
                        layersState.items.Insert(newViewIndex, sender);

                        if (newViewIndex != viewIndex)
                        {
                            var delta = newViewIndex > viewIndex ? -1 : 1;
                            var (start, end) = (Math.Min(newViewIndex, viewIndex), Math.Max(newViewIndex, viewIndex));

                            for (int i = 0; i < layersState.indexMap.Count; i++)
                            {
                                if (layersState.indexMap[i] >= start && layersState.indexMap[i] < end)
                                {
                                    layersState.indexMap[i] += delta;
                                }
                            }

                            layersState.indexMap[sourceIndex] = newViewIndex;

                            RaiseCollectionChanged(new(NotifyCollectionChangedAction.Move, sender, newViewIndex, viewIndex));
                        }
                        break;
                    case (int viewIndex, false):
                        layersState.indexMap[sourceIndex] = -1;
                        ShiftIndexMapOnViewChanged(sourceIndex + 1, -1);
                        layersState.items.RemoveAt(viewIndex);
                        RaiseCollectionChanged(new(NotifyCollectionChangedAction.Remove, sender, viewIndex));
                        break;
                }
            }
        }
        finally
        {
            layerScopes?.Dispose();
        }

        if (passes == null) // item is no longer in the collection, we can unsubscribe
        {
            Debug.Assert(PropertyChangedSubscriptions.ContainsKey(inpc));
            WeakEvents.ThreadSafePropertyChanged.Unsubscribe(inpc, this);
            PropertyChangedSubscriptions.Remove(inpc);
        }
    }

    private void UpdateLayersForCollectionChangedEvent(NotifyCollectionChangedEventArgs e)
    {
        BlockUpdateFromBackgroundThreads();

        if (_layersState is not { } layersState)
        {
            throw new InvalidOperationException("Layers not initialised.");
        }

        using var layerScopes = e.Action switch 
        { 
            NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace => EnterLayersBatchScope(),
            NotifyCollectionChangedAction.Move => EnterLayersBatchScope(filters: false),
            _ => null
        };

        NotifyCollectionChangedEventArgs? rewrittenArgs;

        List<object?>? viewItems = null;
        int? viewStartIndex = null; // null means that item sorting has lead to multiple discontinuous changes, in which case we issue a single Reset event instead

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                Debug.Assert(e.NewItems != null);

                for (var i = 0; i < e.NewItems.Count; i++)
                {
                    var sourceIndex = e.NewStartingIndex + i;
                    if (ItemPassesFilters(Filters, e.NewItems[i]))
                    {
                        var viewIndex = ViewIndex(sourceIndex, e.NewItems[i]);
                        if (viewItems == null)
                        {
                            viewItems = new(e.NewItems.Count);
                            viewStartIndex = viewIndex;
                        }

                        if (viewStartIndex.HasValue && viewIndex != viewStartIndex + i)
                        {
                            viewStartIndex = null;
                        }

                        // during add operations this has to be done incrementally, so that ViewIndex can find the right result
                        // for the next item.
                        ShiftIndexMapOnSourceChanged(sourceIndex, 1);

                        layersState.items.Insert(viewIndex, e.NewItems[i]);
                        layersState.indexMap.Insert(sourceIndex, viewIndex);
                        viewItems.Add(e.NewItems[i]);
                    }
                    else
                    {
                        layersState.indexMap.Insert(sourceIndex, -1);
                    }

                    if (layersState.invalidationProperties.Count > 0 && e.NewItems[i] is INotifyPropertyChanged inpc)
                    {
                        if (PropertyChangedSubscriptions.ContainsKey(inpc))
                        {
                            PropertyChangedSubscriptions[inpc] += 1;
                        }
                        else
                        {
                            PropertyChangedSubscriptions[inpc] = 1;
                            WeakEvents.ThreadSafePropertyChanged.Subscribe(inpc, this);
                        }
                    }
                }

                if (viewItems != null)
                {
                    rewrittenArgs = viewStartIndex == null ? CollectionUtils.ResetEventArgs : new(NotifyCollectionChangedAction.Add, viewItems, viewStartIndex.Value);
                }
                else
                {
                    rewrittenArgs = null;
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                Debug.Assert(e.OldItems != null);
                for (var i = 0; i < e.OldItems.Count; i++)
                {
                    var sourceIndex = e.OldStartingIndex + i;
                    var viewIndex = layersState.indexMap[sourceIndex];

                    if (viewIndex != -1)
                    {
                        if (viewItems == null)
                        {
                            viewItems = new(e.OldItems.Count);
                            viewStartIndex = viewIndex;
                        }

                        if (viewStartIndex.HasValue && viewIndex != viewStartIndex + i)
                        {
                            viewStartIndex = null;
                        }

                        layersState.items.RemoveAt(viewIndex);
                        ShiftIndexMapOnSourceChanged(viewIndex, -1);

                        viewItems.Add(e.OldItems[i]);
                    }

                    layersState.indexMap.RemoveAt(sourceIndex);

                    if (layersState.invalidationProperties.Count > 0 && e.OldItems[i] is INotifyPropertyChanged inpc)
                    {
                        if (PropertyChangedSubscriptions.TryGetValue(inpc, out var subscribeCount))
                        {
                            if (subscribeCount <= 1)
                            {
                                Debug.Assert(subscribeCount == 1);
                                WeakEvents.ThreadSafePropertyChanged.Unsubscribe(inpc, this);
                                PropertyChangedSubscriptions.Remove(inpc);
                            }
                            else
                            {
                                PropertyChangedSubscriptions[inpc] -= 1;
                            }
                        }
                        else
                        {
                            Debug.Fail("An INotifyPropertyChanged object was removed, but there is no record of a PropertyChanged subscription for it.");
                        }
                    }
                }

                if (viewItems != null)
                {
                    rewrittenArgs = viewStartIndex == null ? CollectionUtils.ResetEventArgs : new(NotifyCollectionChangedAction.Remove, viewItems, viewStartIndex.Value);
                }
                else
                {
                    rewrittenArgs = null;
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                throw new NotImplementedException();
            case NotifyCollectionChangedAction.Reset:
                Refresh();
                rewrittenArgs = null;
                break;
            case NotifyCollectionChangedAction.Move:
                throw new NotImplementedException();
            default:
                throw new NotImplementedException();
        }

        s_rewrittenCollectionChangedEvents.Value.Add(e, rewrittenArgs);
    }

    private void ShiftIndexMapOnViewChanged(int inclusiveSourceStartIndex, int delta)
    {
        var map = _layersState!.Value.indexMap;
        for (var i = inclusiveSourceStartIndex; i < map.Count; i++)
        {
            if (map[i] != -1)
            {
                map[i] += delta;
            }
        }
    }

    private void ShiftIndexMapOnSourceChanged(int inclusiveViewStartIndex, int delta)
    {
        var map = _layersState!.Value.indexMap;
        for (var i = 0; i < map.Count; i++)
        {
            if (map[i] >= inclusiveViewStartIndex)
            {
                map[i] += delta;
            }
        }
    }

    private int ViewIndex(int sourceIndex, object? item)
    {
        if (_layersState is not { } layersState)
        {
            throw new InvalidOperationException("Layers not initialised.");
        }

        if (HasActiveSorters)
        {
            var sorter = s_compoundSorter.Value ??= new();
            sorter.Sorters = Sorters;
            try
            {
                var searchResult = layersState.items.BinarySearch(item, sorter);

                return searchResult < 0 ? ~searchResult : searchResult;
            }
            finally
            {
                sorter.Sorters = null;
            }
        }

        var candidateIndex = sourceIndex;
        var insertAtEnd = candidateIndex >= layersState.indexMap.Count;

        if (insertAtEnd)
        {
            candidateIndex = layersState.indexMap.Count - 1;
        }

        int filteredIndex;
        do
        {
            if (candidateIndex == -1)
            {
                return 0;
            }

            filteredIndex = layersState.indexMap[candidateIndex--];
        }
        while (filteredIndex < 0);

        return filteredIndex + (insertAtEnd ? 1 : 0);
    }

    private class CompoundSorter : IComparer<object?>
    {
        public IList<ItemSorter>? Sorters { get; set; }

        public int Compare(object? x, object? y)
        {
            if (Sorters == null)
            {
                throw new InvalidOperationException("No sorters provided");
            }

            for (int i = 0; i < Sorters.Count; i++)
            {
                if (!Sorters[i].IsActive)
                {
                    continue;
                }

                var comparison = Sorters[i].Compare(x, y);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return -1; // this default result will give us the source collection's order
        }
    }

    /// <seealso cref="EnterDeferredRefreshScope"/>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Shared dispose method")]
    public sealed class DeferredRefreshScope : IDisposable, IAsyncDisposable
    {
        private readonly object _disposeLock = new();

        private ItemsSourceView? _owner;
        private Task? _disposeTask;

        public DeferredRefreshScope(ItemsSourceView owner)
        {
            _owner = owner;
            Interlocked.Increment(ref _owner._deferredRefreshDepth);
        }

        public void Dispose() => DisposeInternal().GetAwaiter().GetResult();

        public ValueTask DisposeAsync() => new(DisposeInternal());

        private Task DisposeInternal()
        {
            GC.SuppressFinalize(this);
            lock (_disposeLock)
            {
                if (_owner is { } owner)
                {
                    _owner = null;
                    if (Interlocked.Decrement(ref owner._deferredRefreshDepth) == 0)
                    {
                        if (Dispatcher.UIThread.CheckAccess())
                        {
                            ApplyDeferredUpdates(owner);
                            _disposeTask = Task.CompletedTask;
                        }
                        else
                        {
                            _disposeTask = Dispatcher.UIThread.InvokeAsync(() => ApplyDeferredUpdates(owner)).GetTask();
                        }
                    }
                    else
                    {
                        _disposeTask = Task.CompletedTask;
                    }
                }
            }

            Debug.Assert(_owner == null);
            Debug.Assert(_disposeTask != null);

            return _disposeTask;
        }

        ~DeferredRefreshScope()
        {
            lock (_disposeLock)
            {
                if (_owner is { } owner)
                {
                    _owner = null;
                    if (Interlocked.Decrement(ref owner._deferredRefreshDepth) == 0)
                    {
                        Dispatcher.UIThread.Post(() => ApplyDeferredUpdates(owner));
                    }
                    Logging.Logger.TryGet(Logging.LogEventLevel.Warning, nameof(ItemsSourceView))?.Log(owner, $"A {nameof(DeferredRefreshScope)} was finalized without having been disposed.");
                }
            }
        }

        private static void ApplyDeferredUpdates(ItemsSourceView owner) => owner.Refresh(applyingDeferredUpdates: true, raiseEvents: true);
    }

    /// <summary>
    /// Thrown when an <see cref="ItemsSourceView"/> with an active <see cref="ItemsSourceViewLayer"/> is asked to process a change to its source data on a background thread.
    /// </summary>
    public class InvalidThreadException : InvalidOperationException
    {
        public InvalidThreadException() : base($"{nameof(ItemsSourceView)} does not support data changes on background threads " +
            $"while any {nameof(ItemsSourceViewLayer)} is active. Either make these changes on the UI thread, or call " +
            $"{nameof(ItemsSourceView)}.{nameof(EnterDeferredRefreshScope)} to defer processing of data changes until they can be " +
            $"executed on the UI thread.")
        { }
    }
}
