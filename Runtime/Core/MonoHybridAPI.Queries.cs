using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Query and entity enumeration helpers for the HybridMono world.
    /// </summary>
    public static partial class MonoHybridAPI
    {
        /// <summary>
        /// Creates an entity query for the HybridMono world.
        /// </summary>
        /// <param name="componentTypes">The component filters used by the query.</param>
        /// <returns>A new entity query for the HybridMono world.</returns>
        public static EntityQuery CreateQuery(params ComponentType[] componentTypes)
        {
            EnsureInitialized();
            return _entityManager.CreateEntityQuery(componentTypes);
        }

        /// <summary>
        /// Creates an entity query from a query description.
        /// </summary>
        /// <param name="queryDescription">The query description to materialize.</param>
        /// <returns>A new entity query for the HybridMono world.</returns>
        public static EntityQuery CreateQuery(EntityQueryDesc queryDescription)
        {
            EnsureInitialized();
            return _entityManager.CreateEntityQuery(queryDescription);
        }

        /// <summary>
        /// Gets the number of entities currently matched by a query.
        /// </summary>
        /// <param name="query">The query to evaluate.</param>
        /// <returns>The current entity count for the query.</returns>
        public static int GetEntityCount(EntityQuery query)
        {
            EnsureInitialized();
            return query.CalculateEntityCount();
        }

        /// <summary>
        /// Returns true when the query matches no entities.
        /// </summary>
        /// <param name="query">The query to evaluate.</param>
        /// <param name="ignoreFilter">Whether to ignore query filters during the emptiness check.</param>
        /// <returns>True when the query is currently empty.</returns>
        public static bool IsQueryEmpty(EntityQuery query, bool ignoreFilter = false)
        {
            EnsureInitialized();
            return ignoreFilter ? query.IsEmptyIgnoreFilter : query.IsEmpty;
        }

        /// <summary>
        /// Gets the entities matched by a query.
        /// </summary>
        /// <param name="query">The query to evaluate.</param>
        /// <param name="allocator">The allocator used for the result array.</param>
        /// <returns>A native array containing the matched entities.</returns>
        public static NativeArray<Entity> GetEntities(EntityQuery query, Allocator allocator = Allocator.TempJob)
        {
            EnsureInitialized();
            return query.ToEntityArray(allocator);
        }

        /// <summary>
        /// Gets registered entities for a collection of GameObjects.
        /// </summary>
        /// <param name="gameObjects">The GameObjects to resolve.</param>
        /// <param name="allocator">The allocator used for the result array.</param>
        /// <returns>A native array containing all successfully resolved entities.</returns>
        public static NativeArray<Entity> GetEntities(IEnumerable<GameObject> gameObjects, Allocator allocator = Allocator.TempJob)
        {
            EnsureInitialized();

            var entities = new List<Entity>();

            foreach (GameObject gameObject in gameObjects)
            {
                if (TryGetEntity(gameObject, out Entity entity))
                    entities.Add(entity);
            }

            return ToNativeArray(entities, allocator);
        }

        /// <summary>
        /// Gets all registered entities.
        /// </summary>
        /// <param name="allocator">The allocator used for the result array.</param>
        /// <returns>A native array containing all currently registered entities.</returns>
        public static NativeArray<Entity> GetAllEntities(Allocator allocator = Allocator.TempJob)
        {
            EnsureInitialized();
            return ToNativeArray(EntityToGameObject.Keys, allocator);
        }
    }
}
