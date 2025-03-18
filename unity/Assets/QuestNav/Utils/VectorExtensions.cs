using UnityEngine;

namespace QuestNav.Utils
{
    /// <summary>
    /// Extension methods for Unity's Vector3 class to convert to array format.
    /// </summary>
    public static class VectorExtensions
    {
        /// <summary>
        /// Converts a Vector3 to a float array containing x, y, and z components.
        /// </summary>
        /// <param name="vector">The Vector3 to convert</param>
        /// <returns>Float array containing [x, y, z] values</returns>
        public static float[] ToArray(this Vector3 vector)
        {
            return new float[] { vector.x, vector.y, vector.z };
        }
    }
}