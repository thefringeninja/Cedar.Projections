﻿namespace Cedar.Projections
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal class TestEventDispatcher : EventDispatcher
    {
        public TestEventDispatcher(IEventHandlerResolver eventHandlerResolver, ICheckpointRepository checkpointRepository)
            : base(eventHandlerResolver, checkpointRepository)
        { }

        internal Task DoDispatch(
            string streamId,
            Guid eventId,
            int version,
            DateTimeOffset timeStamp,
            string checkpointToken,
            IReadOnlyDictionary<string, object> headers,
            object @event)
        {
            return Dispatch(streamId, eventId, version, timeStamp, checkpointToken, headers, @event);
        }
    }
}