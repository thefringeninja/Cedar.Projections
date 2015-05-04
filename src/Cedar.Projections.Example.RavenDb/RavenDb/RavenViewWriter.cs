namespace Cedar.Projections.Example.RavenDb.RavenDb
{
    using System;
    using System.Threading.Tasks;
    using Raven.Client;

    internal class RavenViewWriter<TKey, TView>
        where TView : class
    {
        private static readonly Func<TKey, TView> CreateView;
        private readonly IAsyncObserver<RavenOperation> _raven;

        static RavenViewWriter()
        {
            if(false == typeof(ValueType).IsAssignableFrom(typeof(TKey)))
            {
                throw new ArgumentException(string.Format("The key type {0} is not assignable to a valuetype", typeof(TKey).Name));
            }
            var constructor = typeof(TView).GetConstructor(new[] {typeof(TKey)});

            if(constructor == null)
            {
                throw new ArgumentException(string.Format("The view type {0} does not have a constructor that accepts a parameter of type {1}",
                    typeof(TView).Name, typeof(TKey).Name));
            }

            CreateView = key => (TView)Activator.CreateInstance(typeof(TView), key);
        }

        public RavenViewWriter(IAsyncObserver<RavenOperation> raven)
        {
            _raven = raven;
        }

        public Task AddOrUpdate(TKey key, string checkpointToken, Action<TView> update)
        {
            return _raven.OnNext(new RavenOperation(async session =>
            {
                var id = key as ValueType;

                TView document = await session.LoadAsync<TView>(id);

                if(document == null)
                {
                    document = await CreateInternal(key, session);
                }

                update(document);
            }, checkpointToken));
        }

        public Task TryAdd(TKey key, string checkpointToken, Action<TView> update)
        {
            return _raven.OnNext(new RavenOperation(async session =>
            {
                var id = key as ValueType;

                TView document = await session.LoadAsync<TView>(id);

                if(document != null)
                {
                    return;
                }

                document = await CreateInternal(key, session);

                if(update != null)
                {
                    update(document);
                }

                await session.StoreAsync(document);
            }, checkpointToken));
        }

        public Task TryUpdate(TKey key, string checkpointToken, Action<TView> update)
        {
            return _raven.OnNext(new RavenOperation(async session =>
            {
                string documentKey = GetDocumentKey(key, session);

                TView document = await session.LoadAsync<TView>(documentKey);

                if(document == null)
                {
                    return;
                }

                update(document);
            }, checkpointToken));
        }

        public Task TryDelete(TKey key, string checkpointToken)
        {
            return _raven.OnNext(new RavenOperation(async session =>
            {
                var id = key as ValueType;

                TView document = await session.LoadAsync<TView>(id);

                if(document != null)
                {
                    session.Delete(document);
                }
            }, checkpointToken));
        }

        private static string GetDocumentKey(TKey key, IAsyncDocumentSession session)
        {
            return session.Advanced.DocumentStore.Conventions
                .FindFullDocumentKeyFromNonStringIdentifier(key, typeof(TView), true);
        }

        private static async Task<TView> CreateInternal(TKey key, IAsyncDocumentSession session)
        {
            TView view = CreateView(key);
            await session.StoreAsync(view);
            return view;
        }
    }
}