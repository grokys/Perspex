using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Utilities;

namespace Avalonia.Controls;

public partial class ItemsSourceView : IWeakEventSubscriber<PropertyChangedEventArgs>
{
    private static readonly Lazy<ConditionalWeakTable<NotifyCollectionChangedEventArgs, NotifyCollectionChangedEventArgs?>> s_rewrittenCollectionChangedEvents = new();

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

    public AvaloniaList<ItemSorter> Sorters { get; }

    protected bool HasLayers => Filters.Count + Sorters.Count > 0;

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
        if (_layersState == null && !HasLayers)
        {
            return;
        }

        if (!HasLayers)
        {
            RemoveListenerIfNecessary();
            _layersState = null;
        }
        else
        {
            AddListenerIfNecessary();
            _layersState = EvaluateLayers(Filters, Sorters, CancellationToken.None);
        }

        RaiseCollectionChanged(new(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Re-evaluates <see cref="Filters"/> and <see cref="Sorters"/> asynchronously, and applies results in one operation after processing completes.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_layersState == null && !HasLayers)
        {
            return;
        }

        if (!HasLayers)
        {
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

        RaiseCollectionChanged(new(NotifyCollectionChangedAction.Reset));
    }

    private (List<object?> items, List<int> indexMap, HashSet<string> invalidationProperties) EvaluateLayers(IList<ItemFilter> filters, IList<ItemSorter> sorters, CancellationToken cancellationToken)
    {
        var result = new List<object?>();
        var map = new List<int>();
        var invalidationProperties = new HashSet<string>();

        for (int i = 0; i < filters.Count; i++)
        {
            invalidationProperties.UnionWith(filters[i].GetInvalidationPropertyNamesEnumerator());
        }
        for (int i = 0; i < sorters.Count; i++)
        {
            invalidationProperties.UnionWith(sorters[i].GetInvalidationPropertyNamesEnumerator());
        }

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
                map.Add(result.Count);
                result.Add(_source[i]);
            }
            else
            {
                map.Add(-1);
            }
        }
        return (result, map, invalidationProperties);
    }

    private bool ItemPassesFilters(IList<ItemFilter> itemFilters, object? item)
    {
        for (int i = 0; i < itemFilters.Count; i++)
        {
            if (!itemFilters[i].FilterItem(item))
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
                        var viewIndex = ViewStartingIndex(sourceIndex);

                        layersState.indexMap[sourceIndex] = viewIndex;
                        ShiftIndexMap(sourceIndex + 1, 1);

                        layersState.items.Insert(viewIndex, sender);
                        RaiseCollectionChanged(new(NotifyCollectionChangedAction.Add, sender, viewIndex));
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

        List<object?>? viewItems = null;
        int viewStartIndex = -1;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                Debug.Assert(e.NewItems != null);

                for (var i = 0; i < e.NewItems.Count; i++)
                {
                    var sourceIndex = e.NewStartingIndex + i;
                    if (ItemPassesFilters(Filters, e.NewItems[i]))
                    {
                        if (viewItems == null)
                        {
                            viewItems = new(e.NewItems.Count);
                            viewStartIndex = ViewStartingIndex(e.NewStartingIndex);
                        }

                        var viewIndex = viewStartIndex + viewItems.Count;
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
                    ShiftIndexMap(e.NewStartingIndex + e.NewItems.Count, viewItems.Count);
                }

                s_rewrittenCollectionChangedEvents.Value.Add(e, viewItems == null ? null : new(NotifyCollectionChangedAction.Add, viewItems, viewStartIndex));
                break;

            case NotifyCollectionChangedAction.Remove:
                Debug.Assert(e.OldItems != null);
                for (var i = 0; i < e.OldItems.Count; i++)
                {
                    var sourceIndex = e.OldStartingIndex + i;
                    if (layersState.indexMap[sourceIndex] != -1)
                    {
                        if (viewItems == null)
                        {
                            viewItems = new(e.OldItems.Count);
                            viewStartIndex = ViewStartingIndex(sourceIndex);
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

                s_rewrittenCollectionChangedEvents.Value.Add(e, viewItems == null ? null : new(NotifyCollectionChangedAction.Remove, viewItems, viewStartIndex));
                break;
            case NotifyCollectionChangedAction.Replace:
                throw new NotImplementedException();
            case NotifyCollectionChangedAction.Reset:
                Refresh();
                s_rewrittenCollectionChangedEvents.Value.Add(e, null);
                break;
            case NotifyCollectionChangedAction.Move:
                throw new NotImplementedException();
            default:
                throw new NotImplementedException();
        }
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

    private int ViewStartingIndex(int sourceStartingIndex)
    {
        if (_layersState is not { } layersState)
        {
            throw new InvalidOperationException("Layers not initialised.");
        }

        var candidateIndex = sourceStartingIndex;
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
}
