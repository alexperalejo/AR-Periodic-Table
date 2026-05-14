using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace PeriodicAR.HalfLife
{
    [System.Serializable]
    public class IsotopeData
    {
        [JsonProperty("id")]              public string id;
        [JsonProperty("parent_symbol")]   public string parentSymbol;
        [JsonProperty("mass_number")]     public int    massNumber;
        [JsonProperty("half_life_seconds")] public double halfLifeSeconds;
        [JsonProperty("half_life_display")] public string halfLifeDisplay;
        [JsonProperty("decay_mode")]      public string decayMode;
        [JsonProperty("daughter")]        public string daughter;
        [JsonProperty("daughter_color")]  public string daughterColor;
        [JsonProperty("parent_color")]    public string parentColor;
        [JsonProperty("use")]             public string use;
        [JsonProperty("story")]           public string story;
    }

    public static class IsotopeCatalog
    {
        private static List<IsotopeData> _isotopes;

        private static void EnsureLoaded()
        {
            if (_isotopes != null) return;
            var asset = Resources.Load<TextAsset>("Isotopes");
            if (asset == null) { _isotopes = new List<IsotopeData>(); return; }
            var root = JsonConvert.DeserializeObject<IsotopesRoot>(asset.text);
            _isotopes = root?.isotopes ?? new List<IsotopeData>();
        }

        public static List<IsotopeData> GetAll()
        {
            EnsureLoaded();
            return _isotopes;
        }

        public static IsotopeData Find(string id)
        {
            EnsureLoaded();
            return _isotopes.Find(x => x.id == id);
        }

        private class IsotopesRoot
        {
            [JsonProperty("isotopes")]
            public List<IsotopeData> isotopes;
        }
    }
}
