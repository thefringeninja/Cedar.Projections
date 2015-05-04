namespace Cedar.Projections.Example.RavenDb.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Cedar.Projections.Example.RavenDb;
    using EnsureThat;
    using EventStore.ClientAPI;
    using Newtonsoft.Json;

    public static class SerializationExtensions
    {
        public static EventData SerializeEventData(
    this object @event,
    Guid eventId,
    Action<IDictionary<string, object>> updateHeaders = null,
    Func<Type, string> getClrType = null)
        {
            Ensure.That(@event, "@event").IsNotNull();

            getClrType = getClrType ?? TypeUtilities.ToPartiallyQualifiedName;
            updateHeaders = updateHeaders ?? (_ => { });

            var data = Encode(JsonConvert.SerializeObject(@event));

            var headers = new Dictionary<string, object>();
            updateHeaders(headers);

            var eventType = @event.GetType();

            headers[EventMessageHeaders.Type] = getClrType(eventType);
            headers[EventMessageHeaders.Timestamp] = DateTime.UtcNow;

            var metadata = Encode(JsonConvert.SerializeObject(headers));

            return new EventData(eventId, eventType.Name, true, data, metadata);
        }

        public static object DeserializeEventData(
            this ResolvedEvent resolvedEvent)
        {
            IDictionary<string, object> _;

            return DeserializeEventData(resolvedEvent, out _);
        }

        public static object DeserializeEventData(
            this ResolvedEvent resolvedEvent,
            out IDictionary<string, object> headers)
        {
            
            headers = (IDictionary<string, object>)JsonConvert.DeserializeObject(Decode(resolvedEvent.Event.Metadata), typeof(Dictionary<string, object>));

            var type = Type.GetType((string)headers[EventMessageHeaders.Type]);

            var @event = JsonConvert.DeserializeObject(Decode(resolvedEvent.Event.Data), type);

            return @event;
        }

        private static byte[] Encode(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        private static string Decode(byte[] b)
        {
            return Encoding.UTF8.GetString(b);
        }
    }
}