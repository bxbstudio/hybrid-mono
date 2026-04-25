using System;
using Unity.Profiling;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Extension methods for profiled main-thread execution of <see cref="IMonoJob"/>.
    /// </summary>
    public static class MonoJobExtensions
    {
        /// <summary>
        /// Runs the job sequentially for a specified number of elements.
        /// </summary>
        public static void Run<T>(this T jobData, int length) where T : struct, IMonoJob
        {
            RunRange(jobData, 0, length);
        }

        /// <summary>
        /// Runs the job for a contiguous range of indices.
        /// </summary>
        public static void RunRange<T>(this T jobData, int startIndex, int length) where T : struct, IMonoJob
        {
            if (length <= 0)
                return;

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            ProfilerMarker marker = MonoJobProfiler<T>.Marker;
            int endIndex = checked(startIndex + length);

            using (marker.Auto())
            {
                for (int i = startIndex; i < endIndex; i++)
                    jobData.Execute(i);
            }
        }

        /// <summary>
        /// Runs the job in batches while still executing on the main thread.
        /// </summary>
        public static void RunBatched<T>(this T jobData, int length, int batchSize) where T : struct, IMonoJob
        {
            if (length <= 0)
                return;

            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize));

            for (int batchStart = 0; batchStart < length; batchStart += batchSize)
            {
                int batchLength = Math.Min(batchSize, length - batchStart);
                RunRange(jobData, batchStart, batchLength);
            }
        }
    }
}
