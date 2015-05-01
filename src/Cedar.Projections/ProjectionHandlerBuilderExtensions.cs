namespace Cedar.Projections
{
    using System.Threading.Tasks;

    public static class ProjectionHandlerBuilderExtensions
    {
        /// <summary>
        /// Handles the message and is the last stage in a handler pipeline.
        /// </summary>
        /// <param name="handlerBuilder">The <see cref="IProjectionHandlerBuilder{TMessage}"/>instance.</param>
        /// <param name="handler">The handler.</param>
        public static void Handle<TMessage>(this IProjectionHandlerBuilder<TMessage> handlerBuilder, ProjectionHandlerSync<TMessage> handler)
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