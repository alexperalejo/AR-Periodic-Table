using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class PreviewPanel
{
    public static void ShowCalculator()
    {
        SetActiveOnly("CalculatorPanel");
    }

    public static void ShowElectrons()
    {
        SetActiveOnly("ElectronsPanel");
    }

    public static void ShowProperties()
    {
        SetActiveOnly("PropertiesPanel");
    }

    public static void ShowCompounds()
    {
        SetActiveOnly("CompoundsPanel");
    }

    public static void ShowMenu()
    {
        SetActiveOnly("MenuPanel");
    }

    static void SetActiveOnly(string name)
    {
        string[] panels = new[] { "MenuPanel", "CalculatorPanel", "ElementInfoPanel", "ElectronsPanel", "PropertiesPanel", "CompoundsPanel" };
        foreach (var p in panels)
        {
            var go = GameObject.Find("MenuCanvas/" + p);
            if (go != null) go.SetActive(p == name);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
