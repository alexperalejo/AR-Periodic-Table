using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class FixMenuButtons
{
    public static void Execute()
    {
        GameObject menuPanelObj = GameObject.Find("MenuCanvas/MenuPanel");
        if (menuPanelObj != null)
        {
            Button[] buttons = menuPanelObj.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                Image img = btn.GetComponent<Image>();
                if (img != null)
                {
                    btn.targetGraphic = img;
                    EditorUtility.SetDirty(btn);
                    Debug.Log("Set targetGraphic for " + btn.name);
                }
            }
        }
    }
}
