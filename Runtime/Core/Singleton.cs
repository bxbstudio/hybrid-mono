using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Base class for implementing the Singleton pattern for MonoBehaviours.
    /// </summary>
    /// <typeparam name="T">The type of the MonoBehaviour.</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        private static T instance;
        /// <summary>
        /// Gets the singleton instance. If it doesn't exist, it attempts to find it in the scene or creates a new one.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<T>();
                    if (instance == null)
                    {
                        var go = new GameObject(typeof(T).Name);
                        instance = go.AddComponent<T>();
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Ensures only one instance of the singleton exists in the scene.
        /// </summary>
        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this as T;
        }
    }
}
