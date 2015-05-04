namespace Cedar.Projections
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    public class ProjectionTests
    {
        [Fact]
        public void Can_resolve_handlers()
        {
            var handlerResolver = new EventHandlerResolver(new TestEventModule(new List<object>()));
            var handlers = handlerResolver.ResolveAll<TestEvent>().ToArray();

            handlers.Length.Should().Be(1);
        }

        [Fact]
        public void Can_invoke_handlers()
        {
            List<object> projectedEvents = new List<object>();
            var handlerResolver = new EventHandlerResolver(new TestEventModule(projectedEvents));
            var handlers = handlerResolver.ResolveAll<TestEvent>().ToArray();
            var projectionEvent = new EventMessage<TestEvent>("streamid", Guid.NewGuid(), 1, DateTimeOffset.UtcNow, null, new TestEvent());

            foreach (var handler in handlers)
            {
                handler(projectionEvent, CancellationToken.None);
            }

            projectedEvents.Count.Should().Be(1);
        }

        [Fact]
        public async Task Can_dispatch_event()
        {
            List<object> projectedEvents = new List<object>();
            var handlerResolver = new EventHandlerResolver(new TestEventModule(projectedEvents));
            const string streamId = "stream";
            var eventId = Guid.NewGuid();
            const int version = 2;
            var timeStamp = DateTimeOffset.UtcNow;
            var headers = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

            using (var dispatcher = new TestEventDispatcher(handlerResolver, new InMemoryCheckpointRepository()))
            {
                await dispatcher.Start();
                await dispatcher.DoDispatch(streamId, eventId, version, timeStamp, "checkpoint", headers, new TestEvent());
            }

            projectedEvents.Count.Should().Be(1);

            var projectionEvent = projectedEvents[0].As<EventMessage<TestEvent>>();
            projectionEvent.StreamId.Should().Be(streamId);
            projectionEvent.EventId.Should().Be(eventId);
            projectionEvent.Version.Should().Be(version);
            projectionEvent.TimeStamp.Should().Be(timeStamp);
            projectionEvent.Headers.Should().NotBeNull();
        }

        private class TestEventModule : EventHandlerModule
        {
            public TestEventModule(List<object> projectedEvents)
            {
                For<TestEvent>()
                    .Pipe(next => next)
                    .Handle(projectedEvents.Add);
            }
        }

        private class TestEvent { }
    }
}