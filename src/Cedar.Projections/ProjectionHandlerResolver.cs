namespace Cedar.Projections
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ProjectionHandlerResolver : IProjectionHandlerResolver
    {
        private readonly Dictionary<Type, List<object>> _handlers = new Dictionary<Type, List<object>>();

        public ProjectionHandlerResolver(params ProjectionHandlerModule[] handlerModules)
        {
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

        public IEnumerable<ProjectionHandler<TMessage>> ResolveAll<TMessage>() where TMessage : class
        {
            return _handlers[typeof(ProjectionHandler<TMessage>)]
                .Select(handler => (ProjectionHandler<TMessage>) handler);
        }
    }
}