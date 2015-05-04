namespace Cedar.Projections
{
    using System.Threading.Tasks;

    /// <summary>
    /// Represents durable storage to save and retrieve a checkpoint token.
    /// </summary>
    public interface ICheckpointRepository
    {
        /// <summary>
        /// Gets the current checkpoint.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents the operation.</returns>
        Task<string> Get();

        /// <summary>
        /// Puts the specified checkpoint token.
        /// </summary>
        /// <param name="checkpointToken">The checkpoint token.</param>
        /// <returns>A <see cref="Task"/> that represents the operation.</returns>
        Task Put(string checkpointToken);
    }
}