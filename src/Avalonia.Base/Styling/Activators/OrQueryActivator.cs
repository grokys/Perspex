using System.Collections.Generic;

namespace Avalonia.Styling.Activators
{
    /// <summary>
    /// An aggregate <see cref="ContainerQueryActivatorBase"/> which is active when any of its inputs are
    /// active.
    /// </summary>
    internal class OrQueryActivator : ContainerQueryActivatorBase, IStyleActivatorSink
    {
        private List<IStyleActivator>? _sources;

        public OrQueryActivator(Visual visual) : base(visual)
        {
        }

        public int Count => _sources?.Count ?? 0;

        public void Add(IStyleActivator activator)
        {
            _sources ??= new List<IStyleActivator>();
            _sources.Add(activator);
        }

        void IStyleActivatorSink.OnNext(bool value) => ReevaluateIsActive();

        protected override bool EvaluateIsActive()
        {
            if (_sources is null || _sources.Count == 0)
                return true;

            foreach (var source in _sources)
            {
                if (source.GetIsActive())
                    return true;
            }

            return false;
        }

        protected override void Initialize()
        {
            if (_sources is object)
            {
                foreach (var source in _sources)
                {
                    source.Subscribe(this);
                }
            }
        }

        protected override void Deinitialize()
        {
            if (_sources is object)
            {
                foreach (var source in _sources)
                {
                    source.Unsubscribe(this);
                }
            }
        }
    }
}
