using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Utilities.HybridMono
{
	/// <summary>
	/// Central API for the HybridMono system.
	/// Provides access to a dedicated ECS World and GameObject-Entity mapping.
	/// </summary>
	public static class MonoHybridAPI
	{
		#region Constants
		private const string WORLD_NAME = "HybridMono World";
		#endregion

		#region Fields
		private static World _world;
		private static EntityManager _entityManager;
		private static readonly Dictionary<GameObject, Entity> _gameObjectToEntity = new Dictionary<GameObject, Entity>();
		private static bool _isInitialized;
		#endregion

		#region Properties
		/// <summary>
		/// Gets the dedicated HybridMono World.
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
		/// Gets the EntityManager for the HybridMono World.
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
		/// Returns true if the HybridMono system is initialized.
		/// </summary>
		public static bool IsInitialized => _isInitialized;

		/// <summary>
		/// Gets the count of registered GameObjects.
		/// </summary>
		public static int RegisteredCount => _gameObjectToEntity.Count;
		#endregion

		#region Initialization
		/// <summary>
		/// Initializes the HybridMono World. Called automatically before scene load.
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize()
		{
			if (_isInitialized) return;

			// Create dedicated world
			_world = new World(WORLD_NAME, WorldFlags.Game);
			_entityManager = _world.EntityManager;

			// Add the world to the player loop for automatic updates
			ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(_world);

			_isInitialized = true;
			Debug.Log($"[MonoHybridAPI] Initialized {WORLD_NAME}");
		}

		private static void EnsureInitialized()
		{
			if (!_isInitialized)
			{
				Initialize();
			}
		}

		/// <summary>
		/// Disposes the HybridMono World and clears all registrations.
		/// </summary>
		internal static void Dispose()
		{
			if (!_isInitialized) return;

			_gameObjectToEntity.Clear();

			if (_world != null && _world.IsCreated)
			{
				_world.Dispose();
				_world = null;
			}

			_isInitialized = false;
			Debug.Log($"[MonoHybridAPI] Disposed {WORLD_NAME}");
		}
		#endregion

		#region Registration API (Internal)
		/// <summary>
		/// Registers a GameObject with its associated Entity.
		/// Creates a new Entity if not already registered.
		/// Internal use only - called by MonoBakingSystem.
		/// </summary>
		/// <param name="gameObject">The GameObject to register.</param>
		/// <returns>The Entity associated with the GameObject.</returns>
		internal static Entity RegisterGameObject(GameObject gameObject)
		{
			EnsureInitialized();

			if (gameObject == null)
				throw new ArgumentNullException(nameof(gameObject));

			// Check if already registered
			if (_gameObjectToEntity.TryGetValue(gameObject, out Entity existingEntity))
			{
				return existingEntity;
			}

			// Create new entity
			Entity entity = _entityManager.CreateEntity();

			// Store mapping
			_gameObjectToEntity.Add(gameObject, entity);

			// Add tracker component if not present
			if (!gameObject.TryGetComponent<MonoEntityTracker>(out _))
			{
				var tracker = gameObject.AddComponent<MonoEntityTracker>();
				tracker.hideFlags = HideFlags.HideInInspector;
			}

			return entity;
		}

		/// <summary>
		/// Unregisters a GameObject and destroys its associated Entity.
		/// Called by MonoEntityTracker.OnDestroy().
		/// </summary>
		/// <param name="gameObject">The GameObject to unregister.</param>
		internal static void UnregisterGameObject(GameObject gameObject)
		{
			if (!_isInitialized) return;
			if (gameObject == null) return;

			if (_gameObjectToEntity.TryGetValue(gameObject, out Entity entity))
			{
				_gameObjectToEntity.Remove(gameObject);

				if (_world != null && _world.IsCreated && _entityManager.Exists(entity))
				{
					_entityManager.DestroyEntity(entity);
				}
			}
		}
		#endregion

		#region Entity Lookup
		/// <summary>
		/// Gets the Entity associated with a GameObject.
		/// </summary>
		/// <param name="gameObject">The GameObject to look up.</param>
		/// <param name="entity">The associated Entity if found.</param>
		/// <returns>True if the GameObject is registered.</returns>
		public static bool TryGetEntity(GameObject gameObject, out Entity entity)
		{
			EnsureInitialized();
			return _gameObjectToEntity.TryGetValue(gameObject, out entity);
		}

		/// <summary>
		/// Gets the Entity associated with a GameObject.
		/// Throws if not registered.
		/// </summary>
		/// <param name="gameObject">The GameObject to look up.</param>
		/// <returns>The associated Entity.</returns>
		public static Entity GetEntity(GameObject gameObject)
		{
			if (!TryGetEntity(gameObject, out Entity entity))
				throw new InvalidOperationException($"GameObject '{gameObject.name}' is not registered with HybridMono.");
			return entity;
		}

		/// <summary>
		/// Returns true if the GameObject is registered with the HybridMono system.
		/// </summary>
		/// <param name="gameObject">The GameObject to check.</param>
		/// <returns>True if registered.</returns>
		public static bool IsRegistered(GameObject gameObject)
		{
			EnsureInitialized();
			return gameObject != null && _gameObjectToEntity.ContainsKey(gameObject);
		}
		#endregion

		#region Component Data API
		/// <summary>
		/// Checks if the Entity associated with a GameObject has a component.
		/// </summary>
		/// <typeparam name="T">The component type.</typeparam>
		/// <param name="gameObject">The GameObject.</param>
		/// <returns>True if the component exists.</returns>
		public static bool HasComponent<T>(GameObject gameObject) where T : unmanaged, IComponentData
		{
			if (!TryGetEntity(gameObject, out Entity entity))
				return false;
			return _entityManager.HasComponent<T>(entity);
		}

		/// <summary>
		/// Gets component data from the Entity associated with a GameObject.
		/// </summary>
		/// <typeparam name="T">The component type.</typeparam>
		/// <param name="gameObject">The GameObject.</param>
		/// <returns>The component data.</returns>
		public static T GetComponentData<T>(GameObject gameObject) where T : unmanaged, IComponentData
		{
			Entity entity = GetEntity(gameObject);
			return _entityManager.GetComponentData<T>(entity);
		}

		/// <summary>
		/// Tries to get component data from the Entity associated with a GameObject.
		/// </summary>
		/// <typeparam name="T">The component type.</typeparam>
		/// <param name="gameObject">The GameObject.</param>
		/// <param name="data">The component data if found.</param>
		/// <returns>True if the component exists.</returns>
		public static bool TryGetComponentData<T>(GameObject gameObject, out T data) where T : unmanaged, IComponentData
		{
			if (!TryGetEntity(gameObject, out Entity entity) || !_entityManager.HasComponent<T>(entity))
			{
				data = default;
				return false;
			}
			data = _entityManager.GetComponentData<T>(entity);
			return true;
		}

		/// <summary>
		/// Sets component data on the Entity associated with a GameObject.
		/// The component must already exist.
		/// </summary>
		/// <typeparam name="T">The component type.</typeparam>
		/// <param name="gameObject">The GameObject.</param>
		/// <param name="data">The component data to set.</param>
		public static void SetComponentData<T>(GameObject gameObject, T data) where T : unmanaged, IComponentData
		{
			Entity entity = GetEntity(gameObject);
			_entityManager.SetComponentData(entity, data);
		}

		/// <summary>
		/// Adds or updates a component on the Entity associated with a GameObject.
		/// If the component doesn't exist, it will be added.
		/// </summary>
		/// <typeparam name="T">The component type.</typeparam>
		/// <param name="gameObject">The GameObject.</param>
		/// <param name="data">The component data.</param>
		public static void AddComponentData<T>(GameObject gameObject, T data) where T : unmanaged, IComponentData
		{
			Entity entity = GetEntity(gameObject);
			if (!_entityManager.HasComponent<T>(entity))
			{
				_entityManager.AddComponentData(entity, data);
			}
			else
			{
				_entityManager.SetComponentData(entity, data);
			}
		}

		/// <summary>
		/// Removes a component from the Entity associated with a GameObject.
		/// </summary>
		/// <typeparam name="T">The component type.</typeparam>
		/// <param name="gameObject">The GameObject.</param>
		public static void RemoveComponent<T>(GameObject gameObject) where T : unmanaged, IComponentData
		{
			if (TryGetEntity(gameObject, out Entity entity) && _entityManager.HasComponent<T>(entity))
			{
				_entityManager.RemoveComponent<T>(entity);
			}
		}
		#endregion

		#region Buffer API
		/// <summary>
		/// Checks if the Entity associated with a GameObject has a buffer.
		/// </summary>
		/// <typeparam name="T">The buffer element type.</typeparam>
		/// <param name="gameObject">The GameObject.</param>
		/// <returns>True if the buffer exists.</returns>
		public static bool HasBuffer<T>(GameObject gameObject) where T : unmanaged, IBufferElementData
		{
			if (!TryGetEntity(gameObject, out Entity entity))
				return false;
			return _entityManager.HasBuffer<T>(entity);
		}

		/// <summary>
		/// Gets a DynamicBuffer from the Entity associated with a GameObject.
		/// </summary>
		/// <typeparam name="T">The buffer element type.</typeparam>
		/// <param name="gameObject">The GameObject.</param>
		/// <returns>The DynamicBuffer.</returns>
		public static DynamicBuffer<T> GetBuffer<T>(GameObject gameObject) where T : unmanaged, IBufferElementData
		{
			Entity entity = GetEntity(gameObject);
			return _entityManager.GetBuffer<T>(entity);
		}

		/// <summary>
		/// Tries to get a DynamicBuffer from the Entity associated with a GameObject.
		/// </summary>
		/// <typeparam name="T">The buffer element type.</typeparam>
		/// <param name="gameObject">The GameObject.</param>
		/// <param name="buffer">The buffer if found.</param>
		/// <returns>True if the buffer exists.</returns>
		public static bool TryGetBuffer<T>(GameObject gameObject, out DynamicBuffer<T> buffer) where T : unmanaged, IBufferElementData
		{
			if (!TryGetEntity(gameObject, out Entity entity) || !_entityManager.HasBuffer<T>(entity))
			{
				buffer = default;
				return false;
			}
			buffer = _entityManager.GetBuffer<T>(entity);
			return true;
		}

		/// <summary>
		/// Adds a buffer to the Entity associated with a GameObject if not present.
		/// Returns the buffer (existing or newly added).
		/// </summary>
		/// <typeparam name="T">The buffer element type.</typeparam>
		/// <param name="gameObject">The GameObject.</param>
		/// <returns>The DynamicBuffer.</returns>
		public static DynamicBuffer<T> AddBuffer<T>(GameObject gameObject) where T : unmanaged, IBufferElementData
		{
			Entity entity = GetEntity(gameObject);
			if (!_entityManager.HasBuffer<T>(entity))
			{
				return _entityManager.AddBuffer<T>(entity);
			}
			return _entityManager.GetBuffer<T>(entity);
		}

		/// <summary>
		/// Ensures a buffer exists with specified initial capacity.
		/// </summary>
		/// <typeparam name="T">The buffer element type.</typeparam>
		/// <param name="gameObject">The GameObject.</param>
		/// <param name="capacity">The initial capacity.</param>
		/// <returns>The DynamicBuffer.</returns>
		public static DynamicBuffer<T> EnsureBuffer<T>(GameObject gameObject, int capacity = 0) where T : unmanaged, IBufferElementData
		{
			Entity entity = GetEntity(gameObject);
			DynamicBuffer<T> buffer;
			if (!_entityManager.HasBuffer<T>(entity))
			{
				buffer = _entityManager.AddBuffer<T>(entity);
			}
			else
			{
				buffer = _entityManager.GetBuffer<T>(entity);
			}

			if (capacity > 0)
			{
				buffer.EnsureCapacity(capacity);
			}

			return buffer;
		}

		/// <summary>
		/// Removes a buffer from the Entity associated with a GameObject.
		/// </summary>
		/// <typeparam name="T">The buffer element type.</typeparam>
		/// <param name="gameObject">The GameObject.</param>
		public static void RemoveBuffer<T>(GameObject gameObject) where T : unmanaged, IBufferElementData
		{
			if (TryGetEntity(gameObject, out Entity entity) && _entityManager.HasBuffer<T>(entity))
			{
				_entityManager.RemoveComponent<T>(entity);
			}
		}
		#endregion

		#region Query API
		/// <summary>
		/// Creates an EntityQuery for the HybridMono World.
		/// </summary>
		/// <param name="componentTypes">The component types to query.</param>
		/// <returns>The EntityQuery.</returns>
		public static EntityQuery CreateQuery(params ComponentType[] componentTypes)
		{
			EnsureInitialized();
			return _entityManager.CreateEntityQuery(componentTypes);
		}

		/// <summary>
		/// Creates an EntityQuery using EntityQueryDesc for complex queries.
		/// </summary>
		/// <param name="queryDesc">The query description.</param>
		/// <returns>The EntityQuery.</returns>
		public static EntityQuery CreateQuery(EntityQueryDesc queryDesc)
		{
			EnsureInitialized();
			return _entityManager.CreateEntityQuery(queryDesc);
		}

		/// <summary>
		/// Gets all registered GameObjects.
		/// </summary>
		/// <returns>Enumerable of registered GameObjects.</returns>
		public static IEnumerable<GameObject> GetRegisteredGameObjects()
		{
			return _gameObjectToEntity.Keys;
		}

		/// <summary>
		/// Gets all registered GameObject-Entity pairs.
		/// </summary>
		/// <returns>Read-only dictionary of mappings.</returns>
		public static IReadOnlyDictionary<GameObject, Entity> GetAllMappings()
		{
			return _gameObjectToEntity;
		}
		#endregion
	}
}
