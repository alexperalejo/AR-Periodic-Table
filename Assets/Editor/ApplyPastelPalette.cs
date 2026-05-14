using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ApplyPastelPalette
{
    // Nice cohesive pastel palette (R, G, B on 0-1 scale)
    static readonly Color[] pastelColors = new Color[]
    {
        new Color(1.00f, 0.80f, 0.85f, 0.85f), // soft pink       - BtnBohrModel
        new Color(0.80f, 0.90f, 1.00f, 0.85f), // baby blue       - BtnElementInfo
        new Color(0.82f, 0.98f, 0.85f, 0.85f), // mint green      - BtnCalculator
        new Color(1.00f, 0.95f, 0.78f, 0.85f), // buttery yellow  - BtnElectrons
        new Color(0.90f, 0.82f, 1.00f, 0.85f), // lavender        - BtnProperties
        new Color(1.00f, 0.86f, 0.75f, 0.85f), // peach            - BtnCompounds
        new Color(0.92f, 0.92f, 0.94f, 0.85f), // cool gray        - BtnBackToMenu
    };

    static readonly string[] buttonOrder = new string[]
    {
        "BtnBohrModel",
        "BtnElementInfo",
        "BtnCalculator",
        "BtnElectrons",
        "BtnProperties",
        "BtnCompounds",
        "BtnBackToMenu",
    };

    public static void Execute()
    {
        GameObject menuPanel = GameObject.Find("MenuCanvas/MenuPanel");
        if (menuPanel == null)
        {
            Debug.LogError("MenuCanvas/MenuPanel not found.");
            return;
        }

        // Make the panel background a frosted translucent white
        Image panelImg = menuPanel.GetComponent<Image>();
        if (panelImg != null)
        {
            panelImg.color = new Color(1f, 1f, 1f, 0.18f);
            EditorUtility.SetDirty(panelImg);
        }

        // Apply pastel colors to each button by name
        for (int i = 0; i < buttonOrder.Length; i++)
        {
            Transform btnTransform = menuPanel.transform.Find(buttonOrder[i]);
            if (btnTransform == null)
            {
                Debug.LogWarning($"Button '{buttonOrder[i]}' not found.");
                continue;
            }

            Color baseColor = pastelColors[i];

            // Set the Image color (the button background)
            Image img = btnTransform.GetComponent<Image>();
            if (img != null)
            {
                img.color = baseColor;
                EditorUtility.SetDirty(img);
            }

            // Update the Button transition colors for nice hover/press feedback
            Button btn = btnTransform.GetComponent<Button>();
            if (btn != null)
            {
                ColorBlock cb = btn.colors;
                cb.normalColor = Color.white; // tint multiplier applied on top of Image color
                // Highlighted: slightly brighter
                cb.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
                // Pressed: slightly darker
                cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                cb.selectedColor = new Color(1.02f, 1.02f, 1.02f, 1f);
                cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
                cb.fadeDuration = 0.12f;
                btn.colors = cb;
                EditorUtility.SetDirty(btn);
            }

            // Make the text color a soft dark slate so it is readable on pastels
            var tmp = btnTransform.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.color = new Color(0.18f, 0.20f, 0.28f, 1f);
                tmp.fontStyle = TMPro.FontStyles.Bold;
                EditorUtility.SetDirty(tmp);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Applied pastel palette to MenuPanel buttons.");
    }
}
