using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ReparentOrbitalBox
{
    public static void Execute()
    {
        GameObject orbital = GameObject.Find("OrbitalBoxPrefab");
        GameObject menuCanvas = GameObject.Find("MenuCanvas");

        if (orbital == null)
        {
            Debug.LogError("OrbitalBoxPrefab not found in scene.");
            return;
        }
        if (menuCanvas == null)
        {
            Debug.LogError("MenuCanvas not found in scene.");
            return;
        }

        // Record undo so this can be rolled back with Ctrl-Z if needed
        Undo.SetTransformParent(orbital.transform, menuCanvas.transform, "Reparent OrbitalBoxPrefab");

        RectTransform rt = orbital.GetComponent<RectTransform>();
        if (rt != null)
        {
            Undo.RecordObject(rt, "Reset OrbitalBoxPrefab RectTransform");

            // Reset scale (was 10,10,10)
            rt.localScale = Vector3.one;

            // Anchor to top-right corner of screen (nice out-of-the-way spot for a HUD box)
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);

            // A sensible size for an orbital info box
            rt.sizeDelta = new Vector2(240f, 320f);

            // Offset it a bit inward from the top-right corner so it isn't clipped
            rt.anchoredPosition = new Vector2(-20f, -20f);

            rt.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(rt);
        }

        // Make the panel background a nice translucent pastel so it matches the menu
        Image img = orbital.GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(0.90f, 0.82f, 1.00f, 0.55f); // pastel lavender, translucent
            EditorUtility.SetDirty(img);
        }

        // Make sure it's active and on the UI layer
        orbital.SetActive(true);
        orbital.layer = LayerMask.NameToLayer("UI");
        foreach (Transform t in orbital.transform)
        {
            t.gameObject.layer = LayerMask.NameToLayer("UI");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Reparented OrbitalBoxPrefab under MenuCanvas and reset its RectTransform.");
    }
}
