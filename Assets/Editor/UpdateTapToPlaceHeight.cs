using UnityEngine;
using UnityEditor;
using PeriodicAR.AR;

public class UpdateTapToPlaceHeight
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
                
                SerializedProperty heightProp = so.FindProperty("heightOffset");
                if (heightProp != null) heightProp.floatValue = 0.7f;
                
                SerializedProperty faceCameraProp = so.FindProperty("faceCameraOnPlacement");
                if (faceCameraProp != null) faceCameraProp.boolValue = true;
                
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(tapToPlace);
                Debug.Log("Updated TapToPlace heightOffset to 0.7 and enabled faceCameraOnPlacement.");
            }
        }
    }
}
