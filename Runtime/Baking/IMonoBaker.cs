using UnityEngine;

namespace Utilities.HybridMono
{
    /// <summary>
    /// Internal abstraction used by the baking system to invoke typed bakers generically.
    /// </summary>
    internal interface IMonoBaker
    {
        /// <summary>
        /// Executes a bake pass for the supplied authoring component.
        /// </summary>
        /// <param name="authoring">The authoring component instance to bake.</param>
        void BakeInternal(MonoBehaviour authoring);
    }
}
