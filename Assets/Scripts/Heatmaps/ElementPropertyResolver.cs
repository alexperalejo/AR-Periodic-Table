using System.Collections.Generic;
using PeriodicAR.Data;
using UnityEngine;

namespace PeriodicAR.Heatmaps
{
    /// <summary>
    /// Resolves numeric property values from AtomElementData for heatmap coloring.
    /// Properties available from the JSON: atomic_mass, density, melt, boil.
    /// Electronegativity and ionization energy use compact hardcoded tables.
    /// </summary>
    public static class ElementPropertyResolver
    {
        public enum Property
        {
            AtomicMass,
            Density,
            MeltingPoint,
            BoilingPoint,
            Electronegativity,
            FirstIonizationEnergy,
        }

        // Pauling electronegativity (null = unknown/radioactive/no data).
        private static readonly Dictionary<int, float> s_EN = new()
        {
            {1,2.20f},{2,0f},{3,0.98f},{4,1.57f},{5,2.04f},{6,2.55f},{7,3.04f},{8,3.44f},{9,3.98f},{10,0f},
            {11,0.93f},{12,1.31f},{13,1.61f},{14,1.90f},{15,2.19f},{16,2.58f},{17,3.16f},{18,0f},
            {19,0.82f},{20,1.00f},{21,1.36f},{22,1.54f},{23,1.63f},{24,1.66f},{25,1.55f},{26,1.83f},{27,1.88f},{28,1.91f},{29,1.90f},{30,1.65f},
            {31,1.81f},{32,2.01f},{33,2.18f},{34,2.55f},{35,2.96f},{36,3.00f},
            {37,0.82f},{38,0.95f},{39,1.22f},{40,1.33f},{41,1.60f},{42,2.16f},{43,1.90f},{44,2.20f},{45,2.28f},{46,2.20f},{47,1.93f},{48,1.69f},
            {49,1.78f},{50,1.96f},{51,2.05f},{52,2.10f},{53,2.66f},{54,2.60f},
            {55,0.79f},{56,0.89f},{57,1.10f},{58,1.12f},{59,1.13f},{60,1.14f},{61,1.13f},{62,1.17f},{63,1.20f},{64,1.20f},{65,1.10f},{66,1.22f},{67,1.23f},{68,1.24f},{69,1.25f},{70,1.10f},{71,1.27f},
            {72,1.30f},{73,1.50f},{74,2.36f},{75,1.90f},{76,2.20f},{77,2.20f},{78,2.28f},{79,2.54f},{80,2.00f},
            {81,1.62f},{82,2.33f},{83,2.02f},{84,2.00f},{85,2.20f},{86,0f},
        };

        // First ionization energy in eV.
        private static readonly Dictionary<int, float> s_IE = new()
        {
            {1,13.60f},{2,24.59f},{3,5.39f},{4,9.32f},{5,8.30f},{6,11.26f},{7,14.53f},{8,13.62f},{9,17.42f},{10,21.56f},
            {11,5.14f},{12,7.65f},{13,5.99f},{14,8.15f},{15,10.49f},{16,10.36f},{17,12.97f},{18,15.76f},
            {19,4.34f},{20,6.11f},{21,6.56f},{22,6.83f},{23,6.75f},{24,6.77f},{25,7.43f},{26,7.90f},{27,7.88f},{28,7.64f},{29,7.73f},{30,9.39f},
            {31,6.00f},{32,7.90f},{33,9.81f},{34,9.75f},{35,11.81f},{36,14.00f},
            {37,4.18f},{38,5.69f},{39,6.22f},{40,6.63f},{41,6.76f},{42,7.09f},{43,7.28f},{44,7.36f},{45,7.46f},{46,8.34f},{47,7.58f},{48,8.99f},
            {49,5.79f},{50,7.34f},{51,8.61f},{52,9.01f},{53,10.45f},{54,12.13f},
            {55,3.89f},{56,5.21f},{57,5.58f},{58,5.54f},{59,5.47f},{60,5.53f},{61,5.58f},{62,5.64f},{63,5.67f},{64,6.15f},{65,5.86f},{66,5.94f},{67,6.02f},{68,6.11f},{69,6.18f},{70,6.25f},{71,5.43f},
            {72,6.83f},{73,7.55f},{74,7.86f},{75,7.83f},{76,8.44f},{77,8.97f},{78,8.96f},{79,9.23f},{80,10.44f},
            {81,6.11f},{82,7.42f},{83,7.29f},{84,8.42f},{85,9.30f},{86,10.75f},
            {87,4.07f},{88,5.28f},{89,5.17f},{90,6.31f},{91,5.89f},{92,6.19f},
        };

        public static float GetValue(Property prop, AtomElementData el)
        {
            return prop switch
            {
                Property.AtomicMass             => el.atomic_mass,
                Property.Density                => el.density,
                Property.MeltingPoint           => el.melt,
                Property.BoilingPoint           => el.boil,
                Property.Electronegativity      => s_EN.TryGetValue(el.number, out var en)  ? en  : 0f,
                Property.FirstIonizationEnergy  => s_IE.TryGetValue(el.number, out var ie)  ? ie  : 0f,
                _                               => 0f,
            };
        }

        // Returns (min, max) values across all elements (ignoring 0/missing).
        public static (float min, float max) RangeFor(Property prop, AtomElementData[] elements)
        {
            float mn = float.MaxValue, mx = float.MinValue;
            foreach (var el in elements)
            {
                float v = GetValue(prop, el);
                if (v <= 0f) continue;
                if (v < mn) mn = v;
                if (v > mx) mx = v;
            }
            if (mn == float.MaxValue) { mn = 0f; mx = 1f; }
            return (mn, mx);
        }

        public static string DisplayNameFor(Property prop) => prop switch
        {
            Property.AtomicMass            => "Atomic Mass",
            Property.Density               => "Density",
            Property.MeltingPoint          => "Melting Point",
            Property.BoilingPoint          => "Boiling Point",
            Property.Electronegativity     => "Electronegativity",
            Property.FirstIonizationEnergy => "Ionization Energy",
            _                              => prop.ToString(),
        };

        public static string UnitLabelFor(Property prop) => prop switch
        {
            Property.AtomicMass            => "u",
            Property.Density               => "g/cm³",
            Property.MeltingPoint          => "K",
            Property.BoilingPoint          => "K",
            Property.Electronegativity     => "(Pauling)",
            Property.FirstIonizationEnergy => "eV",
            _                              => "",
        };

        public static Property[] AllProperties() =>
            (Property[])System.Enum.GetValues(typeof(Property));
    }
}
