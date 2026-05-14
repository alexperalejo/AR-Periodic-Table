
// Attach to a dedicated "NucleusRoot" child of your atom, OR to BohrModelRoot.
// Adds two effects:
//   1. Slow tumble of the nucleus cluster (depth + life)
//   2. Gentle breathing scale (nucleus expands/contracts subtly)
//
// If you want ONLY the nucleus to tumble (not the shells),
// reparent your proton/neutron spawns under a separate "NucleusRoot" object
// and attach this script there.

using UnityEngine;

public class NucleusGlow : MonoBehaviour
{
    [Header("Tumble")]
    [Tooltip("Axis + speed of slow rotation. Diagonal axis looks most natural.")]
    public Vector3 tumbleAxis = new Vector3(0.4f, 1f, 0.2f);
    public float tumbleSpeed = 10f;

    [Header("Breathing")]
    public bool enableBreathing = true;
    [Range(0.01f, 0.1f)]
    public float breatheAmount = 0.05f;
    [Range(0.2f, 1.5f)]
    public float breatheSpeed = 0.65f;

    private Vector3 _baseScale;

    void Start() => _baseScale = transform.localScale;

    void Update()
    {
        transform.Rotate(tumbleAxis.normalized * tumbleSpeed * Time.deltaTime, Space.Self);

        if (enableBreathing)
        {
            float s = 1f + Mathf.Sin(Time.time * breatheSpeed * Mathf.PI * 2f) * breatheAmount;
            transform.localScale = _baseScale * s;
        }
    }
}