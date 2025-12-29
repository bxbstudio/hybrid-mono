using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Base class for MonoAuthoring components that trigger baking in the editor.
    /// </summary>
    public abstract class MonoAuthoring : MonoBehaviour
    {   
        /// <summary>
        /// Called when the script is loaded or a value is changed in the inspector.
        /// Triggers the baking process for this authoring component.
        /// </summary>
        protected  void OnValidate()
        {
            MonoBakingSystem.BakeAuthoring(this);
           // Debug.Log($"[MonoAuthoring] OnValidate: {name}");
        }
        
    }
}