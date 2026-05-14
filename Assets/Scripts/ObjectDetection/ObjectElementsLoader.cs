using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace PeriodicAR.ObjectDetection
{
    public static class ObjectElementsLoader
    {
        private static Dictionary<string, ObjectComposition> _catalog;

        private static void EnsureLoaded()
        {
            if (_catalog != null) return;

            var asset = Resources.Load<TextAsset>("ObjectElements");
            if (asset == null)
            {
                Debug.LogError("[ObjectElementsLoader] ObjectElements.json not found in Resources/");
                _catalog = new Dictionary<string, ObjectComposition>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            try
            {
                var data = JsonConvert.DeserializeObject<ObjectElementsCatalog>(asset.text);
                _catalog = data?.Objects ?? new Dictionary<string, ObjectComposition>(StringComparer.OrdinalIgnoreCase);
                // Re-wrap with case-insensitive comparer so lookups are tolerant of casing.
                _catalog = new Dictionary<string, ObjectComposition>(_catalog, StringComparer.OrdinalIgnoreCase);
                Debug.Log($"[ObjectElementsLoader] Loaded {_catalog.Count} object-to-element mappings.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ObjectElementsLoader] Parse error: {ex.Message}");
                _catalog = new Dictionary<string, ObjectComposition>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public static ObjectComposition Lookup(string cocoLabel)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(cocoLabel)) return null;
            return _catalog.TryGetValue(cocoLabel, out var comp) ? comp : null;
        }

        public static bool TryLookup(string cocoLabel, out ObjectComposition comp)
        {
            comp = Lookup(cocoLabel);
            return comp != null;
        }
    }
}
