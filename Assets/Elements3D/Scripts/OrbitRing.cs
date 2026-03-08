//This creates the orbit prefab
//rotates

using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class OrbitRing : MonoBehaviour
{
    public int segments = 100;
    public float radius = 0.2f;
    public float rotationSpeed = 15f; // degrees per second
    public Vector3 rotationAxis = Vector3.up; // axis to spin around

    void Start()
    {
        DrawRing();
    }
    void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
    }

    void DrawRing()
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        lr.positionCount = segments + 1;
        lr.loop = true;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, 0, z));
        }
    }
}