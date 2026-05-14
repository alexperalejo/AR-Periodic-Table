
// Key fix: electrons follow the ring's LOCAL rotation correctly,
// so tilted shells show true 3-D orbits in AR.
// Also fixes float drift over long sessions.

using UnityEngine;

public class ElectronOrbit : MonoBehaviour
{
    [Header("Orbit Parameters")]
    public Transform center;
    public Transform ringTransform;
    public float radius = 0.2f;
    public float speed = 55f;   // degrees/sec  (+ = CCW, - = CW)
    public float angleOffset = 0f;
    public float yOffset = 0f;

    void Update()
    {
        if (center == null) return;

        angleOffset += speed * Time.deltaTime;

        // Clamp to avoid float precision loss over long sessions
        if (angleOffset > 360f) angleOffset -= 360f;
        if (angleOffset < -360f) angleOffset += 360f;

        float rad = angleOffset * Mathf.Deg2Rad;

        // Flat circle in the ring's XZ plane
        Vector3 localPos = new Vector3(
            Mathf.Cos(rad) * radius,
            0f,
            Mathf.Sin(rad) * radius
        );

        // TransformPoint converts local → world using the ring's full TRS
        // This is what makes tilted shells orbit correctly
        if (ringTransform != null)
            transform.position = ringTransform.TransformPoint(localPos);
        else
            transform.position = center.position + localPos + Vector3.up * yOffset;

        // Keep electron facing up — no unwanted tumble
        transform.rotation = Quaternion.identity;
    }
}