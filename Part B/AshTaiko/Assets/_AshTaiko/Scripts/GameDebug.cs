using UnityEngine;
using UnityEngine.InputSystem;

namespace AshTaiko
{
    public class GameDebug
    {
        public static void LogError(object message)
        {
            Debug.LogError(message);
        }
    }
}
