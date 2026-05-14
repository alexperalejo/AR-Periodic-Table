using UnityEngine;

public class MoveTable
{
    public static void Execute()
    {
        GameObject table = GameObject.Find("pTableGroup");
        if (table != null)
        {
            // The table's children are offset by ~28 units in Y locally.
            // We need to move the parent down so the children are at y=1.
            table.transform.position = new Vector3(-1.5f, -27.0f, 6.0f);
            table.transform.rotation = Quaternion.Euler(51.48f, -18.07f, 3.79f);
            Debug.Log("Moved pTableGroup to visible location.");
        }
    }
}
