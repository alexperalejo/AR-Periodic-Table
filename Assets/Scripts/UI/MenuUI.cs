using UnityEngine;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    // ── Screen-space mode buttons (always screen-space overlays) ─────────────────
    public Button btnBohrModel;
    public Button btnElementInfo;
    public Button btnCalculator;
    public Button btnElectrons;
    public Button btnProperties;
    public Button btnCompounds;
    public Button btnBackToMenu;

    // ── Immersive Mode toggle ─────────────────────────────────────────────────────
    // Switches BohrModel and ElementInfoCard between screen-space (default) and
    // AR world-space anchored near the placed periodic table.
    // Properties, Electrons, and Compound Calculator always remain screen-space overlays.
    [Tooltip("Button that toggles Immersive Mode ON/OFF via ImmersiveModeManager.")]
    public Button btnImmersiveToggle;

    void Start()
    {
        // ── AR mode selectors ─────────────────────────────────────────────────────
        btnBohrModel.onClick.AddListener(() =>
            ARModeManager.Instance.SelectMode(ARModeManager.ARMode.BohrModel));

        btnElementInfo.onClick.AddListener(() =>
            ARModeManager.Instance.SelectMode(ARModeManager.ARMode.ElementInfo));

        // "Compound Calculator" button — the unified panel for both formula building and compound lookup
        if (btnCalculator != null)
            btnCalculator.onClick.AddListener(() =>
                ARModeManager.Instance.SelectMode(ARModeManager.ARMode.Calculator));

        btnElectrons.onClick.AddListener(() =>
            ARModeManager.Instance.SelectMode(ARModeManager.ARMode.Electrons));

        btnProperties.onClick.AddListener(() =>
            ARModeManager.Instance.SelectMode(ARModeManager.ARMode.Properties));

        // btnCompounds now routes to the same unified panel; hide it in the scene if not needed
        if (btnCompounds != null)
            btnCompounds.onClick.AddListener(() =>
                ARModeManager.Instance.SelectMode(ARModeManager.ARMode.Calculator));

        if (btnBackToMenu != null)
            btnBackToMenu.onClick.AddListener(() =>
                ARModeManager.Instance.ShowMenu());

        // ── Immersive Mode toggle ─────────────────────────────────────────────────
        // NO runtime listener added here.
        // BtnImmersiveToggle has a persistent (Inspector-wired) onClick listener that
        // calls ImmersiveModeManager.ToggleImmersiveMode() directly on the component.
        // Adding a second listener here would cause a double-toggle (ON→OFF in one
        // frame) with no visible effect. The field is kept for Inspector reference only.
    }
}