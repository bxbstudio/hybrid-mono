using Unity.Entities;
using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Component access helpers for HybridMono GameObject and entity mappings.
    /// </summary>
    public static partial class MonoHybridAPI
    {
        /// <summary>
        /// Checks if the GameObject's entity has a component.
        /// </summary>
        /// <typeparam name="T">The component type to test.</typeparam>
        /// <param name="gameObject">The GameObject whose mirrored entity is checked.</param>
        /// <returns>True when the mirrored entity has the component.</returns>
        public static bool HasComponent<T>(GameObject gameObject) where T : unmanaged, IComponentData
        {
            return TryGetEntity(gameObject, out Entity entity) && HasComponent<T>(entity);
        }

        /// <summary>
        /// Gets component data from the GameObject's entity.
        /// </summary>
        /// <typeparam name="T">The component type to read.</typeparam>
        /// <param name="gameObject">The GameObject whose mirrored entity is read.</param>
        /// <returns>The requested component data.</returns>
        public static T GetComponentData<T>(GameObject gameObject) where T : unmanaged, IComponentData
        {
            return GetComponentData<T>(GetEntity(gameObject));
        }

        /// <summary>
        /// Tries to get component data from the GameObject's entity.
        /// </summary>
        /// <typeparam name="T">The component type to read.</typeparam>
        /// <param name="gameObject">The GameObject whose mirrored entity is read.</param>
        /// <param name="data">The resolved component data when present.</param>
        /// <returns>True when the component exists on the mirrored entity.</returns>
        public static bool TryGetComponentData<T>(GameObject gameObject, out T data) where T : unmanaged, IComponentData
        {
            if (!TryGetEntity(gameObject, out Entity entity))
            {
                data = default;
                return false;
            }

            return TryGetComponentData(entity, out data);
        }

        /// <summary>
        /// Sets component data on the GameObject's entity.
        /// </summary>
        /// <typeparam name="T">The component type to write.</typeparam>
        /// <param name="gameObject">The GameObject whose mirrored entity is updated.</param>
        /// <param name="data">The component value to write.</param>
        public static void SetComponentData<T>(GameObject gameObject, T data) where T : unmanaged, IComponentData
        {
            SetComponentData(GetEntity(gameObject), data);
        }

        /// <summary>
        /// Adds or updates component data on the GameObject's entity.
        /// </summary>
        /// <typeparam name="T">The component type to add or update.</typeparam>
        /// <param name="gameObject">The GameObject whose mirrored entity is updated.</param>
        /// <param name="data">The component value to write.</param>
        public static void AddComponentData<T>(GameObject gameObject, T data) where T : unmanaged, IComponentData
        {
            AddComponentData(GetEntity(gameObject), data);
        }

        /// <summary>
        /// Removes a component from the GameObject's entity.
        /// </summary>
        /// <typeparam name="T">The component type to remove.</typeparam>
        /// <param name="gameObject">The GameObject whose mirrored entity is updated.</param>
        public static void RemoveComponent<T>(GameObject gameObject) where T : unmanaged, IComponentData
        {
            if (TryGetEntity(gameObject, out Entity entity))
                RemoveComponent<T>(entity);
        }

        /// <summary>
        /// Checks if an entity has a component.
        /// </summary>
        /// <typeparam name="T">The component type to test.</typeparam>
        /// <param name="entity">The entity to check.</param>
        /// <returns>True when the entity has the component.</returns>
        public static bool HasComponent<T>(Entity entity) where T : unmanaged, IComponentData
        {
            EnsureInitialized();
            return Exists(entity) && _entityManager.HasComponent<T>(entity);
        }

        /// <summary>
        /// Gets component data from an entity.
        /// </summary>
        /// <typeparam name="T">The component type to read.</typeparam>
        /// <param name="entity">The entity to read.</param>
        /// <returns>The requested component data.</returns>
        public static T GetComponentData<T>(Entity entity) where T : unmanaged, IComponentData
        {
            ThrowIfEntityDoesNotExist(entity);
            return _entityManager.GetComponentData<T>(entity);
        }

        /// <summary>
        /// Tries to get component data from an entity.
        /// </summary>
        /// <typeparam name="T">The component type to read.</typeparam>
        /// <param name="entity">The entity to read.</param>
        /// <param name="data">The resolved component data when present.</param>
        /// <returns>True when the component exists on the entity.</returns>
        public static bool TryGetComponentData<T>(Entity entity, out T data) where T : unmanaged, IComponentData
        {
            EnsureInitialized();

            if (!Exists(entity) || !_entityManager.HasComponent<T>(entity))
            {
                data = default;
                return false;
            }

            data = _entityManager.GetComponentData<T>(entity);
            return true;
        }

        /// <summary>
        /// Sets component data on an entity.
        /// </summary>
        /// <typeparam name="T">The component type to write.</typeparam>
        /// <param name="entity">The entity to update.</param>
        /// <param name="data">The component value to write.</param>
        public static void SetComponentData<T>(Entity entity, T data) where T : unmanaged, IComponentData
        {
            ThrowIfEntityDoesNotExist(entity);
            _entityManager.SetComponentData(entity, data);
        }

        /// <summary>
        /// Adds or updates component data on an entity.
        /// </summary>
        /// <typeparam name="T">The component type to add or update.</typeparam>
        /// <param name="entity">The entity to update.</param>
        /// <param name="data">The component value to write.</param>
        public static void AddComponentData<T>(Entity entity, T data) where T : unmanaged, IComponentData
        {
            ThrowIfEntityDoesNotExist(entity);

            if (_entityManager.HasComponent<T>(entity))
                _entityManager.SetComponentData(entity, data);
            else
                _entityManager.AddComponentData(entity, data);
        }

        /// <summary>
        /// Removes a component from an entity.
        /// </summary>
        /// <typeparam name="T">The component type to remove.</typeparam>
        /// <param name="entity">The entity to update.</param>
        public static void RemoveComponent<T>(Entity entity) where T : unmanaged, IComponentData
        {
            EnsureInitialized();

            if (Exists(entity) && _entityManager.HasComponent<T>(entity))
                _entityManager.RemoveComponent<T>(entity);
        }
    }
}
