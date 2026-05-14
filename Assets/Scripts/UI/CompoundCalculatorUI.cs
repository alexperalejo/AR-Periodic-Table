using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PeriodicAR.Data;

/// <summary>
/// Unified panel replacing both CalculatorUI and CompoundsUI.
/// Tap elements to build a chemical formula, then displays:
///   - Formatted formula (with subscripts)
///   - Common compound name
///   - Category (if available)
///   - IUPAC name (if available)
///   - Molar mass
///   - Element-by-element mass breakdown
/// </summary>
public class CompoundCalculatorUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text formulaText;
    public TMP_Text compoundNameText;
    public TMP_Text categoryText;
    public TMP_Text iupacNameText;
    public TMP_Text molarMassText;
    public TMP_Text breakdownText;
    public Button btnClear;
    public Button btnBack;
    [Tooltip("Optional Backspace button — removes the last element added to the formula.")]
    public Button btnBackspace;

    [Header("Scripts")]
    public MolarMassCalculator molarMassCalculator;
    public CompoundLookup compoundLookup;

    // Tracks counts for building the formula string.
    private Dictionary<string, int> currentElements = new Dictionary<string, int>();

    // Tracks insertion order so Backspace removes the LAST pressed element.
    // Each AddElement call appends the symbol; BackspaceElement pops the tail.
    private List<string> _elementOrder = new List<string>();

    void Start()
    {
        btnClear.onClick.AddListener(ClearFormula);
        btnBack.onClick.AddListener(() => ARModeManager.Instance.ShowMenu());
        if (btnBackspace != null)
            btnBackspace.onClick.AddListener(BackspaceElement);
        UpdateDisplay();
    }

    // Called by ARModeManager when an element tile is tapped.
    public void AddElement(string symbol)
    {
        _elementOrder.Add(symbol);

        if (currentElements.ContainsKey(symbol))
            currentElements[symbol]++;
        else
            currentElements[symbol] = 1;

        UpdateDisplay();
    }

    // Removes the last element that was added (one press = one element entry).
    public void BackspaceElement()
    {
        if (_elementOrder.Count == 0)
        {
            Debug.Log("[CompoundCalculatorUI] Backspace: formula already empty.");
            return;
        }

        string removed = _elementOrder[_elementOrder.Count - 1];
        _elementOrder.RemoveAt(_elementOrder.Count - 1);

        if (currentElements.ContainsKey(removed))
        {
            currentElements[removed]--;
            if (currentElements[removed] <= 0)
                currentElements.Remove(removed);
        }

        Debug.Log($"[CompoundCalculatorUI] Backspace removed '{removed}'. Remaining: {BuildFormula()}");
        UpdateDisplay();
    }

    void ClearFormula()
    {
        currentElements.Clear();
        _elementOrder.Clear();
        UpdateDisplay();
    }

    // Plain formula for parsing/lookup: H2O, NaCl, etc.
    // Built from the dictionary (insertion order reflected via _elementOrder-derived key sequence).
    string BuildFormula()
    {
        // Collect symbols in first-seen order derived from _elementOrder.
        var seen = new List<string>();
        var seenSet = new HashSet<string>();
        foreach (string sym in _elementOrder)
        {
            if (seenSet.Add(sym)) seen.Add(sym);
        }

        string formula = "";
        foreach (string sym in seen)
        {
            if (!currentElements.TryGetValue(sym, out int count)) continue;
            formula += sym;
            if (count > 1) formula += count;
        }
        return formula;
    }

    // Rich-text formula with TMP subscript tags.
    string BuildFormattedFormula()
    {
        var seen = new List<string>();
        var seenSet = new HashSet<string>();
        foreach (string sym in _elementOrder)
        {
            if (seenSet.Add(sym)) seen.Add(sym);
        }

        string formula = "";
        foreach (string sym in seen)
        {
            if (!currentElements.TryGetValue(sym, out int count)) continue;
            formula += sym;
            if (count > 1) formula += "<sub>" + count + "</sub>";
        }
        return formula;
    }

    void UpdateDisplay()
    {
        if (_elementOrder.Count == 0)
        {
            formulaText.text = "Tap elements to build formula";
            SetText(compoundNameText, "");
            SetText(categoryText, "");
            SetText(iupacNameText, "");
            SetText(molarMassText, "");
            SetText(breakdownText, "");
            return;
        }

        string formula = BuildFormula();
        formulaText.text = BuildFormattedFormula();

        // Compound lookup — name, category, IUPAC
        CompoundData compound = compoundLookup.Lookup(formula);
        if (compound != null)
        {
            SetText(compoundNameText, compound.name);
            SetText(categoryText, !string.IsNullOrEmpty(compound.category) ? "Category: " + compound.category : "");
            SetText(iupacNameText, !string.IsNullOrEmpty(compound.iupac_name) ? "IUPAC: " + compound.iupac_name : "");
        }
        else
        {
            SetText(compoundNameText, "Unknown compound");
            SetText(categoryText, "");
            SetText(iupacNameText, "");
        }

        // Molar mass
        float mass = molarMassCalculator.Calculate(formula);
        SetText(molarMassText, mass > 0 ? "Molar Mass: " + mass.ToString("F4") + " g/mol" : "Molar Mass: N/A");

        // Element breakdown
        SetText(breakdownText, molarMassCalculator.GetBreakdown(formula));
    }

    // Null-safe helper so optional UI fields don't cause NullReferenceExceptions.
    static void SetText(TMP_Text label, string value)
    {
        if (label != null) label.text = value;
    }
}
