using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AtomElementData
{
    public string name;
    public string symbol;
    public int number;
    public float atomic_mass;
    public int group;
    public int period;
    public float density;
    public float melt;
    public float boil;
    public string category;
    public string electron_configuration;
    public int[] shells;
}

[System.Serializable]
public class PeriodicTable
{
    public AtomElementData[] elements;
}

public class ElementLoader : MonoBehaviour
{
    [Header("References")]
    public ElementInfoCard infoCard;
    public AtomGenerator atomGenerator;

    [Header("Start Element")]
    public int elementNumber = 1;

    private PeriodicTable _table;

    private void Start()
    {
        LoadElement(elementNumber);
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
        }

        foreach (AtomElementData el in _table.elements)
        {
            if (el.number != atomicNumber)
                continue;

            int neutrons = Mathf.Max(0, Mathf.RoundToInt(el.atomic_mass) - el.number);

            int[] shells = (el.shells != null && el.shells.Length > 0)
                ? (int[])el.shells.Clone()
                : new int[] { el.number };

            if (atomGenerator != null)
            {
                atomGenerator.SetElementData(
                    el.name,
                    el.number,
                    neutrons,
                    shells
                );
            }

            if (infoCard != null)
            {
                string densityStr = el.density > 0f
                    ? el.density.ToString("F3") + " g/cm\u00B3"
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
}