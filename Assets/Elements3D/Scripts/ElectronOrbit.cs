//Making electrons move around the rings

using UnityEngine;

public class ElectronOrbit : MonoBehaviour
{
    public Transform center;
    public Transform ringTransform; // reference to the orbit ring
    public float radius = 0.2f;
    public float speed = 50f;
    public float angleOffset = 0f;
    public float yOffset = 0f;

    void Update()
    {
        if (center == null) return;

        angleOffset += speed * Time.deltaTime;
        float angleRad = angleOffset * Mathf.Deg2Rad;

        // Position in the ring's LOCAL space (flat circle)
        Vector3 localPos = new Vector3(
            Mathf.Cos(angleRad) * radius,
            0f,
            Mathf.Sin(angleRad) * radius
        );

        // Transform to world space using the ring's rotation
        if (ringTransform != null)
            transform.position = ringTransform.TransformPoint(localPos);
        else
            transform.position = center.position + localPos;
    }
}