using System.Collections.Generic;

namespace Avalonia.Styling.Activators
{
    /// <summary>
    /// An aggregate <see cref="ContainerQueryActivatorBase"/> which is active when all of its inputs are
    /// active.
    /// </summary>
    internal class AndQueryActivator : ContainerQueryActivatorBase, IStyleActivatorSink
    {
        private List<IStyleActivator>? _sources;

        public AndQueryActivator(Visual visual) : base(visual)
        {
        }

        public int Count => _sources?.Count ?? 0;

        public void Add(IStyleActivator activator)
        {
            if (IsSubscribed)
                throw new AvaloniaInternalException("AndActivator is already subscribed.");
            _sources ??= new List<IStyleActivator>();
            _sources.Add(activator);
        }

        void IStyleActivatorSink.OnNext(bool value) => ReevaluateIsActive();

        protected override bool EvaluateIsActive()
        {
            if (_sources is null || _sources.Count == 0)
                return true;

            var count = _sources.Count;
            var mask = (1ul << count) - 1;
            var flags = 0UL;

            for (var i = 0; i < count; ++i)
            {
                if (_sources[i].GetIsActive())
                    flags |= 1ul << i;
            }

            return flags == mask;
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
