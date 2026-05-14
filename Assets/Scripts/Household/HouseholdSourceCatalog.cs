using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace PeriodicAR.Household
{
    [Serializable]
    public class HouseholdEntry
    {
        [JsonProperty("items")]    public List<string> items;
        [JsonProperty("fun_fact")] public string funFact;
    }

    public static class HouseholdSourceCatalog
    {
        private static Dictionary<string, HouseholdEntry> _data;

        public static HouseholdEntry Lookup(string symbol)
        {
            EnsureLoaded();
            _data.TryGetValue(symbol, out var entry);
            return entry;
        }

        private static void EnsureLoaded()
        {
            if (_data != null) return;
            _data = new Dictionary<string, HouseholdEntry>(StringComparer.OrdinalIgnoreCase);

            var asset = Resources.Load<TextAsset>("ElementHouseholdSources");
            if (asset == null)
            {
                Debug.LogError("[HouseholdSourceCatalog] ElementHouseholdSources.json not found.");
                return;
            }

            var root = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, HouseholdEntry>>>(asset.text);
            if (root != null && root.TryGetValue("elements", out var elements))
                _data = elements;
        }
    }
}
