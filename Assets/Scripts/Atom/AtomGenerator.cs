using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a Bohr-model 3D atom for the element assigned via SetElementData().
///
/// CLEARING GUARANTEE
/// ──────────────────
/// ClearAtom() performs two passes:
///   1. Destroys every object that was tracked in the _spawned list.
///   2. Sweeps all remaining child transforms and destroys any that match
///      known particle / orbit-ring name patterns.
/// This ensures no stale Proton(Clone), Neutron(Clone), Electron(Clone), or
/// OrbitRing(Clone) objects survive between element changes, even if a code
/// path bypassed the tracked list (prefab-baked children, ElementPopupOnGrab
/// calling ClearAtom externally, etc.).
///
/// START vs SET-ELEMENT-DATA ORDERING
/// ───────────────────────────────────
/// bohrModelRoot starts INACTIVE in the scene.  Unity defers Start() until just
/// before the first Update() after activation.  The typical call order is:
///
///   SetActive(true)  →  OnEnable()  →  [same frame] SetElementData()  →
///   [before next Update] Start()
///
/// Without a guard, Start() would call GenerateAtom() a second time with the
/// Inspector default values (Hydrogen), clobbering the correct atom that
/// SetElementData() already built.  The _externalDataSet flag prevents this.
/// </summary>
public class AtomGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject protonPrefab;
    public GameObject neutronPrefab;
    public GameObject electronPrefab;
    public GameObject orbitRingPrefab;

    [Header("Element Data")]
    public string elementName    = "Hydrogen";
    public int    protonCount    = 1;
    public int    neutronCount   = 0;
    public int[]  electronsPerShell = new int[] { 1 };

    [Header("Layout Settings")]
    [Tooltip("Base radius for the nucleus cluster")]
    public float nucleusRadius = 0.06f;
    [Tooltip("Gap between shell centres")]
    public float shellSpacing  = 0.18f;
    [Tooltip("Uniform scale applied to every spawned electron")]
    public float electronScale = 1f;
    [Tooltip("Y offset of the whole atom — leave 0")]
    public float ringYOffset   = 0f;

    [Header("Motion Settings")]
    [Tooltip("Degrees/sec for shell 0 (innermost)")]
    public float innerShellSpeed = 55f;
    [Tooltip("Each outer shell is this many deg/sec slower")]
    public float shellSpeedStep  = 8f;
    [Tooltip("Minimum orbit speed any shell can reach")]
    public float minShellSpeed   = 12f;

    [Header("Visual Settings")]
    [Tooltip("Multiplier on shell radii so rings are never too small/large in AR")]
    public float masterScale   = 1.0f;

    [Tooltip("localScale applied when ImmersiveModeManager.IsImmersive is true. " +
             "Overrides masterScale in AR world space so element changes do not reset " +
             "the size the user chose via ARWorldObjectManipulator.")]
    public float arWorldScale  = 0.03f;

    [Tooltip("When true, ApplyAdaptiveScale does NOT modify localScale. " +
             "Set by ImmersiveModeManager after it has positioned and scaled the root, " +
             "so subsequent element taps do not reset the user-chosen world size.")]
    public bool suppressAutoScale = false;
    [Tooltip("Width of each orbit ring line")]
    public float ringLineWidth = 0.008f;
    [Tooltip("Tilt X added per shell (degrees)")]
    public float shellTiltXStep = 40f;
    [Tooltip("Tilt Z added per shell (degrees)")]
    public float shellTiltZStep = 63f;

    // ── Private state ─────────────────────────────────────────────────────────────

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

    // Every GameObject spawned by this generator is tracked here so ClearAtom()
    // can destroy them reliably.
    private readonly List<GameObject> _spawned = new List<GameObject>();

    // Set to true by SetElementData() so that Start() does not re-generate the
    // atom with Inspector default values after external code already built the
    // correct element (see class-level doc above).
    private bool _externalDataSet;

    // ── Unity lifecycle ───────────────────────────────────────────────────────────

    private void Start()
    {
        // Do NOT auto-generate from Inspector defaults.
        // The atom stays empty until the user taps an element, which calls
        // SetElementData(). This prevents a Hydrogen atom flashing on screen
        // the moment Bohr Model mode is entered (before any element is selected).
        if (!_externalDataSet)
        {
            Debug.Log("[AtomGenerator] Start(): no element selected yet — atom is empty until tap.");
            ClearAtom(); // ensure no stale particles from a previous session
        }
        else
        {
            // SetElementData was called before Start fired — the correct atom is
            // already built. Nothing to do.
            Debug.Log($"[AtomGenerator] Start(): element already set to '{elementName}' — keeping it.");
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by ElementLoader to assign element data and rebuild the atom.
    /// Clears any previously spawned particles before generating the new ones.
    /// </summary>
    public void SetElementData(string newName, int newProtons, int newNeutrons,
                               int[] newElectronsPerShell)
    {
        // ── Same-element guard ────────────────────────────────────────────────────
        // If the user taps the same element that is already displayed, skip the full
        // GenerateAtom() rebuild (ClearAtom + re-spawn).  This prevents the particle
        // flicker / orbit reset that would otherwise occur on every repeated tap.
        bool sameElement = _externalDataSet
                        && newProtons == protonCount
                        && _spawned.Count > 0;

        if (sameElement)
        {
            Debug.Log($"[AtomGenerator] Same element ({newName}, Z={newProtons}) tapped again — skipping rebuild.");
            return;
        }

        _externalDataSet = true;

        elementName  = newName;
        protonCount  = Mathf.Max(0, newProtons);
        neutronCount = Mathf.Max(0, newNeutrons);

        // ── Validate / normalise shell data ───────────────────────────────────────
        // Strip trailing zeros (some JSON sources include 7 entries like [2,4,0,0,0,0,0]).
        // Fall back to Bohr-rule computation if the array is null, empty, or sums
        // to a different total than the proton count.
        electronsPerShell = NormaliseShells(newElectronsPerShell, protonCount);

        GenerateAtom();
    }

    /// <summary>
    /// Destroys every previously spawned particle, then regenerates the atom
    /// from the current protonCount / neutronCount / electronsPerShell values.
    /// </summary>
    [ContextMenu("Generate Atom")]
    public void GenerateAtom()
    {
        ClearAtom();
        ApplyAdaptiveScale();
        GenerateNucleus();
        GenerateShellsAndElectrons();

        // ── Generation summary log ────────────────────────────────────────────────
        int totalElectrons = 0;
        if (electronsPerShell != null)
            foreach (int e in electronsPerShell) totalElectrons += e;

        string shellStr = electronsPerShell != null && electronsPerShell.Length > 0
            ? "[" + string.Join(",", electronsPerShell) + "]"
            : "[]";

        Debug.Log($"[AtomGenerator] Generated {elementName}: " +
                  $"p={protonCount} n={neutronCount} e={totalElectrons} shells={shellStr}");
    }

    /// <summary>
    /// Destroys ALL particles spawned by this generator.
    /// Pass 1 destroys tracked objects; Pass 2 sweeps child transforms by name
    /// to catch any that escaped the tracked list.
    /// </summary>
    [ContextMenu("Clear Atom")]
    public void ClearAtom()
    {
        // ── Pass 1: destroy tracked spawns ────────────────────────────────────────
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i] == null) continue;

            if (Application.isPlaying)
                Destroy(_spawned[i]);
            else
                DestroyImmediate(_spawned[i]);
        }
        _spawned.Clear();

        // ── Pass 2: safety sweep of remaining children ────────────────────────────
        // Catches prefab-baked children, objects from old AtomGenerator versions,
        // or any particle whose reference was dropped from _spawned externally.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            string n = child.name;

            bool isParticle = n.Contains("Proton")    || n.Contains("Neutron")  ||
                              n.Contains("Electron")   || n.Contains("Orbit")    ||
                              n.Contains("(Clone)");

            if (!isParticle) continue;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    // ── Shell computation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Distributes <paramref name="atomicNumber"/> electrons into shells using the
    /// standard Bohr-model capacities (2 / 8 / 18 / 32 / 32 / 18 / 8).
    ///
    /// Examples:
    ///   H (Z=1)  → [1]
    ///   C (Z=6)  → [2, 4]
    ///   O (Z=8)  → [2, 6]
    ///   Na (Z=11)→ [2, 8, 1]
    /// </summary>
    public static int[] ComputeBohrShells(int atomicNumber)
    {
        if (atomicNumber <= 0) return new int[0];

        int[] capacity  = { 2, 8, 18, 32, 32, 18, 8 };
        var   shells    = new List<int>();
        int   remaining = atomicNumber;

        for (int s = 0; s < capacity.Length && remaining > 0; s++)
        {
            int fill = Mathf.Min(remaining, capacity[s]);
            shells.Add(fill);
            remaining -= fill;
        }

        return shells.ToArray();
    }

    // ── Private helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Strips trailing zeros from a shell array and validates that its sum equals
    /// <paramref name="protonCount"/>.  Returns a Bohr-computed fallback when the
    /// input is null, empty, or sum-mismatched.
    /// </summary>
    private static int[] NormaliseShells(int[] raw, int protonCount)
    {
        if (raw == null || raw.Length == 0)
            return ComputeBohrShells(protonCount);

        // Strip trailing zeros
        int last = raw.Length - 1;
        while (last > 0 && raw[last] == 0) last--;

        // If the whole array was zeros (e.g. only entry was 0), compute from scratch
        if (last == 0 && raw[0] == 0)
            return ComputeBohrShells(protonCount);

        int[] trimmed = new int[last + 1];
        System.Array.Copy(raw, trimmed, last + 1);

        // Validate sum == electron count for a neutral atom
        int sum = 0;
        foreach (int e in trimmed) sum += e;

        if (sum != protonCount)
        {
            Debug.LogWarning($"[AtomGenerator] Shell sum {sum} ≠ proton count {protonCount} " +
                             $"— recomputing from Bohr rules.");
            return ComputeBohrShells(protonCount);
        }

        return trimmed;
    }

    /// <summary>
    /// Re-applies scale without rebuilding particle geometry.
    /// Call after modifying arWorldScale (e.g. from ARWorldObjectManipulator) to
    /// update localScale immediately without destroying and re-spawning particles.
    /// </summary>
    public void ApplyScaleOnly() => ApplyAdaptiveScale();

    private void ApplyAdaptiveScale()
    {
        // When suppressAutoScale is true, ImmersiveModeManager (or the user via
        // ARWorldObjectManipulator) owns the localScale.  Skip so element taps
        // never silently reset the world size the user has chosen.
        if (suppressAutoScale)
        {
            Debug.Log($"[AtomGenerator] ApplyAdaptiveScale skipped for '{elementName}' — suppressAutoScale=true.");
            return;
        }

        int totalElectrons = 0;
        if (electronsPerShell != null)
            foreach (int e in electronsPerShell) totalElectrons += e;

        float adaptive = Mathf.Lerp(0.85f, 1.30f, Mathf.InverseLerp(1, 118, totalElectrons));

        // In Immersive Mode the atom is a world-space AR object — use arWorldScale
        // so it appears at a sensible physical size and ARWorldObjectManipulator
        // adjustments persist across element changes.
        // In Screen Mode use masterScale (the Inspector-authored value, default 1.0).
        bool   inImmersive = ImmersiveModeManager.Instance != null
                          && ImmersiveModeManager.Instance.IsImmersive;
        float  baseScale   = inImmersive ? arWorldScale : masterScale;

        transform.localScale = Vector3.one * (baseScale * adaptive);
        Debug.Log($"[AtomGenerator] Bohr scale applied: baseScale={baseScale:F4} adaptive={adaptive:F3} " +
                  $"→ localScale={transform.localScale.x:F4} (immersive={inImmersive})");
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

    private void GenerateShellsAndElectrons()
    {
        if (orbitRingPrefab == null || electronPrefab == null) return;
        if (electronsPerShell == null || electronsPerShell.Length == 0) return;

        for (int s = 0; s < electronsPerShell.Length; s++)
        {
            int shellCount = electronsPerShell[s];

            // Skip shells with no electrons — don't spawn a visible ring for them
            if (shellCount <= 0) continue;

            float radius    = shellSpacing * (s + 1);
            Color shellColor = ShellColors[s % ShellColors.Length];

            // ── Orbit ring ────────────────────────────────────────────────────────
            GameObject ring = Instantiate(orbitRingPrefab, transform);
            ring.transform.localPosition   = new Vector3(0f, ringYOffset, 0f);
            ring.transform.localEulerAngles = new Vector3(
                s * shellTiltXStep, 0f, s * shellTiltZStep);

            OrbitRing orbitScript = ring.GetComponent<OrbitRing>();
            if (orbitScript != null)
            {
                orbitScript.radius    = radius;
                orbitScript.lineWidth = ringLineWidth;
                orbitScript.SetRadius(radius);
                orbitScript.SetColor(shellColor);
            }

            // Remove any legacy OrbitSpin — ElectronOrbit drives rotation instead
            OrbitSpin spin = ring.GetComponent<OrbitSpin>();
            if (spin != null)
            {
                if (Application.isPlaying) Destroy(spin);
                else DestroyImmediate(spin);
            }

            _spawned.Add(ring);

            // ── Electrons on this shell ───────────────────────────────────────────
            float speed       = Mathf.Max(minShellSpeed, innerShellSpeed - s * shellSpeedStep);
            float signedSpeed = (s % 2 == 0) ? speed : -speed; // alternate orbit direction

            for (int e = 0; e < shellCount; e++)
            {
                float startAngle = (360f / Mathf.Max(1, shellCount)) * e;

                GameObject electron = Instantiate(electronPrefab, transform);
                electron.transform.localPosition = Vector3.zero;
                electron.transform.localRotation = Quaternion.identity;
                electron.transform.localScale    =
                    electronPrefab.transform.localScale * electronScale;

                ElectronOrbit orbit = electron.GetComponent<ElectronOrbit>()
                                   ?? electron.AddComponent<ElectronOrbit>();

                orbit.center        = transform;
                orbit.ringTransform = ring.transform;
                orbit.radius        = radius;
                orbit.yOffset       = ringYOffset;
                orbit.angleOffset   = startAngle;
                orbit.speed         = signedSpeed;

                StyleElectron(electron, shellColor);
                _spawned.Add(electron);
            }
        }
    }

    // ── Styling ───────────────────────────────────────────────────────────────────

    private void StyleNucleon(GameObject go, bool isProton)
    {
        Renderer r = go.GetComponentInChildren<Renderer>();
        if (r == null) return;

        Material m = new Material(
            r.sharedMaterial != null
                ? r.sharedMaterial
                : new Material(Shader.Find("Universal Render Pipeline/Lit")));

        Color baseColor = isProton
            ? new Color(0.92f, 0.22f, 0.16f)   // red  — proton
            : new Color(0.20f, 0.46f, 0.80f);   // blue — neutron

        m.color = baseColor;
        TrySetFloat(m, "_Smoothness", 0.82f);
        TrySetFloat(m, "_Metallic",   0.05f);
        TrySetEmission(m, baseColor * 0.35f);

        r.material = m;
    }

    private void StyleElectron(GameObject go, Color color)
    {
        Renderer r = go.GetComponentInChildren<Renderer>();
        if (r == null) return;

        Material m = new Material(
            r.sharedMaterial != null
                ? r.sharedMaterial
                : new Material(Shader.Find("Universal Render Pipeline/Lit")));

        m.color = color;
        TrySetFloat(m, "_Smoothness", 0.95f);
        TrySetFloat(m, "_Metallic",   0.0f);
        TrySetEmission(m, color * 1.4f);

        r.material = m;
    }

    private static void TrySetFloat(Material m, string prop, float val)
    {
        if (m.HasProperty(prop)) m.SetFloat(prop, val);
    }

    private static void TrySetEmission(Material m, Color color)
    {
        if (!m.HasProperty("_EmissionColor")) return;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", color);
    }

    // ── Geometry helpers ──────────────────────────────────────────────────────────

    /// Evenly distributes <paramref name="total"/> points on a sphere surface
    /// using the Fibonacci spiral method.
    private static Vector3 FibonacciSphere(int index, int total)
    {
        if (total <= 1) return Vector3.zero;

        float golden = Mathf.PI * (3f - Mathf.Sqrt(5f));
        float y      = 1f - (index / (float)(total - 1)) * 2f;
        float r      = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
        float theta  = golden * index;

        return new Vector3(Mathf.Cos(theta) * r, y, Mathf.Sin(theta) * r);
    }
}
