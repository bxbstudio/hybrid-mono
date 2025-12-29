using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Base class for baking authoring components into MonoBehaviours.
    /// </summary>
    /// <typeparam name="TAuthoring">The type of the authoring MonoBehaviour.</typeparam>
    public abstract class MonoBaker<TAuthoring> where TAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Gets the authoring component being baked.
        /// </summary>
        protected TAuthoring Authoring { get; private set; }
        /// <summary>
        /// Gets the GameObject associated with the authoring component.
        /// </summary>
        protected GameObject GameObject { get; private set; }


        /// <summary>
        /// Internal method to trigger the baking process.
        /// </summary>
        /// <param name="authoring">The authoring component to bake.</param>
         internal void BakeInternal(TAuthoring authoring)
        {
            Authoring = authoring;
            GameObject = authoring.gameObject;

            Bake(authoring);
        }
        /// <summary>
        /// Abstract method to be implemented by derived bakers to perform the actual baking logic.
        /// </summary>
        /// <param name="authoring">The authoring component to bake.</param>
        public abstract void Bake(TAuthoring authoring);

        #region Protected API
        /// <summary>
        /// Adds a MonoComponent to the GameObject if it doesn't already exist.
        /// </summary>
        /// <typeparam name="T">The type of the MonoComponent.</typeparam>
        /// <typeparam name="TData">The type of the IComponentData.</typeparam>
        /// <returns>The added or existing MonoComponent.</returns>
        protected T AddMonoComponent<T,TData>() where T : MonoComponent<TData> where TData:unmanaged,IComponentData 
        {
            // TODO: Find Alternative to fix OnDidAddComponent Warning When Calling Bake From OnValidate
             if (!GameObject.TryGetComponent(out T component))
                component = GameObject.AddComponent<T>();
            
            return component;
        }

        /// <summary>
        /// Adds a MonoBuffer to the GameObject if it doesn't already exist and initializes it.
        /// </summary>
        /// <typeparam name="TBuffer">The type of the MonoBuffer.</typeparam>
        /// <typeparam name="TElement">The type of the IBufferElementData.</typeparam>
        /// <param name="capacity">The initial capacity of the buffer.</param>
        /// <returns>The added or existing MonoBuffer.</returns>
    protected TBuffer AddMonoBuffer<TBuffer, TElement>(int capacity)
         where TBuffer : MonoBuffer<TElement>
         where TElement : unmanaged, IBufferElementData

         {
           // TBuffer buffer = AddMonoComponent<TBuffer>();
         if (!GameObject.TryGetComponent(out TBuffer buffer))
             buffer = GameObject.AddComponent<TBuffer>();
             buffer.Initialize(capacity);
            return buffer;
         }

        /// <summary>
        /// Gets a component of type T from the GameObject.
        /// </summary>
        /// <typeparam name="T">The type of the component to get.</typeparam>
        /// <returns>The component if found, otherwise null.</returns>
        protected T GetMonoComponent<T>() where T : Component
        {
             return GameObject.GetComponent<T>();
        }

        /// <summary>
        /// Gets a component of type T in the parent GameObjects.
        /// </summary>
        /// <typeparam name="T">The type of the component to get.</typeparam>
        /// <returns>The component if found, otherwise null.</returns>
           protected T GetComponentInParent<T>()
            where T : Component
        {
            return GameObject.GetComponentInParent<T>();
        }
        #endregion Protected API


    }   
}
