using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Profiling;
using Utilities.Core;
using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Base class for HybridMono runtime systems.
    /// Use <see cref="MonoSystemBootstrap.GetSystem{T}"/> for external lookup.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public abstract class MonoSystem<T> : PersistentSingleton<T> where T : MonoSystem<T>
    {
        /// <summary>
        /// Profiler marker for per-frame update execution.
        /// </summary>
        private static readonly ProfilerMarker UpdateMarker = new($"{typeof(T).Name}.Update");

        /// <summary>
        /// Profiler marker for fixed-step update execution.
        /// </summary>
        private static readonly ProfilerMarker FixedUpdateMarker = new($"{typeof(T).Name}.FixedUpdate");

        /// <summary>
        /// Profiler marker for late update execution.
        /// </summary>
        private static readonly ProfilerMarker LateUpdateMarker = new($"{typeof(T).Name}.LateUpdate");

        /// <summary>
        /// Gets the HybridMono world instance.
        /// </summary>
        protected World World => MonoHybridAPI.World;

        /// <summary>
        /// Gets the HybridMono entity manager.
        /// </summary>
        protected EntityManager EntityManager => MonoHybridAPI.EntityManager;

        /// <summary>
        /// Gets or sets the pending dependency for this system.
        /// </summary>
        protected JobHandle Dependency { get; set; }

        /// <summary>
        /// Gets a value indicating whether the initial HybridMono bake has completed.
        /// </summary>
        protected bool IsInitialBakeComplete => MonoBakingSystem.InitialBakeCompleted;

        /// <summary>
        /// Registers the system instance and invokes the creation hook.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (ToBeDestroyed)
                return;

            MonoSystemBootstrap.RegisterSystem(this);
            Dependency = default;

            OnCreate();
        }

        /// <summary>
        /// Ensures the initial bake has completed before allowing frame callbacks to run.
        /// </summary>
        private void Start()
        {
            if (ToBeDestroyed)
                return;

            OnStartRunning();
        }

        /// <summary>
        /// Runs the per-frame update hook once the initial bake gate has opened.
        /// </summary>
        private void Update()
        {
            if (!CanRunFrameCallbacks())
                return;

            using (UpdateMarker.Auto())
            {
                OnUpdate();
            }
        }

        /// <summary>
        /// Runs the fixed-step update hook once the initial bake gate has opened.
        /// </summary>
        private void FixedUpdate()
        {
            if (!CanRunFrameCallbacks())
                return;

            using (FixedUpdateMarker.Auto())
            {
                OnFixedUpdate();
            }
        }

        /// <summary>
        /// Runs the late update hook once the initial bake gate has opened.
        /// </summary>
        private void LateUpdate()
        {
            if (!CanRunFrameCallbacks())
                return;

            using (LateUpdateMarker.Auto())
            {
                OnLateUpdate();
            }
        }

        /// <summary>
        /// Completes outstanding dependencies, unregisters the system, and invokes the cleanup hook.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (ToBeDestroyed)
                return;

            CompleteDependency();
            MonoSystemBootstrap.UnregisterSystem(this);
        }

        /// <summary>
        /// Called once after the persistent system instance is created.
        /// </summary>
        protected virtual void OnCreate() { }

        /// <summary>
        /// Returns true when frame callbacks are allowed to run, which requires the initial bake to have completed.
        /// </summary>
        protected virtual void OnStartRunning() { }

        /// <summary>
        /// Called every frame after the initial bake has completed.
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// Called every fixed-step frame after the initial bake has completed.
        /// </summary>
        protected virtual void OnFixedUpdate() { }

        /// <summary>
        /// Called every late frame after the initial bake has completed.
        /// </summary>
        protected virtual void OnLateUpdate() { }

        /// <summary>
        /// Completes the currently tracked dependency handle.
        /// </summary>
        protected void CompleteDependency()
        {
            Dependency.Complete();
        }

        /// <summary>
        /// Completes the currently tracked dependency combined with any additional handles.
        /// </summary>
        /// <param name="additionalDependencies">Additional job handles that must finish before continuing.</param>
        protected void CompleteDependencies(params JobHandle[] additionalDependencies)
        {
            if (additionalDependencies == null || additionalDependencies.Length == 0)
            {
                CompleteDependency();
                return;
            }

            JobHandle combinedDependency = Dependency;

            for (int i = 0; i < additionalDependencies.Length; i++)
                combinedDependency = JobHandle.CombineDependencies(combinedDependency, additionalDependencies[i]);

            combinedDependency.Complete();
            Dependency = default;
        }

        /// <summary>
        /// Completes outstanding work and runs a main-thread apply job.
        /// </summary>
        /// <typeparam name="TJob">The apply job type.</typeparam>
        /// <param name="jobData">The apply job instance to execute.</param>
        /// <param name="length">The number of indices to process.</param>
        protected void RunApplyJob<TJob>(TJob jobData, int length) where TJob : struct, IMonoJob
        {
            CompleteDependency();
            jobData.Run(length);
        }

        /// <summary>
        /// Completes outstanding work and runs a batched main-thread apply job.
        /// </summary>
        /// <typeparam name="TJob">The apply job type.</typeparam>
        /// <param name="jobData">The apply job instance to execute.</param>
        /// <param name="length">The number of indices to process.</param>
        /// <param name="batchSize">The number of indices per batch.</param>
        protected void RunApplyJobBatched<TJob>(TJob jobData, int length, int batchSize) where TJob : struct, IMonoJob
        {
            CompleteDependency();
            jobData.RunBatched(length, batchSize);
        }

        /// <summary>
        /// Creates an entity query in the HybridMono world.
        /// </summary>
        /// <param name="componentTypes">The component filters used by the query.</param>
        /// <returns>A new entity query.</returns>
        protected EntityQuery CreateQuery(params ComponentType[] componentTypes) => MonoHybridAPI.CreateQuery(componentTypes);

        /// <summary>
        /// Creates an entity query in the HybridMono world from a query description.
        /// </summary>
        /// <param name="queryDescription">The query description to materialize.</param>
        /// <returns>A new entity query.</returns>
        protected EntityQuery CreateQuery(EntityQueryDesc queryDescription) => MonoHybridAPI.CreateQuery(queryDescription);

        /// <summary>
        /// Gets the number of entities currently matched by a query.
        /// </summary>
        /// <param name="query">The query to evaluate.</param>
        /// <returns>The current entity count.</returns>
        protected int GetEntityCount(EntityQuery query) => MonoHybridAPI.GetEntityCount(query);

        /// <summary>
        /// Returns true when a query currently matches no entities.
        /// </summary>
        /// <param name="query">The query to evaluate.</param>
        /// <param name="ignoreFilter">Whether to ignore query filters during the emptiness check.</param>
        /// <returns>True when the query is empty.</returns>
        protected bool IsQueryEmpty(EntityQuery query, bool ignoreFilter = false) => MonoHybridAPI.IsQueryEmpty(query, ignoreFilter);

        /// <summary>
        /// Gets the entities currently matched by a query.
        /// </summary>
        /// <param name="query">The query to evaluate.</param>
        /// <param name="allocator">The allocator used for the result array.</param>
        /// <returns>A native array containing the matched entities.</returns>
        protected NativeArray<Entity> GetEntities(EntityQuery query, Allocator allocator = Allocator.TempJob) => MonoHybridAPI.GetEntities(query, allocator);

        /// <summary>
        /// Tries to resolve the HybridMono entity for a GameObject.
        /// </summary>
        protected bool TryGetEntity(GameObject gameObject, out Entity entity) => MonoHybridAPI.TryGetEntity(gameObject, out entity);

        /// <summary>
        /// Resolves the HybridMono entity for a GameObject.
        /// </summary>
        protected Entity GetEntity(GameObject gameObject) => MonoHybridAPI.GetEntity(gameObject);

        /// <summary>
        /// Tries to resolve the source GameObject for a HybridMono entity.
        /// </summary>
        protected bool TryGetGameObject(Entity entity, out GameObject gameObject) => MonoHybridAPI.TryGetGameObject(entity, out gameObject);

        /// <summary>
        /// Resolves the source GameObject for a HybridMono entity.
        /// </summary>
        protected GameObject GetGameObject(Entity entity) => MonoHybridAPI.GetGameObject(entity);

        /// <summary>
        /// Returns true when a GameObject is registered with HybridMono.
        /// </summary>
        protected bool IsRegistered(GameObject gameObject) => MonoHybridAPI.IsRegistered(gameObject);

        /// <summary>
        /// Returns true when an entity is registered with a GameObject in HybridMono.
        /// </summary>
        protected bool IsRegistered(Entity entity) => MonoHybridAPI.IsRegistered(entity);

        /// <summary>
        /// Gets a snapshot of all registered HybridMono entities.
        /// </summary>
        protected IReadOnlyCollection<Entity> GetRegisteredEntities() => MonoHybridAPI.GetRegisteredEntities();

        /// <summary>
        /// Gets a snapshot of all registered HybridMono GameObjects.
        /// </summary>
        protected IEnumerable<GameObject> GetRegisteredGameObjects() => MonoHybridAPI.GetRegisteredGameObjects();

        /// <summary>
        /// Returns true when the mirrored entity for a GameObject has the requested component.
        /// </summary>
        protected bool HasComponent<TData>(GameObject gameObject) where TData : unmanaged, IComponentData => MonoHybridAPI.HasComponent<TData>(gameObject);

        /// <summary>
        /// Returns true when an entity has the requested component.
        /// </summary>
        protected bool HasComponent<TData>(Entity entity) where TData : unmanaged, IComponentData => MonoHybridAPI.HasComponent<TData>(entity);

        /// <summary>
        /// Gets component data from the mirrored entity for a GameObject.
        /// </summary>
        protected TData GetComponentData<TData>(GameObject gameObject) where TData : unmanaged, IComponentData => MonoHybridAPI.GetComponentData<TData>(gameObject);

        /// <summary>
        /// Gets component data from an entity.
        /// </summary>
        protected TData GetComponentData<TData>(Entity entity) where TData : unmanaged, IComponentData => MonoHybridAPI.GetComponentData<TData>(entity);

        /// <summary>
        /// Tries to get component data from the mirrored entity for a GameObject.
        /// </summary>
        protected bool TryGetComponentData<TData>(GameObject gameObject, out TData data) where TData : unmanaged, IComponentData =>
            MonoHybridAPI.TryGetComponentData(gameObject, out data);

        /// <summary>
        /// Tries to get component data from an entity.
        /// </summary>
        protected bool TryGetComponentData<TData>(Entity entity, out TData data) where TData : unmanaged, IComponentData =>
            MonoHybridAPI.TryGetComponentData(entity, out data);

        /// <summary>
        /// Sets component data on the mirrored entity for a GameObject.
        /// </summary>
        protected void SetComponentData<TData>(GameObject gameObject, TData data) where TData : unmanaged, IComponentData =>
            MonoHybridAPI.SetComponentData(gameObject, data);

        /// <summary>
        /// Sets component data on an entity.
        /// </summary>
        protected void SetComponentData<TData>(Entity entity, TData data) where TData : unmanaged, IComponentData =>
            MonoHybridAPI.SetComponentData(entity, data);

        /// <summary>
        /// Adds or updates component data on the mirrored entity for a GameObject.
        /// </summary>
        protected void AddComponentData<TData>(GameObject gameObject, TData data) where TData : unmanaged, IComponentData =>
            MonoHybridAPI.AddComponentData(gameObject, data);

        /// <summary>
        /// Adds or updates component data on an entity.
        /// </summary>
        protected void AddComponentData<TData>(Entity entity, TData data) where TData : unmanaged, IComponentData =>
            MonoHybridAPI.AddComponentData(entity, data);

        /// <summary>
        /// Removes a component from the mirrored entity for a GameObject.
        /// </summary>
        protected void RemoveComponent<TData>(GameObject gameObject) where TData : unmanaged, IComponentData =>
            MonoHybridAPI.RemoveComponent<TData>(gameObject);

        /// <summary>
        /// Removes a component from an entity.
        /// </summary>
        protected void RemoveComponent<TData>(Entity entity) where TData : unmanaged, IComponentData =>
            MonoHybridAPI.RemoveComponent<TData>(entity);

        /// <summary>
        /// Returns true when the mirrored entity for a GameObject has the requested buffer.
        /// </summary>
        protected bool HasBuffer<TBuffer>(GameObject gameObject) where TBuffer : unmanaged, IBufferElementData => MonoHybridAPI.HasBuffer<TBuffer>(gameObject);

        /// <summary>
        /// Returns true when an entity has the requested buffer.
        /// </summary>
        protected bool HasBuffer<TBuffer>(Entity entity) where TBuffer : unmanaged, IBufferElementData => MonoHybridAPI.HasBuffer<TBuffer>(entity);

        /// <summary>
        /// Gets a buffer from the mirrored entity for a GameObject.
        /// </summary>
        protected DynamicBuffer<TBuffer> GetBuffer<TBuffer>(GameObject gameObject) where TBuffer : unmanaged, IBufferElementData =>
            MonoHybridAPI.GetBuffer<TBuffer>(gameObject);

        /// <summary>
        /// Gets a buffer from an entity.
        /// </summary>
        protected DynamicBuffer<TBuffer> GetBuffer<TBuffer>(Entity entity) where TBuffer : unmanaged, IBufferElementData =>
            MonoHybridAPI.GetBuffer<TBuffer>(entity);

        /// <summary>
        /// Tries to get a buffer from the mirrored entity for a GameObject.
        /// </summary>
        protected bool TryGetBuffer<TBuffer>(GameObject gameObject, out DynamicBuffer<TBuffer> buffer) where TBuffer : unmanaged, IBufferElementData =>
            MonoHybridAPI.TryGetBuffer(gameObject, out buffer);

        /// <summary>
        /// Tries to get a buffer from an entity.
        /// </summary>
        protected bool TryGetBuffer<TBuffer>(Entity entity, out DynamicBuffer<TBuffer> buffer) where TBuffer : unmanaged, IBufferElementData =>
            MonoHybridAPI.TryGetBuffer(entity, out buffer);

        /// <summary>
        /// Ensures a buffer exists on the mirrored entity for a GameObject.
        /// </summary>
        protected DynamicBuffer<TBuffer> EnsureBuffer<TBuffer>(GameObject gameObject, int capacity = 0) where TBuffer : unmanaged, IBufferElementData =>
            MonoHybridAPI.EnsureBuffer<TBuffer>(gameObject, capacity);

        /// <summary>
        /// Ensures a buffer exists on an entity.
        /// </summary>
        protected DynamicBuffer<TBuffer> EnsureBuffer<TBuffer>(Entity entity, int capacity = 0) where TBuffer : unmanaged, IBufferElementData =>
            MonoHybridAPI.EnsureBuffer<TBuffer>(entity, capacity);

        /// <summary>
        /// Removes a buffer from the mirrored entity for a GameObject.
        /// </summary>
        protected void RemoveBuffer<TBuffer>(GameObject gameObject) where TBuffer : unmanaged, IBufferElementData =>
            MonoHybridAPI.RemoveBuffer<TBuffer>(gameObject);

        /// <summary>
        /// Removes a buffer from an entity.
        /// </summary>
        protected void RemoveBuffer<TBuffer>(Entity entity) where TBuffer : unmanaged, IBufferElementData =>
            MonoHybridAPI.RemoveBuffer<TBuffer>(entity);

        /// <summary>
        /// Gets the enabled state of an enableable component on the mirrored entity for a GameObject.
        /// </summary>
        protected bool IsComponentEnabled<TData>(GameObject gameObject) where TData : unmanaged, IComponentData, IEnableableComponent =>
            MonoHybridAPI.IsComponentEnabled<TData>(gameObject);

        /// <summary>
        /// Gets the enabled state of an enableable component on an entity.
        /// </summary>
        protected bool IsComponentEnabled<TData>(Entity entity) where TData : unmanaged, IComponentData, IEnableableComponent =>
            MonoHybridAPI.IsComponentEnabled<TData>(entity);

        /// <summary>
        /// Sets the enabled state of an enableable component on the mirrored entity for a GameObject.
        /// </summary>
        protected void SetComponentEnabled<TData>(GameObject gameObject, bool enabled) where TData : unmanaged, IComponentData, IEnableableComponent =>
            MonoHybridAPI.SetComponentEnabled<TData>(gameObject, enabled);

        /// <summary>
        /// Sets the enabled state of an enableable component on an entity.
        /// </summary>
        protected void SetComponentEnabled<TData>(Entity entity, bool enabled) where TData : unmanaged, IComponentData, IEnableableComponent =>
            MonoHybridAPI.SetComponentEnabled<TData>(entity, enabled);

        /// <summary>
        /// Exports component data for a query into a native array.
        /// </summary>
        protected NativeArray<TData> GetComponentDataArray<TData>(EntityQuery query, Allocator allocator = Allocator.TempJob) where TData : unmanaged, IComponentData =>
            MonoHybridAPI.GetComponentDataArray<TData>(query, allocator);

        /// <summary>
        /// Exports component data for a GameObject collection into a native array.
        /// </summary>
        protected NativeArray<TData> GetComponentDataArray<TData>(IEnumerable<GameObject> gameObjects, Allocator allocator = Allocator.TempJob) where TData : unmanaged, IComponentData =>
            MonoHybridAPI.GetComponentDataArray<TData>(gameObjects, allocator);

        /// <summary>
        /// Exports component data for an entity collection into a native array.
        /// </summary>
        protected NativeArray<TData> GetComponentDataArray<TData>(IEnumerable<Entity> entities, Allocator allocator = Allocator.TempJob) where TData : unmanaged, IComponentData =>
            MonoHybridAPI.GetComponentDataArray<TData>(entities, allocator);

        /// <summary>
        /// Imports component data into all entities matched by a query.
        /// </summary>
        protected void SetComponentDataArray<TData>(EntityQuery query, NativeArray<TData> data) where TData : unmanaged, IComponentData =>
            MonoHybridAPI.SetComponentDataArray(query, data);

        /// <summary>
        /// Imports component data into the mirrored entities for a GameObject collection.
        /// </summary>
        protected void SetComponentDataArray<TData>(IEnumerable<GameObject> gameObjects, NativeArray<TData> data) where TData : unmanaged, IComponentData =>
            MonoHybridAPI.SetComponentDataArray(gameObjects, data);

        /// <summary>
        /// Imports component data into an entity collection.
        /// </summary>
        protected void SetComponentDataArray<TData>(IEnumerable<Entity> entities, NativeArray<TData> data) where TData : unmanaged, IComponentData =>
            MonoHybridAPI.SetComponentDataArray(entities, data);

        /// <summary>
        /// Exports buffers for a query into a managed array.
        /// </summary>
        protected DynamicBuffer<TBuffer>[] GetBufferArray<TBuffer>(EntityQuery query) where TBuffer : unmanaged, IBufferElementData =>
            MonoHybridAPI.GetBufferArray<TBuffer>(query);

        /// <summary>
        /// Exports buffers for a GameObject collection into a managed array.
        /// </summary>
        protected DynamicBuffer<TBuffer>[] GetBufferArray<TBuffer>(IEnumerable<GameObject> gameObjects) where TBuffer : unmanaged, IBufferElementData =>
            MonoHybridAPI.GetBufferArray<TBuffer>(gameObjects);

        /// <summary>
        /// Exports buffers for an entity collection into a managed array.
        /// </summary>
        protected DynamicBuffer<TBuffer>[] GetBufferArray<TBuffer>(IEnumerable<Entity> entities) where TBuffer : unmanaged, IBufferElementData =>
            MonoHybridAPI.GetBufferArray<TBuffer>(entities);

        /// <summary>
        /// Returns true when runtime frame callbacks are allowed to execute.
        /// </summary>
        /// <returns>True once the initial HybridMono bake has completed.</returns>
        private static bool CanRunFrameCallbacks()
        {
            return MonoBakingSystem.InitialBakeCompleted;
        }
    }
}
