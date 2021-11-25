using UnityEngine;

namespace TeammateRevive.Common
{
    // source: https://answers.unity.com/questions/13840/how-to-detect-if-a-gameobject-has-been-destroyed.html
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Checks if a GameObject has been destroyed.
        /// </summary>
        /// <param name="gameObject">GameObject reference to check for destructedness</param>
        /// <returns>If the game object has been marked as destroyed by UnityEngine</returns>
        public static bool IsDestroyed(this GameObject gameObject)
        {
            // UnityEngine overloads the == operator for the GameObject type
            // and returns null when the object has been destroyed, but 
            // actually the object is still there but has not been cleaned up yet
            // if we test both we can determine if the object has been destroyed.
            return gameObject == null && !ReferenceEquals(gameObject, null);
        }

        public static T AddIfMissing<T>(this GameObject gameObject) where T : Component
        {
            var existing = gameObject.GetComponent<T>();
            if (existing) return existing;
            return gameObject.AddComponent<T>();
        }
    }
}