namespace Avalonia.Automation
{
    /// <summary>
    /// This enum offers different ways of evaluating the IsOffscreen AutomationProperty
    /// </summary>
    public enum IsOffscreenBehavior
    {
        /// <summary>
        /// The AutomationProperty IsOffscreen is calculated based on IsEffectivelyVisible.
        /// </summary>
        Default,
        /// <summary>
        /// The AutomationProperty IsOffscreen is false.
        /// </summary>
        Onscreen,
        /// <summary>
        /// The AutomationProperty IsOffscreen if true.
        /// </summary>
        Offscreen,
        /// <summary>
        /// The AutomationProperty IsOffscreen is calculated based on clip regions.
        /// </summary>
        FromClip,
    }
}

