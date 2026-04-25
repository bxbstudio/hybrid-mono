using Unity.Profiling;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Internal helper class for profiling MonoJobs.
    /// </summary>
    /// <typeparam name="T">The type of the job data.</typeparam>
    internal static class MonoJobProfiler<T> where T : struct, IMonoJob
    {
        /// <summary>
        /// Shared profiler marker used by the MonoJob execution helpers for this job type.
        /// </summary>
        public static readonly ProfilerMarker Marker = new ProfilerMarker($"MonoJob<{typeof(T).Name}>.Run");
    }
}
