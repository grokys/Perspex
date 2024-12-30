using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core.ExpressionNodes;

/// <summary>
/// Indicates that a <see cref="ExpressionNode"/> accesses a property on an object.
/// </summary>
internal interface IPropertyAccessorNode
{
    string PropertyName { get; }
    bool CanBeNull { get; }
    IPropertyAccessor? Accessor { get; }
    void EnableDataValidation();
}
