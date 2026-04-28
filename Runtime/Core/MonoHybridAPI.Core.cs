using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Central runtime API for the HybridMono world and GameObject-Entity bridge.
    /// </summary>
    public static partial class MonoHybridAPI
    {
        /// <summary>
        /// The display name used for the dedicated HybridMono world.
        /// </summary>
        private const string WorldName = "HybridMono World";

        /// <summary>
        /// The dedicated world that stores HybridMono entities.
        /// </summary>
        private static World _world;

        /// <summary>
        /// Cached entity manager for the HybridMono world.
        /// </summary>
        private static EntityManager _entityManager;

        /// <summary>
        /// Maps registered GameObjects to their mirrored entities.
        /// </summary>
        private static readonly Dictionary<GameObject, Entity> GameObjectToEntity = new();

        /// <summary>
        /// Maps mirrored entities back to their source GameObjects.
        /// </summary>
        private static readonly Dictionary<Entity, GameObject> EntityToGameObject = new();

        /// <summary>
        /// Tracks whether the world has been initialized for the current domain.
        /// </summary>
        private static bool _isInitialized;

        /// <summary>
        /// Gets the dedicated HybridMono world.
        /// </summary>
        public static World World
        {
            get
            {
                EnsureInitialized();
                return _world;
            }
        }

        /// <summary>
        /// Gets the entity manager for the HybridMono world.
        /// </summary>
        public static EntityManager EntityManager
        {
            get
            {
                EnsureInitialized();
                return _entityManager;
            }
        }

        /// <summary>
        /// Returns true if the HybridMono world is initialized and created.
        /// </summary>
        public static bool IsInitialized => _isInitialized && _world != null && _world.IsCreated;

        /// <summary>
        /// Gets the number of registered GameObject-Entity mappings.
        /// </summary>
        public static int RegisteredCount => GameObjectToEntity.Count;

        /// <summary>
        /// Resets static state when the Unity subsystem is re-registered.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Dispose();
            GameObjectToEntity.Clear();
            EntityToGameObject.Clear();
        }

        /// <summary>
        /// Initializes the HybridMono world before scene load.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (IsInitialized)
                return;

            _world = new World(WorldName, WorldFlags.Game);
            _entityManager = _world.EntityManager;
            _isInitialized = true;

            Application.quitting -= Dispose;
            Application.quitting += Dispose;
        }

        /// <summary>
        /// Disposes the HybridMono world and clears all cached registrations.
        /// </summary>
        internal static void Dispose()
        {
            if (!_isInitialized)
                return;

            Application.quitting -= Dispose;

            GameObjectToEntity.Clear();
            EntityToGameObject.Clear();

            if (_world != null && _world.IsCreated)
                _world.Dispose();

            _world = null;
            _entityManager = default;
            _isInitialized = false;
        }

        /// <summary>
        /// Ensures the world exists before any API call touches cached state.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                _isInitialized = false;
                Initialize();
            }
        }

        /// <summary>
        /// Throws when the supplied GameObject reference is missing.
        /// </summary>
        /// <param name="gameObject">The GameObject that is about to be used.</param>
        private static void ValidateGameObject(GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));
        }

        /// <summary>
        /// Throws when the supplied entity is not valid inside the HybridMono world.
        /// </summary>
        /// <param name="entity">The entity that is expected to exist.</param>
        private static void ThrowIfEntityDoesNotExist(Entity entity)
        {
            if (!Exists(entity))
                throw new InvalidOperationException($"Entity '{entity}' does not exist in the HybridMono world.");
        }

        /// <summary>
        /// Copies managed enumerable content into a native array using the requested allocator.
        /// </summary>
        /// <typeparam name="T">The value type stored in the resulting native array.</typeparam>
        /// <param name="values">The source values to copy.</param>
        /// <param name="allocator">The allocator used for the native array.</param>
        /// <returns>A newly allocated native array containing the copied values.</returns>
        private static NativeArray<T> ToNativeArray<T>(IEnumerable<T> values, Allocator allocator) where T : struct
        {
            var list = values as IList<T> ?? new List<T>(values);
            var array = new NativeArray<T>(list.Count, allocator);

            for (int i = 0; i < list.Count; i++)
                array[i] = list[i];

            return array;
        }
    }
}
