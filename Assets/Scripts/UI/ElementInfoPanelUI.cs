using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PeriodicAR.Data;

/// <summary>
/// Drives the ElementInfoPanel — a scrollable, resizable panel that shows
/// a selected element's full details.
///
/// Text is split into three TMP fields so each can be styled independently:
///   <see cref="titleText"/>       — element name + symbol (large, centred)
///   <see cref="propertiesText"/>  — key properties, one per line
///   <see cref="descriptionText"/> — full summary paragraph
///
/// Call <see cref="ShowElement"/> to populate and activate the panel.
/// The panel hides itself when the Back button is pressed.
/// </summary>
public class ElementInfoPanelUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text titleText;
    public TMP_Text propertiesText;
    public TMP_Text descriptionText;
    public Button   btnBack;

    void Start()
    {
        if (btnBack != null)
            btnBack.onClick.AddListener(() => gameObject.SetActive(false));
    }

    public void ShowElement(AtomElementData el)
    {
        if (el == null) return;

        if (titleText != null)
            titleText.text = $"{el.name}  ({el.symbol})";

        if (propertiesText != null)
        {
            propertiesText.text =
                $"Atomic Number: {el.number}\n" +
                $"Category: {Val(el.category)}\n" +
                $"Phase: {Val(el.phase)}\n" +
                $"Appearance: {Val(el.appearance)}\n" +
                $"Electronegativity: {(el.electronegativity_pauling > 0 ? el.electronegativity_pauling.ToString("F2") : "N/A")}\n" +
                $"Electron Affinity: {(el.electron_affinity != 0 ? el.electron_affinity.ToString("F3") + " kJ/mol" : "N/A")}\n" +
                $"Molar Heat: {(el.molar_heat > 0 ? el.molar_heat.ToString("F3") + " J/mol·K" : "N/A")}\n" +
                $"Discovered By: {Val(el.discovered_by)}\n" +
                $"Named By: {Val(el.named_by)}";
        }

        if (descriptionText != null)
            descriptionText.text = string.IsNullOrEmpty(el.summary) ? "" : el.summary;

        gameObject.SetActive(true);
    }

    static string Val(string s) => string.IsNullOrEmpty(s) ? "N/A" : s;
}
