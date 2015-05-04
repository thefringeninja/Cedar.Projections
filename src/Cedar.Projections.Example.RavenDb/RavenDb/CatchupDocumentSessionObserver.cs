namespace Cedar.Projections.Example.RavenDb.RavenDb
{
    using System;
    using System.Threading.Tasks;
    using Cedar.Projections.Example.RavenDb.Logging;
    using Raven.Client;

    internal class CatchupDocumentSessionObserver<TView> : IAsyncObserver<RavenOperation>, ICheckpointRepository
    {
        private readonly Func<IAsyncDocumentSession> _sessionFactory;
        private ICheckpointRepository _checkpoints;
        private IAsyncObserver<RavenOperation> _inner;
        private static readonly ILog s_log;

        static CatchupDocumentSessionObserver()
        {
            s_log = LogProvider.For<CatchupDocumentSessionObserver<TView>>();
        }

        public CatchupDocumentSessionObserver(Func<IAsyncDocumentSession> sessionFactory, int requestCount = 1024)
        {
            _sessionFactory = sessionFactory;
            var observer = new BatchingDocumentSessionObserver(sessionFactory, requestCount);
            _inner = observer;
            _checkpoints = observer;
        }

        public Task OnNext(RavenOperation next)
        {
            return _inner.OnNext(next);
        }

        public Task OnCompleted()
        {
            return _inner.OnCompleted();
        }

        public Task<string> Get()
        {
            return _checkpoints.Get();
        }

        public Task Put(string checkpointToken)
        {
            return _checkpoints.Put(checkpointToken);
        }

        public void CaughtUp()
        {
            s_log.Info("Caught up received.");
            _inner.OnCompleted();
            var observer = new ImmediateDocumentSessionObserver<TView>(_sessionFactory);
            _inner = observer;
            _checkpoints = observer;
            s_log.Info("Switched to immediate mode.");
        }
    }
}