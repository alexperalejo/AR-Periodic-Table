using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace PeriodicAR.Safety
{
    [System.Serializable]
    public class DangerousCombo
    {
        [JsonProperty("id")]               public string       id;
        [JsonProperty("ingredient_a")]     public string       ingredientA;
        [JsonProperty("ingredient_b")]     public string       ingredientB;
        [JsonProperty("reaction")]         public string       reaction;
        [JsonProperty("hazard")]           public string       hazard;
        [JsonProperty("hazard_color")]     public string       hazardColor;
        [JsonProperty("products")]         public List<string> products;
        [JsonProperty("effects")]          public string       effects;
        [JsonProperty("common_scenario")]  public string       commonScenario;
        [JsonProperty("advice")]           public string       advice;
    }

    public static class SafetyCatalog
    {
        private static List<DangerousCombo> _combos;

        private static void EnsureLoaded()
        {
            if (_combos != null) return;
            var asset = Resources.Load<TextAsset>("DangerousCombos");
            if (asset == null) { _combos = new List<DangerousCombo>(); return; }
            var root = JsonConvert.DeserializeObject<Root>(asset.text);
            _combos = root?.combos ?? new List<DangerousCombo>();
        }

        public static List<DangerousCombo> GetAll() { EnsureLoaded(); return _combos; }
        public static DangerousCombo Find(string id) { EnsureLoaded(); return _combos.Find(c => c.id == id); }

        private class Root { [JsonProperty("combos")] public List<DangerousCombo> combos; }
    }
}
