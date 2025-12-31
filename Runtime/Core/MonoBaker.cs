using Unity.Entities;
using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Base class for baking authoring components into ECS Entities.
    /// </summary>
    /// <typeparam name="TAuthoring">The type of the authoring MonoBehaviour.</typeparam>
    public abstract class MonoBaker<TAuthoring> where TAuthoring : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Gets the authoring component being baked.
        /// </summary>
        protected TAuthoring Authoring { get; private set; }

        /// <summary>
        /// Gets the GameObject associated with the authoring component.
        /// </summary>
        protected GameObject GameObject { get; private set; }

        /// <summary>
        /// Gets the Entity associated with this GameObject.
        /// Created/retrieved during BakeInternal.
        /// </summary>
        protected Entity Entity { get; private set; }

        /// <summary>
        /// Returns true if this is a rebake (Entity already exists with components).
        /// </summary>
        protected bool IsRebake { get; private set; }
        #endregion

        #region Internal API
        /// <summary>
        /// Internal method to trigger the baking process.
        /// </summary>
        /// <param name="authoring">The authoring component to bake.</param>
        internal void BakeInternal(TAuthoring authoring)
        {
            Authoring = authoring;
            GameObject = authoring.gameObject;

            // Register or get existing entity
            bool wasRegistered = MonoHybridAPI.IsRegistered(GameObject);
            Entity = MonoHybridAPI.RegisterGameObject(GameObject);
            IsRebake = wasRegistered;

            Bake(authoring);
        }
        #endregion

        #region Abstract API
        /// <summary>
        /// Abstract method to be implemented by derived bakers to perform the actual baking logic.
        /// </summary>
        /// <param name="authoring">The authoring component to bake.</param>
        public abstract void Bake(TAuthoring authoring);
        #endregion

        #region Component API
        /// <summary>
        /// Adds or updates an IComponentData on the Entity.
        /// If the component already exists, it will be updated (not duplicated).
        /// </summary>
        /// <typeparam name="T">The type of the IComponentData.</typeparam>
        /// <param name="data">The component data to add or set.</param>
        protected void AddComponent<T>(T data) where T : unmanaged, IComponentData
        {
            if (MonoHybridAPI.EntityManager.HasComponent<T>(Entity))
            {
                MonoHybridAPI.EntityManager.SetComponentData(Entity, data);
            }
            else
            {
                MonoHybridAPI.EntityManager.AddComponentData(Entity, data);
            }
        }

        /// <summary>
        /// Sets component data on the Entity.
        /// The component must already exist.
        /// </summary>
        /// <typeparam name="T">The type of the IComponentData.</typeparam>
        /// <param name="data">The component data to set.</param>
        protected void SetComponent<T>(T data) where T : unmanaged, IComponentData
        {
            MonoHybridAPI.EntityManager.SetComponentData(Entity, data);
        }

        /// <summary>
        /// Checks if the Entity has a specific component.
        /// </summary>
        /// <typeparam name="T">The type of the IComponentData.</typeparam>
        /// <returns>True if the component exists.</returns>
        protected bool HasComponent<T>() where T : unmanaged, IComponentData
        {
            return MonoHybridAPI.EntityManager.HasComponent<T>(Entity);
        }

        /// <summary>
        /// Gets component data from the Entity.
        /// </summary>
        /// <typeparam name="T">The type of the IComponentData.</typeparam>
        /// <returns>The component data.</returns>
        protected T GetComponent<T>() where T : unmanaged, IComponentData
        {
            return MonoHybridAPI.EntityManager.GetComponentData<T>(Entity);
        }

        /// <summary>
        /// Removes a component from the Entity if present.
        /// </summary>
        /// <typeparam name="T">The type of the IComponentData.</typeparam>
        protected void RemoveComponent<T>() where T : unmanaged, IComponentData
        {
            if (MonoHybridAPI.EntityManager.HasComponent<T>(Entity))
            {
                MonoHybridAPI.EntityManager.RemoveComponent<T>(Entity);
            }
        }
        #endregion

        #region Buffer API
        /// <summary>
        /// Adds or gets a DynamicBuffer on the Entity.
        /// If the buffer already exists, returns the existing buffer (data preserved).
        /// </summary>
        /// <typeparam name="T">The type of the IBufferElementData.</typeparam>
        /// <returns>The DynamicBuffer.</returns>
        protected DynamicBuffer<T> AddBuffer<T>() where T : unmanaged, IBufferElementData
        {
            if (MonoHybridAPI.EntityManager.HasBuffer<T>(Entity))
            {
                return MonoHybridAPI.EntityManager.GetBuffer<T>(Entity);
            }
            return MonoHybridAPI.EntityManager.AddBuffer<T>(Entity);
        }

        /// <summary>
        /// Adds or gets a DynamicBuffer with specified capacity.
        /// </summary>
        /// <typeparam name="T">The type of the IBufferElementData.</typeparam>
        /// <param name="capacity">The initial capacity.</param>
        /// <returns>The DynamicBuffer.</returns>
        protected DynamicBuffer<T> AddBuffer<T>(int capacity) where T : unmanaged, IBufferElementData
        {
            DynamicBuffer<T> buffer = AddBuffer<T>();
            buffer.EnsureCapacity(capacity);
            return buffer;
        }

        /// <summary>
        /// Gets a DynamicBuffer from the Entity.
        /// The buffer must already exist.
        /// </summary>
        /// <typeparam name="T">The type of the IBufferElementData.</typeparam>
        /// <returns>The DynamicBuffer.</returns>
        protected DynamicBuffer<T> GetBuffer<T>() where T : unmanaged, IBufferElementData
        {
            return MonoHybridAPI.EntityManager.GetBuffer<T>(Entity);
        }

        /// <summary>
        /// Checks if the Entity has a specific buffer.
        /// </summary>
        /// <typeparam name="T">The type of the IBufferElementData.</typeparam>
        /// <returns>True if the buffer exists.</returns>
        protected bool HasBuffer<T>() where T : unmanaged, IBufferElementData
        {
            return MonoHybridAPI.EntityManager.HasBuffer<T>(Entity);
        }

        /// <summary>
        /// Removes a buffer from the Entity if present.
        /// </summary>
        /// <typeparam name="T">The type of the IBufferElementData.</typeparam>
        protected void RemoveBuffer<T>() where T : unmanaged, IBufferElementData
        {
            if (MonoHybridAPI.EntityManager.HasBuffer<T>(Entity))
            {
                MonoHybridAPI.EntityManager.RemoveComponent<T>(Entity);
            }
        }
        #endregion

        #region Utility API
        /// <summary>
        /// Gets a component from the authoring GameObject.
        /// </summary>
        /// <typeparam name="T">The type of the UnityEngine.Component.</typeparam>
        /// <returns>The component if found, otherwise null.</returns>
        protected T GetMonoComponent<T>() where T : Component
        {
            return GameObject.GetComponent<T>();
        }

        /// <summary>
        /// Gets a component in the parent GameObjects.
        /// </summary>
        /// <typeparam name="T">The type of the UnityEngine.Component.</typeparam>
        /// <returns>The component if found, otherwise null.</returns>
        protected T GetComponentInParent<T>() where T : Component
        {
            return GameObject.GetComponentInParent<T>();
        }

        /// <summary>
        /// Gets a component in the child GameObjects.
        /// </summary>
        /// <typeparam name="T">The type of the UnityEngine.Component.</typeparam>
        /// <returns>The component if found, otherwise null.</returns>
        protected T GetComponentInChildren<T>() where T : Component
        {
            return GameObject.GetComponentInChildren<T>();
        }
        #endregion
    }
}
