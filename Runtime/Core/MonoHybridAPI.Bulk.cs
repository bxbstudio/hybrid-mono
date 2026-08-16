using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Bulk component and buffer helpers for HybridMono queries and mappings.
    /// </summary>
    public static partial class MonoHybridAPI
    {
        /// <summary>
        /// Gets component data for all entities in a query.
        /// </summary>
        /// <typeparam name="T">The component type to export.</typeparam>
        /// <param name="query">The query whose entities are exported.</param>
        /// <param name="allocator">The allocator used for the result array.</param>
        /// <returns>A native array containing the matched component data.</returns>
        public static NativeArray<T> GetComponentDataArray<T>(EntityQuery query, Allocator allocator = Allocator.TempJob) where T : unmanaged, IComponentData
        {
            EnsureInitialized();
            return query.ToComponentDataArray<T>(allocator);
        }

        /// <summary>
        /// Gets component data for a collection of GameObjects.
        /// </summary>
        /// <typeparam name="T">The component type to export.</typeparam>
        /// <param name="gameObjects">The GameObjects whose mirrored entities are exported.</param>
        /// <param name="allocator">The allocator used for the result array.</param>
        /// <returns>A native array containing all successfully resolved component data.</returns>
        public static NativeArray<T> GetComponentDataArray<T>(IEnumerable<GameObject> gameObjects, Allocator allocator = Allocator.TempJob) where T : unmanaged, IComponentData
        {
            EnsureInitialized();

            var values = new List<T>();

            foreach (GameObject gameObject in gameObjects)
            {
                if (TryGetComponentData(gameObject, out T data))
                    values.Add(data);
            }

            return ToNativeArray(values, allocator);
        }

        /// <summary>
        /// Gets component data for a collection of entities.
        /// </summary>
        /// <typeparam name="T">The component type to export.</typeparam>
        /// <param name="entities">The entities to export.</param>
        /// <param name="allocator">The allocator used for the result array.</param>
        /// <returns>A native array containing all successfully resolved component data.</returns>
        public static NativeArray<T> GetComponentDataArray<T>(IEnumerable<Entity> entities, Allocator allocator = Allocator.TempJob) where T : unmanaged, IComponentData
        {
            EnsureInitialized();

            var values = new List<T>();

            foreach (Entity entity in entities)
            {
                if (TryGetComponentData(entity, out T data))
                    values.Add(data);
            }

            return ToNativeArray(values, allocator);
        }

        /// <summary>
        /// Gets component data for all registered entities that have the component.
        /// </summary>
        /// <typeparam name="T">The component type to export.</typeparam>
        /// <param name="allocator">The allocator used for the result array.</param>
        /// <returns>A native array containing all registered component data of the requested type.</returns>
        public static NativeArray<T> GetAllComponentData<T>(Allocator allocator = Allocator.TempJob) where T : unmanaged, IComponentData
        {
            return GetComponentDataArray<T>(CreateQuery(ComponentType.ReadOnly<T>()), allocator);
        }

        /// <summary>
        /// Copies component data into all entities matched by a query.
        /// </summary>
        /// <typeparam name="T">The component type to import.</typeparam>
        /// <param name="query">The query whose entities are updated.</param>
        /// <param name="data">The component data to apply.</param>
        public static void SetComponentDataArray<T>(EntityQuery query, NativeArray<T> data) where T : unmanaged, IComponentData
        {
            EnsureInitialized();

            if (query.CalculateEntityCount() != data.Length)
                throw new InvalidOperationException("The data length must match the query entity count.");

            query.CopyFromComponentDataArray(data);
        }

        /// <summary>
        /// Copies component data into a collection of GameObjects.
        /// </summary>
        /// <typeparam name="T">The component type to import.</typeparam>
        /// <param name="gameObjects">The GameObjects whose mirrored entities are updated.</param>
        /// <param name="data">The component data to apply.</param>
        public static void SetComponentDataArray<T>(IEnumerable<GameObject> gameObjects, NativeArray<T> data) where T : unmanaged, IComponentData
        {
            EnsureInitialized();

            int index = 0;

            foreach (GameObject gameObject in gameObjects)
            {
                if (index >= data.Length)
                    break;

                if (!TryGetEntity(gameObject, out Entity entity) || !_entityManager.HasComponent<T>(entity))
                    continue;

                _entityManager.SetComponentData(entity, data[index++]);
            }
        }

        /// <summary>
        /// Copies component data into a collection of entities.
        /// </summary>
        /// <typeparam name="T">The component type to import.</typeparam>
        /// <param name="entities">The entities to update.</param>
        /// <param name="data">The component data to apply.</param>
        public static void SetComponentDataArray<T>(IEnumerable<Entity> entities, NativeArray<T> data) where T : unmanaged, IComponentData
        {
            EnsureInitialized();

            int index = 0;

            foreach (Entity entity in entities)
            {
                if (index >= data.Length)
                    break;

                if (!Exists(entity) || !_entityManager.HasComponent<T>(entity))
                    continue;

                _entityManager.SetComponentData(entity, data[index++]);
            }
        }

        /// <summary>
        /// Copies component data into all registered entities that have the component.
        /// </summary>
        /// <typeparam name="T">The component type to import.</typeparam>
        /// <param name="data">The component data to apply.</param>
        public static void SetAllComponentData<T>(NativeArray<T> data) where T : unmanaged, IComponentData
        {
            SetComponentDataArray(CreateQuery(ComponentType.ReadWrite<T>()), data);
        }

        /// <summary>
        /// Gets buffers for all entities matched by a query.
        /// </summary>
        /// <typeparam name="T">The buffer element type to export.</typeparam>
        /// <param name="query">The query whose entity buffers are exported.</param>
        /// <returns>An array of dynamic buffers for the matched entities.</returns>
        public static DynamicBuffer<T>[] GetBufferArray<T>(EntityQuery query) where T : unmanaged, IBufferElementData
        {
            EnsureInitialized();

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            return GetBufferArray<T>(entities);
        }

        /// <summary>
        /// Gets buffers for a collection of GameObjects.
        /// </summary>
        /// <typeparam name="T">The buffer element type to export.</typeparam>
        /// <param name="gameObjects">The GameObjects whose mirrored entity buffers are exported.</param>
        /// <returns>An array of all successfully resolved buffers.</returns>
        public static DynamicBuffer<T>[] GetBufferArray<T>(IEnumerable<GameObject> gameObjects) where T : unmanaged, IBufferElementData
        {
            EnsureInitialized();

            var buffers = new List<DynamicBuffer<T>>();

            foreach (GameObject gameObject in gameObjects)
            {
                if (TryGetBuffer(gameObject, out DynamicBuffer<T> buffer))
                    buffers.Add(buffer);
            }

            return buffers.ToArray();
        }

        /// <summary>
        /// Gets buffers for a collection of entities.
        /// </summary>
        /// <typeparam name="T">The buffer element type to export.</typeparam>
        /// <param name="entities">The entities whose buffers are exported.</param>
        /// <returns>An array of all successfully resolved buffers.</returns>
        public static DynamicBuffer<T>[] GetBufferArray<T>(IEnumerable<Entity> entities) where T : unmanaged, IBufferElementData
        {
            EnsureInitialized();

            var buffers = new List<DynamicBuffer<T>>();

            foreach (Entity entity in entities)
            {
                if (TryGetBuffer(entity, out DynamicBuffer<T> buffer))
                    buffers.Add(buffer);
            }

            return buffers.ToArray();
        }

        /// <summary>
        /// Gets buffers for all registered entities that have the buffer.
        /// </summary>
        /// <typeparam name="T">The buffer element type to export.</typeparam>
        /// <returns>An array of all registered buffers of the requested type.</returns>
        public static DynamicBuffer<T>[] GetAllBuffers<T>() where T : unmanaged, IBufferElementData
        {
            return GetBufferArray<T>(CreateQuery(ComponentType.ReadWrite<T>()));
        }
    }
}
