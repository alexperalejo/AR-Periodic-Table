using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ElementTileSetup : MonoBehaviour
{
#if UNITY_EDITOR
    [ContextMenu("Auto Setup All Tiles")]
    void AutoSetupTiles()
    {
        TextAsset json = Resources.Load<TextAsset>("PeriodicTableJSON");
        if (json == null) { Debug.LogError("JSON not found!"); return; }

        PeriodicTable table = JsonUtility.FromJson<PeriodicTable>(json.text);

        // Get all child objects
        foreach (Transform child in transform)
        {
            string tileName = child.name.ToLower();

            foreach (AtomElementData el in table.elements)
            {
                if (el.symbol.ToLower() == tileName ||
                    el.name.ToLower() == tileName ||
                    tileName == "hcube" && el.symbol.ToLower() == "h" ||
                    tileName == "tl 1" && el.symbol.ToLower() == "tl")
                {
                    ElementTile tile = child.gameObject.GetComponent<ElementTile>();
                    if (tile == null)
                        tile = child.gameObject.AddComponent<ElementTile>();

                    tile.atomicNumber = el.number;
                    EditorUtility.SetDirty(child.gameObject);
                    Debug.Log($"Set {child.name} → Atomic Number {el.number}");
                    break;
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Done setting up all tiles!");
    }
#endif
}