using System.Collections.Generic;
using Newtonsoft.Json;

namespace PeriodicAR.Reactions
{
    [System.Serializable]
    public class Reaction
    {
        [JsonProperty("reactant_a")]     public string reactantA;
        [JsonProperty("reactant_b")]     public string reactantB;
        [JsonProperty("product_formula")]public string productFormula;
        [JsonProperty("product_name")]   public string productName;
        [JsonProperty("animation_type")] public string animationType;  // electron_transfer | electron_sharing | no_reaction
        [JsonProperty("energy")]         public string energy;          // exothermic | endothermic | none
        [JsonProperty("description")]    public string description;
    }

    public class ReactionCatalogData
    {
        [JsonProperty("reactions")] public List<Reaction> reactions;
    }
}
