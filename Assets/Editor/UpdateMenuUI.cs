using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class UpdateMenuUI
{
    public static void Execute()
    {
        GameObject menuPanelObj = GameObject.Find("MenuCanvas/MenuPanel");
        if (menuPanelObj != null)
        {
            // Update Panel Background
            Image panelImage = menuPanelObj.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(1.0f, 1.0f, 1.0f, 0.25f); // Translucent white/glass
                EditorUtility.SetDirty(panelImage);
            }

            // Update Buttons with Pastel Palette
            Color[] pastelColors = new Color[]
            {
                new Color(1.0f, 0.8f, 0.85f, 0.85f), // Pastel Pink
                new Color(0.8f, 0.9f, 1.0f, 0.85f),  // Pastel Blue
                new Color(0.8f, 1.0f, 0.85f, 0.85f), // Pastel Green
                new Color(1.0f, 0.95f, 0.8f, 0.85f), // Pastel Yellow
                new Color(0.9f, 0.8f, 1.0f, 0.85f),  // Pastel Purple
                new Color(1.0f, 0.85f, 0.75f, 0.85f),// Pastel Orange
                new Color(0.9f, 0.9f, 0.9f, 0.85f)   // Pastel Gray
            };

            Button[] buttons = menuPanelObj.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Image btnImage = buttons[i].GetComponent<Image>();
                if (btnImage != null)
                {
                    btnImage.color = pastelColors[i % pastelColors.Length];
                    EditorUtility.SetDirty(btnImage);
                }
            }

            Debug.Log("Updated MenuCanvas UI to a translucent pastel palette.");
        }
    }
}
