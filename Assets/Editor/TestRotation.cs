using UnityEngine;

public class TestRotation
{
    public static void Execute()
    {
        GameObject table = GameObject.Find("pTableGroup");
        if (table != null)
        {
            Vector3 forward = table.transform.forward;
            Vector3 up = table.transform.up;
            Vector3 right = table.transform.right;
            Debug.Log($"Table Forward: {forward}, Up: {up}, Right: {right}");
        }
    }
}
