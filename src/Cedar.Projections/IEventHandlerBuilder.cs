namespace Cedar.Projections
{
    /// <summary>
    /// Provides a mechanism to fluently build a message handler pipeline.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message the handler will handle.</typeparam>
    public interface IEventHandlerBuilder<TMessage> where TMessage : class
    {
        /// <summary>
        /// Pipes the message through handler middleware.
        /// </summary>
        /// <param name="pipe">The next handler middleware to invoke.</param>
        /// <returns>The <see cref="IEventHandlerBuilder{TMessage}"/> instance.</returns>
        IEventHandlerBuilder<TMessage> Pipe(Pipe<TMessage> pipe);

        /// <summary>
        /// Handles the message and is the last stage in a handler pipeline.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns>A <see cref="Handler{TMessage}"/>.</returns>
        void Handle(Handler<TMessage> handler);
    }
}