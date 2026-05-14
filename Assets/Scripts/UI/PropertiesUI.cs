using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PeriodicAR.Data;

public class PropertiesUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text elementNameText;
    public TMP_Text symbolText;
    public TMP_Text categoryText;
    public TMP_Text phaseText;
    public TMP_Text appearanceText;
    public TMP_Text electronegativityText;
    public TMP_Text electronAffinityText;
    public TMP_Text molarHeatText;
    public TMP_Text discoveredByText;
    public TMP_Text namedByText;
    public TMP_Text summaryText;
    public Button btnBack;

    void Start()
    {
        btnBack.onClick.AddListener(() => ARModeManager.Instance.ShowMenu());
    }

    public void ShowProperties(AtomElementData el)
    {
        Debug.Log($"[PropertiesUI] ShowProperties('{el.name}') — gameObject.activeInHierarchy={gameObject.activeInHierarchy}");

        if (elementNameText == null) { Debug.LogError("[PropertiesUI] elementNameText is NULL"); return; }
        if (symbolText      == null) { Debug.LogError("[PropertiesUI] symbolText is NULL"); return; }
        if (summaryText     == null) { Debug.LogError("[PropertiesUI] summaryText is NULL"); return; }

        elementNameText.text = el.name;
        symbolText.text = "Symbol: " + el.symbol;
        categoryText.text = "Category: " + (string.IsNullOrEmpty(el.category) ? "N/A" : el.category);
        phaseText.text = "Phase: " + (string.IsNullOrEmpty(el.phase) ? "N/A" : el.phase);
        appearanceText.text = "Appearance: " + (string.IsNullOrEmpty(el.appearance) ? "N/A" : el.appearance);
        electronegativityText.text = "Electronegativity: " + (el.electronegativity_pauling > 0 ? el.electronegativity_pauling.ToString("F2") : "N/A");
        electronAffinityText.text = "Electron Affinity: " + (el.electron_affinity != 0 ? el.electron_affinity.ToString("F3") + " kJ/mol" : "N/A");
        molarHeatText.text = "Molar Heat: " + (el.molar_heat > 0 ? el.molar_heat.ToString("F3") + " J/mol\u00b7K" : "N/A");
        discoveredByText.text = "Discovered By: " + (string.IsNullOrEmpty(el.discovered_by) ? "N/A" : el.discovered_by);
        namedByText.text = "Named By: " + (string.IsNullOrEmpty(el.named_by) ? "N/A" : el.named_by);
        summaryText.text = el.summary;

        Debug.Log($"[PropertiesUI] Text set. elementNameText='{elementNameText.text}' " +
                  $"activeInHierarchy={elementNameText.gameObject.activeInHierarchy} " +
                  $"enabled={elementNameText.enabled}");

        // Walk up and log each ancestor's active state to spot any inactive node.
        var t = elementNameText.transform.parent;
        while (t != null)
        {
            Debug.Log($"[PropertiesUI] Ancestor '{t.name}' activeSelf={t.gameObject.activeSelf}");
            t = t.parent;
        }

        gameObject.SetActive(true);
    }
}
