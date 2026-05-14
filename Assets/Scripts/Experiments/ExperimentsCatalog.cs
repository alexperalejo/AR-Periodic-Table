using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace PeriodicAR.Experiments
{
    [System.Serializable]
    public class ExperimentData
    {
        [JsonProperty("id")]             public string       id;
        [JsonProperty("name")]           public string       name;
        [JsonProperty("scientist")]      public string       scientist;
        [JsonProperty("year")]           public int          year;
        [JsonProperty("elements")]       public List<string> elements;
        [JsonProperty("summary")]        public string       summary;
        [JsonProperty("what_it_proved")] public string       whatItProved;
        [JsonProperty("surprise")]       public string       surprise;
        [JsonProperty("animation_hint")] public string       animationHint;
        [JsonProperty("color")]          public string       color;
    }

    public static class ExperimentsCatalog
    {
        private static List<ExperimentData> _experiments;

        private static void EnsureLoaded()
        {
            if (_experiments != null) return;
            var asset = Resources.Load<TextAsset>("Experiments");
            if (asset == null) { _experiments = new List<ExperimentData>(); return; }
            var root = JsonConvert.DeserializeObject<Root>(asset.text);
            _experiments = root?.experiments ?? new List<ExperimentData>();
        }

        public static List<ExperimentData> GetAll() { EnsureLoaded(); return _experiments; }
        public static ExperimentData Find(string id) { EnsureLoaded(); return _experiments.Find(e => e.id == id); }

        private class Root { [JsonProperty("experiments")] public List<ExperimentData> experiments; }
    }
}
