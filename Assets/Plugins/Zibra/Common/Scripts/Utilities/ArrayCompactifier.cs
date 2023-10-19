using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace com.zibra.common.Utilities
{
    internal static class ArrayCompactifier
    {
        public static string IntToString(this IEnumerable<int> array)
        {
            var byteArray =
                array.SelectMany(BitConverter.GetBytes)
                    .ToArray(); // possible GarbageCollector overload (GetBytes generates new array every call)

            return Convert.ToBase64String(byteArray);
        }

        public static string FloatToString(this IEnumerable<float> array)
        {
            var byteArray =
                array.SelectMany(BitConverter.GetBytes)
                    .ToArray(); // possible GarbageCollector overload (GetBytes generates new array every call)

            return Convert.ToBase64String(byteArray);
        }

        public static byte[] StringToBytes(this string input)
        {
            var bytes = Convert.FromBase64String(input);

            return bytes;
        }

        public static float[] StringToFloat(this string input)
        {
            var bytes = Convert.FromBase64String(input);
            var result = new float[Mathf.CeilToInt(bytes.Length / (float)sizeof(float))];

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = BitConverter.ToSingle(bytes, i * sizeof(float));
            }

            return result;
        }

        public static string Vector3ToString(this IEnumerable<Vector3> array)
        {
            var byteArray =
                array
                    .SelectMany(vec => BitConverter.GetBytes(vec.x)
                                           .Concat(BitConverter.GetBytes(vec.y))
                                           .Concat(BitConverter.GetBytes(vec.z)))
                    .ToArray(); // possible GarbageCollector overload (GetBytes generates new array every call)

            return Convert.ToBase64String(byteArray);
        }
    }
}