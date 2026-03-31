using System;
using System.Collections.Generic;

[Serializable]
public class PeriodicTableRoot
{
    public List<ElementData> elements;
}

[Serializable]
public class ElementData
{
    public int number;
    public string symbol;
    public string name;
    public float atomic_mass;
    public int period;
    public int group;

    // There are more fields in the JSON.
    // Add what we need later (e.g., category, phase, density, etc.)
}