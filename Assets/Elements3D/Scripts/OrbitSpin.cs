
// Spinning a ring that ElectronOrbit.TransformPoint() reads from
// causes electrons to spiral off-axis.
//
// CORRECT USAGE: Attach to the NUCLEUS root only (the atom centre object)
// for a slow tumble that adds visual life to the nucleus cluster.
// AtomGenerator already removes OrbitSpin from ring GameObjects at spawn time.

using UnityEngine;

public class OrbitSpin : MonoBehaviour
{
    [Tooltip("Axis of rotation in local space")]
    public Vector3 rotationAxis = new Vector3(0.3f, 1f, 0.15f);
    [Tooltip("Degrees per second")]
    public float spinSpeed = 12f;

    void Update()
    {
        transform.Rotate(rotationAxis.normalized * spinSpeed * Time.deltaTime, Space.Self);
    }
}