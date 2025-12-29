
using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// A Singleton that persists across scene loads.
    /// </summary>
    /// <typeparam name="T">The type of the MonoBehaviour.</typeparam>
    public abstract class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour
    {
        /// <summary>
        /// Ensures the instance persists across scene loads.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (this == Instance)
                DontDestroyOnLoad(gameObject);
        }
    }
}
