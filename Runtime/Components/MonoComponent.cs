using Unity.Entities;
using UnityEngine;


namespace Utilities.HybridMono
{

    /// <summary>
    /// Base MonoBehaviour wrapper for ECS IComponentData.
    /// Allows ECS-style components to live on GameObjects.
    /// </summary>
    /// <typeparam name="T">ECS component type</typeparam>

    public abstract class MonoComponent<T> : MonoBehaviour where T : unmanaged, IComponentData
    {
        /// <summary>
        /// Stored ECS component data.
        /// Visible and editable in the Inspector.
        /// </summary>
        public T data;

        /// <summary>
        /// Implicit conversion to ECS component type.
        /// </summary>
        public static implicit operator T(MonoComponent<T> component) => component.data;

        /// <summary>
        /// Reference access to underlying data (for jobs / systems).
        /// </summary>
        public ref T DataRef => ref data;



    }


}
