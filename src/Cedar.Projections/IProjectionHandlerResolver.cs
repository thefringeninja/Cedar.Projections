namespace Cedar.Projections
{
    using System.Collections.Generic;

    public interface IProjectionHandlerResolver
    {
        IEnumerable<ProjectionHandler<TMessage>> ResolveAll<TMessage>() where TMessage : class;
    }
}