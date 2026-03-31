
// Adds a comet-glow TrailRenderer to each electron.
// AtomGenerator.StyleElectron() sets trailColor before first frame.
// No manual setup needed.

using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class ElectronTrailFX : MonoBehaviour
{
    [Header("Trail Shape")]
    [Range(0.005f, 0.04f)] public float trailStartWidth = 0.018f;
    [Range(0f, 0.01f)] public float trailEndWidth = 0f;
    [Range(0.06f, 0.45f)] public float trailTime = 0.20f;

    [Header("Colour (set by AtomGenerator)")]
    public Color trailColor = new Color(0.1f, 0.9f, 1f);

    [Range(0f, 1f)] public float trailAlpha = 0.80f;

    private TrailRenderer _trail;
    private bool _configured;

    void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
        Configure();
    }

    void Start()
    {
        // Colour might be set after Awake by AtomGenerator — apply here too
        if (!_configured) Configure();
        ApplyColor();
    }

    // Called by AtomGenerator after Instantiate
    void OnEnable()
    {
        if (_trail != null) ApplyColor();
    }

    private void Configure()
    {
        if (_trail == null) _trail = GetComponent<TrailRenderer>();
        if (_trail == null) return;

        _trail.time = trailTime;
        _trail.startWidth = trailStartWidth;
        _trail.endWidth = trailEndWidth;
        _trail.minVertexDistance = 0.003f;
        _trail.numCapVertices = 4;
        _trail.numCornerVertices = 4;
        _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _trail.receiveShadows = false;

        // Build material — try URP Particles first, then fallbacks
        Material mat = TryBuildMaterial();
        if (mat != null) _trail.material = mat;

        _configured = true;
    }

    private void ApplyColor()
    {
        if (_trail == null) return;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white,          0.00f),
                new GradientColorKey(trailColor,           0.15f),
                new GradientColorKey(trailColor * 0.55f,   1.00f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(trailAlpha,           0.00f),
                new GradientAlphaKey(trailAlpha * 0.55f,   0.55f),
                new GradientAlphaKey(0f,                   1.00f),
            }
        );
        _trail.colorGradient = g;

        if (_trail.material != null)
        {
            if (_trail.material.HasProperty("_TintColor"))
                _trail.material.SetColor("_TintColor", trailColor);
            if (_trail.material.HasProperty("_Color"))
                _trail.material.SetColor("_Color", trailColor);
        }
    }

    private static Material TryBuildMaterial()
    {
        string[] candidates = {
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Particles/Additive",
            "Sprites/Default",
        };
        foreach (string s in candidates)
        {
            Shader sh = Shader.Find(s);
            if (sh != null) return new Material(sh);
        }
        return null;
    }
}