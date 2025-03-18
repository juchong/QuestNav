using UnityEngine;

namespace QuestNav.Utils
{
    /// <summary>
    /// Extension methods for Unity's Quaternion class to convert to array format.
    /// </summary>
    public static class QuaternionExtensions
    {
        /// <summary>
        /// Converts a Quaternion to a float array containing x, y, z, and w components.
        /// </summary>
        /// <param name="quaternion">The Quaternion to convert</param>
        /// <returns>Float array containing [x, y, z, w] values</returns>
        public static float[] ToArray(this Quaternion quaternion)
        {
            return new float[] { quaternion.x, quaternion.y, quaternion.z, quaternion.w };
        }
    }
}