using PeriodicAR.Data;
using UnityEngine;
using UnityEngine.InputSystem;

public class ARModeManager : MonoBehaviour
{
    public enum ARMode
    {
        None,
        BohrModel,
        ElementInfo,
        Calculator,
        Electrons,
        Properties,
        Compounds,
        Categories
    }

    public static ARModeManager Instance;

    [Header("Current Mode")]
    public ARMode currentMode = ARMode.None;

    [Header("Main UI")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject compoundCalculatorPanel; // unified Calculator + Compounds panel
    [SerializeField] private GameObject electronsPanel;
    [SerializeField] private GameObject propertiesPanel;
    [SerializeField] private GameObject filterBar;

    [Header("Element / Atom Systems")]
    [SerializeField] private GameObject bohrModelRoot;
    [SerializeField] private ElementInfoCard elementInfoCard;
    [SerializeField] private ElementLoader elementLoader;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        ShowMenu();
    }

    private void Update()
    {
        // Editor keyboard testing only
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SelectMode(ARMode.ElementInfo);

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SelectMode(ARMode.Calculator);

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SelectMode(ARMode.Electrons);

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            Debug.Log("[ARModeManager] Digit 4 pressed → SelectMode(Properties)");
            SelectMode(ARMode.Properties);
        }

        if (Keyboard.current.digit5Key.wasPressedThisFrame)
            SelectMode(ARMode.Compounds);

        if (Keyboard.current.digit6Key.wasPressedThisFrame)
            SelectMode(ARMode.Categories);

        if (Keyboard.current.bKey.wasPressedThisFrame)
            SelectMode(ARMode.BohrModel);

        if (Keyboard.current.hKey.wasPressedThisFrame)
            OnElementSelected(1); // Hydrogen

        if (Keyboard.current.cKey.wasPressedThisFrame)
            OnElementSelected(6); // Carbon

