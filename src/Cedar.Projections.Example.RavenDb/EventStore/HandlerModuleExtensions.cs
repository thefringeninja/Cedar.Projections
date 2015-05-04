namespace Cedar.Projections.Example.RavenDb.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using EnsureThat;
    using EventStore.ClientAPI;

    public static class HandlerModuleExtensions
    {
        private static readonly MethodInfo DispatchDomainEventMethod;

        static HandlerModuleExtensions()
        {
            DispatchDomainEventMethod = typeof(HandlerModuleExtensions)
                .GetMethod("DispatchDomainEvent", BindingFlags.Static | BindingFlags.NonPublic);
        }

        public static Task DispatchResolvedEvent(
            this IEventHandlerResolver handlerResolver,
            ResolvedEvent resolvedEvent,
            bool isSubscribedToAll,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.That(handlerResolver).IsNotNull();

            IDictionary<string, object> headers;
            var @event = resolvedEvent.DeserializeEventData(out headers);

            return (Task)DispatchDomainEventMethod.MakeGenericMethod(@event.GetType()).Invoke(null,
                new[]
                {
                    handlerResolver, @event, headers, resolvedEvent, isSubscribedToAll, cancellationToken
                });
        }

        private static Task DispatchDomainEvent<TDomainEvent>(
            IEventHandlerResolver handlerResolver,
            TDomainEvent @event,
            IDictionary<string, object> headers,
            ResolvedEvent resolvedEvent,
            bool isSubscribedToAll,
            CancellationToken cancellationToken)
            where TDomainEvent : class
        {
            var timeStamp = new DateTimeOffset(DateTime.FromFileTime(resolvedEvent.Event.CreatedEpoch));

            return handlerResolver.Dispatch(
                resolvedEvent.Event.EventStreamId,
                resolvedEvent.Event.EventId,
                resolvedEvent.Event.EventNumber,
                timeStamp,
                new Dictionary<string, object>(headers)
                {
                    {
                        EventMessageHeaders.CheckpointToken,
                        isSubscribedToAll
                            ? resolvedEvent.OriginalPosition.ToCheckpointToken()
                            : resolvedEvent.OriginalEventNumber.ToString()
                    }
                },
                @event,
                cancellationToken);
        }
    }
}