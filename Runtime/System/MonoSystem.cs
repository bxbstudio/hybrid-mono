using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Base class for custom systems that run on MonoBehaviours, providing life-cycle hooks and job execution.
    /// </summary>
    public abstract class MonoSystem : PersistentSingleton<MonoSystem>
    {
        /// <summary>
        /// Profiler marker for the Update loop.
        /// </summary>
        private static readonly ProfilerMarker UpdateMarker =
                  new ProfilerMarker($"{nameof(MonoSystem)}.Update");

        /// <summary>
        /// Profiler marker for the FixedUpdate loop.
        /// </summary>
        private static readonly ProfilerMarker FixedUpdateMarker =
            new ProfilerMarker($"{nameof(MonoSystem)}.FixedUpdate");

        /// <summary>
        /// Profiler marker for the LateUpdate loop.
        /// </summary>
           private static readonly ProfilerMarker LateUpdateMarker =
            new ProfilerMarker($"{nameof(MonoSystem)}.LateUpdate");   

        /// <summary>
        /// Standard Unity Update message. Calls <see cref="OnUpdate"/>.
        /// </summary>
        protected virtual void Update()
        {
            using (UpdateMarker.Auto())
            {
                OnUpdate();
            }
        }

        /// <summary>
        /// Standard Unity FixedUpdate message. Calls <see cref="OnFixedUpdate"/>.
        /// </summary>
        protected virtual void FixedUpdate()
        {
            using (FixedUpdateMarker.Auto())
            {
                OnFixedUpdate();
            }
        }

        /// <summary>
        /// Standard Unity LateUpdate message. Calls <see cref="OnLateUpdate"/>.
        /// </summary>
        protected virtual void LateUpdate()
        {
            using (LateUpdateMarker.Auto())
            {
                OnLateUpdate();
            }
        }

        /// <summary>
        /// Standard Unity OnDestroy message. Calls <see cref="OnCleanup"/>.
        /// </summary>
        protected virtual void OnDestroy()
        {
            OnCleanup();
        }



        /// <summary>
        /// Overridable method for custom update logic.
        /// </summary>
        protected virtual void OnUpdate() { }
        /// <summary>
        /// Overridable method for custom fixed update logic.
        /// </summary>
        protected virtual void OnFixedUpdate() { }

        /// <summary>
        /// Overridable method for custom late update logic.
        /// </summary>
        protected virtual void OnLateUpdate() { }
        /// <summary>
        /// Overridable method for custom cleanup logic when the system is destroyed.
        /// </summary>
        protected virtual void OnCleanup() { }

        /// <summary>
        /// Finds all MonoBehaviours of type T in the scene.
        /// </summary>
        /// <typeparam name="T">The type of MonoBehaviour to find.</typeparam>
        /// <returns>An array of found MonoBehaviours.</returns>
         protected virtual T[] FindMonoComponents<T>() where T : MonoBehaviour
        {
            return FindObjectsOfType<T>();
        }
        /// <summary>
        /// Runs a MonoJob for a specified number of elements.
        /// </summary>
        /// <typeparam name="TJob">The type of the job.</typeparam>
        /// <param name="job">The job instance.</param>
        /// <param name="length">The number of times to execute the job.</param>
      protected void RunMonoJob<TJob>(TJob job, int length) where TJob : struct, IMonoJob
        {
            job.Run(length);
        }

    }
}
