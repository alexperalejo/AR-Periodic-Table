//to parse it and feed data into AtomGenerator

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
    public ElementInfoCard infoCard;
    public AtomGenerator atomGenerator;
    public int elementNumber = 1; // change this to test different elements

    void Start()
    {
       LoadElement(elementNumber);
    }

    public void LoadElement(int atomicNumber)
    {
        TextAsset json = Resources.Load<TextAsset>("PeriodicTableJSON");
        if (json == null) { Debug.LogError("JSON not found in Resources!"); return; }

        PeriodicTable table = JsonUtility.FromJson<PeriodicTable>(json.text);

        if (table == null) { Debug.LogError("Failed to parse JSON!"); return; }

        foreach (AtomElementData el in table.elements)
        {
            if (el.number == atomicNumber)
            {
                // Feed atom generator
                atomGenerator.elementName = el.name;
                atomGenerator.protonCount = el.number;
                atomGenerator.neutronCount = el.number;
                atomGenerator.electronsPerShell = el.shells;
                atomGenerator.GenerateAtom();

                // Feed info card
                if (infoCard != null)
                {
                    infoCard.ShowCard(
                        el.name,
                        el.symbol,
                        el.number,
                        el.atomic_mass.ToString("F3"),
                        el.group.ToString(),
                        el.density.ToString("F3") + " g/cm³",
                        el.melt.ToString("F2") + " K",
                        el.boil.ToString("F2") + " K",
                        el.electron_configuration
                    );
                }

                Debug.Log($"Loaded: {el.name} (Z={el.number})");
                return;
            }
        }
        Debug.LogWarning($"Element {atomicNumber} not found.");
    }
}