using UnityEngine.Scripting;

namespace Utilities.Core
{
    /// <summary>
    /// Behaviour singleton that always persists across scene loads.
    /// </summary>
    [Preserve]
    public abstract class PersistentSingleton<T> : BehaviourSingleton<T> where T : PersistentSingleton<T>
    {
        /// <summary>
        /// Gets the supported singleton instance for the concrete type.
        /// </summary>
        public static T Instance => Default;

        /// <inheritdoc />
        public override bool Persistent => true;
    }
}
