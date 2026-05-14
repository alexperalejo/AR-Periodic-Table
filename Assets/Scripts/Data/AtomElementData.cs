// Assets/Scripts/Data/AtomElementData.cs
using System;
namespace PeriodicAR.Data
{
    [Serializable]
    public class AtomElementData
    {
        public int number;
        public string symbol;
        public string name;
        public float atomic_mass;
        public int period;
        public int group;
        public string category;
        public string phase;
        public string appearance;
        public string summary;
        public string discovered_by;
        public string named_by;
        public string electron_configuration;
        public int[] shells;
        public float density;
        public float melt;
        public float boil;
        public float electronegativity_pauling;
        public float electron_affinity;
        public float molar_heat;
        public int xpos;
        public int ypos;
        public int wxpos;
        public int wypos;

        [NonSerialized] public string cpkHex;
    }

    [Serializable]
    public class PeriodicTable
    {
        public AtomElementData[] elements;
    }
}