namespace Cedar.Projections
{
    using System.Collections.Generic;

    public interface IEventHandlerResolver
    {
        IEnumerable<Handler<TMessage>> ResolveAll<TMessage>() where TMessage : class;
    }
}