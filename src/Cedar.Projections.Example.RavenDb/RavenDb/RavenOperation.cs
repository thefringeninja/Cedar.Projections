namespace Cedar.Projections.Example.RavenDb.RavenDb
{
    using System;
    using System.Threading.Tasks;
    using Raven.Client;

    internal struct RavenOperation
    {
        public readonly string CheckpointToken;
        public readonly Func<IAsyncDocumentSession, Task> Operation;

        public RavenOperation(Func<IAsyncDocumentSession, Task> operation, string checkpointToken)
        {
            Operation = operation;
            CheckpointToken = checkpointToken;
        }
    }
}