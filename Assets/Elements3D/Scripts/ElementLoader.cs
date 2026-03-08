//to parse it and feed data into AtomGenerator

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AtomElementData
{
    public string name;
    public int number;         // proton count
    public string electron_configuration; // e.g. "2, 8, 2"
    public int[] shells;       // electrons per shell
}

[System.Serializable]
public class PeriodicTable
{
    public AtomElementData[] elements;
}

public class ElementLoader : MonoBehaviour
{
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

        foreach (AtomElementData el in table.elements)
        {
            if (el.number == atomicNumber)
            {
                atomGenerator.elementName = el.name;
                atomGenerator.protonCount = el.number;
                atomGenerator.neutronCount = el.number; // approximation
                atomGenerator.electronsPerShell = el.shells;
                atomGenerator.GenerateAtom();
                Debug.Log($"Loaded: {el.name} (Z={el.number})");
                return;
            }
        }

        Debug.LogWarning($"Element {atomicNumber} not found.");
    }
}