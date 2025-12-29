using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

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

    /// <summary>
    /// Extension methods for running IMonoJob implementations.
    /// </summary>
    public static class MonoJobExtensions
    {
        /// <summary>
        /// Runs the job sequentially for a specified number of elements.
        /// </summary>
        /// <typeparam name="T">The type of the job data.</typeparam>
        /// <param name="jobData">The job data instance.</param>
        /// <param name="length">The number of times to execute the job.</param>
        public static void Run<T>(this T jobData, int length)  where T : struct, IMonoJob
        {
            // One marker per job type (cached by generic static)
            ProfilerMarker marker = MonoJobProfiler<T>.Marker;

            using (marker.Auto())
            {
                for (int i = 0; i < length; i++)
                {
                    jobData.Execute(i);
                }
            }
        }
    }

    /// <summary>
    /// Internal helper class for profiling MonoJobs.
    /// </summary>
    /// <typeparam name="T">The type of the job data.</typeparam>
    internal static class MonoJobProfiler<T>  where T : struct, IMonoJob 
    {
        /// <summary>
        /// Profiler marker for the specific job type.
        /// </summary>
        public static readonly ProfilerMarker Marker =  new ProfilerMarker($"MonoJob<{typeof(T).Name}>.Run");   
    }
  }
