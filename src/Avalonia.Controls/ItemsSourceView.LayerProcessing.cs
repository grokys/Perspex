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

    internal static int[]? GetDiagnosticItemMap(ItemsSourceView itemsSourceView) => itemsSourceView._layersState?.indexMap.ToArray();

    public AvaloniaList<ItemFilter> Filters { get; }

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

    public AvaloniaList<ItemSorter> Sorters { get; }

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

    private static void ValidateLayer(ItemsSourceViewLayer layer)
    {
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

    public void Refresh()
    {
        if (!HasActiveLayers)
        {
            if (_layersState == null)
            {
                return;
            }

            RemoveListenerIfNecessary();
            _layersState = null;
        }
        else
        {
            AddListenerIfNecessary();
            _layersState = EvaluateLayers(Filters, Sorters, CancellationToken.None);
        }

        RaiseCollectionChanged(CollectionUtils.ResetEventArgs);
    }

    /// <summary>
    /// Re-evaluates <see cref="Filters"/> and <see cref="Sorters"/> asynchronously, and applies results in one operation after processing completes.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!HasActiveLayers)
        {
            if (_layersState == null)
            {
                return;
            }

            RemoveListenerIfNecessary();
            _layersState = null;
        }
        else
        {
            AddListenerIfNecessary();
            var filtersCopy = new ItemFilter[Filters.Count];
            Filters.CopyTo(filtersCopy, 0);
            var sortersCopy = new ItemSorter[Sorters.Count];
            Sorters.CopyTo(sortersCopy, 0);
            _layersState = await Task.Run(() => EvaluateLayers(filtersCopy, sortersCopy, cancellationToken), cancellationToken);
        }

        RaiseCollectionChanged(CollectionUtils.ResetEventArgs);
    }

    private (List<object?> items, List<int> indexMap, HashSet<string> invalidationProperties) EvaluateLayers(IList<ItemFilter> filters, IList<ItemSorter> sorters, CancellationToken cancellationToken)
    {
        var result = new List<object?>(_source.Count);
        var map = new List<int>(_source.Count);
        var viewIndexToSourceIndex = new List<int>(_source.Count);

        var invalidationProperties = new HashSet<string>();

        for (int i = 0; i < filters.Count; i++)
        {
            if (filters[i].IsActive)
                invalidationProperties.UnionWith(filters[i].GetInvalidationPropertyNamesEnumerator());
        }
        for (int i = 0; i < sorters.Count; i++)
        {
            if (sorters[i].IsActive)
                invalidationProperties.UnionWith(sorters[i].GetInvalidationPropertyNamesEnumerator());
        }

        CompoundSorter? comparer = null;
        if (HasActiveSorters)
        {
            comparer = s_compoundSorter.Value ??= new();
            comparer.Sorters = sorters;
        }

        try
        {
            for (int i = 0; i < _source.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (invalidationProperties.Count > 0 && _source[i] is INotifyPropertyChanged inpc)
                {
                    WeakEvents.ThreadSafePropertyChanged.Unsubscribe(inpc, this);
                    WeakEvents.ThreadSafePropertyChanged.Subscribe(inpc, this);
                }

                if (ItemPassesFilters(filters, _source[i]))
                {
                    if (comparer != null)
                    {
                        var index = result.BinarySearch(_source[i], comparer);
                        if (index < 0)
                        {
                            index = ~index;
                        }

                        viewIndexToSourceIndex.Insert(index, i);
                        result.Insert(index, _source[i]);
                    }
                    else
                    {
                        viewIndexToSourceIndex.Add(i);
                        result.Add(_source[i]);
                    }
                }
            }

            map.InsertMany(0, -1, _source.Count);

            for (int i = 0; i < viewIndexToSourceIndex.Count; i++)
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
        if (sender is not INotifyPropertyChanged inpc
            || _layersState is not { } layersState
            || e.PropertyName is not { } propertyName
            || !layersState.invalidationProperties.Contains(propertyName))
        {
            return;
        }

        bool? passes = null;

        for (int sourceIndex = 0; sourceIndex < Source.Count; sourceIndex++)
        {
            if (Source[sourceIndex] != sender)
            {
                continue;
            }

            // If a collection is reset, we aren't able to unsubscribe from stale items. So we can sometimes receive
            // this event from items which are no longer in the collection. Don't execute the filter until we are sure
            // that the item is still present.
            passes ??= ItemPassesFilters(Filters, sender);

            switch ((layersState.indexMap[sourceIndex], passes))
            {
                case (-1, true):
                    {
                        var viewIndex = ViewIndex(sourceIndex, sender);

                        layersState.indexMap[sourceIndex] = viewIndex;
                        ShiftIndexMap(sourceIndex + 1, 1);

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
                    ShiftIndexMap(sourceIndex + 1, -1);
                    layersState.items.RemoveAt(viewIndex);
                    RaiseCollectionChanged(new(NotifyCollectionChangedAction.Remove, sender, viewIndex));
                    break;
            }
        }

        if (passes == null) // item is no longer in the collection, we can unsubscribe
        {
            WeakEvents.ThreadSafePropertyChanged.Unsubscribe(inpc, this);
        }
    }

    private void UpdateLayersForCollectionChangedEvent(NotifyCollectionChangedEventArgs e)
    {
        if (_layersState is not { } layersState)
        {
            throw new InvalidOperationException("Layers not initialised.");
        }

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
                        ShiftIndexMap(sourceIndex, 1);

                        layersState.items.Insert(viewIndex, e.NewItems[i]);
                        layersState.indexMap.Insert(sourceIndex, viewIndex);
                        viewItems.Add(e.NewItems[i]);
                    }
                    else
                    {
                        layersState.indexMap.Insert(sourceIndex, -1);
                    }

                    if (layersState.invalidationProperties.Count > 0 && e.NewItems[i] is INotifyPropertyChanged inpc)
                        WeakEvents.ThreadSafePropertyChanged.Subscribe(inpc, this);
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

                    if (layersState.indexMap[sourceIndex] != -1)
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

                        layersState.items.RemoveAt(layersState.indexMap[sourceIndex]);

                        viewItems.Add(e.OldItems[i]);
                    }

                    layersState.indexMap.RemoveAt(sourceIndex);

                    if (layersState.invalidationProperties.Count > 0 && e.OldItems[i] is INotifyPropertyChanged inpc)
                        WeakEvents.ThreadSafePropertyChanged.Unsubscribe(inpc, this);
                }

                if (viewItems != null)
                {
                    ShiftIndexMap(e.OldStartingIndex - e.OldItems.Count, -viewItems.Count);
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

    private void ShiftIndexMap(int inclusiveSourceStartIndex, int delta)
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
}
