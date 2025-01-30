using System;
using Avalonia.Logging;

namespace Avalonia.Controls;

/// <summary>
/// An <see cref="ItemsSourceView"/> layer which can exclude items of the source collection from the transformed view.
/// </summary>
public abstract class ItemFilter : ItemsSourceViewLayer
{
    /// <summary>
    /// Determines whether an item passes this filter.
    /// </summary>
    /// <returns>True if the item passes the filter, otherwise false.</returns>
    public abstract bool FilterItem(object? item);
}

/// <summary>
/// Excludes items from an <see cref="ItemsSourceView"/> as determined by the delegate currently assigned to its <see cref="Filter"/> property.
/// </summary>
public class FunctionItemFilter : ItemFilter
{
    private EventHandler<FilterEventArgs>? _filter;

    private FilterEventArgs? _batchArgs;

    /// <summary>
    /// Gets or sets a method which determines whether an item passes this filter.
    /// </summary>
    /// <remarks>
    /// If a multicast delegate is assigned, all invocations must accept the item in order for it to pass the filter.
    /// </remarks>
    public EventHandler<FilterEventArgs>? Filter
    {
        get => _filter;
        set => SetAndRaise(FilterProperty, ref _filter, value);
    }

    /// <seealso cref="Filter"/>
    public static readonly DirectProperty<FunctionItemFilter, EventHandler<FilterEventArgs>?> FilterProperty =
        AvaloniaProperty.RegisterDirect<FunctionItemFilter, EventHandler<FilterEventArgs>?>(nameof(Filter), o => o.Filter, (o, v) => o.Filter = v);

    public FunctionItemFilter() { }

    public FunctionItemFilter(Func<object?, bool> filterFunc)
    {
        Filter = (s, e) => e.Accept = filterFunc(e.Item);
    }

    public override bool FilterItem(object? item)
    {
        if (Filter == null)
        {
            return true;
        }

        var args = _batchArgs ?? new() { FilterState = State };
        args.ResetFor(item);

        var handlers = Filter.GetInvocationList();
        for (var i = 0; i < handlers.Length; i++)
        {
            var method = (EventHandler<FilterEventArgs>)handlers[i];

            method(this, args);
            if (!args.Accept)
            {
                return false;
            }
        }

        return true;
    }

    protected internal override void BeginBatchOperation()
    {
        if (_batchArgs != null)
        {
            throw new InvalidOperationException("Already refreshing.");
        }

        _batchArgs = new() { FilterState = State };
    }

    protected internal override void EndBatchOperation() => _batchArgs = null;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FilterProperty)
        {
            OnInvalidated();
        }
        else if (change.Property == StateProperty)
        {
            if (_batchArgs != null)
                Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(this, "State changed during batch operation!");
        }
    }

    public class FilterEventArgs : EventArgs
    {
        private object? _item;

        /// <summary>
        /// Gets the item being filtered.
        /// </summary>
        public object? Item { get => _item; init => _item = value; }

        /// <summary>
        /// Gets the object retrieved from <see cref="ItemsSourceViewLayer.State"/> when the event was raised, or null.
        /// </summary>
        public object? FilterState { get; init; }

        /// <summary>
        /// Gets or sets whether <see cref="Item"/> should pass the filter.
        /// </summary>
        public bool Accept { get; set; } = true;

        protected internal void ResetFor(object? item)
        {
            _item = item;
            Accept = true;
        }
    }
}
