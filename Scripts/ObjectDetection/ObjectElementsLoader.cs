// Assets/Scripts/ObjectDetection/ObjectElementsLoader.cs
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PeriodicAR.ObjectDetection
{
    /// <summary>
    /// Loads Resources/ObjectElements.json once and exposes a label -> composition
    /// lookup. Uses Newtonsoft.Json (already in your project via
    /// com.unity.nuget.newtonsoft-json) because JsonUtility cannot deserialize
    /// dictionaries with arbitrary string keys.
    /// </summary>
    public static class ObjectElementsLoader
    {
        private const string ResourcePath = "ObjectElements";
        private static ObjectElementsCatalog _cached;

        public static ObjectElementsCatalog Load()
        {
            if (_cached != null) return _cached;

            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogError($"[ObjectElementsLoader] Resources/{ResourcePath}.json not found!");
                _cached = new ObjectElementsCatalog(new Dictionary<string, ObjectComposition>());
                return _cached;
            }

            var dict = new Dictionary<string, ObjectComposition>();
            try
            {
                JObject root = JObject.Parse(asset.text);
                JObject objs = (JObject)root["objects"];
                if (objs == null)
                {
                    Debug.LogError("[ObjectElementsLoader] JSON missing 'objects' field.");
                }
                else
                {
                    foreach (var kv in objs)
                    {
                        var entry = kv.Value as JObject;
                        if (entry == null) continue;

                        var comp = new ObjectComposition
                        {
                            primary = ToStringArray(entry["primary"]),
                            trace   = ToStringArray(entry["trace"]),
                            note    = entry["note"]?.ToString() ?? string.Empty,
                        };
                        dict[kv.Key] = comp;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ObjectElementsLoader] Parse failed: {e.Message}");
            }

            Debug.Log($"[ObjectElementsLoader] Loaded {dict.Count} object compositions.");
            _cached = new ObjectElementsCatalog(dict);
            return _cached;
        }

        private static string[] ToStringArray(JToken token)
        {
            if (token == null || token.Type != JTokenType.Array) return System.Array.Empty<string>();
            var arr = (JArray)token;
            var result = new string[arr.Count];
            for (int i = 0; i < arr.Count; i++) result[i] = arr[i].ToString();
            return result;
        }
    }
}
