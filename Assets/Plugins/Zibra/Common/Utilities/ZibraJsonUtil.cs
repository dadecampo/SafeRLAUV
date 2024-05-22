
using System;
using UnityEngine;

namespace com.zibra.common.Utilities
{
    /// <summary>
    ///     Wrapper for Unity's JsonUtility that can handle arrays.
    /// </summary>
    static class ZibraJsonUtil
    {
        const string WRAPPER_PREFIX = "{\"data\":";
        const string WRAPPER_SUFFIX = "}";

        /// <summary>
        ///     Equivalent to JsonUtility.FromJson<T>(json), but can handle arrays.
        /// </summary>
        public static T FromJson<T>(string json)
        {
            if (typeof(T).IsArray)
            {
                Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(WRAPPER_PREFIX + json + WRAPPER_SUFFIX);
                return wrapper.data;
            }
            else
            {
                return JsonUtility.FromJson<T>(json);
            }
        }

        /// <summary>
        ///     Equivalent to JsonUtility.ToJson(obj), but can handle arrays.
        /// </summary>
        public static string ToJson<T>(T obj)
        {
            if (typeof(T).IsArray)
            {
                Wrapper<T> wrapper = new Wrapper<T>();
                wrapper.data = obj;
                string json = JsonUtility.ToJson(wrapper);
                json = json.Substring(WRAPPER_PREFIX.Length, json.Length - WRAPPER_PREFIX.Length - WRAPPER_SUFFIX.Length);
                return json;
            }
            else
            {
                return JsonUtility.ToJson(obj);
            }
        }

        [Serializable]
        struct Wrapper<T>
        {
            public T data;
        }
    }
}