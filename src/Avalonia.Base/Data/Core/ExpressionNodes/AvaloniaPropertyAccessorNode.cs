using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Utilities;

namespace Avalonia.Data.Core.ExpressionNodes;

internal sealed class AvaloniaPropertyAccessorNode :
    ExpressionNode,
    ISettableNode,
    IWeakEventSubscriber<AvaloniaPropertyChangedEventArgs>
{
    public AvaloniaPropertyAccessorNode(AvaloniaProperty property, bool canBeNull = false)
    {
        Property = property;
        CanBeNull = canBeNull;
    }

    public bool CanBeNull { get; }
    public AvaloniaProperty Property { get; }
    public Type? ValueType => Property.PropertyType;

    override public void BuildString(StringBuilder builder)
    {
        if (builder.Length > 0 && builder[builder.Length - 1] != '!')
            builder.Append('.');
        builder.Append(Property.Name);
    }

    public bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes)
    {
        if (Source is AvaloniaObject o)
        {
            o.SetValue(Property, value);
            return true;
        }

        return false;
    }

    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (!ValidateNonNullSource(source))
            return;

        if (source is AvaloniaObject newObject)
        {
            WeakEvents.AvaloniaPropertyChanged.Subscribe(newObject, this);
            SetValue(newObject.GetValue(Property));
        }
    }

    protected override void Unsubscribe(object oldSource)
    {
        if (oldSource is AvaloniaObject oldObject)
            WeakEvents.AvaloniaPropertyChanged.Unsubscribe(oldObject, this);
    }

    public void OnEvent(object? sender, WeakEvent ev, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Property && Source is AvaloniaObject o)
            SetValue(o.GetValue(Property));
    }
}
