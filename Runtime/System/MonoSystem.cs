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
            new ProfilerMarker($"{nameof(MonoSystem)}.Update");

        private static readonly ProfilerMarker FixedUpdateMarker =
            new ProfilerMarker($"{nameof(MonoSystem)}.FixedUpdate");

        private static readonly ProfilerMarker LateUpdateMarker =
            new ProfilerMarker($"{nameof(MonoSystem)}.LateUpdate");
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

        #region GameObject Component Access
        /// <summary>
        /// Gets component data via GameObject reference.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="gameObject">The GameObject.</param>
        /// <returns>The component data.</returns>
        protected T GetComponent<T>(GameObject gameObject) where T : unmanaged, IComponentData
        {
            return MonoHybridAPI.GetComponentData<T>(gameObject);
        }

        /// <summary>
        /// Sets component data via GameObject reference.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="gameObject">The GameObject.</param>
        /// <param name="data">The component data.</param>
        protected void SetComponent<T>(GameObject gameObject, T data) where T : unmanaged, IComponentData
        {
            MonoHybridAPI.SetComponentData(gameObject, data);
        }

        /// <summary>
        /// Checks if a GameObject's Entity has a component.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="gameObject">The GameObject.</param>
        /// <returns>True if the component exists.</returns>
        protected bool HasComponent<T>(GameObject gameObject) where T : unmanaged, IComponentData
        {
            return MonoHybridAPI.HasComponent<T>(gameObject);
        }

        /// <summary>
        /// Gets a buffer via GameObject reference.
        /// </summary>
        /// <typeparam name="T">The buffer element type.</typeparam>
        /// <param name="gameObject">The GameObject.</param>
        /// <returns>The DynamicBuffer.</returns>
        protected DynamicBuffer<T> GetBuffer<T>(GameObject gameObject) where T : unmanaged, IBufferElementData
        {
            return MonoHybridAPI.GetBuffer<T>(gameObject);
        }

        /// <summary>
        /// Checks if a GameObject's Entity has a buffer.
        /// </summary>
        /// <typeparam name="T">The buffer element type.</typeparam>
        /// <param name="gameObject">The GameObject.</param>
        /// <returns>True if the buffer exists.</returns>
        protected bool HasBuffer<T>(GameObject gameObject) where T : unmanaged, IBufferElementData
        {
            return MonoHybridAPI.HasBuffer<T>(gameObject);
        }
        #endregion

        #region Deprecated API
        /// <summary>
        /// Finds all MonoBehaviours of type T in the scene.
        /// </summary>
        /// <typeparam name="T">The type of MonoBehaviour to find.</typeparam>
        /// <returns>An array of found MonoBehaviours.</returns>
        [Obsolete("Use EntityQuery and component queries instead. This method will be removed in a future version.")]
        protected virtual T[] FindMonoComponents<T>() where T : MonoBehaviour
        {
            return FindObjectsOfType<T>();
        }
        #endregion
    }
}
