using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace PeriodicAR.CompoundScanner
{
    [System.Serializable]
    public class CompoundEntry
    {
        [JsonProperty("formula")]    public string       formula;
        [JsonProperty("name")]       public string       name;
        [JsonProperty("elements")]   public List<string> elements;
        [JsonProperty("common_in")]  public string       commonIn;
    }

    public static class CompoundDatabase
    {
        private static List<CompoundEntry> _compounds;

        private static void EnsureLoaded()
        {
            if (_compounds != null) return;
            var asset = Resources.Load<TextAsset>("Compounds");
            if (asset == null) { _compounds = new List<CompoundEntry>(); return; }
            var root = JsonConvert.DeserializeObject<Root>(asset.text);
            _compounds = root?.compounds ?? new List<CompoundEntry>();
        }

        public static CompoundEntry Find(string formula)
        {
            EnsureLoaded();
            return _compounds.Find(c =>
                string.Equals(c.formula, formula, System.StringComparison.OrdinalIgnoreCase));
        }

        public static List<CompoundEntry> FindByElement(string symbol)
        {
            EnsureLoaded();
            return _compounds.FindAll(c => c.elements != null && c.elements.Contains(symbol));
        }

        private class Root { [JsonProperty("compounds")] public List<CompoundEntry> compounds; }
    }
}
