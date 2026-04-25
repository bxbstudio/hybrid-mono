namespace Utilities.HybridMono
{
    /// <summary>
    /// Interface for jobs that can be executed on MonoBehaviours.
    /// </summary>
    public interface IMonoJob
    {
        /// <summary>
        /// Executes the job logic for a specific index.
        /// </summary>
        /// <param name="index">The index to process.</param>
        void Execute(int index);
    }
}
