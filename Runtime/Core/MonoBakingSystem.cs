using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Utilities.HybridMono
{
	/// <summary>
	/// Static system responsible for discovering and executing MonoBakers.
	/// Handles runtime-only baking of authoring MonoBehaviours into Entities.
	/// </summary>
	public static class MonoBakingSystem
	{
		#region Fields
		/// <summary>
		/// Cache of baker instances, indexed by the authoring component type they handle.
		/// </summary>
		private static readonly Dictionary<Type, object> _bakerCache = new Dictionary<Type, object>();


	/// <summary>
		/// Sorted list of baker types in dependency order (respects BakeAfter attributes).
		/// </summary>
		private static List<Type> _bakerOrder = new List<Type>();


		/// <summary>
		/// Flag indicating if the system has been initialized.
		/// </summary>
		private static bool _initialized;
		#endregion

		#region Initialization
		/// <summary>
		/// Automatically initializes the system when the runtime starts, before any scenes are loaded.
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void RuntimeInitialize()
		{
			InitializeSystem();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Subscribe to application quit for builds
			Application.quitting += OnApplicationQuitting;
			Debug.Log("[MonoBakingSystem] Initialization flag reset");


		}
		/// <summary>
		/// Called when application is quitting (builds).
		/// </summary>
		private static void OnApplicationQuitting()
		{
			_initialized = false;
			SceneManager.sceneLoaded -= OnSceneLoaded;
			Debug.Log("[MonoBakingSystem] Application Quitting - Reset");
		}
		/// <summary>
		/// Initializes the baking system by discovering bakers and subscribing to scene events.
		/// </summary>
		private static void InitializeSystem()
		{
			
			if (_initialized) return;

			Debug.Log("Bake All Scenes");		
			DiscoverBakers();
			SortBakersByDependencies();
			//OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

			// Subscribe to scene loads to automatically bake
			SceneManager.sceneLoaded += OnSceneLoaded;

			// Reload current scene asynchronously
			Scene activeScene = SceneManager.GetActiveScene();
			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(activeScene.name);



			_initialized = true;
			Debug.Log($"[MonoBakingSystem] Initialized: {_bakerCache.Count} bakers cached.");
		}
		#endregion



		#region Baker Discovery
		/// <summary>
		/// Scans all loaded assemblies for classes inheriting from MonoBaker and populates the cache.
		/// </summary>
		private static void DiscoverBakers()
		{
			_bakerCache.Clear();

			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				Type[] types;
				try { types = assembly.GetTypes(); }
				catch { continue; }

				foreach (var type in types)
				{
					if (type.IsAbstract || type.IsInterface) continue;

					var baseType = type.BaseType;
					while (baseType != null)
					{
						if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(MonoBaker<>))
						{
							Type authoringType = baseType.GetGenericArguments()[0];

							if (!_bakerCache.ContainsKey(authoringType))
							{
								var bakerInstance = Activator.CreateInstance(type);
								_bakerCache.Add(authoringType, bakerInstance);
							}
							break;
						}
						baseType = baseType.BaseType;
					}
				}
			}
		}

			private static void SortBakersByDependencies()
		{
			// Build dependency graph
			var dependencies = new Dictionary<Type, List<Type>>();
			var allBakerTypes = _bakerCache.Values.Select(b => b.GetType()).ToList();

			foreach (var bakerType in allBakerTypes)
			{
				dependencies[bakerType] = new List<Type>();

				var bakeAfterAttrs = bakerType.GetCustomAttributes<BakeAfterAttribute>();
				foreach (var attr in bakeAfterAttrs)
				{
					if (attr.TargetType != null && allBakerTypes.Contains(attr.TargetType))
					{
						dependencies[bakerType].Add(attr.TargetType);
					}
				}
			}

			// Topological sort
			_bakerOrder = TopologicalSort(allBakerTypes, dependencies);

			Debug.Log($"[MonoBakingSystem] Baker order: {string.Join(" -> ", _bakerOrder.Select(t => t.Name))}");
		}

		/// <summary>
		/// Performs topological sort on baker types based on dependencies.
		/// </summary>
		private static List<Type> TopologicalSort(List<Type> types, Dictionary<Type, List<Type>> dependencies)
		{
			var sorted = new List<Type>();
			var visited = new HashSet<Type>();
			var visiting = new HashSet<Type>();

			void Visit(Type type)
			{
				if (visited.Contains(type)) return;

				if (visiting.Contains(type))
				{
					Debug.LogWarning($"[MonoBakingSystem] Circular dependency detected involving {type.Name}");
					return;
				}

				visiting.Add(type);

				if (dependencies.TryGetValue(type, out var deps))
				{
					foreach (var dep in deps)
					{
						Visit(dep);
					}
				}

				visiting.Remove(type);
				visited.Add(type);
				sorted.Add(type);
			}

			foreach (var type in types)
			{
				Visit(type);
			}

			return sorted;
		}


		/// <summary>
		/// Event handler called when a scene is loaded.
		/// </summary>
		private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			Debug.Log($"Bake Scene: {scene.name}");
			BakeAllInScene();
			
		}

		/// <summary>
		/// Finds and bakes all MonoBehaviours with registered bakers in loaded scenes.
		/// </summary>
		public static void BakeAllInScene()
		{
			var allComponents = GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

			// Group components by authoring type
			var componentsByType = new Dictionary<Type, List<MonoBehaviour>>();
			foreach (var comp in allComponents)
			{
				var compType = comp.GetType();
				if (_bakerCache.ContainsKey(compType))
				{
					if (!componentsByType.ContainsKey(compType))
					{
						componentsByType[compType] = new List<MonoBehaviour>();
					}
					componentsByType[compType].Add(comp);
				}
			}

			// Bake in dependency order
			foreach (var bakerType in _bakerOrder)
			{
				// Find the authoring type for this baker
				var authoringType = bakerType.BaseType?.GetGenericArguments()[0];
				if (authoringType == null) continue;

				if (componentsByType.TryGetValue(authoringType, out var components))
				{
					Debug.Log($"[MonoBakingSystem] Baking {components.Count} '{authoringType.Name}' components using '{bakerType.Name}'");
					foreach (var comp in components)
					{
						BakeAuthoring(comp);
					}
				}
			}
		}

		/// <summary>
		/// Bakes a single authoring component if a corresponding baker exists.
		/// Creates or updates the associated Entity.
		/// </summary>
		/// <param name="authoring">The authoring component to bake.</param>
		public static void BakeAuthoring(MonoBehaviour authoring)
		{
			if (authoring == null) return;

			// Ensure scene is fully loaded
			if (!authoring.gameObject.scene.isLoaded) return;

			// Skip if in a SubScene (handled by standard ECS baking)
			if (IsInSubScene(authoring)) return;

			Type authoringType = authoring.GetType();

			if (!_bakerCache.TryGetValue(authoringType, out var baker))
			{
				// No baker for this type - that's fine, not all MonoBehaviours need baking
				return;
			}

			try
			{
				var bakeMethod = baker.GetType().GetMethod("BakeInternal", BindingFlags.Instance | BindingFlags.NonPublic);

				bakeMethod.Invoke(baker, new object[] { authoring });
			}
			catch (Exception ex)
			{
				Debug.LogError($"[MonoBakingSystem] Failed to bake {authoringType.Name}\n{ex}", authoring);
			}
		}

		/// <summary>
		/// Checks if an authoring component has already been baked.
		/// </summary>
		/// <param name="authoring">The authoring component to check.</param>
		/// <returns>True if the component's GameObject is registered with MonoHybridAPI.</returns>
		public static bool IsBaked(MonoBehaviour authoring)
		{
			if (authoring == null) return false;
			return MonoHybridAPI.IsRegistered(authoring.gameObject);
		}

		/// <summary>
		/// Checks if a baker exists for a given authoring type.
		/// </summary>
		/// <param name="authoringType">The authoring type to check.</param>
		/// <returns>True if a baker exists for this type.</returns>
		public static bool HasBaker(Type authoringType)
		{
			return _bakerCache.ContainsKey(authoringType);
		}

		/// <summary>
		/// Checks if a baker exists for a given authoring type.
		/// </summary>
		/// <typeparam name="T">The authoring type to check.</typeparam>
		/// <returns>True if a baker exists for this type.</returns>
		public static bool HasBaker<T>() where T : MonoBehaviour
		{
			return _bakerCache.ContainsKey(typeof(T));
		}

		/// <summary>
		/// Gets the count of registered bakers.
		/// </summary>
		public static int BakerCount => _bakerCache.Count;
		#endregion

		#region SubScene Detection
		/// <summary>
		/// Checks if a component is part of a SubScene.
		/// </summary>
		/// <param name="component">The component to check.</param>
		/// <returns>True if it is in a SubScene.</returns>
		private static bool IsInSubScene(MonoBehaviour component)
		{
			return component.GetComponentInParent<SubScene>();
		}
		#endregion

		#region Cleanup
		/// <summary>
		/// Clears the baking system state.
		/// Does not affect already-baked entities.
		/// </summary>
		public static void Clear()
		{
			// No internal tracking needed - MonoHybridAPI handles entity tracking
		}
		#endregion
	}
}
