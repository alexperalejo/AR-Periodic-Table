using PeriodicAR.Data;
using System.Collections.Generic;
using UnityEngine;

public class MolarMassCalculator : MonoBehaviour
{
    private Dictionary<string, float> elementWeights = new Dictionary<string, float>();
    private bool isLoaded = false;

    void Start()
    {
        LoadElementWeights();
    }

    void LoadElementWeights()
    {
        TextAsset json = Resources.Load<TextAsset>("PeriodicTableJSON");
        if (json == null)
        {
            Debug.LogError("PeriodicTableJSON not found!");
            return;
        }

        PeriodicTable table = JsonUtility.FromJson<PeriodicTable>(json.text);
        foreach (AtomElementData el in table.elements)
        {
            if (!elementWeights.ContainsKey(el.symbol))
                elementWeights[el.symbol] = el.atomic_mass;
        }

        isLoaded = true;
        Debug.Log($"Loaded {elementWeights.Count} element weights.");
    }

    // Calculate molar mass from a formula string
    // Returns -1 if formula contains unknown elements
    public float Calculate(string formula)
    {
        if (!isLoaded)
            LoadElementWeights();

        Dictionary<string, int> elements = FormulaParser.Parse(formula);

        if (elements.Count == 0)
        {
            Debug.LogWarning("Could not parse formula: " + formula);
            return -1f;
        }

        float total = 0f;
        foreach (var pair in elements)
        {
            if (elementWeights.TryGetValue(pair.Key, out float weight))
            {
                total += weight * pair.Value;
            }
            else
            {
                Debug.LogWarning($"Unknown element: {pair.Key}");
                return -1f;
            }
        }

        return (float)System.Math.Round(total, 3);
    }

    // Returns a breakdown string like "H: 2 x 1.008 = 2.016 | O: 1 x 15.999 = 15.999"
    public string GetBreakdown(string formula)
    {
        if (!isLoaded)
            LoadElementWeights();

        Dictionary<string, int> elements = FormulaParser.Parse(formula);
        string breakdown = "";

        foreach (var pair in elements)
        {
            if (elementWeights.TryGetValue(pair.Key, out float weight))
            {
                float subtotal = weight * pair.Value;
                breakdown += $"{pair.Key}: {pair.Value} x {weight} = {System.Math.Round(subtotal, 3)}\n";
            }
        }

        return breakdown;
    }

    [ContextMenu("Test Calculator")]
    void TestCalculator()
    {
        string[] tests = { "H2O", "NaCl", "H2SO4", "Ca(OH)2", "C6H12O6" };
        foreach (string formula in tests)
        {
            float mass = Calculate(formula);
            Debug.Log($"{formula} = {mass} g/mol");
            Debug.Log(GetBreakdown(formula));
        }
    }
}