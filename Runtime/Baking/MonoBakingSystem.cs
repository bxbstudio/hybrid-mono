using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Discovers and executes runtime Mono bakers.
    /// </summary>
    public static class MonoBakingSystem
    {
        /// <summary>
        /// Stores one runtime baker instance per authoring component type.
        /// </summary>
        private static readonly Dictionary<Type, IMonoBaker> BakerCache = new();

        /// <summary>
        /// Tracks whether baker discovery and event hookup have completed.
        /// </summary>
        private static bool _initialized;

        /// <summary>
        /// Tracks whether the first full runtime bake pass has completed.
        /// </summary>
        private static bool _initialBakeCompleted;

        /// <summary>
        /// Raised after the first runtime bake pass completes.
        /// </summary>
        public static event Action InitialBakeCompletedEvent;

        /// <summary>
        /// Returns true once the initial runtime bake pass has completed.
        /// </summary>
        public static bool InitialBakeCompleted => _initialBakeCompleted;

        /// <summary>
        /// Gets the number of registered bakers.
        /// </summary>
        public static int BakerCount => BakerCache.Count;

        /// <summary>
        /// Clears cached baker state on subsystem registration.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            BakerCache.Clear();
            InitialBakeCompletedEvent = null;
            _initialized = false;
            _initialBakeCompleted = false;
        }

        /// <summary>
        /// Initializes the baking system before scene load.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitialize()
        {
            InitializeSystem();
        }

        /// <summary>
        /// Ensures the initial runtime bake has completed.
        /// </summary>
        public static void EnsureInitialBakeCompleted()
        {
            InitializeSystem();

            if (_initialBakeCompleted)
                return;

            BakeAllInScene();
            CompleteInitialBake();
        }

        /// <summary>
        /// Initializes baker discovery and scene event subscriptions.
        /// </summary>
        private static void InitializeSystem()
        {
            if (_initialized)
                return;

            DiscoverBakers();
            SceneManager.sceneLoaded += OnSceneLoaded;
            _initialized = true;

            Debug.Log($"[MonoBakingSystem] Initialized: {BakerCache.Count} bakers cached.");
        }

        /// <summary>
        /// Discovers all concrete <see cref="MonoBaker{TAuthoring}"/> implementations loaded in the AppDomain.
        /// </summary>
        private static void DiscoverBakers()
        {
            BakerCache.Clear();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types.Where(type => type != null).ToArray();
                }
                catch
                {
                    continue;
                }

                for (int i = 0; i < types.Length; i++)
                {
                    Type type = types[i];

                    if (type == null || type.IsAbstract || type.IsInterface)
                        continue;

                    Type baseType = type.BaseType;

                    while (baseType != null)
                    {
                        if (!baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(MonoBaker<>))
                        {
                            baseType = baseType.BaseType;
                            continue;
                        }

                        // Each authoring type is allowed a single active baker implementation.
                        Type authoringType = baseType.GetGenericArguments()[0];

                        if (!BakerCache.ContainsKey(authoringType))
                        {
                            BakerCache.Add(authoringType, (IMonoBaker)Activator.CreateInstance(type));
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"[MonoBakingSystem] Duplicate baker for {authoringType.Name}: " +
                                $"{type.Name} ignored (already registered: {BakerCache[authoringType].GetType().Name})");
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Re-bakes the scene that has just been loaded.
        /// </summary>
        /// <param name="scene">The scene that was loaded.</param>
        /// <param name="mode">The load mode for the scene event.</param>
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BakeScene(scene);

            if (!_initialBakeCompleted)
                CompleteInitialBake();
        }

        /// <summary>
        /// Finds and bakes all supported authoring components in loaded scenes.
        /// </summary>
        public static void BakeAllInScene()
        {
            InitializeSystem();

            foreach (var kvp in BakerCache)
                BakeAuthorings(kvp.Key, kvp.Value, default, filterByScene: false);
        }

        /// <summary>
        /// Bakes all supported authoring components in a specific scene.
        /// </summary>
        /// <param name="scene">The scene to bake.</param>
        public static void BakeScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            InitializeSystem();

            foreach (var kvp in BakerCache)
                BakeAuthorings(kvp.Key, kvp.Value, scene, filterByScene: true);
        }

        /// <summary>
        /// Bakes a single authoring component if a corresponding baker exists.
        /// </summary>
        /// <param name="authoring">The authoring component to bake.</param>
        public static void BakeAuthoring(MonoBehaviour authoring)
        {
            if (authoring == null || !authoring.gameObject.scene.isLoaded || IsInSubScene(authoring))
                return;

            InitializeSystem();

            if (!BakerCache.TryGetValue(authoring.GetType(), out IMonoBaker baker))
                return;

            TryBake(authoring, baker);
        }

        /// <summary>
        /// Checks if an authoring component has already been baked.
        /// </summary>
        /// <param name="authoring">The authoring component to test.</param>
        /// <returns>True when the authoring GameObject already has a registered entity.</returns>
        public static bool IsBaked(MonoBehaviour authoring)
        {
            return authoring != null && MonoHybridAPI.IsRegistered(authoring.gameObject);
        }

        /// <summary>
        /// Checks if a baker exists for a given authoring type.
        /// </summary>
        /// <param name="authoringType">The authoring type to test.</param>
        /// <returns>True when a runtime baker has been discovered for the type.</returns>
        public static bool HasBaker(Type authoringType)
        {
            InitializeSystem();
            return authoringType != null && BakerCache.ContainsKey(authoringType);
        }

        /// <summary>
        /// Checks if a baker exists for a given authoring type.
        /// </summary>
        /// <typeparam name="T">The authoring type to test.</typeparam>
        /// <returns>True when a runtime baker has been discovered for the type.</returns>
        public static bool HasBaker<T>() where T : MonoBehaviour
        {
            return HasBaker(typeof(T));
        }

        /// <summary>
        /// Clears the baking system state and unsubscribes from scene events.
        /// </summary>
        public static void Clear()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            BakerCache.Clear();
            _initialized = false;
            _initialBakeCompleted = false;
        }

        /// <summary>
        /// Bakes all authoring components of a specific runtime type.
        /// </summary>
        /// <param name="authoringType">The authoring component type to search for.</param>
        /// <param name="baker">The baker used for matching authorings.</param>
        /// <param name="scene">The scene to filter by when <paramref name="filterByScene"/> is true.</param>
        /// <param name="filterByScene">Whether to restrict baking to a specific scene.</param>
        private static void BakeAuthorings(Type authoringType, IMonoBaker baker, Scene scene, bool filterByScene)
        {
            UnityEngine.Object[] authorings = UnityEngine.Object.FindObjectsByType(authoringType, FindObjectsSortMode.None);

            for (int i = 0; i < authorings.Length; i++)
            {
                if (authorings[i] is not MonoBehaviour authoring)
                    continue;

                if (!authoring.gameObject.scene.isLoaded)
                    continue;

                if (filterByScene && authoring.gameObject.scene.handle != scene.handle)
                    continue;

                // SubScenes are baked by Unity's ECS pipeline and should not be mirrored here.
                if (IsInSubScene(authoring))
                    continue;

                TryBake(authoring, baker);
            }
        }

        /// <summary>
        /// Executes a baker for a single authoring component with error isolation.
        /// </summary>
        /// <param name="authoring">The authoring component to bake.</param>
        /// <param name="baker">The baker to execute.</param>
        private static void TryBake(MonoBehaviour authoring, IMonoBaker baker)
        {
            try
            {
                baker.BakeInternal(authoring);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MonoBakingSystem] Failed to bake {authoring.GetType().Name}\n{exception}", authoring);
            }
        }

        /// <summary>
        /// Returns true when the component belongs to a SubScene hierarchy.
        /// </summary>
        /// <param name="component">The component to inspect.</param>
        /// <returns>True when the component is part of a SubScene.</returns>
        private static bool IsInSubScene(MonoBehaviour component)
        {
            return component.GetComponentInParent<SubScene>();
        }

        /// <summary>
        /// Marks the initial bake as complete and notifies listeners once.
        /// </summary>
        private static void CompleteInitialBake()
        {
            if (_initialBakeCompleted)
                return;

            _initialBakeCompleted = true;
            InitialBakeCompletedEvent?.Invoke();
        }
    }
}
