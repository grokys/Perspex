using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core.ExpressionNodes.Reflection;

/// <summary>
/// A node in the binding path of an <see cref="BindingExpression"/> that reads a property
/// via an <see cref="IPropertyAccessorPlugin"/> selected at runtime from the registered
/// <see cref="BindingPlugins.PropertyAccessors"/>.
/// </summary>
[RequiresUnreferencedCode(TrimmingMessages.ExpressionNodeRequiresUnreferencedCodeMessage)]
internal sealed class DynamicPluginPropertyAccessorNode : ExpressionNode, IPropertyAccessorNode, ISettableNode
{
    private readonly Action<object?> _onValueChanged;
    private IPropertyAccessor? _accessor;
    private bool _enableDataValidation;

    public DynamicPluginPropertyAccessorNode(string propertyName, bool canBeNull = false)
    {
        _onValueChanged = OnValueChanged;
        PropertyName = propertyName;
        CanBeNull = canBeNull;
    }

    public IPropertyAccessor? Accessor => _accessor;
    public bool CanBeNull { get; }
    public string PropertyName { get; }
    public Type? ValueType => _accessor?.PropertyType;

    override public void BuildString(StringBuilder builder)
    {
        if (builder.Length > 0 && builder[builder.Length - 1] != '!')
            builder.Append('.');
        builder.Append(PropertyName);
    }

    public void EnableDataValidation() => _enableDataValidation = true;

    public bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes)
    {
        return _accessor?.SetValue(value, BindingPriority.LocalValue) ?? false;
    }

    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (!ValidateNonNullSource(source))
            return;

        var reference = new WeakReference<object?>(source);

        if (GetPlugin(source) is { } plugin &&
            plugin.Start(reference, PropertyName) is { } accessor)
        {
            if (_enableDataValidation)
            {
                foreach (var validator in BindingPlugins.s_dataValidators)
                {
                    if (validator.Match(reference, PropertyName))
                        accessor = validator.Start(reference, PropertyName, accessor);
                }
            }

            _accessor = accessor;
            _accessor.Subscribe(_onValueChanged);
        }
        else
        {
            SetError(
                $"Could not find a matching property accessor for '{PropertyName}' on '{source.GetType()}'.");
        }
    }

    protected override void Unsubscribe(object oldSource)
    {
        _accessor?.Dispose();
        _accessor = null;
    }

    private void OnValueChanged(object? newValue)
    {
        SetValue(newValue);
    }

    private IPropertyAccessorPlugin? GetPlugin(object? source)
    {
        if (source is null)
            return null;

        foreach (var plugin in BindingPlugins.s_propertyAccessors)
        {
            if (plugin.Match(source, PropertyName))
                return plugin;
        }

        return null;
    }
}
