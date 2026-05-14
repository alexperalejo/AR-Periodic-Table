using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CompoundData
{
    public string formula;
    public string name;
    public string iupac_name;
    public float molecular_weight;
    public string category;
}

[System.Serializable]
public class CompoundDatabase
{
    public CompoundData[] compounds;
}

public class CompoundLookup : MonoBehaviour
{
    private Dictionary<string, CompoundData> compoundDict = new Dictionary<string, CompoundData>();
    private bool isLoaded = false;

    void Start()
    {
        LoadCompounds();
    }

    void LoadCompounds()
    {
        TextAsset json = Resources.Load<TextAsset>("CompoundsJSON");
        if (json == null)
        {
            Debug.LogError("CompoundsJSON not found in Resources!");
            return;
        }

        CompoundDatabase db = JsonUtility.FromJson<CompoundDatabase>(json.text);
        foreach (CompoundData c in db.compounds)
        {
            if (!compoundDict.ContainsKey(c.formula))
                compoundDict[c.formula] = c;
        }

        isLoaded = true;
        Debug.Log($"Loaded {compoundDict.Count} compounds.");
    }

    // Look up a compound by formula — returns null if not found
    public CompoundData Lookup(string formula)
    {
        if (!isLoaded) LoadCompounds();

        if (compoundDict.TryGetValue(formula, out CompoundData compound))
            return compound;

        return null;
    }

    // Returns just the common name, or formula if not found
    public string GetName(string formula)
    {
        CompoundData data = Lookup(formula);
        return data != null ? data.name : formula;
    }

    // Returns category (acid, base, salt, oxide, organic, common)
    public string GetCategory(string formula)
    {
        CompoundData data = Lookup(formula);
        return data != null ? data.category : "unknown";
    }

    [ContextMenu("Test Lookup")]
    void TestLookup()
    {
        string[] tests = { "H2O", "NaCl", "H2SO4", "Ca(OH)2", "C6H12O6", "XYZ" };
        foreach (string formula in tests)
        {
            CompoundData data = Lookup(formula);
            if (data != null)
                Debug.Log($"{formula} → {data.name} | {data.molecular_weight} g/mol | {data.category}");
            else
                Debug.Log($"{formula} → Not found in database");
        }
    }
}