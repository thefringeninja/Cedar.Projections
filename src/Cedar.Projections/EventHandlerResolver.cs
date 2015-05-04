namespace Cedar.Projections
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EnsureThat;

    public class EventHandlerResolver : IEventHandlerResolver
    {
        private readonly Dictionary<Type, List<object>> _handlers = new Dictionary<Type, List<object>>();

        public EventHandlerResolver(params EventHandlerModule[] handlerModules)
        {
            Ensure.That(handlerModules).IsNotNull();

            foreach(var registration in handlerModules.SelectMany(module => module.HandlerRegistrations))
            {
                List<object> handlers;
                if(!_handlers.TryGetValue(registration.RegistrationType, out handlers))
                {
                    handlers = new List<object>();
                }
                handlers.Add(registration.HandlerInstance);
                _handlers[registration.RegistrationType] = handlers;
            }
        }

        public IEnumerable<Handler<TMessage>> ResolveAll<TMessage>() where TMessage : class
        {
            return _handlers[typeof(Handler<TMessage>)]
                .Select(handler => (Handler<TMessage>) handler);
        }
    }
}