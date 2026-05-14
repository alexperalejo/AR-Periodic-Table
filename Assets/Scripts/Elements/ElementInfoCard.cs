//Ties tile tap -> Show info card + load element

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ElementInfoCard : MonoBehaviour
{
    [Header("Text Fields")]
    public TMP_Text titleText;
    public TMP_Text symbolText;
    public TMP_Text atomicNumberText;
    public TMP_Text atomicMassText;
    public TMP_Text groupText;
    public TMP_Text densityText;
    public TMP_Text meltingPointText;
    public TMP_Text boilingPointText;
    public TMP_Text electronConfigText;

    [Header("Bohr Model")]
    public ElementLoader elementLoader;

    [Header("AR Readability")]
    [Tooltip("All TMP_Text children will be set to at least this font size when the card is shown in AR.")]
    [SerializeField] private float minARFontSize = 24f;
    [Tooltip("Adds a dark background Image to the card root if none exists.")]
    [SerializeField] private bool autoAddBackground = true;
    [SerializeField] private Color backgroundColour = new Color(0.08f, 0.08f, 0.12f, 0.82f);

    private bool _backgroundInitialised;

    private void Awake()
    {
        EnsureBackground();
    }

    public void ShowCard(
     string elementName,
     string symbol,
     int atomicNumber,
     string atomicMass,
     string group,
     string density,
     string meltingPoint,
     string boilingPoint,
     string electronConfig
 )
    {
        // Activate FIRST so the card is visible even if some text fields are unassigned.
        gameObject.SetActive(true);

        // Null-guard every field — a missing Inspector wire logs a warning instead
        // of throwing a NullReferenceException that would prevent SetActive from running.
        SetText(titleText,          elementName,                      "titleText");
        SetText(symbolText,         "Symbol: " + symbol,              "symbolText");
        SetText(atomicNumberText,   "Atomic Number: " + atomicNumber, "atomicNumberText");
        SetText(atomicMassText,     "Atomic Mass: " + atomicMass,     "atomicMassText");
        SetText(groupText,          "Group: " + group,                "groupText");
        SetText(densityText,        "Density: " + density,            "densityText");
        SetText(meltingPointText,   "Melting Point: " + meltingPoint, "meltingPointText");
        SetText(boilingPointText,   "Boiling Point: " + boilingPoint, "boilingPointText");
        SetText(electronConfigText, "Electron Config: " + electronConfig, "electronConfigText");

        EnforceMinFontSize();
        Debug.Log($"[ElementInfoCard] ShowCard: activated for {elementName} (Z={atomicNumber}).");
    }

    private void SetText(TMP_Text field, string value, string fieldName)
    {
        if (field != null)
            field.text = value;
        else
            Debug.LogWarning($"[ElementInfoCard] '{fieldName}' is not assigned in Inspector — text skipped.");
    }

    public void HideCard()
    {
        Debug.Log($"[HIDE TRACE] ElementInfoCard.HideCard hid ElementInfoCard\n{System.Environment.StackTrace}");
        gameObject.SetActive(false);
        Debug.Log("[ElementInfoCard] Info closed.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    /// <summary>Ensures every TMP_Text child meets the minimum AR font size.</summary>
    private void EnforceMinFontSize()
    {
        var texts = GetComponentsInChildren<TMP_Text>(includeInactive: true);
        foreach (var t in texts)
        {
            if (t.fontSize < minARFontSize)
                t.fontSize = minARFontSize;
        }
    }

    /// <summary>
    /// Adds a semi-transparent background Image to the card root if autoAddBackground
    /// is enabled and no Image is already present on this GameObject.
    /// </summary>
    private void EnsureBackground()
    {
        if (!autoAddBackground || _backgroundInitialised) return;
        _backgroundInitialised = true;

        // ElementInfoCard root uses a regular Transform (not RectTransform).
        // Calling AddComponent<Image> on a non-RectTransform silently converts
        // it to a RectTransform, which destroys the world-space position set by
        // ImmersiveModeManager and makes the card invisible. Skip if no RectTransform.
        if (GetComponent<RectTransform>() == null) return;

        if (GetComponent<Image>() != null) return;

        var cr = GetComponent<CanvasRenderer>() ?? gameObject.AddComponent<CanvasRenderer>();
        _ = cr;
        var img   = gameObject.AddComponent<Image>();
        img.color = backgroundColour;
        img.transform.SetAsFirstSibling();
    }
}