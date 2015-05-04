namespace Cedar.Projections.Example.RavenDb.Handlers
{
    public static class EventMessageHeaders
    {
        public const string Type = "ClrType";
        public const string CheckpointToken = "CheckpointToken";
        public const string Timestamp = "Timestamp";

        public static string GetCheckpointToken<TMessage>(this EventMessage<TMessage> message) where TMessage : class
        {
            object checkpointToken;
            message.Headers.TryGetValue(CheckpointToken, out checkpointToken);

            return checkpointToken == null ? null : (checkpointToken as string ?? checkpointToken.ToString());
        }
    }
}