using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// A node in the binding path of an <see cref="BindingExpression"/> that reads a property
/// via a predefined <see cref="IPropertyAccessorPlugin"/>.
/// </summary>
internal sealed class PropertyAccessorNode : ExpressionNode, IPropertyAccessorNode, ISettableNode
{
    private readonly Action<object?> _onValueChanged;
    private readonly IPropertyAccessorPlugin _plugin;
    private IPropertyAccessor? _accessor;
    private bool _enableDataValidation;

    public PropertyAccessorNode(string propertyName, IPropertyAccessorPlugin plugin)
    {
        _plugin = plugin;
        _onValueChanged = OnValueChanged;
        PropertyName = propertyName;
    }

    public IPropertyAccessor? Accessor => _accessor;
    public bool CanBeNull { get; }
    public string PropertyName { get; }
    public Type? ValueType => _accessor?.PropertyType;

    public override void BuildString(StringBuilder builder)
    {
        if (builder.Length > 0 && builder[builder.Length - 1] != '!')
            builder.Append('.');
        builder.Append(PropertyName);
    }

    public void EnableDataValidation() => _enableDataValidation = true;

    public bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes)
    {
        if (_accessor?.PropertyType is not null)
        {
            return _accessor.SetValue(value, BindingPriority.LocalValue);
        }

        return false;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (!ValidateNonNullSource(source))
            return;

        var reference = new WeakReference<object?>(source);

        if (_plugin.Start(reference, PropertyName) is { } accessor)
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
            ClearValue();
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
}
