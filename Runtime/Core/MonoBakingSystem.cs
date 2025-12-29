using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Object=UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utilities.HybridMono
{
    /// <summary>
    /// Static system responsible for discovering and executing MonoBakers.
    /// It handles the baking of authoring MonoBehaviours into components and buffers.
    /// </summary>
    [ExecuteAlways]
    public static class MonoBakingSystem 
    {

        /// <summary>
        /// Cache of baker instances, indexed by the authoring component type they handle.
        /// </summary>
        private static readonly Dictionary<Type, object> _bakerCache = new Dictionary<Type, object>();
        /// <summary>
        /// Set of authoring components that have already been baked.
        /// </summary>
        private static readonly HashSet<MonoBehaviour> _bakedAuthoring = new HashSet<MonoBehaviour>();

        /// <summary>
        /// Flag indicating if the system has been initialized.
        /// </summary>
        private static bool _initialized;

#region SubScene Detection

        /// <summary>
        /// Type object for Unity.Scenes.SubScene, retrieved via reflection to avoid direct dependency.
        /// </summary>
        private static readonly Type SubSceneType = AppDomain.CurrentDomain.GetAssemblies()
         .SelectMany(a =>
              {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })  
                .FirstOrDefault(t => t.FullName == "Unity.Scenes.SubScene");
#endregion


        #region Initialization
        /// <summary>
        /// Automatically initializes the system when the runtime starts, before any scenes are loaded.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitialize()
        {
             InitializeSystem();
           //  BakeAllInScene();
        }
        #if UNITY_EDITOR
        /// <summary>
        /// Automatically initializes the system in the editor when it finishes loading.
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorInitialize()
        {
            InitializeSystem();
            // EditorApplication.hierarchyChanged += BakeAllInScene;
        }
        #endif

        /// <summary>
        /// Initializes the baking system by discovering bakers and subscribing to scene events.
        /// </summary>
        private static void InitializeSystem()
        {
            if (_initialized) return;

            DiscoverBakers();
            
            // Subscribe to scene loads to automatically bake static level data
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            _initialized = true;
          //  Debug.Log($"[MonoBakingSystem] Initialized: {_bakerCache.Count} bakers cached.");

        }

#endregion

        #region Baker Discovery

        /// <summary>
        /// Scans all loaded assemblies for classes inheriting from MonoBaker and populates the cache.
        /// </summary>
        private static void DiscoverBakers()
        {
            //clear baker cache
            _bakerCache.Clear();

            // Scan all loaded assemblies for any class inheriting from MonoBaker<T>
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsAbstract || type.IsInterface) continue;

                    var baseType = type.BaseType;
                    while (baseType != null)
                    {
                        // Check if the base type is the generic MonoBaker<>
                        if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(MonoBaker<>))
                        {
                            // Extract the generic parameter TAuthoring
                            Type authoringType = baseType.GetGenericArguments()[0];
                            
                            if (!_bakerCache.ContainsKey(authoringType))
                            {
                                // Instantiate the baker once and cache it
                                var bakerInstance = Activator.CreateInstance(type);
                                var bakeMethod = baseType.GetMethod("BakeInternal", BindingFlags.Instance | BindingFlags.NonPublic);
                                
                                _bakerCache.Add(authoringType, bakerInstance);
                            }
                            break;
                        }
                        baseType = baseType.BaseType;
                    }
                }
            }
        }
        /// <summary>
        /// Checks if a type inherits from MonoBaker.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if it is a MonoBaker, otherwise false.</returns>
 private static bool IsMonoBaker(Type type)
        {
            while (type != null)
            {
                if (type.IsGenericType &&type.GetGenericTypeDefinition() == typeof(MonoBaker<>))
                     return true;  
                     type = type.BaseType;   
                    
            }

            return false;
        }

        /// <summary>
        /// Event handler called when a scene is loaded. Triggers baking of all components in the scene.
        /// </summary>
        /// <param name="scene">The loaded scene.</param>
        /// <param name="mode">The load scene mode.</param>
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
          //  Debug.Log($"[MonoBakingSystem] Scene loaded: {scene.name}");
            BakeAllInScene();
        }

        /// <summary>
        /// Finds and bakes all MonoBehaviours in the current hierarchy.
        /// </summary>
        public static void BakeAllInScene()
        {
            // FindObjectsByType ensures we grab all active MonoBehaviours in the hierarchy
            var allComponents = GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var comp in allComponents)
            {
                BakeAuthoring(comp);
            }
        }


        /// <summary>
        /// Bakes a single authoring component if a corresponding baker exists.
        /// </summary>
        /// <param name="authoring">The authoring component to bake.</param>
       public static void BakeAuthoring(MonoBehaviour authoring)
    {
         if (!authoring) return;

         //  Ensure scene is fully loaded
        if (!authoring.gameObject.scene.isLoaded)
        return;


        if (_bakedAuthoring.Contains(authoring))
            {
               //  Object.Destroy(authoring);
            }
        //return;

        Type authoringType = authoring.GetType();

        if (!_bakerCache.TryGetValue(authoringType, out var baker))
        {
             Debug.LogWarning($"[MonoBakingSystem] No baker found for {authoringType.Name}",authoring);
            return;
           
    }

    try
        {
        var bakeMethod = baker.GetType()
            .GetMethod("BakeInternal", BindingFlags.Instance | BindingFlags.NonPublic);

        bakeMethod.Invoke(baker, new object[] { authoring });

      //  _bakedAuthoring.Add(authoring);

       // Debug.Log( $"[MonoBakingSystem] Baked {authoringType.Name}",   authoring);
           
         
        }
    catch (Exception ex)
        {
        Debug.LogError(
            $"[MonoBakingSystem] Failed to bake {authoringType.Name}\n{ex}",
            authoring);
        }
    }   
        #endregion

    

        #region SubSceneDetection

        /// <summary>
        /// Checks if a component belongs to a scene other than the active one.
        /// </summary>
        /// <param name="component">The component to check.</param>
        /// <returns>True if it is in a different scene, otherwise false.</returns>
        private static bool IsSubSceneComponent(MonoBehaviour component)
        {
            // Check if the component is part of a sub-scene
            return component.gameObject.scene.name != SceneManager.GetActiveScene().name;
        }
        /// <summary>
        /// Checks if a component is part of a SubScene using reflection to detect the SubScene component.
        /// </summary>
        /// <param name="component">The component to check.</param>
        /// <returns>True if it is in a SubScene, otherwise false.</returns>
        private static bool IsInSubScene(MonoBehaviour component)
        {

            if (SubSceneType == null)
            return false;

            return component.GetComponentInParent(SubSceneType) != null;

          //return component.gameObject.scene.name.EndsWith(".SubScene");
        }
        
        #endregion
    

        
    }
}
