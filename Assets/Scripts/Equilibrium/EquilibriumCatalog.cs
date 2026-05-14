using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace PeriodicAR.Equilibrium
{
    [System.Serializable]
    public class EquilibriumSpecies
    {
        [JsonProperty("symbol")]      public string symbol;
        [JsonProperty("coefficient")] public int    coefficient;
        [JsonProperty("color")]       public string color;
    }

    [System.Serializable]
    public class EquilibriumReaction
    {
        [JsonProperty("id")]             public string id;
        [JsonProperty("name")]           public string name;
        [JsonProperty("equation")]       public string equation;
        [JsonProperty("reactants")]      public List<EquilibriumSpecies> reactants;
        [JsonProperty("products")]       public List<EquilibriumSpecies> products;
        [JsonProperty("Keq_25C")]        public double keq;
        [JsonProperty("delta_H_kJ")]     public float  deltaH;
        [JsonProperty("is_exothermic")]  public bool   isExothermic;
        [JsonProperty("context")]        public string context;
        [JsonProperty("le_chatelier_notes")] public Dictionary<string, string> leChatelier;
    }

    public static class EquilibriumCatalog
    {
        private static List<EquilibriumReaction> _reactions;

        private static void EnsureLoaded()
        {
            if (_reactions != null) return;
            var asset = Resources.Load<TextAsset>("Equilibria");
            if (asset == null) { _reactions = new List<EquilibriumReaction>(); return; }
            var root = JsonConvert.DeserializeObject<EquilibriaRoot>(asset.text);
            _reactions = root?.reactions ?? new List<EquilibriumReaction>();
        }

        public static List<EquilibriumReaction> GetAll() { EnsureLoaded(); return _reactions; }

        public static EquilibriumReaction Find(string id)
        {
            EnsureLoaded();
            return _reactions.Find(r => r.id == id);
        }

        private class EquilibriaRoot
        {
            [JsonProperty("reactions")] public List<EquilibriumReaction> reactions;
        }
    }
}
