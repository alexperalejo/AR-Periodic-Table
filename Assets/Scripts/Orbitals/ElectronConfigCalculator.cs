using System.Collections.Generic;

namespace PeriodicAR.Orbitals
{
    public struct OrbitalOccupancy
    {
        public int    n;
        public string l;     // s, p, d, f
        public int    electrons;
    }

    /// <summary>
    /// Fills electron configuration using Aufbau order.
    /// Returns per-orbital occupancy for elements 1–118.
    /// </summary>
    public static class ElectronConfigCalculator
    {
        // Aufbau fill order: (n, l-label, max-electrons)
        private static readonly (int n, string l, int max)[] AufbauOrder =
        {
            (1,"s",2),(2,"s",2),(2,"p",6),(3,"s",2),(3,"p",6),(4,"s",2),(3,"d",10),(4,"p",6),
            (5,"s",2),(4,"d",10),(5,"p",6),(6,"s",2),(4,"f",14),(5,"d",10),(6,"p",6),(7,"s",2),
            (5,"f",14),(6,"d",10),(7,"p",6)
        };

        public static List<OrbitalOccupancy> GetConfig(int atomicNumber)
        {
            var result = new List<OrbitalOccupancy>();
            int remaining = atomicNumber;
            foreach (var (n, l, max) in AufbauOrder)
            {
                if (remaining <= 0) break;
                int fill = System.Math.Min(remaining, max);
                result.Add(new OrbitalOccupancy { n = n, l = l, electrons = fill });
                remaining -= fill;
            }
            return result;
        }

        public static string FormatConfig(int atomicNumber)
        {
            var config = GetConfig(atomicNumber);
            var sb = new System.Text.StringBuilder();
            foreach (var o in config)
                sb.Append($"{o.n}{o.l}<sup>{o.electrons}</sup> ");
            return sb.ToString().TrimEnd();
        }
    }
}
