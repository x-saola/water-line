using System;

namespace Delta.Modules.SaveSystem
{
    /// <summary>
    /// Interface for objects whose state can be saved and restored.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// A unique identifier for this saveable object.
        /// </summary>
        string SaveId { get; }

        /// <summary>
        /// Capture the current state for saving.
        /// </summary>
        object CaptureState();

        /// <summary>
        /// Restore the state from a previously saved object.
        /// </summary>
        /// <param name="state">The saved state object.</param>
        void RestoreState(object state);
    }
}
