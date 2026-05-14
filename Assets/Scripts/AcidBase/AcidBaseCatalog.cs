using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace PeriodicAR.AcidBase
{
    [System.Serializable]
    public class SubstanceData
    {
        [JsonProperty("name")]        public string name;
        [JsonProperty("formula")]     public string formula;
        [JsonProperty("type")]        public string type;   // strong_acid | weak_acid | neutral | weak_base | strong_base
        [JsonProperty("ph")]          public float  ph;
        [JsonProperty("color")]       public string color;
        [JsonProperty("description")] public string description;
    }

    [System.Serializable]
    public class IndicatorData
    {
        [JsonProperty("name")]          public string name;
        [JsonProperty("acid_color")]    public string acidColor;
        [JsonProperty("base_color")]    public string baseColor;
        [JsonProperty("neutral_color")] public string neutralColor;
        [JsonProperty("range_low")]     public float  rangeLow;
        [JsonProperty("range_high")]    public float  rangeHigh;
    }

    public static class AcidBaseCatalog
    {
        private static List<SubstanceData>  _substances;
        private static List<IndicatorData>  _indicators;

        private static void EnsureLoaded()
        {
            if (_substances != null) return;
            var asset = Resources.Load<TextAsset>("AcidsBases");
            if (asset == null)
            {
                _substances = new List<SubstanceData>();
                _indicators = new List<IndicatorData>();
                return;
            }
            var root = JsonConvert.DeserializeObject<AcidsRoot>(asset.text);
            _substances = root?.substances ?? new List<SubstanceData>();
            _indicators = root?.indicators ?? new List<IndicatorData>();
        }

        public static List<SubstanceData>  GetSubstances() { EnsureLoaded(); return _substances; }
        public static List<IndicatorData>  GetIndicators()  { EnsureLoaded(); return _indicators; }

        public static Color GetBeakerColor(float ph, string indicatorName = "Universal")
        {
            EnsureLoaded();
            var ind = _indicators.Find(i => string.Equals(i.name, indicatorName, System.StringComparison.OrdinalIgnoreCase));
            if (ind == null && _indicators.Count > 0) ind = _indicators[_indicators.Count - 1];
            if (ind == null) return Color.white;

            if (ph < ind.rangeLow)
            {
                ColorUtility.TryParseHtmlString(ind.acidColor, out var c); return c;
            }
            if (ph > ind.rangeHigh)
            {
                ColorUtility.TryParseHtmlString(ind.baseColor, out var c); return c;
            }
            ColorUtility.TryParseHtmlString(ind.acidColor,    out var ac);
            ColorUtility.TryParseHtmlString(ind.neutralColor, out var nc);
            ColorUtility.TryParseHtmlString(ind.baseColor,    out var bc);
            float t = (ph - ind.rangeLow) / (ind.rangeHigh - ind.rangeLow);
            return t < 0.5f ? Color.Lerp(ac, nc, t * 2f) : Color.Lerp(nc, bc, (t - 0.5f) * 2f);
        }

        public static string ClassifyPh(float ph)
        {
            if (ph < 7f)  return "acidic";
            if (ph > 7f)  return "basic";
            return "neutral";
        }

        private class AcidsRoot
        {
            [JsonProperty("substances")] public List<SubstanceData>  substances;
            [JsonProperty("indicators")] public List<IndicatorData>  indicators;
        }
    }
}
