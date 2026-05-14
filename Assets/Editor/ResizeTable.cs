using UnityEngine;
using UnityEditor;
using PeriodicAR.AR;

public class ResizeTable
{
    public static void Execute()
    {
        // Update the TapToPlace component for AR placement
        GameObject xrOrigin = GameObject.Find("XR Origin (AR Rig)");
        if (xrOrigin != null)
        {
            TapToPlace tapToPlace = xrOrigin.GetComponent<TapToPlace>();
            if (tapToPlace != null)
            {
                SerializedObject so = new SerializedObject(tapToPlace);
                so.Update();
                
                SerializedProperty scaleProp = so.FindProperty("absoluteScale");
                if (scaleProp != null) scaleProp.floatValue = 1.2f;
                
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(tapToPlace);
                Debug.Log("Updated TapToPlace absoluteScale to 1.2.");
            }
        }

        // Update the in-scene preview object
        GameObject table = GameObject.Find("pTableGroup");
        if (table != null)
        {
            table.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            EditorUtility.SetDirty(table);
            Debug.Log("Updated pTableGroup localScale to 1.2.");
        }
    }
}
