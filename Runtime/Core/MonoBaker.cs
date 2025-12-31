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
    }
}
