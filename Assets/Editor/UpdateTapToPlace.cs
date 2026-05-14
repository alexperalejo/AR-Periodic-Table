using UnityEngine;
using UnityEditor;
using PeriodicAR.AR;

public class UpdateTapToPlace
{
    public static void Execute()
    {
        GameObject xrOrigin = GameObject.Find("XR Origin (AR Rig)");
        if (xrOrigin != null)
        {
            TapToPlace tapToPlace = xrOrigin.GetComponent<TapToPlace>();
            if (tapToPlace != null)
            {
                SerializedObject so = new SerializedObject(tapToPlace);
                so.Update();
                
                SerializedProperty scaleProp = so.FindProperty("absoluteScale");
                if (scaleProp != null) scaleProp.floatValue = 0.4f;
                
                SerializedProperty heightProp = so.FindProperty("heightOffset");
                if (heightProp != null) heightProp.floatValue = 1.2f;
                
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(tapToPlace);
                Debug.Log("Updated TapToPlace properties on XR Origin.");
            }
        }
    }
}
