using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Discovers and creates typed HybridMono systems.
    /// Use the lookup methods here instead of direct singleton access.
    /// </summary>
    public static class MonoSystemBootstrap
    {
        /// <summary>
        /// Tracks the active runtime system instance for each concrete system type.
        /// </summary>
        private static readonly Dictionary<Type, Component> Systems = new();

        /// <summary>
        /// Tracks whether the bootstrap pass has already discovered and created systems.
        /// </summary>
        private static bool _bootstrapped;

        /// <summary>
        /// Clears cached system state on subsystem registration.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Systems.Clear();
            _bootstrapped = false;
        }

        /// <summary>
        /// Boots HybridMono systems after scenes have loaded.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureBootstrapped();
        }

        /// <summary>
        /// Returns the typed HybridMono system singleton.
        /// </summary>
        /// <typeparam name="T">The concrete system type to retrieve.</typeparam>
        /// <returns>The active system instance for the requested type.</returns>
        public static T GetSystem<T>() where T : MonoSystem<T>
        {
            if (TryGetSystem(out T system))
                return system;

            throw new InvalidOperationException($"HybridMono system '{typeof(T).Name}' is not available.");
        }

        /// <summary>
        /// Tries to get the typed HybridMono system singleton.
        /// </summary>
        /// <typeparam name="T">The concrete system type to retrieve.</typeparam>
        /// <param name="system">The active system instance when one exists.</param>
        /// <returns>True when the requested system type is available.</returns>
        public static bool TryGetSystem<T>(out T system) where T : MonoSystem<T>
        {
            EnsureBootstrapped();

            if (Systems.TryGetValue(typeof(T), out Component component) && component is T typedSystem)
            {
                system = typedSystem;
                return true;
            }

            component = EnsureSystemInstance(typeof(T));

            if (component is T createdSystem)
            {
                system = createdSystem;
                return true;
            }

            system = null;
            return false;
        }

        /// <summary>
        /// Returns true when the typed HybridMono system singleton exists.
        /// </summary>
        /// <typeparam name="T">The concrete system type to test.</typeparam>
        /// <returns>True when the requested system type is available.</returns>
        public static bool HasSystem<T>() where T : MonoSystem<T>
        {
            return TryGetSystem(out T _);
        }

        /// <summary>
        /// Registers an active system instance for its concrete runtime type.
        /// </summary>
        /// <param name="system">The system instance to cache.</param>
        internal static void RegisterSystem(Component system)
        {
            if (system == null)
                return;

            Systems[system.GetType()] = system;
        }

        /// <summary>
        /// Removes a system instance from the cache if it matches the active registration.
        /// </summary>
        /// <param name="system">The system instance to unregister.</param>
        internal static void UnregisterSystem(Component system)
        {
            if (system == null)
                return;

            Type systemType = system.GetType();

            if (Systems.TryGetValue(systemType, out Component currentSystem) && currentSystem == system)
                Systems.Remove(systemType);
        }

        /// <summary>
        /// Ensures the HybridMono world, initial bake, and runtime systems are all available.
        /// </summary>
        private static void EnsureBootstrapped()
        {
            if (_bootstrapped)
            {
                RegisterExistingSystems();
                return;
            }

            _ = MonoHybridAPI.World;
            MonoBakingSystem.EnsureInitialBakeCompleted();

            // Existing scene objects should win so bootstrap-created objects only fill true gaps.
            RegisterExistingSystems();

            foreach (Type systemType in DiscoverSystemTypes())
                EnsureSystemInstance(systemType);

            RegisterExistingSystems();
            _bootstrapped = true;
        }

        /// <summary>
        /// Discovers all concrete runtime system types loaded in the current AppDomain.
        /// </summary>
        /// <returns>A distinct set of concrete <see cref="MonoSystem{T}"/> implementations.</returns>
        private static IEnumerable<Type> DiscoverSystemTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetTypesSafely)
                .Where(IsConcreteMonoSystemType)
                .Distinct();
        }

        /// <summary>
        /// Reads all loadable types from an assembly without failing the full bootstrap on partial load errors.
        /// </summary>
        /// <param name="assembly">The assembly to inspect.</param>
        /// <returns>All successfully resolved types from the assembly.</returns>
        private static Type[] GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null).ToArray();
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        /// <summary>
        /// Returns true when a type is a concrete <see cref="MonoSystem{T}"/> implementation.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns>True when the type should be instantiated by the bootstrap.</returns>
        private static bool IsConcreteMonoSystemType(Type type)
        {
            if (type == null || type.IsAbstract || !typeof(Component).IsAssignableFrom(type) || type.IsGenericTypeDefinition)
                return false;

            for (Type baseType = type; baseType != null; baseType = baseType.BaseType)
            {
                if (!baseType.IsGenericType)
                    continue;

                if (baseType.GetGenericTypeDefinition() == typeof(MonoSystem<>))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets or creates the runtime instance for a concrete system type.
        /// </summary>
        /// <param name="systemType">The concrete system type to resolve.</param>
        /// <returns>The registered or newly created system instance.</returns>
        private static Component EnsureSystemInstance(Type systemType)
        {
            if (Systems.TryGetValue(systemType, out Component registeredSystem) && registeredSystem != null)
                return registeredSystem;

            Component existingSystem = FindExistingSystem(systemType);

            if (existingSystem != null)
            {
                RegisterSystem(existingSystem);
                return existingSystem;
            }

            GameObject gameObject = new($"[MonoSystem] {systemType.Name}");
            Component createdSystem = gameObject.AddComponent(systemType);
            RegisterSystem(createdSystem);
            return createdSystem;
        }

        /// <summary>
        /// Registers any system instances that were already present in loaded scenes.
        /// </summary>
        private static void RegisterExistingSystems()
        {
            foreach (Type systemType in DiscoverSystemTypes())
            {
                Component existingSystem = FindExistingSystem(systemType);

                if (existingSystem != null)
                    RegisterSystem(existingSystem);
            }
        }

        /// <summary>
        /// Finds an existing runtime instance for the supplied system type.
        /// </summary>
        /// <param name="systemType">The concrete system type to search for.</param>
        /// <returns>The first matching runtime instance, or null when none exists.</returns>
        private static Component FindExistingSystem(Type systemType)
        {
            UnityEngine.Object[] objects = Resources.FindObjectsOfTypeAll(systemType);

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] is not Component component)
                    continue;

                if (!IsRuntimeInstance(component))
                    continue;

                return component;
            }

            return null;
        }

        /// <summary>
        /// Returns true when the component belongs to a valid runtime scene instance.
        /// </summary>
        /// <param name="component">The component to validate.</param>
        /// <returns>True when the component belongs to a live scene object.</returns>
        private static bool IsRuntimeInstance(Component component)
        {
            return component != null && component.gameObject.scene.IsValid();
        }
    }
}
