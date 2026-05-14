using System.Collections.Generic;
using Newtonsoft.Json;

namespace PeriodicAR.Origins
{
    public class OriginEntry
    {
        [JsonProperty("source")] public string source;
        [JsonProperty("mix")]    public List<string> mix;
        [JsonProperty("story")]  public string story;
    }

    public class OriginCategory
    {
        [JsonProperty("label")] public string label;
        [JsonProperty("color")] public string color; // hex e.g. "#6E3CB8"
    }

    public class ElementOriginsFile
    {
        [JsonProperty("_categories")] public Dictionary<string, OriginCategory> categories;
        [JsonProperty("origins")]     public Dictionary<string, OriginEntry>    origins;
    }
}
