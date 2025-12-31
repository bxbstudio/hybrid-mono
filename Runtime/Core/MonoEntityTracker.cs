using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Lightweight tracker component that unregisters the GameObject's Entity
    /// when the GameObject is destroyed.
    /// Added automatically by MonoHybridAPI.RegisterGameObject().
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")] // Hide from Add Component menu
    internal sealed class MonoEntityTracker : MonoBehaviour
    {
        /// <summary>
        /// Called when the GameObject is destroyed.
        /// Unregisters the Entity from MonoHybridAPI.
        /// </summary>
        private void OnDestroy()
        {
            // Check if we're exiting play mode or quitting application
            // to avoid unnecessary cleanup during shutdown
            if (!MonoHybridAPI.IsInitialized)
                return;

            MonoHybridAPI.UnregisterGameObject(gameObject);
        }

        /// <summary>
        /// Hide this component in the inspector on reset.
        /// </summary>
        private void Reset()
        {
            hideFlags = HideFlags.HideInInspector;
        }
    }
}
