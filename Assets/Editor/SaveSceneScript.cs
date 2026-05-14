using UnityEditor;
using UnityEditor.SceneManagement;

public class SaveSceneScript
{
    public static void Execute()
    {
        EditorSceneManager.SaveOpenScenes();
    }
}
