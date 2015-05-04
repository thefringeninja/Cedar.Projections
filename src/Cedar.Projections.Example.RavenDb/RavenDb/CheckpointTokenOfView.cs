namespace Cedar.Projections.Example.RavenDb.RavenDb
{
    internal class CheckpointTokenOfView
    {
        public string Tag { get; set; }
        public string Id { get; set; }
        public string CheckpointToken { get; set; }
    }
}