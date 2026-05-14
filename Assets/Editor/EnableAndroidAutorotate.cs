using UnityEditor;
using UnityEngine;

/// <summary>
/// Switches Player Settings → Resolution and Presentation → Default Orientation to
/// AutoRotation, and enables all four allowed orientations. Without this, Android
/// will refuse to rotate the screen no matter how well the canvas is anchored.
/// Idempotent.
/// </summary>
public static class EnableAndroidAutorotate
{
    public static void Execute()
    {
        var prev = PlayerSettings.defaultInterfaceOrientation;

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait          = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
        PlayerSettings.allowedAutorotateToLandscapeLeft     = true;
        PlayerSettings.allowedAutorotateToLandscapeRight    = true;

        AssetDatabase.SaveAssets();

        Debug.Log($"[EnableAndroidAutorotate] defaultInterfaceOrientation: {prev} -> AutoRotation. " +
                  $"All 4 orientations allowed.");
    }
}
