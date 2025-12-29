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
        /// Called before the first scene loads.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var systemTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t =>
                    !t.IsAbstract &&
                    typeof(MonoSystem).IsAssignableFrom(t)
                );

            foreach (var systemType in systemTypes)
            {
                // Singleton will prevent duplicates
                var go = new GameObject(systemType.Name);
                go.AddComponent(systemType);

              //  Debug.Log($"[MonoSystemBootstrap] Created system: {systemType.Name}");
            }
        }
    }
    
}
