using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace PeriodicAR.Abundance
{
    public static class AbundanceCatalog
    {
        private static Dictionary<string, Dictionary<string, float>> _contexts;

        private static void EnsureLoaded()
        {
            if (_contexts != null) return;
            var asset = Resources.Load<TextAsset>("Abundances");
            if (asset == null) { _contexts = new(); return; }
            var root = JsonConvert.DeserializeObject<AbundancesRoot>(asset.text);
            _contexts = new Dictionary<string, Dictionary<string, float>>();
            if (root?.contexts == null) return;
            foreach (var kv in root.contexts)
            {
                var flat = new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase);
                foreach (var kv2 in kv.Value)
                {
                    if (kv2.Key == "label") continue;
                    if (float.TryParse(kv2.Value.ToString(), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float v))
                        flat[kv2.Key] = v;
                }
                _contexts[kv.Key] = flat;
            }
        }

        public static List<string> GetContextIds()
        {
            EnsureLoaded();
            return new List<string>(_contexts.Keys);
        }

        public static float GetAbundance(string contextId, string elementSymbol)
        {
            EnsureLoaded();
            if (_contexts.TryGetValue(contextId, out var dict) &&
                dict.TryGetValue(elementSymbol, out float v))
                return v;
            return 0f;
        }

        public static (float min, float max) GetLogRange(string contextId)
        {
            EnsureLoaded();
            if (!_contexts.TryGetValue(contextId, out var dict)) return (0f, 1f);
            float logMin =  float.MaxValue;
            float logMax = -float.MaxValue;
            foreach (var v in dict.Values)
            {
                if (v <= 0f) continue;
                float lv = Mathf.Log10(v);
                if (lv < logMin) logMin = lv;
                if (lv > logMax) logMax = lv;
            }
            return logMin > logMax ? (0f, 1f) : (logMin, logMax);
        }

        // ---- JSON types -------------------------------------------------------

        private class AbundancesRoot
        {
            [JsonProperty("contexts")]
            public Dictionary<string, Dictionary<string, object>> contexts;
        }
    }
}
