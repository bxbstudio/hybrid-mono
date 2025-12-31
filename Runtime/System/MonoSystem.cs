using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Base class for custom systems that process Entities in the HybridMono World.
    /// Provides life-cycle hooks and job execution support.
    /// </summary>
    public abstract class MonoSystem : PersistentSingleton<MonoSystem>
    {
        #region Profiler Markers
        private static readonly ProfilerMarker UpdateMarker =
            new($"{nameof(MonoSystem)}.Update");

        private static readonly ProfilerMarker FixedUpdateMarker =
            new($"{nameof(MonoSystem)}.FixedUpdate");

        private static readonly ProfilerMarker LateUpdateMarker =
            new($"{nameof(MonoSystem)}.LateUpdate");
        #endregion

        #region Properties
        /// <summary>
        /// Gets the HybridMono World.
        /// </summary>
        protected World World => MonoHybridAPI.World;

        /// <summary>
        /// Gets the EntityManager for the HybridMono World.
        /// </summary>
        protected EntityManager EntityManager => MonoHybridAPI.EntityManager;

        /// <summary>
        /// Dependency handle for job scheduling.
        /// Systems should update this when scheduling jobs.
        /// </summary>
        protected JobHandle Dependency { get; set; }
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            OnCreate();
        }

        protected virtual void Update()
        {
            using (UpdateMarker.Auto())
            {
                OnUpdate();
            }
        }

        protected virtual void FixedUpdate()
        {
            using (FixedUpdateMarker.Auto())
            {
                OnFixedUpdate();
            }
        }

        protected virtual void LateUpdate()
        {
            using (LateUpdateMarker.Auto())
            {
                OnLateUpdate();
            }
        }

        protected virtual void OnDestroy()
        {
            OnCleanup();
        }
        #endregion

        #region System Lifecycle (Override These)
        /// <summary>
        /// Called when the system is created (during Awake).
        /// Use for initialization, creating queries, etc.
        /// </summary>
        protected virtual void OnCreate() { }

        /// <summary>
        /// Called every frame during Update.
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// Called every fixed timestep during FixedUpdate.
        /// </summary>
        protected virtual void OnFixedUpdate() { }

        /// <summary>
        /// Called every frame after Update during LateUpdate.
        /// </summary>
        protected virtual void OnLateUpdate() { }

        /// <summary>
        /// Called when the system is destroyed.
        /// </summary>
        protected virtual void OnCleanup() { }
        #endregion
    }
}
