using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PeriodicAR.Spectroscopy
{
    [System.Serializable]
    public class EmissionLine
    {
        public float  wavelength;  // nm
        public float  intensity;   // 0–1
        public string transition;
    }

    public static class EmissionLineCatalog
    {
        private static Dictionary<string, List<EmissionLine>> _data;

        private static void EnsureLoaded()
        {
            if (_data != null) return;
            _data = new Dictionary<string, List<EmissionLine>>(System.StringComparer.OrdinalIgnoreCase);
            var asset = Resources.Load<TextAsset>("EmissionLines");
            if (asset == null) return;

            var root = JObject.Parse(asset.text);
            var elems = root["elements"] as JObject;
            if (elems == null) return;

            foreach (var kv in elems)
            {
                var lines = kv.Value["lines"]?.ToObject<List<EmissionLine>>();
                if (lines != null) _data[kv.Key] = lines;
            }
        }

        public static List<EmissionLine> GetLines(string symbol)
        {
            EnsureLoaded();
            return _data.TryGetValue(symbol, out var list) ? list : null;
        }

        public static bool HasLines(string symbol)
        {
            EnsureLoaded();
            return _data.ContainsKey(symbol);
        }

        /// <summary>Returns wavelength (nm) converted to an sRGB Color (400–700 nm range).</summary>
        public static Color WavelengthToColor(float nm)
        {
            float r, g, b;
            if      (nm >= 380 && nm < 440) { r = -(nm - 440f) / 60f; g = 0f;                    b = 1f; }
            else if (nm >= 440 && nm < 490) { r = 0f;                   g = (nm - 440f) / 50f;    b = 1f; }
            else if (nm >= 490 && nm < 510) { r = 0f;                   g = 1f;                    b = -(nm - 510f) / 20f; }
            else if (nm >= 510 && nm < 580) { r = (nm - 510f) / 70f;   g = 1f;                    b = 0f; }
            else if (nm >= 580 && nm < 645) { r = 1f;                   g = -(nm - 645f) / 65f;   b = 0f; }
            else if (nm >= 645 && nm <= 750){ r = 1f;                   g = 0f;                    b = 0f; }
            else                            { r = 0f; g = 0f; b = 0f; }

            // Dim near the edges of visible range
            float factor = 1f;
            if      (nm < 420) factor = 0.3f + 0.7f * (nm - 380f) / 40f;
            else if (nm > 700) factor = 0.3f + 0.7f * (750f - nm) / 50f;

            return new Color(r * factor, g * factor, b * factor, 1f);
        }
    }
}
