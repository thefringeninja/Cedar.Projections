namespace Cedar.Projections.Example.RavenDb.Handlers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;
    using Cedar.Projections.Example.RavenDb.Logging;
    using EnsureThat;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.SystemData;

    public class ResolvedEventDispatcher : IDisposable
    {
        private readonly ICheckpointRepository _checkpoints;
        private readonly CancellationTokenSource _disposed = new CancellationTokenSource();
        private readonly IEventStoreConnection _eventStore;
        private readonly InterlockedBoolean _isDisposed;
        private readonly InterlockedBoolean _isStarted;
        private readonly Action _onCaughtUp;
        private readonly Subject<ResolvedEvent> _projectedEvents;
        private readonly SimpleQueue _queue;
        private readonly string _streamId;
        private readonly UserCredentials _userCredentials;
        private EventStoreCatchUpSubscription _subscription;

        public ResolvedEventDispatcher(
            IEventStoreConnection eventStore,
            ICheckpointRepository checkpoints,
            IEventHandlerResolver resolver,
            Action onCaughtUp = null,
            string streamId = null,
            UserCredentials userCredentials = null)
        {
            _isStarted = new InterlockedBoolean();
            _isDisposed = new InterlockedBoolean();

            _eventStore = eventStore;
            _checkpoints = checkpoints;
            _onCaughtUp = onCaughtUp ?? (() => { });
            _streamId = streamId;
            _userCredentials = userCredentials;
            _projectedEvents = new Subject<ResolvedEvent>();
            _projectedEvents.SelectMany(
                resolvedEvent =>
                    resolver.DispatchResolvedEvent(resolvedEvent, _subscription.IsSubscribedToAll, _disposed.Token)
                        .ContinueWith(_ => Unit.Default))
                .Subscribe();

            _queue = new SimpleQueue(_disposed);
            _queue.Register<ResolvedEvent>(async (resolvedEvent, token) =>
            {
                try
                {
                    await resolver.DispatchResolvedEvent(resolvedEvent, _subscription.IsSubscribedToAll, _disposed.Token);
                }
                catch(Exception ex)
                {
                    _projectedEvents.OnError(ex);
                    throw;
                }

                if(_isDisposed.Value)
                {
                    return;
                }

                _projectedEvents.OnNext(resolvedEvent);

            });
            _queue.Register<CaughtUp>((_, token) =>
            {
                _onCaughtUp();

                return Task.FromResult(0);
            });
        }

        public void Dispose()
        {
            if(_isDisposed.EnsureCalledOnce())
            {
                return;
            }

            _disposed.Cancel();
            _projectedEvents.Dispose();

            if(_subscription != null)
            {
                _subscription.Stop();
                _subscription = null;
            }
        }

        public async Task Start()
        {
            if(_isStarted.EnsureCalledOnce())
            {
                return;
            }

            await RecoverSubscription();
        }

        private async Task RecoverSubscription()
        {
            var checkpointToken = await _checkpoints.Get();

            _subscription = _streamId == null
                ? SubscribeToAllFrom(checkpointToken.ParsePosition())
                : SubscribeToStreamFrom(checkpointToken == null ? default(int?) : int.Parse(checkpointToken));
        }

        private EventStoreCatchUpSubscription SubscribeToStreamFrom(int? lastCheckpoint)
        {
            return _eventStore.SubscribeToStreamFrom(_streamId,
                lastCheckpoint,
                true,
                EventAppeared,
                _ => _queue.Enqueue(new CaughtUp()),
                SubscriptionDropped,
                _userCredentials);
        }

        private EventStoreCatchUpSubscription SubscribeToAllFrom(Position lastCheckpoint)
        {
            return _eventStore.SubscribeToAllFrom(lastCheckpoint,
                false,
                EventAppeared,
                _ => _queue.Enqueue(new CaughtUp()),
                SubscriptionDropped,
                _userCredentials);
        }

        private void SubscriptionDropped(EventStoreCatchUpSubscription _, SubscriptionDropReason reason, Exception ex)
        {
            if(reason == SubscriptionDropReason.UserInitiated)
            {
                return;
            }

            RecoverSubscription().Wait(TimeSpan.FromSeconds(2));
        }

        private void EventAppeared(EventStoreCatchUpSubscription _, ResolvedEvent resolvedEvent)
        {
            if(resolvedEvent.OriginalEvent.EventType.StartsWith("$")
               || resolvedEvent.OriginalStreamId.StartsWith("$"))
            {
                return;
            }

            _queue.Enqueue(resolvedEvent);
        }

        private class SimpleQueue
        {
            private static readonly ILog s_logger;
            private readonly ConcurrentQueue<object> _events;
            private readonly InterlockedBoolean _isPushing;
            private readonly CancellationTokenSource _isDisposed;
            private readonly IDictionary<Type, Func<object, CancellationToken, Task>> _handlers; 

            static SimpleQueue()
            {
                s_logger = LogProvider.For<SimpleQueue>();
            }

            public SimpleQueue(CancellationTokenSource isDisposed)
            {
                _isDisposed = isDisposed;
                _events = new ConcurrentQueue<object>();
                _isPushing = new InterlockedBoolean();
                _handlers = new Dictionary<Type, Func<object, CancellationToken, Task>>();
            }

            public void Register<T>(Func<T, CancellationToken, Task> handler)
            {
                _handlers[typeof(T)] = (message, token) => handler((T)message, token);
            }

            public void Enqueue(object message)
            {
                Ensure.That(message, "message").IsNotNull();
                if(_isDisposed.IsCancellationRequested)
                {
                    throw new InvalidOperationException("Cancellation requested.");
                }
                _events.Enqueue(message);
                Push();
            }

            private void Push()
            {
                if(_isPushing.CompareExchange(true, false))
                {
                    return;
                }
                Task.Run(async () =>
                {
                    object message;
                    while(!_isDisposed.IsCancellationRequested && _events.TryDequeue(out message))
                    {
                        try
                        {
                            Func<object, CancellationToken, Task> handler;
                            if(!_handlers.TryGetValue(message.GetType(), out handler))
                            {
                                s_logger.WarnFormat("No handler for type {0} registered.", message.GetType());
                                continue;
                            }
                            await handler(message, _isDisposed.Token);
                        }
                        catch(Exception ex)
                        {
                            s_logger.FatalException(ex.Message, ex);
                            _isDisposed.Cancel();
                        }
                    }
                    _isPushing.Set(false);
                },
                    _isDisposed.Token);
            }
        }
        class CaughtUp { }
    }
}