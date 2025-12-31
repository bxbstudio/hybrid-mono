using System;
using System.Linq;
using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Static class responsible for automatically bootstrapping MonoSystems at runtime.
    /// </summary>
    public static class MonoSystemBootstrap
    {
        /// <summary>
        /// Automatically discovers and instantiates all non-abstract classes inheriting from MonoSystem.
        /// Called after scenes load to ensure MonoHybridAPI is initialized first.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Ensure MonoHybridAPI is initialized (should already be from BeforeSceneLoad)
            _ = MonoHybridAPI.World;

            var systemTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t =>
                    !t.IsAbstract &&
                    t != typeof(MonoSystem) &&
                    typeof(MonoSystem).IsAssignableFrom(t)
                );

            foreach (var systemType in systemTypes)
            {
                // Singleton will prevent duplicates
                var go = new GameObject($"[MonoSystem] {systemType.Name}");
                go.AddComponent(systemType);
                Debug.Log($"[MonoSystemBootstrap] Created system: {systemType.Name}");
            }
        }
    }
}
