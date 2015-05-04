namespace Cedar.Projections.Example.RavenDb.RavenDb
{
    using System;
    using System.Threading.Tasks;
    using Raven.Client;
    using Raven.Client.Document;

    internal static class RavenExtensions
    {
        public static IAsyncObserver<RavenOperation> CreateObserver<T>(this Func<IAsyncDocumentSession> sessionFactory)
        {
            return new ImmediateDocumentSessionObserver<T>(sessionFactory);
        }

        public static async Task<string> GetCheckpointToken<T>(this IDocumentStore store)
        {
            using(IAsyncDocumentSession session = store.OpenAsyncSession())
            {
                return await session.GetCheckpointToken<T>();
            }
        }

        public static async Task PersistCheckpointToken<T>(this IDocumentStore store, string checkpointToken)
        {
            using(IAsyncDocumentSession session = store.OpenAsyncSession())
            {
                await session.PersistCheckpointToken<T>(checkpointToken);
            }
        }

        public static async Task<string> GetCheckpointToken<T>(this IAsyncDocumentSession session)
        {
            DocumentConvention conventions = session.Advanced.DocumentStore.Conventions;

            string tag = conventions.GetTypeTagName(typeof(T));

            string id = conventions.FindFullDocumentKeyFromNonStringIdentifier(tag, typeof(CheckpointTokenOfView), true);

            var checkpointTokenOfView = await session.LoadAsync<CheckpointTokenOfView>(id);

            return checkpointTokenOfView == null ? null : checkpointTokenOfView.CheckpointToken;
        }

        public static async Task PersistCheckpointToken<T>(this IAsyncDocumentSession session, string checkpointToken)
        {
            DocumentConvention conventions = session.Advanced.DocumentStore.Conventions;

            string tag = conventions.GetTypeTagName(typeof(T));
            string id = conventions.FindFullDocumentKeyFromNonStringIdentifier(tag, typeof(CheckpointTokenOfView), true);

            CheckpointTokenOfView positionOfView = await session.LoadAsync<CheckpointTokenOfView>(id) ?? new CheckpointTokenOfView
            {
                Tag = tag,
                Id = id
            };

            if(checkpointToken == null)
            {
                return;
            }

            positionOfView.CheckpointToken = checkpointToken;

            await session.StoreAsync(positionOfView);
        }
    }
}