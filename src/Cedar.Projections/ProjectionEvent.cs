namespace Cedar.Projections
{
    using System;
    using System.Collections.Generic;

    public sealed class ProjectionEvent<T>
        where T : class
    {
        private readonly string _streamId;
        private readonly Guid _eventId;
        private readonly int _version;
        private readonly DateTimeOffset _timeStamp;
        private readonly IReadOnlyDictionary<string, object> _headers;
        private readonly T _event;

        public ProjectionEvent(
            string streamId,
            Guid eventId,
            int version,
            DateTimeOffset timeStamp,
            IReadOnlyDictionary<string, object> headers,
            T @event)
        {
            _streamId = streamId;
            _eventId = eventId;
            _version = version;
            _timeStamp = timeStamp;
            _headers = headers;
            _event = @event;
        }

        public string StreamId
        {
            get { return _streamId; }
        }

        public Guid EventId
        {
            get { return _eventId; }
        }

        public int Version
        {
            get { return _version; }
        }

        public DateTimeOffset TimeStamp
        {
            get { return _timeStamp; }
        }

        public IReadOnlyDictionary<string, object> Headers
        {
            get { return _headers; }
        }

        public T Event
        {
            get { return _event; }
        }
    }
}