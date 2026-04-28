using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Registration and lookup helpers for the HybridMono GameObject-Entity bridge.
    /// </summary>
    public static partial class MonoHybridAPI
    {
        /// <summary>
        /// Registers a GameObject and creates its mirrored entity if needed.
        /// </summary>
        /// <param name="gameObject">The GameObject to register.</param>
        /// <returns>The existing or newly created entity for the GameObject.</returns>
        public static Entity RegisterGameObject(GameObject gameObject)
        {
            EnsureInitialized();
            ValidateGameObject(gameObject);

            if (GameObjectToEntity.TryGetValue(gameObject, out Entity existingEntity))
            {
                EntityToGameObject[existingEntity] = gameObject;
                return existingEntity;
            }

            Entity entity = _entityManager.CreateEntity();
            GameObjectToEntity.Add(gameObject, entity);
            EntityToGameObject[entity] = gameObject;

            // The tracker keeps the runtime map consistent when the GameObject disappears.
            if (!gameObject.TryGetComponent(out MonoEntityTracker tracker))
            {
                tracker = gameObject.AddComponent<MonoEntityTracker>();
                tracker.hideFlags = HideFlags.HideInInspector;
            }

            return entity;
        }

        /// <summary>
        /// Unregisters a GameObject and destroys its mirrored entity if it still exists.
        /// </summary>
        /// <param name="gameObject">The GameObject to unregister.</param>
        internal static void UnregisterGameObject(GameObject gameObject)
        {
            if (!_isInitialized || gameObject == null)
                return;

            if (!GameObjectToEntity.TryGetValue(gameObject, out Entity entity))
                return;

            GameObjectToEntity.Remove(gameObject);
            EntityToGameObject.Remove(entity);

            if (_world != null && _world.IsCreated && _entityManager.Exists(entity))
                _entityManager.DestroyEntity(entity);
        }

        /// <summary>
        /// Returns true if the entity exists in the HybridMono world.
        /// </summary>
        /// <param name="entity">The entity to validate.</param>
        /// <returns>True when the entity exists in the HybridMono world.</returns>
        public static bool Exists(Entity entity)
        {
            EnsureInitialized();
            return entity != Entity.Null && _entityManager.Exists(entity);
        }

        /// <summary>
        /// Tries to get the entity associated with a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to resolve.</param>
        /// <param name="entity">The resolved entity when the mapping exists.</param>
        /// <returns>True when the GameObject is currently registered.</returns>
        public static bool TryGetEntity(GameObject gameObject, out Entity entity)
        {
            EnsureInitialized();

            if (gameObject == null)
            {
                entity = Entity.Null;
                return false;
            }

            return GameObjectToEntity.TryGetValue(gameObject, out entity) && Exists(entity);
        }

        /// <summary>
        /// Gets the entity associated with a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to resolve.</param>
        /// <returns>The registered entity for the GameObject.</returns>
        public static Entity GetEntity(GameObject gameObject)
        {
            ValidateGameObject(gameObject);

            if (!TryGetEntity(gameObject, out Entity entity))
                throw new InvalidOperationException($"GameObject '{gameObject.name}' is not registered with HybridMono.");

            return entity;
        }

        /// <summary>
        /// Tries to get the GameObject associated with an entity.
        /// </summary>
        /// <param name="entity">The entity to resolve.</param>
        /// <param name="gameObject">The resolved GameObject when the mapping exists.</param>
        /// <returns>True when the entity is currently mapped back to a GameObject.</returns>
        public static bool TryGetGameObject(Entity entity, out GameObject gameObject)
        {
            EnsureInitialized();

            if (!Exists(entity))
            {
                gameObject = null;
                return false;
            }

            if (!EntityToGameObject.TryGetValue(entity, out gameObject) || gameObject == null)
            {
                EntityToGameObject.Remove(entity);
                gameObject = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the GameObject associated with an entity.
        /// </summary>
        /// <param name="entity">The entity to resolve.</param>
        /// <returns>The mapped GameObject.</returns>
        public static GameObject GetGameObject(Entity entity)
        {
            if (!TryGetGameObject(entity, out GameObject gameObject))
                throw new InvalidOperationException($"Entity '{entity}' is not registered with a GameObject in HybridMono.");

            return gameObject;
        }

        /// <summary>
        /// Returns true if a GameObject is registered with HybridMono.
        /// </summary>
        /// <param name="gameObject">The GameObject to test.</param>
        /// <returns>True when the GameObject is currently registered.</returns>
        public static bool IsRegistered(GameObject gameObject)
        {
            return TryGetEntity(gameObject, out _);
        }

        /// <summary>
        /// Returns true if an entity is registered with a GameObject in HybridMono.
        /// </summary>
        /// <param name="entity">The entity to test.</param>
        /// <returns>True when the entity is currently mapped back to a GameObject.</returns>
        public static bool IsRegistered(Entity entity)
        {
            return TryGetGameObject(entity, out _);
        }

        /// <summary>
        /// Gets a snapshot of all registered GameObjects.
        /// </summary>
        /// <returns>A copied list of the currently registered GameObjects.</returns>
        public static IEnumerable<GameObject> GetRegisteredGameObjects()
        {
            return new List<GameObject>(GameObjectToEntity.Keys);
        }

        /// <summary>
        /// Gets a snapshot of all registered entities.
        /// </summary>
        /// <returns>A copied collection of the currently registered entities.</returns>
        public static IReadOnlyCollection<Entity> GetRegisteredEntities()
        {
            return new List<Entity>(EntityToGameObject.Keys);
        }

        /// <summary>
        /// Gets the current GameObject-to-Entity mapping snapshot.
        /// </summary>
        /// <returns>A copied dictionary of GameObject-to-Entity mappings.</returns>
        public static IReadOnlyDictionary<GameObject, Entity> GetAllMappings()
        {
            return new Dictionary<GameObject, Entity>(GameObjectToEntity);
        }

        /// <summary>
        /// Gets the current Entity-to-GameObject mapping snapshot.
        /// </summary>
        /// <returns>A copied dictionary of Entity-to-GameObject mappings.</returns>
        public static IReadOnlyDictionary<Entity, GameObject> GetReverseMappings()
        {
            return new Dictionary<Entity, GameObject>(EntityToGameObject);
        }
    }
}
