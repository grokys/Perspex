using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Controls;

public class ItemSourceViewFilterEventArgs : EventArgs
{
    /// <summary>
    /// Gets the item being filtered.
    /// </summary>
    public object? Item { get; init; }

    /// <summary>
    /// Gets or sets whether <see cref="Item"/> should pass the filter.
    /// </summary>
    public bool Accept { get; set; } = true;
}

public partial class ItemsSourceView
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

    private (List<object?> items, List<int> indexMap)? _filterState;
    private EventHandler<ItemSourceViewFilterEventArgs>? _filter;

    public EventHandler<ItemSourceViewFilterEventArgs>? Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            Refresh();
        }
    }

    private bool ItemPassesFilter(object? item, EventHandler<ItemSourceViewFilterEventArgs> filter)
    {
        var args = new ItemSourceViewFilterEventArgs() { Item = item };

        var handlers = filter.GetInvocationList();
        for (var i = 0; i < handlers.Length; i++)
        {
            var method = (EventHandler<ItemSourceViewFilterEventArgs>)handlers[i];

            method(this, args);
            if (!args.Accept)
            {
                return false;
            }
        }

        return true;
    }

    public void Refresh()
    {
        if (_filterState == null && Filter == null)
        {
            return;
        }

        if (Filter == null)
        {
            RemoveListenerIfNecessary();
            _filterState = null;
        }
        else
        {
            AddListenerIfNecessary();
            _filterState = ExecuteFilter(Filter, CancellationToken.None);
        }

        RaiseCollectionChanged(new(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Executes <see cref="Filter"/> asynchronously, and applies results in one operation after processing completes.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_filterState == null && Filter == null)
        {
            return;
        }

        if (Filter is not { } filter)
        {
            RemoveListenerIfNecessary();
            _filterState = null;
        }
        else
        {
            AddListenerIfNecessary();

            _filterState = await Task.Run(() => ExecuteFilter(filter, cancellationToken), cancellationToken);
        }

        RaiseCollectionChanged(new(NotifyCollectionChangedAction.Reset));
    }

    private (List<object?> items, List<int> indexMap) ExecuteFilter(EventHandler<ItemSourceViewFilterEventArgs> filter, CancellationToken cancellationToken)
    {
        var result = new List<object?>();
        var map = new List<int>();

        for (int i = 0; i < _source.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ItemPassesFilter(_source[i], filter))
            {
                map.Add(result.Count);
                result.Add(_source[i]);
            }
            else
            {
                map.Add(-1);
            }
        }
        return (result, map);
    }

    private void FilterCollectionChangedEvent(NotifyCollectionChangedEventArgs e, EventHandler<ItemSourceViewFilterEventArgs> filter)
    {
        if (_filterState is not { } filterState)
        {
            throw new InvalidOperationException("Filter feature not initialised.");
        }

        List<object?>? filteredNewItems = null;
        int filteredNewItemsStartingIndex = -1;

        List<object?>? filteredOldItems = null;
        int filteredOldItemsStartingIndex = -1;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                Debug.Assert(e.NewItems != null);

                for (var i = 0; i < e.NewItems.Count; i++)
                {
                    var sourceIndex = e.NewStartingIndex + i;
                    if (ItemPassesFilter(e.NewItems[i], filter))
                    {
                        if (filteredNewItems == null)
                        {
                            filteredNewItems = new(e.NewItems.Count);
                            filteredNewItemsStartingIndex = FilteredStartingIndex(e.NewStartingIndex);
                        }

                        var filteredItemIndex = filteredNewItemsStartingIndex + filteredNewItems.Count;
                        filterState.items.Insert(filteredItemIndex, e.NewItems[i]);

                        ShiftIndexMap(sourceIndex, 1);
                        filterState.indexMap.Insert(sourceIndex, filteredItemIndex);

                        filteredNewItems.Add(e.NewItems[i]);
                    }
                    else
                    {
                        filterState.indexMap.Insert(sourceIndex, -1);
                    }
                }
                s_rewrittenCollectionChangedEvents.Value.Add(e, filteredNewItems == null ? null : new(NotifyCollectionChangedAction.Add, filteredNewItems, filteredNewItemsStartingIndex));
                break;
            case NotifyCollectionChangedAction.Remove:
                Debug.Assert(e.OldItems != null);
                for (var i = 0; i < e.OldItems.Count; i++)
                {
                    var sourceIndex = e.OldStartingIndex + i;
                    if (filterState.indexMap[sourceIndex] != -1)
                    {
                        if (filteredOldItems == null)
                        {
                            filteredOldItems = new(e.OldItems.Count);
                            filteredOldItemsStartingIndex = FilteredStartingIndex(e.OldStartingIndex);
                        }

                        filterState.items.RemoveAt(filterState.indexMap[sourceIndex]);
                        filteredOldItems.Add(e.OldItems[i]);
                    }
                    ShiftIndexMap(sourceIndex, -1);
                    filterState.indexMap.RemoveAt(sourceIndex);
                }
                s_rewrittenCollectionChangedEvents.Value.Add(e, filteredOldItems == null ? null : new(NotifyCollectionChangedAction.Remove, filteredOldItems, filteredOldItemsStartingIndex));
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

    private void ShiftIndexMap(int inclusiveStart, int delta)
    {
        var map = _filterState!.Value.indexMap;
        for (var i = 0; i < map.Count; i++)
        {
            if (map[i] >= inclusiveStart)
            {
                map[i] += delta;
            }
        }
    }

    private int FilteredStartingIndex(int sourceIndex)
    {
        if (_filterState is not { } filterState)
        {
            throw new InvalidOperationException("Filter feature not initialised.");
        }

        var candidateIndex = sourceIndex;
        var insertAtEnd = candidateIndex >= filterState.indexMap.Count;

        if (insertAtEnd)
        {
            candidateIndex = filterState.indexMap.Count - 1;
        }

        int filteredIndex;
        do
        {
            if (candidateIndex == -1)
            {
                return 0;
            }

            filteredIndex = filterState.indexMap[candidateIndex--];
        }
        while (filteredIndex < 0);

        return filteredIndex + (insertAtEnd ? 1 : 0);
    }
}
