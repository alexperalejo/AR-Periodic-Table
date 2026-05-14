using System.Collections.Generic;
using UnityEngine;
using PeriodicAR.Data;

public class ElementLoader : MonoBehaviour
{
    [Header("References")]
    public ElementInfoCard infoCard;
    public AtomGenerator atomGenerator;

    [Header("Start Element")]
    public int elementNumber = 1;

    [Header("Mode Control")]
    public bool showInfoCard = true;
    public bool showAtom = true;

    private PeriodicTable _table;
    public PeriodicTable Table => _table;

    public AtomElementData FindByNumber(int atomicNumber)
    {
        if (_table == null || _table.elements == null) return null;
        for (int i = 0; i < _table.elements.Length; i++)
            if (_table.elements[i].number == atomicNumber) return _table.elements[i];
        return null;
    }

    private void Start()
    {
        //LoadElement(elementNumber);
    }

    public void LoadElement(int atomicNumber)
    {
        if (_table == null)
        {
            TextAsset json = Resources.Load<TextAsset>("PeriodicTableJSON");
            if (json == null)
            {
                Debug.LogError("[ElementLoader] PeriodicTableJSON not found in Resources!");
                return;
            }
            _table = JsonUtility.FromJson<PeriodicTable>(json.text);
            if (_table == null || _table.elements == null)
            {
                Debug.LogError("[ElementLoader] Failed to parse JSON!");
                return;
            }
            ApplyCpkHexFixup(json.text, _table);
        }

        foreach (AtomElementData el in _table.elements)
        {
            if (el.number != atomicNumber)
                continue;

            int neutrons = Mathf.Max(0, Mathf.RoundToInt(el.atomic_mass) - el.number);

            // Use JSON shell data when available; fall back to the Bohr-rule
            // computation (2 / 8 / 18 / 32…) so the distribution is always
            // correct even if the JSON entry is missing or malformed.
            // The OLD fallback was new int[] { el.number } which put ALL electrons
            // in shell 1 (e.g. Carbon → [6] instead of [2, 4]).
            int[] shells = (el.shells != null && el.shells.Length > 0)
                ? (int[])el.shells.Clone()
                : AtomGenerator.ComputeBohrShells(el.number);

            if (atomGenerator != null && showAtom)
            {
                atomGenerator.SetElementData(
                    el.name,
                    el.number,
                    neutrons,
                    shells
                );
            }

            if (infoCard != null && showInfoCard)
            {
                string densityStr = el.density > 0f
                    ? el.density.ToString("F3") + " g/cm³"
                    : "N/A";
                string meltStr = el.melt > 0f
                    ? el.melt.ToString("F2") + " K"
                    : "N/A";
                string boilStr = el.boil > 0f
                    ? el.boil.ToString("F2") + " K"
                    : "N/A";

                infoCard.ShowCard(
                    el.name,
                    el.symbol,
                    el.number,
                    el.atomic_mass.ToString("F3"),
                    el.group.ToString(),
                    densityStr,
                    meltStr,
                    boilStr,
                    string.IsNullOrEmpty(el.electron_configuration) ? "N/A" : el.electron_configuration
                );
            }

            Debug.Log($"[ElementLoader] Loaded: {el.name}  Z={el.number}  N={neutrons}  shells=[{string.Join(",", shells)}]");
            return;
        }

        Debug.LogWarning($"[ElementLoader] Element Z={atomicNumber} not found in JSON.");
    }

    private void ApplyCpkHexFixup(string rawJson, PeriodicTable table)
    {
        var jobj = Newtonsoft.Json.Linq.JObject.Parse(rawJson);
        var arr = (Newtonsoft.Json.Linq.JArray)jobj["elements"];
        for (int i = 0; i < table.elements.Length && i < arr.Count; i++)
        {
            var tok = arr[i]["cpk-hex"];
            table.elements[i].cpkHex = (tok != null && tok.Type != Newtonsoft.Json.Linq.JTokenType.Null)
                ? tok.ToString()
                : null;
        }
    }
}