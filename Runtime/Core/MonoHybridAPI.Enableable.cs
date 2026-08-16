using Unity.Entities;
using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Enableable-component helpers for HybridMono GameObject and entity mappings.
    /// </summary>
    public static partial class MonoHybridAPI
    {
        /// <summary>
        /// Gets the enabled state of an enableable component on the GameObject's entity.
        /// </summary>
        /// <typeparam name="T">The enableable component type to read.</typeparam>
        /// <param name="gameObject">The GameObject whose mirrored entity is checked.</param>
        /// <returns>True when the enableable component is enabled.</returns>
        public static bool IsComponentEnabled<T>(GameObject gameObject) where T : unmanaged, IComponentData, IEnableableComponent
        {
            return IsComponentEnabled<T>(GetEntity(gameObject));
        }

        /// <summary>
        /// Sets the enabled state of an enableable component on the GameObject's entity.
        /// </summary>
        /// <typeparam name="T">The enableable component type to update.</typeparam>
        /// <param name="gameObject">The GameObject whose mirrored entity is updated.</param>
        /// <param name="enabled">The enabled state to apply.</param>
        public static void SetComponentEnabled<T>(GameObject gameObject, bool enabled) where T : unmanaged, IComponentData, IEnableableComponent
        {
            SetComponentEnabled<T>(GetEntity(gameObject), enabled);
        }

        /// <summary>
        /// Gets the enabled state of an enableable component on an entity.
        /// </summary>
        /// <typeparam name="T">The enableable component type to read.</typeparam>
        /// <param name="entity">The entity to inspect.</param>
        /// <returns>True when the enableable component is enabled.</returns>
        public static bool IsComponentEnabled<T>(Entity entity) where T : unmanaged, IComponentData, IEnableableComponent
        {
            ThrowIfEntityDoesNotExist(entity);
            return _entityManager.IsComponentEnabled<T>(entity);
        }

        /// <summary>
        /// Sets the enabled state of an enableable component on an entity.
        /// </summary>
        /// <typeparam name="T">The enableable component type to update.</typeparam>
        /// <param name="entity">The entity to update.</param>
        /// <param name="enabled">The enabled state to apply.</param>
        public static void SetComponentEnabled<T>(Entity entity, bool enabled) where T : unmanaged, IComponentData, IEnableableComponent
        {
            ThrowIfEntityDoesNotExist(entity);
            _entityManager.SetComponentEnabled<T>(entity, enabled);
        }
    }
}
