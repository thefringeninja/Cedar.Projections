namespace Cedar.Projections.Example.RavenDb.RavenDb
{
    using System;
    using System.Threading.Tasks;
    using Cedar.Projections.Example.RavenDb.Logging;
    using Raven.Client;

    internal class ImmediateDocumentSessionObserver<TView> : IAsyncObserver<RavenOperation>, ICheckpointRepository
    {
        private readonly Func<IAsyncDocumentSession> _sessionFactory;
        private readonly ICheckpointRepository _checkpointRepository;
        private static readonly ILog s_log;

        static ImmediateDocumentSessionObserver()
        {
            s_log = LogProvider.For<ImmediateDocumentSessionObserver<TView>>();
        }

        public ImmediateDocumentSessionObserver(Func<IAsyncDocumentSession> sessionFactory)
        {
            _sessionFactory = sessionFactory;
            _checkpointRepository = new RavenCheckpointRepository<TView>(sessionFactory);
        }

        public async Task OnNext(RavenOperation next)
        {
            using(IAsyncDocumentSession session = _sessionFactory())
            {
                await next.Operation(session);
                await session.PersistCheckpointToken<TView>(next.CheckpointToken);
                await session.SaveChangesAsync();
            }

            s_log.Info("Saved one document.");
        }

        public Task OnCompleted()
        {
            return Task.FromResult(true);
        }

        public Task<string> Get()
        {
            return _checkpointRepository.Get();
        }

        public Task Put(string checkpointToken)
        {
            return _checkpointRepository.Put(checkpointToken);
        }
    }
}