using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PeriodicAR.Phase
{
    [System.Serializable]
    public class PhasePoint { public float T; public float P; }

    [System.Serializable]
    public class PhaseBoundaries
    {
        public List<PhasePoint> solid_liquid;
        public List<PhasePoint> liquid_gas;
        public List<PhasePoint> solid_gas;
    }

    [System.Serializable]
    public class SubstancePhaseDiagram
    {
        public string name;
        public string formula;
        [JsonProperty("triple_point")]    public PhasePoint triplePoint;
        [JsonProperty("critical_point")]  public PhasePoint criticalPoint;
        [JsonProperty("melting_at_1atm")] public float meltingAt1Atm;
        [JsonProperty("boiling_at_1atm")] public float boilingAt1Atm;
        [JsonProperty("ice_floats")]      public bool   iceFloats;
        [JsonProperty("molecule_color")]  public string moleculeColor;
        [JsonProperty("molecule_radius")] public float  moleculeRadius;
        [JsonProperty("boundaries")]      public PhaseBoundaries boundaries;
    }

    public enum PhaseMode { Solid, Liquid, Gas, Supercritical }

    public static class PhaseDiagramData
    {
        private static Dictionary<string, SubstancePhaseDiagram> _data;

        private static void EnsureLoaded()
        {
            if (_data != null) return;
            _data = new Dictionary<string, SubstancePhaseDiagram>();
            var asset = Resources.Load<TextAsset>("PhaseDiagrams");
            if (asset == null) return;

            var root = JObject.Parse(asset.text);
            var subs = root["substances"] as JObject;
            if (subs == null) return;

            foreach (var kv in subs)
            {
                var sub = kv.Value.ToObject<SubstancePhaseDiagram>();
                if (sub != null) _data[kv.Key] = sub;
            }
        }

        public static List<string> GetKeys()
        {
            EnsureLoaded();
            return new List<string>(_data.Keys);
        }

        public static SubstancePhaseDiagram Get(string key)
        {
            EnsureLoaded();
            return _data.TryGetValue(key, out var v) ? v : null;
        }

        /// <summary>Classifies current phase given T (Kelvin) and P (atm).</summary>
        public static PhaseMode Classify(SubstancePhaseDiagram sub, float T, float P)
        {
            if (sub == null) return PhaseMode.Gas;

            // Above critical point
            if (T >= sub.criticalPoint.T && P >= sub.criticalPoint.P)
                return PhaseMode.Supercritical;

            // Above critical temp regardless of pressure → supercritical fluid
            if (T > sub.criticalPoint.T) return PhaseMode.Supercritical;

            // Determine if solid or not: T below melting curve
            float meltT = InterpolateMeltTemp(sub, P);
            if (T < meltT) return PhaseMode.Solid;

            // Determine if gas or not: T above boiling curve
            float boilT = InterpolateBoilTemp(sub, P);
            if (T > boilT) return PhaseMode.Gas;

            return PhaseMode.Liquid;
        }

        private static float InterpolateMeltTemp(SubstancePhaseDiagram sub, float P)
        {
            if (sub.boundaries?.solid_liquid == null || sub.boundaries.solid_liquid.Count < 2)
                return sub.meltingAt1Atm > 0 ? sub.meltingAt1Atm : sub.triplePoint.T;

            var pts = sub.boundaries.solid_liquid;
            return InterpolateBoundary(pts, P, byPressure: true);
        }

        private static float InterpolateBoilTemp(SubstancePhaseDiagram sub, float P)
        {
            if (sub.boundaries?.liquid_gas == null || sub.boundaries.liquid_gas.Count < 2)
                return sub.boilingAt1Atm > 0 ? sub.boilingAt1Atm : sub.criticalPoint.T;

            var pts = sub.boundaries.liquid_gas;
            return InterpolateBoundary(pts, P, byPressure: true);
        }

        private static float InterpolateBoundary(List<PhasePoint> pts, float P, bool byPressure)
        {
            // Find nearest two points bracketing P on log scale
            for (int i = 0; i < pts.Count - 1; i++)
            {
                float p0 = pts[i].P,     p1 = pts[i + 1].P;
                float t0 = pts[i].T,     t1 = pts[i + 1].T;
                float pMin = Mathf.Min(p0, p1), pMax = Mathf.Max(p0, p1);
                if (P >= pMin && P <= pMax)
                {
                    float tt = Mathf.InverseLerp(p0, p1, P);
                    return Mathf.Lerp(t0, t1, tt);
                }
            }
            // Extrapolate using the last segment
            var a = pts[pts.Count - 2];
            var b = pts[pts.Count - 1];
            float tNorm = Mathf.InverseLerp(a.P, b.P, P);
            return Mathf.Lerp(a.T, b.T, tNorm);
        }
    }
}
