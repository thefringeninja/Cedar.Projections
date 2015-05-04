namespace Cedar.Projections.Example.RavenDb
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cedar.Projections.Example.RavenDb.Handlers;
    using Cedar.Projections.Example.RavenDb.RavenDb;
    using EventStore.ClientAPI;
    using Raven.Abstractions.Extensions;
    using Raven.Client;

    internal class Projections : IDisposable
    {
        private readonly IList<ResolvedEventDispatcher> _dispatchers;
        private readonly IEventStoreConnection _eventStoreConnection;
        private readonly Func<IAsyncDocumentSession> _sessionFactory;

        public Projections(IEventStoreConnection eventStoreConnection, Func<IAsyncDocumentSession> sessionFactory)
        {
            _eventStoreConnection = eventStoreConnection;
            _sessionFactory = sessionFactory;
            _dispatchers = new List<ResolvedEventDispatcher>();

            Project<Guid, InventoryItemView>((module, writer) =>
            {
                module.For<InventoryCheckedIn>()
                    .Handle(
                        (message, token) => writer.AddOrUpdate(message.Event.Id,
                            message.GetCheckpointToken(),
                            view =>
                            {
                                view.Sku = message.Event.Sku;
                                view.Quantity += message.Event.Quantity;
                            }));

                module.For<InventoryCheckedOut>()
                    .Handle(
                        (message, token) => writer.AddOrUpdate(message.Event.Id,
                            message.GetCheckpointToken(),
                            view =>
                            {
                                view.Sku = message.Event.Sku;
                                view.Quantity -= message.Event.Quantity;
                            }));
            });
        }

        public void Dispose()
        {
            _dispatchers.ForEach(d => d.Dispose());
        }

        private void Project<TKey, TView>(Action<EventHandlerModule, RavenViewWriter<TKey, TView>> onModule)
            where TView : class
        {
            var observer = new CatchupDocumentSessionObserver<TView>(_sessionFactory, 100);

            var writer = new RavenViewWriter<TKey, TView>(observer);

            var module = new EventHandlerModule();

            onModule(module, writer);

            var dispatcher = new ResolvedEventDispatcher(
                _eventStoreConnection,
                observer,
                new EventHandlerResolver(module),
                observer.CaughtUp);

            _dispatchers.Add(dispatcher);
        }

        public Task Start()
        {
            return Task.WhenAll(_dispatchers.Select(d => d.Start()));
        }
    }
}