using UnityEngine;
using UnityEngine.InputSystem;

namespace AshTaiko
{
    /// <summary>
    /// Provides centralized debugging functionality for the game.
    /// Offers a single point of control for debug output and logging.
    /// </summary>
    public class GameDebug
    {
        /// <summary>
        /// Logs an error message using Unity's debug system.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        public static void LogError(object message)
        {
            Debug.LogError(message);
        }
    }
}
