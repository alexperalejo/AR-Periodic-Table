using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PeriodicAR.BondBuilder
{
    [System.Serializable]
    public class MolAtom
    {
        [JsonProperty("symbol")]   public string  symbol;
        [JsonProperty("position")] public float[] position;
    }

    [System.Serializable]
    public class MoleculeRecipe
    {
        public string         id;
        public string         name;
        public string         formula;
        public string         geometry;
        public float          bondAngle;
        public List<MolAtom>  atoms        = new();
        public List<int[]>    bonds        = new();
        public List<string>   bondTypes    = new();
        public string         polarity;
        public List<int>      formalCharges = new();
        public string         story;
    }

    public static class MoleculeRecipeCatalog
    {
        private static List<MoleculeRecipe> _molecules;

        private static void EnsureLoaded()
        {
            if (_molecules != null) return;
            _molecules = new List<MoleculeRecipe>();
            var asset = Resources.Load<TextAsset>("MoleculeRecipes");
            if (asset == null) return;

            var root = JObject.Parse(asset.text);
            if (root["molecules"] is not JArray arr) return;

            foreach (var tok in arr)
            {
                var mol = new MoleculeRecipe
                {
                    id            = tok["id"]?.ToString(),
                    name          = tok["name"]?.ToString(),
                    formula       = tok["formula"]?.ToString(),
                    geometry      = tok["geometry"]?.ToString(),
                    bondAngle     = tok["bond_angle"]?.Value<float>() ?? 0f,
                    polarity      = tok["polarity"]?.ToString(),
                    story         = tok["story"]?.ToString(),
                    atoms         = tok["atoms"]?.ToObject<List<MolAtom>>()       ?? new(),
                    bondTypes     = tok["bond_types"]?.ToObject<List<string>>()   ?? new(),
                    formalCharges = tok["formal_charges"]?.ToObject<List<int>>()  ?? new(),
                };
                if (tok["bonds"] is JArray ba)
                    foreach (var b in ba) mol.bonds.Add(b.ToObject<int[]>());
                _molecules.Add(mol);
            }
        }

        public static List<MoleculeRecipe> GetAll() { EnsureLoaded(); return _molecules; }
        public static MoleculeRecipe Find(string id) { EnsureLoaded(); return _molecules.Find(m => m.id == id); }
    }
}
