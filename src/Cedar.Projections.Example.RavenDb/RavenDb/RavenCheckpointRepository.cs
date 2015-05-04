namespace Cedar.Projections.Example.RavenDb.RavenDb
{
    using System;
    using System.Threading.Tasks;
    using Raven.Client;

    internal class RavenCheckpointRepository<T> : ICheckpointRepository
    {
        private readonly Func<IAsyncDocumentSession> _sessionFactory;

        public RavenCheckpointRepository(Func<IAsyncDocumentSession> sessionFactory)
        {
            _sessionFactory = sessionFactory;
        }

        public Task<string> Get()
        {
            var session = _sessionFactory();

            return session.GetCheckpointToken<T>().ContinueWith(task =>
            {
                session.Dispose();

                return task.Result;
            });
        }

        public Task Put(string checkpointToken)
        {
            var session = _sessionFactory();

            return session.PersistCheckpointToken<T>(checkpointToken).ContinueWith(_ => session.Dispose());
        }
    }
}