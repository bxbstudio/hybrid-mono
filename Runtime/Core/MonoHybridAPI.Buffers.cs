using Unity.Entities;
using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Buffer access helpers for HybridMono GameObject and entity mappings.
    /// </summary>
    public static partial class MonoHybridAPI
    {
        /// <summary>
        /// Checks if the GameObject's entity has a buffer.
        /// </summary>
        /// <typeparam name="T">The buffer element type to test.</typeparam>
        /// <param name="gameObject">The GameObject whose mirrored entity is checked.</param>
        /// <returns>True when the mirrored entity has the buffer.</returns>
        public static bool HasBuffer<T>(GameObject gameObject) where T : unmanaged, IBufferElementData
        {
            return TryGetEntity(gameObject, out Entity entity) && HasBuffer<T>(entity);
        }

        /// <summary>
        /// Gets a buffer from the GameObject's entity.
        /// </summary>
        /// <typeparam name="T">The buffer element type to read.</typeparam>
        /// <param name="gameObject">The GameObject whose mirrored entity is read.</param>
        /// <returns>The requested buffer.</returns>
        public static DynamicBuffer<T> GetBuffer<T>(GameObject gameObject) where T : unmanaged, IBufferElementData
        {
            return GetBuffer<T>(GetEntity(gameObject));
        }

        /// <summary>
        /// Tries to get a buffer from the GameObject's entity.
        /// </summary>
        /// <typeparam name="T">The buffer element type to read.</typeparam>
        /// <param name="gameObject">The GameObject whose mirrored entity is read.</param>
        /// <param name="buffer">The resolved buffer when present.</param>
        /// <returns>True when the buffer exists on the mirrored entity.</returns>
        public static bool TryGetBuffer<T>(GameObject gameObject, out DynamicBuffer<T> buffer) where T : unmanaged, IBufferElementData
        {
            if (!TryGetEntity(gameObject, out Entity entity))
            {
                buffer = default;
                return false;
            }

            return TryGetBuffer(entity, out buffer);
        }

        /// <summary>
        /// Adds a buffer to the GameObject's entity if it is missing.
        /// </summary>
        /// <typeparam name="T">The buffer element type to add.</typeparam>
        /// <param name="gameObject">The GameObject whose mirrored entity is updated.</param>
        /// <returns>The existing or newly created buffer.</returns>
        public static DynamicBuffer<T> AddBuffer<T>(GameObject gameObject) where T : unmanaged, IBufferElementData
        {
            return AddBuffer<T>(GetEntity(gameObject));
        }

        /// <summary>
        /// Ensures a buffer exists on the GameObject's entity.
        /// </summary>
        /// <typeparam name="T">The buffer element type to ensure.</typeparam>
        /// <param name="gameObject">The GameObject whose mirrored entity is updated.</param>
        /// <param name="capacity">Optional capacity to reserve for the buffer.</param>
        /// <returns>The ensured buffer.</returns>
        public static DynamicBuffer<T> EnsureBuffer<T>(GameObject gameObject, int capacity = 0) where T : unmanaged, IBufferElementData
        {
            return EnsureBuffer<T>(GetEntity(gameObject), capacity);
        }

        /// <summary>
        /// Removes a buffer from the GameObject's entity.
        /// </summary>
        /// <typeparam name="T">The buffer element type to remove.</typeparam>
        /// <param name="gameObject">The GameObject whose mirrored entity is updated.</param>
        public static void RemoveBuffer<T>(GameObject gameObject) where T : unmanaged, IBufferElementData
        {
            if (TryGetEntity(gameObject, out Entity entity))
                RemoveBuffer<T>(entity);
        }

        /// <summary>
        /// Checks if an entity has a buffer.
        /// </summary>
        /// <typeparam name="T">The buffer element type to test.</typeparam>
        /// <param name="entity">The entity to check.</param>
        /// <returns>True when the entity has the buffer.</returns>
        public static bool HasBuffer<T>(Entity entity) where T : unmanaged, IBufferElementData
        {
            EnsureInitialized();
            return Exists(entity) && _entityManager.HasBuffer<T>(entity);
        }

        /// <summary>
        /// Gets a buffer from an entity.
        /// </summary>
        /// <typeparam name="T">The buffer element type to read.</typeparam>
        /// <param name="entity">The entity to read.</param>
        /// <returns>The requested buffer.</returns>
        public static DynamicBuffer<T> GetBuffer<T>(Entity entity) where T : unmanaged, IBufferElementData
        {
            ThrowIfEntityDoesNotExist(entity);
            return _entityManager.GetBuffer<T>(entity);
        }

        /// <summary>
        /// Tries to get a buffer from an entity.
        /// </summary>
        /// <typeparam name="T">The buffer element type to read.</typeparam>
        /// <param name="entity">The entity to read.</param>
        /// <param name="buffer">The resolved buffer when present.</param>
        /// <returns>True when the buffer exists on the entity.</returns>
        public static bool TryGetBuffer<T>(Entity entity, out DynamicBuffer<T> buffer) where T : unmanaged, IBufferElementData
        {
            EnsureInitialized();

            if (!Exists(entity) || !_entityManager.HasBuffer<T>(entity))
            {
                buffer = default;
                return false;
            }

            buffer = _entityManager.GetBuffer<T>(entity);
            return true;
        }

        /// <summary>
        /// Adds a buffer to an entity if it is missing.
        /// </summary>
        /// <typeparam name="T">The buffer element type to add.</typeparam>
        /// <param name="entity">The entity to update.</param>
        /// <returns>The existing or newly created buffer.</returns>
        public static DynamicBuffer<T> AddBuffer<T>(Entity entity) where T : unmanaged, IBufferElementData
        {
            ThrowIfEntityDoesNotExist(entity);
            return _entityManager.HasBuffer<T>(entity) ? _entityManager.GetBuffer<T>(entity) : _entityManager.AddBuffer<T>(entity);
        }

        /// <summary>
        /// Ensures a buffer exists on an entity and optionally preallocates capacity.
        /// </summary>
        /// <typeparam name="T">The buffer element type to ensure.</typeparam>
        /// <param name="entity">The entity to update.</param>
        /// <param name="capacity">Optional capacity to reserve for the buffer.</param>
        /// <returns>The ensured buffer.</returns>
        public static DynamicBuffer<T> EnsureBuffer<T>(Entity entity, int capacity = 0) where T : unmanaged, IBufferElementData
        {
            DynamicBuffer<T> buffer = AddBuffer<T>(entity);

            if (capacity > 0)
                buffer.EnsureCapacity(capacity);

            return buffer;
        }

        /// <summary>
        /// Removes a buffer from an entity.
        /// </summary>
        /// <typeparam name="T">The buffer element type to remove.</typeparam>
        /// <param name="entity">The entity to update.</param>
        public static void RemoveBuffer<T>(Entity entity) where T : unmanaged, IBufferElementData
        {
            EnsureInitialized();

            if (Exists(entity) && _entityManager.HasBuffer<T>(entity))
                _entityManager.RemoveComponent<T>(entity);
        }
    }
}
