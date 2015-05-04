namespace Cedar.Projections
{
    using System.Threading.Tasks;

    public static class EventHandlerBuilderExtensions
    {
        /// <summary>
        /// Handles the message and is the last stage in a handler pipeline.
        /// </summary>
        /// <param name="handlerBuilder">The <see cref="IEventHandlerBuilder{TMessage}"/>instance.</param>
        /// <param name="handler">The handler.</param>
        public static void Handle<TMessage>(this IEventHandlerBuilder<TMessage> handlerBuilder, EventHandlerSync<TMessage> handler)
            where TMessage : class
        {
            handlerBuilder.Handle((message, _) =>
            {
                handler(message);
                return Task.FromResult(0);
            });
        }
    }
}