        if (Keyboard.current.oKey.wasPressedThisFrame)
            OnElementSelected(8); // Oxygen

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            ShowMenu();
    }

    // ── Public getters so other systems (ARHudController) can access AR objects ────

    /// <summary>The BohrModelRoot GameObject managed by this ARModeManager.</summary>
    public GameObject BohrModelRootGO => bohrModelRoot;

    /// <summary>The ElementInfoCard managed by this ARModeManager.</summary>
    public ElementInfoCard ElementInfoCardRef => elementInfoCard;

    // ── Screen-only panel hiding (NEVER touches AR world objects) ────────────────

    /// <summary>
    /// Hides only the screen-space overlay panels (menu, calculator, electrons,
    /// properties, filter bar).  Does NOT touch BohrModelRoot or ElementInfoCard —
    /// those are AR world objects with their own lifecycle.
    /// </summary>
    private void HideScreenPanels()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (compoundCalculatorPanel != null) compoundCalculatorPanel.SetActive(false);
        if (electronsPanel != null) electronsPanel.SetActive(false);
        if (propertiesPanel != null) propertiesPanel.SetActive(false);
        if (filterBar != null) filterBar.SetActive(false);
        Debug.Log("[ARModeManager] HideScreenPanels called.");
    }

    // ── AR object hiding (called explicitly, never from HideScreenPanels) ────────

    /// <summary>Hide BohrModelRoot and notify ImmersiveModeManager to reset its position flag.</summary>
    public void HideBohrModel()
    {
        if (bohrModelRoot != null && bohrModelRoot.activeSelf)
        {
            Debug.Log($"[HIDE TRACE] ARModeManager.HideBohrModel hid BohrModelRoot\n{System.Environment.StackTrace}");
            bohrModelRoot.SetActive(false);
            Debug.Log("[ARModeManager] HideBohrModel: BohrModelRoot hidden.");
        }
        ImmersiveModeManager.Instance?.NotifyBohrHidden();
    }

    /// <summary>Hide ElementInfoCard and notify ImmersiveModeManager to reset its position flag.</summary>
    public void HideInfoCard()
    {
        if (elementInfoCard != null && elementInfoCard.gameObject.activeSelf)
        {
            Debug.Log($"[HIDE TRACE] ARModeManager.HideInfoCard hid ElementInfoCard\n{System.Environment.StackTrace}");
            elementInfoCard.gameObject.SetActive(false);
            Debug.Log("[ARModeManager] HideInfoCard: ElementInfoCard hidden.");
        }
        ImmersiveModeManager.Instance?.NotifyInfoHidden();
    }

    public void ShowMenu()
    {
        currentMode = ARMode.None;
        // Only show the menu overlay — NEVER touch AR world objects here.
        // BohrModelRoot and ElementInfoCard are independent world objects managed
        // by HideBohrModel() / HideInfoCard() (called only by Close buttons or
        // explicit mode switches). Calling ShowMenu must never destroy visible AR content.
        HideScreenPanels();
        if (menuPanel != null)
            menuPanel.SetActive(true);
        Debug.Log("[ARModeManager] ShowMenu: menu panel shown. AR objects NOT touched.");
    }

    public void SelectMode(ARMode mode)
    {
        ARMode prevMode = currentMode;
        currentMode = mode;
        Debug.Log($"[ARModeManager] SelectMode({mode}) — propertiesPanel={(propertiesPanel != null ? propertiesPanel.name : "NULL")} active={propertiesPanel?.activeSelf}");
        HideScreenPanels();

        // Hide AR objects only when the mode actively switches AWAY from them.
        // Staying in BohrModel mode (e.g. tapping another element) must NOT hide the atom.
        if (prevMode == ARMode.BohrModel && mode != ARMode.BohrModel)
            HideBohrModel();
        if (prevMode == ARMode.ElementInfo && mode != ARMode.ElementInfo)
            HideInfoCard();

        switch (mode)
        {
            case ARMode.BohrModel:
                if (bohrModelRoot != null)
                {
                    bohrModelRoot.SetActive(true);
                    // Position and scale in world space immediately.
                    ImmersiveModeManager.Instance?.PlaceArObjectsNow();
                    Debug.Log("[ARModeManager] BohrModelRoot shown — PlaceArObjectsNow called.");
                }
                break;

            case ARMode.ElementInfo:
                if (elementInfoCard != null)
                {
                    elementInfoCard.gameObject.SetActive(true);
                    // Position and scale in world space immediately.
                    ImmersiveModeManager.Instance?.PlaceArObjectsNow();
                    Debug.Log("[ARModeManager] ElementInfoCard shown — PlaceArObjectsNow called.");
                }
                break;

            case ARMode.Calculator:
            case ARMode.Compounds: // both modes use the same unified panel
                if (compoundCalculatorPanel != null) compoundCalculatorPanel.SetActive(true);
                break;

            case ARMode.Electrons:
                if (electronsPanel != null) electronsPanel.SetActive(true);
                break;

            case ARMode.Properties:
                if (propertiesPanel != null) propertiesPanel.SetActive(true);
                break;

            case ARMode.Categories:
                if (filterBar != null) filterBar.SetActive(true);
                break;

            default:
                ShowMenu();
                break;
        }
    }

    public void ShowBohrModel()
    {
        SelectMode(ARMode.BohrModel);
    }

    public void ShowElementInfo()
    {
        SelectMode(ARMode.ElementInfo);
    }

    public void ShowCompoundCalculator()
    {
        SelectMode(ARMode.Calculator);
    }

    // Kept for backward compatibility with any existing event references
    public void ShowCalculator() => ShowCompoundCalculator();

    public void ShowElectrons()
    {
        SelectMode(ARMode.Electrons);
    }

    public void ShowProperties()
    {
        SelectMode(ARMode.Properties);
    }

    // Kept for backward compatibility; routes to the unified CompoundCalculator panel
    public void ShowCompounds() => ShowCompoundCalculator();

    public void ShowCategories()
    {
        SelectMode(ARMode.Categories);
    }

    public void OnElementSelected(int atomicNumber)
    {
        // ── Diagnostic: log every tap so you can see exactly which gate stops it ──
        Debug.Log($"[ARModeManager] OnElementSelected({atomicNumber}) — currentMode={currentMode} " +
                  $"elementLoader={(elementLoader != null ? elementLoader.name : "NULL")} " +
                  $"infoCard={(elementLoader != null && elementLoader.infoCard != null ? elementLoader.infoCard.name : "NULL")} " +
                  $"elementInfoCard={(elementInfoCard != null ? elementInfoCard.name : "NULL")}");

        if (currentMode == ARMode.None)
        {
            Debug.LogWarning("[ARModeManager] OnElementSelected: currentMode is None — " +
                             "press Element Info or Bohr Model button first.");
            return;
        }

        if (elementLoader != null)
        {
            elementLoader.showInfoCard = (currentMode == ARMode.ElementInfo);
            elementLoader.showAtom = (currentMode == ARMode.BohrModel);

            if (currentMode == ARMode.BohrModel || currentMode == ARMode.ElementInfo)
            {
                bool alreadyVisible = currentMode == ARMode.BohrModel
                    ? (bohrModelRoot != null && bohrModelRoot.activeSelf)
                    : (elementInfoCard != null && elementInfoCard.gameObject.activeSelf);
                Debug.Log($"[ARModeManager] {(alreadyVisible ? "Updating" : "Showing")} {currentMode} for element Z={atomicNumber}.");
                elementLoader.LoadElement(atomicNumber);

                // Re-apply world-space scale after GenerateAtom may have rebuilt the atom.
                // PlaceArObjectsNow respects suppressAutoScale, so the user-chosen size
                // is preserved if suppressAutoScale is already true.
                ImmersiveModeManager.Instance?.PlaceArObjectsNow();
            }
        }

        if (currentMode == ARMode.Calculator || currentMode == ARMode.Compounds)
            AddElementToCompoundCalculator(atomicNumber);

        if (currentMode == ARMode.Electrons)
            AddElementToElectrons(atomicNumber);

        if (currentMode == ARMode.Properties)
            AddElementToProperties(atomicNumber);
    }

    private void AddElementToCompoundCalculator(int atomicNumber)
    {
        if (compoundCalculatorPanel == null)
        {
            Debug.LogError("CompoundCalculatorPanel is not assigned.");
            return;
        }

        CompoundCalculatorUI ui = compoundCalculatorPanel.GetComponentInChildren<CompoundCalculatorUI>(true);

        if (ui == null)
        {
            Debug.LogError("CompoundCalculatorUI not found on CompoundCalculatorPanel or its children.");
            return;
        }

        TextAsset json = Resources.Load<TextAsset>("PeriodicTableJSON");

        if (json == null)
        {
            Debug.LogError("PeriodicTableJSON not found in Resources folder.");
            return;
        }

        PeriodicTable table = JsonUtility.FromJson<PeriodicTable>(json.text);

        foreach (AtomElementData el in table.elements)
        {
            if (el.number == atomicNumber)
            {
                ui.AddElement(el.symbol);
                return;
            }
        }

        Debug.LogWarning($"Element with atomic number {atomicNumber} not found.");
    }

    private void AddElementToElectrons(int atomicNumber)
    {
        if (electronsPanel == null)
        {
            Debug.LogError("ElectronsPanel is not assigned.");
            return;
        }

        ElectronsUI electronsUI = electronsPanel.GetComponentInChildren<ElectronsUI>(true);

        if (electronsUI == null)
        {
            Debug.LogError("ElectronsUI not found on ElectronsPanel or its children.");
            return;
        }

        TextAsset json = Resources.Load<TextAsset>("PeriodicTableJSON");
        if (json == null)
        {
            Debug.LogError("PeriodicTableJSON not found.");
            return;
        }

        PeriodicTable table = JsonUtility.FromJson<PeriodicTable>(json.text);
        foreach (AtomElementData el in table.elements)
        {
            if (el.number == atomicNumber)
            {
                electronsUI.ShowElectrons(el.name, el.electron_configuration);
                return;
            }
        }

        Debug.LogWarning($"Element {atomicNumber} not found.");
    }

    private void AddElementToProperties(int atomicNumber)
    {
        Debug.Log($"[ARModeManager] AddElementToProperties({atomicNumber}) called.");

        if (propertiesPanel == null)
        {
            Debug.LogError("[ARModeManager] propertiesPanel is NULL — not assigned in Inspector.");
            return;
        }
        Debug.Log($"[ARModeManager] propertiesPanel='{propertiesPanel.name}' activeSelf={propertiesPanel.activeSelf} activeInHierarchy={propertiesPanel.activeInHierarchy}");

        PropertiesUI propertiesUI = propertiesPanel.GetComponentInChildren<PropertiesUI>(true);
        if (propertiesUI == null)
        {
            Debug.LogError("[ARModeManager] PropertiesUI component not found on PropertiesPanel or its children.");
            return;
        }
        Debug.Log($"[ARModeManager] Found PropertiesUI on '{propertiesUI.gameObject.name}' activeInHierarchy={propertiesUI.gameObject.activeInHierarchy}");

        TextAsset json = Resources.Load<TextAsset>("PeriodicTableJSON");
        if (json == null)
        {
            Debug.LogError("[ARModeManager] PeriodicTableJSON not found in Resources.");
            return;
        }

        PeriodicTable table = JsonUtility.FromJson<PeriodicTable>(json.text);
        if (table == null || table.elements == null)
        {
            Debug.LogError("[ARModeManager] Failed to parse PeriodicTableJSON.");
            return;
        }
        Debug.Log($"[ARModeManager] JSON loaded — {table.elements.Length} elements.");

        foreach (AtomElementData el in table.elements)
        {
            if (el.number == atomicNumber)
            {
                Debug.Log($"[ARModeManager] Calling ShowProperties for '{el.name}'.");
                propertiesUI.ShowProperties(el);
                return;
            }
        }

        Debug.LogWarning($"[ARModeManager] Element #{atomicNumber} not found in JSON.");
    }

}