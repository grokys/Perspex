using System;
using System.Collections.Generic;
using Avalonia.Diagnostics;
using Avalonia.Layout;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Interactivity
{
    /// <summary>
    /// Base class for objects that raise routed events.
    /// </summary>
    public class Interactive : Layoutable
    {
        internal static int TotalHandlersCount { get; private set; }
        private Dictionary<RoutedEvent, List<EventSubscription>>? _eventHandlers;

        /// <summary>
        /// Gets the interactive parent of the object for bubbling and tunneling events.
        /// </summary>
        internal virtual Interactive? InteractiveParent => VisualParent as Interactive;

        /// <summary>
        /// Adds a handler for the specified routed event.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="routes">The routing strategies to listen to.</param>
        /// <param name="handledEventsToo">Whether handled events should also be listened for.</param>
        public void AddHandler(
            RoutedEvent routedEvent,
            Delegate handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false)
        {
            routedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
            handler = handler ?? throw new ArgumentNullException(nameof(handler));

            var subscription = new EventSubscription(handler, routes, handledEventsToo);

            AddEventSubscription(routedEvent, subscription);
        }

        /// <summary>
        /// Adds a handler for the specified routed event.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of the event's args.</typeparam>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="routes">The routing strategies to listen to.</param>
        /// <param name="handledEventsToo">Whether handled events should also be listened for.</param>
        public void AddHandler<TEventArgs>(
            RoutedEvent<TEventArgs> routedEvent,
            EventHandler<TEventArgs>? handler,
            RoutingStrategies routes = RoutingStrategies.Direct | RoutingStrategies.Bubble,
            bool handledEventsToo = false) where TEventArgs : RoutedEventArgs
        {
            routedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));

            if (handler is null)
                return;

            static void InvokeAdapter(Delegate baseHandler, object sender, RoutedEventArgs args)
            {
                var typedHandler = (EventHandler<TEventArgs>)baseHandler;
                var typedArgs = (TEventArgs)args;

                typedHandler(sender, typedArgs);
            }

            var subscription = new EventSubscription(handler, routes, handledEventsToo, (baseHandler, sender, args) => InvokeAdapter(baseHandler, sender, args));

            AddEventSubscription(routedEvent, subscription);
        }

        /// <summary>
        /// Removes a handler for the specified routed event.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
        {
            routedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
            handler = handler ?? throw new ArgumentNullException(nameof(handler));

            if (_eventHandlers is object &&
                _eventHandlers.TryGetValue(routedEvent, out var subscriptions))
            {
                for (var i = subscriptions.Count - 1; i >= 0; i--)
                {
                    if (subscriptions[i].Handler == handler)
                    {
                        subscriptions.RemoveAt(i);
                        TotalHandlersCount--;
                    }
                }
            }
        }

        /// <summary>
        /// Removes a handler for the specified routed event.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of the event's args.</typeparam>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="handler">The handler.</param>
        public void RemoveHandler<TEventArgs>(RoutedEvent<TEventArgs> routedEvent, EventHandler<TEventArgs>? handler)
            where TEventArgs : RoutedEventArgs
        {
            if (handler is not null)
                RemoveHandler(routedEvent, (Delegate)handler);
        }

        /// <summary>
        /// Raises a routed event.
        /// </summary>
        /// <param name="e">The event args.</param>
        public void RaiseEvent(RoutedEventArgs e)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            if (e.RoutedEvent == null)
            {
                throw new ArgumentException("Cannot raise an event whose RoutedEvent is null.");
            }

            using var route = BuildEventRoute(e.RoutedEvent);
            route.RaiseEvent(this, e);
        }

        /// <summary>
        /// Builds an event route for a routed event.
        /// </summary>
        /// <param name="e">The routed event.</param>
        /// <returns>An <see cref="EventRoute"/> describing the route.</returns>
        /// <remarks>
        /// Usually, calling <see cref="RaiseEvent(RoutedEventArgs)"/> is sufficient to raise a routed
        /// event, however there are situations in which the construction of the event args is expensive
        /// and should be avoided if there are no handlers for an event. In these cases you can call
        /// this method to build the event route and check the <see cref="EventRoute.HasHandlers"/>
        /// property to see if there are any handlers registered on the route. If there are, call
        /// <see cref="EventRoute.RaiseEvent(Interactive, RoutedEventArgs)"/> to raise the event.
        /// </remarks>
        protected EventRoute BuildEventRoute(RoutedEvent e)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            var result = new EventRoute(e);
            var hasClassHandlers = e.HasRaisedSubscriptions;

            if (e.RoutingStrategies.HasAllFlags(RoutingStrategies.Bubble) ||
                e.RoutingStrategies.HasAllFlags(RoutingStrategies.Tunnel))
            {
                Interactive? element = this;

                while (element != null)
                {
                    if (hasClassHandlers)
                    {
                        result.AddClassHandler(element);
                    }

                    element.AddToEventRoute(e, result);
                    element = element.InteractiveParent;
                }
            }
            else
            {
                if (hasClassHandlers)
                {
                    result.AddClassHandler(this);
                }

                AddToEventRoute(e, result);
            }

            return result;
        }

        private void AddEventSubscription(RoutedEvent routedEvent, EventSubscription subscription)
        {
            _eventHandlers ??= new Dictionary<RoutedEvent, List<EventSubscription>>();

            if (!_eventHandlers.TryGetValue(routedEvent, out var subscriptions))
            {
                subscriptions = new List<EventSubscription>();
                _eventHandlers.Add(routedEvent, subscriptions);
            }

            subscriptions.Add(subscription);
            TotalHandlersCount++;
        }

        private void AddToEventRoute(RoutedEvent routedEvent, EventRoute route)
        {
            routedEvent = routedEvent ?? throw new ArgumentNullException(nameof(routedEvent));
            route = route ?? throw new ArgumentNullException(nameof(route));

            if (_eventHandlers != null &&
                _eventHandlers.TryGetValue(routedEvent, out var subscriptions))
            {
                foreach (var sub in subscriptions)
                {
                    route.Add(this, sub.Handler, sub.Routes, sub.HandledEventsToo, sub.InvokeAdapter);
                }
            }
        }

        private readonly struct EventSubscription
        {
            public EventSubscription(
                Delegate handler,
                RoutingStrategies routes,
                bool handledEventsToo,
                Action<Delegate, object, RoutedEventArgs>? invokeAdapter = null)
            {
                Handler = handler;
                Routes = routes;
                HandledEventsToo = handledEventsToo;
                InvokeAdapter = invokeAdapter;
            }

            public Action<Delegate, object, RoutedEventArgs>? InvokeAdapter { get; }

            public Delegate Handler { get; }

            public RoutingStrategies Routes { get; }

            public bool HandledEventsToo { get; }
        }
    }
}
