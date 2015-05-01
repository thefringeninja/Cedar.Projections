namespace Cedar.Projections
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public delegate Task ProjectionHandler<TMessage>(ProjectionEvent<TMessage> projectionEvent, CancellationToken ct)
        where TMessage: class;

    public delegate void ProjectionHandlerSync<TMessage>(ProjectionEvent<TMessage> projectionEvent) 
        where TMessage : class;

    public delegate ProjectionHandler<TMessage> Pipe<TMessage>(ProjectionHandler<TMessage> next) 
        where TMessage : class;

    /// <summary>
    /// Represents a collection of handlers pipelines.
    /// </summary>
    public class ProjectionHandlerModule
    {
        private readonly List<ProjectionHandlerRegistration> _handlerRegistrations = new List<ProjectionHandlerRegistration>();

        internal IEnumerable<ProjectionHandlerRegistration> HandlerRegistrations
        {
            get { return _handlerRegistrations; }
        }

        /// <summary>
        /// Starts to build a handler pipeline for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message the pipeline will handle.</typeparam>
        /// <returns>A a handler builder to continue defining the pipeline.</returns>
        public IProjectionHandlerBuilder<TMessage> For<TMessage>() where TMessage : class
        {
            return new HandlerBuilder<TMessage>(_handlerRegistrations.Add);
        }

        private class HandlerBuilder<TMessage> : IProjectionHandlerBuilder<TMessage> where TMessage : class
        {
            private readonly Action<ProjectionHandlerRegistration> _registerHandler;
            private readonly Stack<Pipe<TMessage>> _pipes = new Stack<Pipe<TMessage>>();

            internal HandlerBuilder(Action<ProjectionHandlerRegistration> registerHandler)
            {
                _registerHandler = registerHandler;
            }

            public IProjectionHandlerBuilder<TMessage> Pipe(Pipe<TMessage> pipe)
            {
                _pipes.Push(pipe);
                return this;
            }

            public void Handle(ProjectionHandler<TMessage> projectionHandler)
            {
                while (_pipes.Count > 0)
                {
                    var pipe = _pipes.Pop();
                    projectionHandler = pipe(projectionHandler);
                }

                _registerHandler(new ProjectionHandlerRegistration(typeof(ProjectionHandler<TMessage>), projectionHandler));
            }
        }
    }
}