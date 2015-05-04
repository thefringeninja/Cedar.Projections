namespace Cedar.Projections.Example.RavenDb.Handlers
{
    using System;
    using System.Linq;
    using EventStore.ClientAPI;

    public static class EventStoreExtensions
    {
        public static string ToCheckpointToken(this Position? position)
        {
            return position.HasValue
                ? position.Value.CommitPosition + "/" +
                  position.Value.PreparePosition
                : null;
        }

        internal static Position ParsePosition(this string checkpointToken)
        {
            var position = Position.Start;

            if(checkpointToken != null)
            {
                var positions = checkpointToken.Split(new[] {'/'}, 2).Select(s => s.Trim()).ToArray();
                if(positions.Length != 2)
                {
                    throw new ArgumentException();
                }

                position = new Position(long.Parse(positions[0]), long.Parse(positions[1]));
            }
            return position;
        }
    }
}