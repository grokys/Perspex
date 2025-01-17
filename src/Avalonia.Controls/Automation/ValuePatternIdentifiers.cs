using Avalonia.Automation.Provider;

namespace Avalonia.Automation
{
    /// <summary>
    /// Contains values used as identifiers by <see cref="IValueProvider"/>.
    /// </summary>
    public static class ValuePatternIdentifiers
    {
        /// <summary>
        /// Identifies <see cref="IValueProvider.IsReadOnly"/> automation property.
        /// </summary>
        public static AutomationProperty IsReadOnlyProperty { get; } = new AutomationProperty();

        /// <summary>
        /// Identifies <see cref="IValueProvider.Value"/> automation property.
        /// </summary>
        public static AutomationProperty ValueProperty { get; } = new AutomationProperty();
    }
}
