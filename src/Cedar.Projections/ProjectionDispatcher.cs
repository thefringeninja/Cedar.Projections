namespace Cedar.Projections
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Cedar.Handlers;
    using Cedar.Projections.Logging;

    public abstract class ProjectionDispatcher : IDisposable
    {
        private static readonly ILog s_logger = LogProvider.For<ProjectionDispatcher>();
        private readonly IProjectionHandlerResolver _handlerResolver;
        private readonly ICheckpointRepository _checkpointRepository;
        private readonly InterlockedBoolean _isStarted = new InterlockedBoolean();
        private readonly InterlockedBoolean _isDisposed = new InterlockedBoolean();
        private readonly CancellationTokenSource _disposed = new CancellationTokenSource();
        
        protected ProjectionDispatcher(
            IProjectionHandlerResolver handlerResolver,
            ICheckpointRepository checkpointRepository)
        {
            _handlerResolver = handlerResolver;
            _checkpointRepository = checkpointRepository;
        }

        public async Task Start()
        {
            if (_isStarted.EnsureCalledOnce())
            {
                return;
            }

            string checkpointToken = await _checkpointRepository.Get();
            await OnStart(checkpointToken);
        }

        protected abstract Task OnStart(string fromCheckpoint);

        protected async Task Dispatch(
            string streamId,
            Guid eventId,
            int version,
            DateTimeOffset timeStamp,
            string checkpointToken,
            IReadOnlyDictionary<string, object> headers, 
            object @event)
        {
            var eventType = @event.GetType();
            var dispatchInternalMethod = typeof(ProjectionDispatcher).GetRuntimeMethods()
                .Single(m => m.Name.Equals("DispatchInternal", StringComparison.Ordinal))
                .MakeGenericMethod(eventType);

            try
            {
                await (Task)dispatchInternalMethod.Invoke(this, new[] { streamId, eventId, version, timeStamp, headers, @event});
                await _checkpointRepository.Put(checkpointToken);
            }
            catch (Exception ex)
            {
                s_logger.ErrorException(
                    "Exception occured dispatching an event",
                    ex,
                    eventId);
                throw;
            }
        }

        public virtual void Dispose()
        {
            if(_isDisposed.EnsureCalledOnce())
            {
                return;
            }
            _disposed.Cancel();
        }

        private async Task DispatchInternal<T>(
            string streamid,
            Guid eventId,
            int version,
            DateTimeOffset timeStamp,
            IReadOnlyDictionary<string, object> headers,
            T @event) where T : class
        {
            var projectionEvent = new ProjectionEvent<T>(streamid, eventId, version, timeStamp, headers, @event);

            var handlers = _handlerResolver.ResolveAll<T>();

            foreach(var handler in handlers)
            {
                await handler(projectionEvent, _disposed.Token);
            }
        }
    }
}