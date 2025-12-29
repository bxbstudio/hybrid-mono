using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;


namespace Utilities.HybridMono
{
    /// <summary>
    /// A MonoBehaviour wrapper for a NativeList of IBufferElementData.
    /// Used to bridge MonoBehaviours with ECS Dynamic Buffers.
    /// </summary>
    /// <typeparam name="T">The type of the buffer element, which must be unmanaged and implement IBufferElementData.</typeparam>
    public abstract class MonoBuffer<T> : MonoBehaviour where T : unmanaged, IBufferElementData
    {
        /// <summary>
        /// The underlying NativeList storing the buffer elements.
        /// </summary>
        private NativeList<T> buffer;

        /// <summary>
        /// Gets the current capacity of the buffer.
        /// </summary>
        public int Capacity { get=> buffer.Capacity; }
        /// <summary>
        /// Gets the current number of elements in the buffer.
        /// </summary>
        public int Length { get => buffer.Length; }
        
        /// <summary>
        /// Implicitly converts a MonoBuffer to its underlying NativeList.
        /// </summary>
        /// <param name="monoBuffer">The MonoBuffer to convert.</param>
        /// <returns>The underlying NativeList.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the buffer is not initialized or has been disposed.</exception>
        public static implicit operator NativeList<T>(MonoBuffer<T> monoBuffer)
        {
            if (monoBuffer == null || !monoBuffer.buffer.IsCreated)
                throw new InvalidOperationException("Buffer not initialized or already disposed.");
            return monoBuffer.buffer;
        }

        /// <summary>
        /// Returns the underlying NativeList.
        /// </summary>
        /// <returns>The underlying NativeList.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the buffer is not initialized or has been disposed.</exception>
        public NativeList<T> AsNativeList()
        {
            if (!buffer.IsCreated)
                throw new InvalidOperationException("Buffer not initialized or already disposed.");
            return buffer;
        }

        /// <summary>
        /// Initializes the buffer with a specified capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the buffer.</param>
        public void Initialize(int capacity )
        {
            if (buffer.IsCreated)
                buffer.Dispose();

            buffer = new NativeList<T>(capacity,Allocator.Persistent);
        }

        /// <summary>
        /// Adds an element to the buffer.
        /// </summary>
        /// <param name="element">The element to add.</param>
        /// <exception cref="InvalidOperationException">Thrown if the buffer is not initialized.</exception>
        public void Add(T element)
        {
            if (!buffer.IsCreated)
                throw new InvalidOperationException("Buffer not initialized.");
         
            buffer.Add(element);  
        }

        /// <summary>
        /// Clears all elements from the buffer.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the buffer is not initialized.</exception>
        public void Clear() 
    {
        // check if buffer is created then Reset Length to 0
        if (!buffer.IsCreated)
        throw new InvalidOperationException("Buffer not initialized.");
        buffer.Clear();
    }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the buffer is not initialized.</exception>
        public T this[int index]
        {
            get
            {
                if (!buffer.IsCreated)
                    throw new InvalidOperationException("Buffer not initialized.");
                return buffer[index];
            }
            set
            {
                if (!buffer.IsCreated)
                    throw new InvalidOperationException("Buffer not initialized.");
                buffer[index] = value;
            }
        }

        /// <summary>
        /// Disposes of the underlying NativeList when the MonoBehaviour is destroyed.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (buffer.IsCreated)
                buffer.Dispose();
        }


    }
}
