// Assets/Scripts/Data/AtomElementData.cs
using System;

namespace PeriodicAR.Data
{
    /// <summary>
    /// One element entry from Resources/PeriodicTableJSON.json.
    /// Field names MUST match the JSON keys exactly because JsonUtility is case-sensitive
    /// and has no attribute-based rename. Do not add [Serializable]-incompatible members.
    /// </summary>
    [Serializable]
    public class AtomElementData
    {
        public int    number;
        public string symbol;
        public string name;
        public float  atomic_mass;
        public int    period;
        public int    group;
        public string category;
        public string electron_configuration;
        public int[]  shells;
        public float  density;
        public float  melt;
        public float  boil;
        public int    xpos;
        public int    ypos;
        public int    wxpos;
        public int    wypos;

        // JsonUtility cannot map "cpk-hex" to a field named cpk_hex because of the dash.
        // We read it via a post-parse fixup in ElementLoader (see that file). Keep this
        // public so the highlighter can read it.
        [NonSerialized] public string cpkHex;
    }

    [Serializable]
    public class PeriodicTable
    {
        public AtomElementData[] elements;
    }
}
