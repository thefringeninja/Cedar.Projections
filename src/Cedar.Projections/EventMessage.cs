namespace Cedar.Projections
{
    using System;
    using System.Collections.Generic;

    public sealed class EventMessage<T>
        where T : class
    {
        public readonly string StreamId;
        public readonly Guid EventId;
        public readonly int Version;
        public readonly DateTimeOffset TimeStamp;
        public readonly IReadOnlyDictionary<string, object> Headers;
        public readonly T Event;

        public EventMessage(
            string streamId,
            Guid eventId,
            int version,
            DateTimeOffset timeStamp,
            IReadOnlyDictionary<string, object> headers,
            T @event)
        {
            StreamId = streamId;
            EventId = eventId;
            Version = version;
            TimeStamp = timeStamp;
            Headers = headers;
            Event = @event;
        }
    }
}