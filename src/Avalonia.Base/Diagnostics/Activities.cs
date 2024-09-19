using System;
using System.Diagnostics;

// ReSharper disable ExplicitCallerInfoArgument

namespace Avalonia.Diagnostics;

public abstract class AvaloniaActivity : IDisposable
{
    internal AvaloniaActivity(string operationName)
    {
        OperationName = operationName;
    }

    public string OperationName { get; }

    public AvaloniaActivity AddTag(string key, object? value)
    {
        AddTagCore(key, value);
        return this;
    }

    protected abstract void AddTagCore(string key, object? value);
    
    public abstract void Dispose();
}

public interface IActivitySource
{
    AvaloniaActivity? StartActivity(string operationName);
}

internal class DiagnosticActivities
{
    public static class Tags
    {
        public const string Style = nameof(Style);
        public const string SelectorResult = nameof(SelectorResult);

        public const string Key = nameof(Key);
        public const string ThemeVariant = nameof(ThemeVariant);
        public const string Result = nameof(Result);

        public const string Activator = nameof(Activator);
        public const string IsActive = nameof(IsActive);
        public const string Selector = nameof(Selector);
        public const string Control = nameof(Control);
    }

    public const string Measure = "Avalonia.Layout.Layoutable.Measure";
    public const string Arrange = "Avalonia.Layout.Layoutable.Arrange";
    public const string Render = "Avalonia.Styling.Style.Attach";

    public const string StyleAttach = "Avalonia.Styling.Style.Attach";
    public const string FindResource = "Avalonia.Controls.ResourceNode.FindResource";
    public const string StyleActivator = "Avalonia.Styling.Activators.StyleActivatorBase.EvaluateIsActive";

    public static IActivitySource? Diagnostic { get; set; }

    public static AvaloniaActivity? StartStyleAttach()
    {
        return Diagnostic?.StartActivity(StyleAttach);
    }

    public static AvaloniaActivity? StartFindResource()
    {
        return Diagnostic?.StartActivity(FindResource);
    }

    public static AvaloniaActivity? StartStyleActivator()
    {
        return Diagnostic?.StartActivity(StyleActivator);
    }

    public static AvaloniaActivity? StartMeasure()
    {
        return Diagnostic?.StartActivity(Measure);
    }

    public static AvaloniaActivity? StartArrange()
    {
        return Diagnostic?.StartActivity(Arrange);
    }

    public static AvaloniaActivity? StartRender()
    {
        return Diagnostic?.StartActivity(Render);
    }
}
