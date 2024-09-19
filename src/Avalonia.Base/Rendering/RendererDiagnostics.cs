using System.ComponentModel;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Manages configurable diagnostics that can be displayed by a renderer.
    /// </summary>
    public sealed class RendererDiagnostics : INotifyPropertyChanged
    {
        private RendererDebugOverlays _debugOverlays;
        private PropertyChangedEventArgs? _debugOverlaysChangedEventArgs;

        /// <summary>
        /// Gets or sets which debug overlays are displayed by the renderer.
        /// </summary>
        public RendererDebugOverlays DebugOverlays
        {
            get => _debugOverlays;
            set
            {
                if (_debugOverlays != value)
                {
                    _debugOverlays = value;
                    OnPropertyChanged(_debugOverlaysChangedEventArgs ??= new(nameof(DebugOverlays)));
                }
            }
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Called when a property changes on the object.
        /// </summary>
        /// <param name="args">The property change details.</param>
        private void OnPropertyChanged(PropertyChangedEventArgs args)
            => PropertyChanged?.Invoke(this, args);
    }
}
