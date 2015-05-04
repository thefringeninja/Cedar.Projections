namespace Cedar.Projections.Example.RavenDb.RavenDb
{
    using System;
    using System.Threading.Tasks;
    using Cedar.Projections.Example.RavenDb.Logging;
    using EnsureThat;
    using Raven.Client;

    internal class BatchingDocumentSessionObserver : IAsyncObserver<RavenOperation>, ICheckpointRepository
    {
        private readonly Func<IAsyncDocumentSession> _sessionFactory;
        private string _checkpointToken;
        private IAsyncDocumentSession _session;
        private static readonly ILog s_log;
        private readonly int _maxRequestCount;
        private readonly int _maxSafeRequestCount;

        static BatchingDocumentSessionObserver()
        {
            s_log = LogProvider.For<BatchingDocumentSessionObserver>();
        }

        public BatchingDocumentSessionObserver(Func<IAsyncDocumentSession> sessionFactory, int maxRequestCount = 1024)
        {
            Ensure.That(maxRequestCount, "maxRequestCount").IsGt(20);

            _maxRequestCount = maxRequestCount;
            _maxSafeRequestCount = maxRequestCount - 10;
            _sessionFactory = sessionFactory;
            _session = OpenDocumentSession();
        }

        public async Task OnNext(RavenOperation next)
        {
            await next.Operation(_session);
            _checkpointToken = next.CheckpointToken;
            await FlushIfNecessary();
        }

        public async Task OnCompleted()
        {
            await Flush();
            _session.Dispose();
            _session = null;
        }

        public Task<string> Get()
        {
            return Task.FromResult(_checkpointToken);
        }

        public Task Put(string checkpointToken)
        {
            return Task.FromResult(true);
        }

        private IAsyncDocumentSession OpenDocumentSession()
        {
            IAsyncDocumentSession session = _sessionFactory();
            session.Advanced.MaxNumberOfRequestsPerSession = _maxRequestCount;
            
            s_log.Info("New batch started."); 
            
            return session;
        }

        private Task FlushIfNecessary()
        {
            return _session.Advanced.NumberOfRequests < _maxSafeRequestCount ? Task.FromResult(true) : Flush();
        }

        private async Task Flush()
        {
            s_log.InfoFormat("Flushing batch of {0} documents.", _session.Advanced.WhatChanged().Count);
            await _session.SaveChangesAsync();
            _session.Dispose();
            _session = OpenDocumentSession();
        }
    }
}