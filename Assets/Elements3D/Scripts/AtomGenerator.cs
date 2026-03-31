using System.Collections.Generic;
using UnityEngine;

public class AtomGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject protonPrefab;
    public GameObject neutronPrefab;
    public GameObject electronPrefab;
    public GameObject orbitRingPrefab;

    [Header("Element Data")]
    public string elementName = "Hydrogen";
    public int protonCount = 1;
    public int neutronCount = 0;
    public int[] electronsPerShell = new int[] { 1 };

    [Header("Layout Settings")]
    [Tooltip("Base radius for the nucleus cluster")]
    public float nucleusRadius = 0.06f;
    [Tooltip("Gap between shell centres")]
    public float shellSpacing = 0.18f;
    [Tooltip("Uniform scale applied to every spawned electron")]
    public float electronScale = 1f;
    [Tooltip("Y offset of the whole atom - leave 0")]
    public float ringYOffset = 0f;

    [Header("Motion Settings")]
    [Tooltip("Degrees/sec for shell 0 (innermost)")]
    public float innerShellSpeed = 55f;
    [Tooltip("Each outer shell is this many deg/sec slower")]
    public float shellSpeedStep = 8f;
    [Tooltip("Minimum orbit speed any shell can reach")]
    public float minShellSpeed = 12f;

    [Header("Visual Settings")]
    [Tooltip("Multiplier on shell radii so rings are never too small/large in AR")]
    public float masterScale = 1.0f;
    [Tooltip("Width of each orbit ring line")]
    public float ringLineWidth = 0.008f;
    [Tooltip("Tilt X added per shell (degrees)")]
    public float shellTiltXStep = 40f;
    [Tooltip("Tilt Z added per shell (degrees)")]
    public float shellTiltZStep = 63f;

    private static readonly Color[] ShellColors =
    {
        new Color(0.10f, 0.90f, 1.00f),
        new Color(0.30f, 1.00f, 0.40f),
        new Color(1.00f, 0.88f, 0.10f),
        new Color(1.00f, 0.35f, 0.72f),
        new Color(0.78f, 0.35f, 1.00f),
        new Color(1.00f, 0.52f, 0.12f),
        new Color(0.20f, 0.55f, 1.00f)
    };

    private readonly List<GameObject> _spawned = new List<GameObject>();

    private void Start()
    {
        GenerateAtom();
    }

    [ContextMenu("Generate Atom")]
    public void GenerateAtom()
    {
        ClearAtom();
        ApplyAdaptiveScale();
        GenerateNucleus();
        GenerateShellsAndElectrons();
    }

    [ContextMenu("Clear Atom")]
    public void ClearAtom()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i] != null)
            {
                if (Application.isPlaying)
                    Destroy(_spawned[i]);
                else
                    DestroyImmediate(_spawned[i]);
            }
        }

        _spawned.Clear();
    }

    public void SetElementData(string newName, int newProtons, int newNeutrons, int[] newElectronsPerShell)
    {
        elementName = newName;
        protonCount = newProtons;
        neutronCount = newNeutrons;

        if (newElectronsPerShell != null)
        {
            electronsPerShell = (int[])newElectronsPerShell.Clone();
        }
        else
        {
            electronsPerShell = new int[] { 1 };
        }

        GenerateAtom();
    }

    private void ApplyAdaptiveScale()
    {
        int totalElectrons = 0;

        if (electronsPerShell != null)
        {
            foreach (int e in electronsPerShell)
            {
                totalElectrons += e;
            }
        }

        float adaptive = Mathf.Lerp(0.85f, 1.30f, Mathf.InverseLerp(1, 118, totalElectrons));
        transform.localScale = Vector3.one * (masterScale * adaptive);
    }

    private void GenerateNucleus()
    {
        int total = protonCount + neutronCount;
        if (total <= 0) return;

        for (int i = 0; i < total; i++)
        {
            bool isProton = i < protonCount;
            GameObject prefab = isProton ? protonPrefab : neutronPrefab;
            if (prefab == null) continue;

            Vector3 pos = (total == 1)
                ? Vector3.zero
                : FibonacciSphere(i, total) * nucleusRadius * Mathf.Pow(total, 0.32f);

            GameObject nucleon = Instantiate(prefab, transform);
            nucleon.transform.localPosition = pos;
            nucleon.transform.localRotation = Quaternion.identity;

            StyleNucleon(nucleon, isProton);
            _spawned.Add(nucleon);
        }
    }

    private void StyleNucleon(GameObject go, bool isProton)
    {
        Renderer r = go.GetComponentInChildren<Renderer>();
        if (r == null) return;

        Material m = new Material(
            r.sharedMaterial != null
                ? r.sharedMaterial
                : new Material(Shader.Find("Universal Render Pipeline/Lit"))
        );

        Color baseColor = isProton
            ? new Color(0.92f, 0.22f, 0.16f)
            : new Color(0.20f, 0.46f, 0.80f);

        m.color = baseColor;

        TrySetFloat(m, "_Smoothness", 0.82f);
        TrySetFloat(m, "_Metallic", 0.05f);
        TrySetEmission(m, baseColor * 0.35f);

        r.material = m;
    }

    private void GenerateShellsAndElectrons()
    {
        if (orbitRingPrefab == null || electronPrefab == null || electronsPerShell == null) return;

        for (int s = 0; s < electronsPerShell.Length; s++)
        {
            float radius = shellSpacing * (s + 1);
            Color shellColor = ShellColors[s % ShellColors.Length];

            GameObject ring = Instantiate(orbitRingPrefab, transform);
            ring.transform.localPosition = new Vector3(0f, ringYOffset, 0f);
            ring.transform.localEulerAngles = new Vector3(
                s * shellTiltXStep,
                0f,
                s * shellTiltZStep
            );

            OrbitRing orbitScript = ring.GetComponent<OrbitRing>();
            if (orbitScript != null)
            {
                orbitScript.radius = radius;
                orbitScript.lineWidth = ringLineWidth;
                orbitScript.SetRadius(radius);
                orbitScript.SetColor(shellColor);
            }

            OrbitSpin spin = ring.GetComponent<OrbitSpin>();
            if (spin != null)
            {
                if (Application.isPlaying)
                    Destroy(spin);
                else
                    DestroyImmediate(spin);
            }

            _spawned.Add(ring);

            int count = electronsPerShell[s];
            float speed = Mathf.Max(minShellSpeed, innerShellSpeed - s * shellSpeedStep);
            float signedSpeed = (s % 2 == 0) ? speed : -speed;

            for (int e = 0; e < count; e++)
            {
                float startAngle = (360f / Mathf.Max(1, count)) * e;

                GameObject electron = Instantiate(electronPrefab, transform);
                electron.transform.localPosition = Vector3.zero;
                electron.transform.localRotation = Quaternion.identity;
                electron.transform.localScale = electronPrefab.transform.localScale * electronScale;

                ElectronOrbit orbit = electron.GetComponent<ElectronOrbit>();
                if (orbit == null)
                {
                    orbit = electron.AddComponent<ElectronOrbit>();
                }

                orbit.center = transform;
                orbit.ringTransform = ring.transform;
                orbit.radius = radius;
                orbit.yOffset = ringYOffset;
                orbit.angleOffset = startAngle;
                orbit.speed = signedSpeed;

                StyleElectron(electron, shellColor);
                _spawned.Add(electron);
            }
        }
    }

    private void StyleElectron(GameObject go, Color color)
    {
        Renderer r = go.GetComponentInChildren<Renderer>();
        if (r == null) return;

        Material m = new Material(
            r.sharedMaterial != null
                ? r.sharedMaterial
                : new Material(Shader.Find("Universal Render Pipeline/Lit"))
        );

        m.color = color;
        TrySetFloat(m, "_Smoothness", 0.95f);
        TrySetFloat(m, "_Metallic", 0.0f);
        TrySetEmission(m, color * 1.4f);

        r.material = m;
    }

    private static void TrySetFloat(Material m, string prop, float val)
    {
        if (m.HasProperty(prop))
        {
            m.SetFloat(prop, val);
        }
    }

    private static void TrySetEmission(Material m, Color color)
    {
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color);
        }
    }

    private static Vector3 FibonacciSphere(int index, int total)
    {
        if (total <= 1) return Vector3.zero;

        float golden = Mathf.PI * (3f - Mathf.Sqrt(5f));
        float y = 1f - (index / (float)(total - 1)) * 2f;
        float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
        float theta = golden * index;

        return new Vector3(Mathf.Cos(theta) * r, y, Mathf.Sin(theta) * r);
    }
}