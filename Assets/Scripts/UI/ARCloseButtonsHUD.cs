// Assets/Scripts/UI/ARCloseButtonsHUD.cs
//
// Screen-space overlay close buttons for BohrModelRoot and ElementInfoCard.
//
// Each button is only visible when its target object is active in the hierarchy.
// Clicking a button calls ARModeManager.ShowMenu(), which:
//   • hides all AR objects (BohrModelRoot, ElementInfoCard, all panels)
//   • shows the menu panel
//   • does NOT remove the placed periodic table
//
// Works identically in Screen Mode and Immersive Mode because ShowMenu() is
// mode-agnostic — it does not care where in world space the objects are.
//
// Scene wiring (done by SetupARScreenCloseButtons editor tool)
// ─────────────────────────────────────────────────────────────
// Attach to the root GO of the screen-space overlay Canvas that contains the
// close button hierarchy.  Assign bohrModelRoot, elementInfoCard, and the two
// button GameObjects (btnCloseAtom, btnCloseInfo) in the Inspector, or let the
// editor tool fill them automatically.

using UnityEngine;
using UnityEngine.UI;

public class ARCloseButtonsHUD : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────────

    [Header("Target objects — match ARModeManager's serialized fields")]
    [Tooltip("The BohrModelRoot GameObject (same reference as in ARModeManager).")]
    [SerializeField] private GameObject bohrModelRoot;

    [Tooltip("The ElementInfoCard's root GameObject (same reference as in ARModeManager).")]
    [SerializeField] private GameObject elementInfoCard;

    [Header("Button GameObjects (created by SetupARScreenCloseButtons)")]
    [SerializeField] private GameObject btnCloseAtom;
    [SerializeField] private GameObject btnCloseInfo;

    // ── Unity lifecycle ───────────────────────────────────────────────────────────

    private void Start()
    {
        // Wire onClick at runtime as a safety net in case persistent listeners
        // were not set by the editor tool.
        if (btnCloseAtom != null)
        {
            var btn = btnCloseAtom.GetComponentInChildren<Button>(includeInactive: true);
            if (btn != null) btn.onClick.AddListener(CloseAtom);
        }
        if (btnCloseInfo != null)
        {
            var btn = btnCloseInfo.GetComponentInChildren<Button>(includeInactive: true);
            if (btn != null) btn.onClick.AddListener(CloseInfo);
        }
    }

    private void Update()
    {
        // Show each button only while its target object is live in the hierarchy.
        // This works for both Screen Mode and Immersive Mode: ARModeManager.ShowMenu()
        // deactivates these objects, so the buttons disappear automatically after click.
        if (btnCloseAtom != null)
            btnCloseAtom.SetActive(bohrModelRoot    != null && bohrModelRoot.activeInHierarchy);

        if (btnCloseInfo != null)
            btnCloseInfo.SetActive(elementInfoCard  != null && elementInfoCard.activeInHierarchy);
    }

    // ── Click handlers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called when the "Close Atom" button is clicked.
    /// Returns to menu (hides BohrModelRoot) without removing the periodic table.
    /// </summary>
    public void CloseAtom()
    {
        Debug.Log("[ARCloseButtonsHUD] Close Atom clicked — hiding BohrModelRoot.");
        if (ARModeManager.Instance != null)
        {
            ARModeManager.Instance.HideBohrModel();
            ARModeManager.Instance.ShowMenu();
        }
        else if (bohrModelRoot != null)
        {
            Debug.Log($"[HIDE TRACE] ARCloseButtonsHUD.CloseAtom hid BohrModelRoot\n{System.Environment.StackTrace}");
            Debug.Log("[ARCloseButtonsHUD] BohrModelRoot hidden by ARCloseButtonsHUD.CloseAtom (fallback).");
            bohrModelRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Called when the "Close Info" button is clicked.
    /// Hides ElementInfoCard and returns to menu.
    /// </summary>
    public void CloseInfo()
    {
        Debug.Log("[ARCloseButtonsHUD] Close Info clicked — hiding ElementInfoCard.");
        if (ARModeManager.Instance != null)
        {
            ARModeManager.Instance.HideInfoCard();
            ARModeManager.Instance.ShowMenu();
        }
        else if (elementInfoCard != null)
        {
            Debug.Log($"[HIDE TRACE] ARCloseButtonsHUD.CloseInfo hid ElementInfoCard\n{System.Environment.StackTrace}");
            Debug.Log("[ARCloseButtonsHUD] ElementInfoCard hidden by ARCloseButtonsHUD.CloseInfo (fallback).");
            elementInfoCard.SetActive(false);
        }
    }
}
