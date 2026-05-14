using UnityEngine;
using UnityEditor;

public class FixMenuCanvas
{
    public static void Execute()
    {
        GameObject menuCanvasObj = GameObject.Find("MenuCanvas");
        if (menuCanvasObj != null)
        {
            Canvas canvas = menuCanvasObj.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 101;
                EditorUtility.SetDirty(menuCanvasObj);
                Debug.Log("Set MenuCanvas sortingOrder to 101");
            }
        }
    }
}
