#nullable enable

namespace Avalonia.Styling.Activators
{
    /// <summary>
    /// Builds an <see cref="OrActivator"/>.
    /// </summary>
    /// <remarks>
    /// When ORing style activators, if there is more than one input then creates an instance of
    /// <see cref="OrActivator"/>. If there is only one input, returns the input directly.
    /// </remarks>
    internal struct OrQueryActivatorBuilder
    {
        private IStyleActivator? _single;
        private OrQueryActivator? _multiple;
        private Visual _visual;

        public OrQueryActivatorBuilder(Visual visual) : this()
        {
            _visual = visual;
        }

        public int Count => _multiple?.Count ?? (_single is object ? 1 : 0);

        public void Add(IStyleActivator? activator)
        {
            if (activator == null)
            {
                return;
            }

            if (_single is null && _multiple is null)
            {
                _single = activator;
            }
            else
            {
                if (_multiple is null)
                {
                    _multiple = new OrQueryActivator(_visual);
                    _multiple.Add(_single!);
                    _single = null;
                }

                _multiple.Add(activator);
            }
        }

        public IStyleActivator Get() => _single ?? _multiple!;
    }
}
