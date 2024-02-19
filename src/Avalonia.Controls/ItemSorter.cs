using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Controls;

/// <summary>
/// An <see cref="ItemsSourceView"/> layer which can re-order items of the source collection within the transformed view.
/// </summary>
public abstract class ItemSorter : ItemsSourceViewLayer, IComparer<object?>, IComparer
{
    private ListSortDirection _sortDirection;

    /// <summary>
    /// Gets or sets a value that indicates whether to sort in ascending (1, 2, 3...) or descending (3, 2, 1...) order.
    /// </summary>
    public ListSortDirection SortDirection
    {
        get => _sortDirection;
        set => SetAndRaise(SortDirectionProperty, ref _sortDirection, value);
    }

    /// <seealso cref="SortDirection"/>
    public static readonly DirectProperty<ItemSorter, ListSortDirection> SortDirectionProperty =
        AvaloniaProperty.RegisterDirect<ItemSorter, ListSortDirection>(nameof(SortDirection), o => o.SortDirection, (o, v) => o.SortDirection = v);

    /// <summary>
    /// Compares two objects to determine their sort order.
    /// </summary>
    /// <returns>
    /// <list type="table">
    /// <item><term>A negative value</term> <description>If <paramref name="x"/> should come before <paramref name="y"/></description></item>
    /// <item><term>0</term> <description>If <paramref name="x"/> and <paramref name="y"/> have the same precedence</description></item>
    /// <item><term>A positive value</term> <description>If <paramref name="x"/> should come after <paramref name="y"/></description></item>
    /// </list>
    /// </returns>
    public abstract int Compare(object? x, object? y);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SortDirectionProperty)
        {
            OnInvalidated();
        }
    }

    /// <summary>
    /// Gets an <see cref="IValueConverter"/> which converts <c>true</c> to <see cref="ListSortDirection.Descending"/> and <c>false</c> to <see cref="ListSortDirection.Ascending"/>.
    /// </summary>
    public static IValueConverter BooleanToDescendingSortConverter { get; } = new BooleanToDescendingSortConverter_();

    private class BooleanToDescendingSortConverter_ : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
        {
            true => ListSortDirection.Descending,
            false => ListSortDirection.Ascending,
            _ => AvaloniaProperty.UnsetValue,
        };

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
        {
            ListSortDirection.Descending => true,
            ListSortDirection.Ascending => false,
            _ => AvaloniaProperty.UnsetValue,
        };
    }
}

/// <summary>
/// Sorts items in an <see cref="ItemsSourceView"/> according to the order provided by passing an <see cref="IComparable"/> 
/// object selected for each item to an <see cref="IComparer"/> object.
/// </summary>
/// <remarks>
/// If any item passed to <see cref="Compare(object?, object?)"/> is not an <see cref="IComparable"/>, and no 
/// <see cref="ComparableSelector"/> value has been provided, an <see cref="InvalidCastException"/> will be thrown.
/// </remarks>
public class ComparableSorter : ItemSorter
{
    private EventHandler<ComparableSelectEventArgs>? _comparableSelector;
    private IComparer? _comparer;

    /// <summary>
    /// Gets or sets a delegate that will be executed twice each time a comparison is made, to select 
    /// an <see cref="IComparable"/> for the two items being compared.
    /// </summary>
    /// <remarks>
    /// If no value is provided, the item itself will be used. In this case, an <see cref="InvalidCastException"/> will be thrown if the item does not implement <see cref="IComparable"/>.
    /// </remarks>
    public EventHandler<ComparableSelectEventArgs>? ComparableSelector
    {
        get => _comparableSelector;
        set => SetAndRaise(ComparableSelectorProperty, ref _comparableSelector, value);
    }

    /// <seealso cref="ComparableSelector"/>
    public static readonly DirectProperty<ComparableSorter, EventHandler<ComparableSelectEventArgs>?> ComparableSelectorProperty =
        AvaloniaProperty.RegisterDirect<ComparableSorter, EventHandler<ComparableSelectEventArgs>?>(nameof(ComparableSelector), o => o.ComparableSelector, (o, v) => o.ComparableSelector = v);

    /// <summary>
    /// Gets or sets an <see cref="IComparable"/> to use to compare items.
    /// </summary>
    /// <remarks>
    /// If this value is null, <see cref="Comparer.Default"/> will be used.
    /// </remarks>
    public IComparer? Comparer
    {
        get => _comparer;
        set => SetAndRaise(ComparerProperty, ref _comparer, value);
    }

    /// <seealso cref="Comparer"/>
    public static readonly DirectProperty<ComparableSorter, IComparer?> ComparerProperty =
        AvaloniaProperty.RegisterDirect<ComparableSorter, IComparer?>(nameof(Comparer), o => o.Comparer, (o, v) => o.Comparer = v);

    /// <inheritdoc cref="ItemSorter.Compare(object?, object?)"/>
    /// <exception cref="InvalidCastException">Thrown if <paramref name="x"/> or <paramref name="y"/> cannot be converted to <see cref="IComparable"/>.</exception>
    public override int Compare(object? x, object? y)
    {
        var compareResult = (Comparer ?? System.Collections.Comparer.Default).Compare(GetComparable(x), GetComparable(y));
        return SortDirection == ListSortDirection.Descending ? -compareResult : compareResult;
    }

    private IComparable? GetComparable(object? item)
    {
        if (ComparableSelector is { } selector)
        {
            var args = new ComparableSelectEventArgs { Item = item };
            selector(this, args);
            return args.Comparable;
        }

        return (IComparable?)item;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ComparableSelectorProperty)
        {
            OnInvalidated();
        }
        else if (change.Property == ComparerProperty)
        {
            OnInvalidated();
        }
    }

    public class ComparableSelectEventArgs : EventArgs
    {
        /// <summary>
        /// The item for which to provide an <see cref="IComparable"/>.
        /// </summary>
        public object? Item { get; init; }

        /// <summary>
        /// The <see cref="IComparable"/> object selected by the event handler, or null.
        /// </summary>
        public IComparable? Comparable { get; set; }
    }
}
