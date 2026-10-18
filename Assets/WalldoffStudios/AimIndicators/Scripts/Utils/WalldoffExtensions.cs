using UnityEngine;

namespace WalldoffStudios.Extensions
{
    public static class WalldoffExtensions
    {
        public static Vector3 With(this Vector3 original, float? x = null, float? y = null, float? z = null)
        {
            Vector3 newVector;
            newVector.x = x ?? original.x;
            newVector.y = y ?? original.y;
            newVector.z = z ?? original.z;

            return newVector;
        }

        public static Vector3 Flat(this Vector3 original)
        {
            Vector3 flattened;
            flattened.x = original.x;
            flattened.y = 0;
            flattened.z = original.z;

            return flattened;
        }

        public static Vector3 NormalizedDirection(this Vector3 source, Vector3 destination)
        {
            Vector3 direction = destination - source;
            return direction.normalized;
        }

        public static Vector3 ToVector4(this Vector3 original)
        {
            Vector4 converted;
            converted.x = original.x;
            converted.y = original.y;
            converted.z = original.z;
            converted.w = 0;

            return converted;
        }

        public static Vector4[] ToVector4Array(this Vector3[] original)
        {
            int amount = original.Length;
            Vector4[] convertedArray = new Vector4[amount];
            for (int i = 0; i < amount; i++)
            {
                convertedArray[i] = original[i].ToVector4();
            }

            return convertedArray;
        }

        public static Transform ClearChildren(this Transform transform)
        {
            #if UNITY_EDITOR
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(transform.GetChild(i).gameObject);
            }
            #endif
            #if !UNITY_EDITOR
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
            #endif
            
            return transform;
        }

    }
}