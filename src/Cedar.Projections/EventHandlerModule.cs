namespace Cedar.Projections
{
    using System;
    using System.Collections.Generic;

    public delegate Handler<TMessage> Pipe<TMessage>(Handler<TMessage> next) 
        where TMessage : class;

    /// <summary>
    /// Represents a collection of handlers pipelines.
    /// </summary>
    public class EventHandlerModule
    {
        private readonly List<EventHandlerRegistration> _handlerRegistrations = new List<EventHandlerRegistration>();

        internal IEnumerable<EventHandlerRegistration> HandlerRegistrations
        {
            get { return _handlerRegistrations; }
        }

        /// <summary>
        /// Starts to build a handler pipeline for the specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message the pipeline will handle.</typeparam>
        /// <returns>A a handler builder to continue defining the pipeline.</returns>
        public IEventHandlerBuilder<TMessage> For<TMessage>() where TMessage : class
        {
            return new EventHandlerBuilder<TMessage>(_handlerRegistrations.Add);
        }

        private class EventHandlerBuilder<TMessage> : IEventHandlerBuilder<TMessage> where TMessage : class
        {
            private readonly Action<EventHandlerRegistration> _registerHandler;
            private readonly Stack<Pipe<TMessage>> _pipes = new Stack<Pipe<TMessage>>();

            internal EventHandlerBuilder(Action<EventHandlerRegistration> registerHandler)
            {
                _registerHandler = registerHandler;
            }

            public IEventHandlerBuilder<TMessage> Pipe(Pipe<TMessage> pipe)
            {
                _pipes.Push(pipe);
                return this;
            }

            public void Handle(Handler<TMessage> handler)
            {
                while (_pipes.Count > 0)
                {
                    var pipe = _pipes.Pop();
                    handler = pipe(handler);
                }

                _registerHandler(new EventHandlerRegistration(typeof(Handler<TMessage>), handler));
            }
        }
    }
}