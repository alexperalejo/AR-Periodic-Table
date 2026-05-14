using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using PeriodicAR.UI;

/// <summary>
/// Rebuilds the mode panels (Calculator / Electrons / Properties / Compounds) under
/// MenuCanvas with a clean, evenly-spaced layout. Each panel gets:
///   - A dark translucent background sized for portrait mobile (700x900)
///   - A header bar with title + Back button (drag handle)
///   - A scrollable / vertical-layouted body with all the relevant text fields
///   - A DraggableUIPanel component so the user can move the panel at runtime
///
/// All TMP_Text references on CompoundCalculatorUI / ElectronsUI / PropertiesUI
/// are re-wired to the freshly-built children, and ARModeManager's panel references are
/// re-assigned. Idempotent: safe to re-run.
/// </summary>
public static class RebuildModePanels
{
    // ---- Layout / palette constants ------------------------------------------------

    const float PANEL_WIDTH       = 700f;
    const float PANEL_HEIGHT      = 900f;
    const float HEADER_HEIGHT     = 70f;
    const float CONTENT_PADDING   = 20f;
    const float CONTENT_SPACING   = 12f;
    const float BUTTON_ROW_HEIGHT = 60f;
    const float BUTTON_ROW_TOTAL  = BUTTON_ROW_HEIGHT + 26f; // height + top/bottom padding

    // TMP auto-size range used for all body text
    const float TMP_FONT_SIZE_MIN = 14f;

    static readonly Color PANEL_BG     = new Color(0.10f, 0.11f, 0.16f, 0.88f);
    static readonly Color HEADER_BG    = new Color(0.18f, 0.20f, 0.30f, 1f);
    static readonly Color TEXT_PRIMARY = Color.white;
    static readonly Color TEXT_MUTED   = new Color(0.78f, 0.82f, 0.92f, 1f);
    static readonly Color ACCENT       = new Color(0.55f, 0.78f, 1f, 1f);

    // Pastel button colors for clear/back buttons
    static readonly Color BTN_BACK_BG  = new Color(0.92f, 0.92f, 0.94f, 0.95f);
    static readonly Color BTN_CLEAR_BG = new Color(1.00f, 0.86f, 0.75f, 0.95f);

    // ---- Public entry point --------------------------------------------------------

    public static void Execute()
    {
        var menuCanvasGO = GameObject.Find("MenuCanvas");
        if (menuCanvasGO == null)
        {
            Debug.LogError("[RebuildModePanels] MenuCanvas not found.");
            return;
        }

        // 1. Make MenuCanvas mobile-friendly
        ConfigureMenuCanvas(menuCanvasGO);

        // 2. Rebuild each panel
        var compoundCalcPanel = RebuildCompoundCalculatorPanel(menuCanvasGO);
        var electronsPanel    = RebuildElectronsPanel(menuCanvasGO);
        var propertiesPanel   = RebuildPropertiesPanel(menuCanvasGO);
        RebuildElementInfoPanel(menuCanvasGO);

        // 3. Re-wire ARModeManager's panel references
        RewireARModeManager(compoundCalcPanel, electronsPanel, propertiesPanel);

        // 4. Save scene
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("[RebuildModePanels] Done. Panels rebuilt and wired.");
    }

    // ---- Canvas config -------------------------------------------------------------

    static void ConfigureMenuCanvas(GameObject canvasGO)
    {
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            EditorUtility.SetDirty(scaler);
        }
    }

    // ---- Panel builders ------------------------------------------------------------

    static GameObject RebuildCompoundCalculatorPanel(GameObject parent)
    {
        var panel = EnsurePanel(parent, "CompoundCalculatorPanel");
        BuildCommonHeader(panel, "Compound Calculator");

        var content = EnsureScrollBody(panel);
        var formula      = AddTextRow(content, "FormulaText",      "Tap elements to build formula", 42, TextAlignmentOptions.Center,  TEXT_PRIMARY);
        var compoundName = AddTextRow(content, "CompoundNameText", "",                               30, TextAlignmentOptions.Center,  ACCENT);
        var category     = AddTextRow(content, "CategoryText",     "",                               24, TextAlignmentOptions.Center,  TEXT_MUTED);
        var iupac        = AddTextRow(content, "IupacNameText",    "",                               24, TextAlignmentOptions.Center,  TEXT_MUTED);
        var molarMass    = AddTextRow(content, "MolarMassText",    "",                               28, TextAlignmentOptions.Center,  TEXT_MUTED);
        var breakdown    = AddTextRow(content, "BreakdownText",    "",                               22, TextAlignmentOptions.TopLeft, TEXT_MUTED);
        SetLayoutElement(breakdown.gameObject, minH: 120, prefH: 160);

        var btnRow   = EnsureButtonRow(panel);
        var btnClear = AddButton(btnRow, "BtnClear", "Clear", BTN_CLEAR_BG);
        var btnBack  = AddButton(btnRow, "BtnBack",  "Back",  BTN_BACK_BG);

        var ui = panel.GetComponent<CompoundCalculatorUI>();
        if (ui == null) ui = panel.AddComponent<CompoundCalculatorUI>();
        ui.formulaText      = formula;
        ui.compoundNameText = compoundName;
        ui.categoryText     = category;
        ui.iupacNameText    = iupac;
        ui.molarMassText    = molarMass;
        ui.breakdownText    = breakdown;
        ui.btnClear         = btnClear;
        ui.btnBack          = btnBack;
        ui.molarMassCalculator = FindOrLoadMolarMassCalculator();
        ui.compoundLookup      = FindOrLoadCompoundLookup();
        EditorUtility.SetDirty(ui);

        AttachDraggable(panel);
        return panel;
    }

    static GameObject RebuildElectronsPanel(GameObject parent)
    {
        var panel = EnsurePanel(parent, "ElectronsPanel");
        BuildCommonHeader(panel, "Electron Configuration");

        var content  = EnsureScrollBody(panel);
        var elemName = AddTextRow(content, "ElementNameText", "Tap an element", 40, TextAlignmentOptions.Center, TEXT_PRIMARY);
        var config   = AddTextRow(content, "ConfigText",      "",               28, TextAlignmentOptions.Center, ACCENT);
        SetLayoutElement(config.gameObject, minH: 50, prefH: 70);

        // Orbital container — a vertical layout where OrbitalBox rows are spawned at runtime.
        // Sits directly in Content so the scroll view grows to fit whatever is added.
        // No ContentSizeFitter here — the parent Content VLG (childControlHeight=true)
        // asks OrbitalContainer for its preferredHeight, which the inner VLG calculates
        // from its children.  LayoutElement gives a sensible minimum when it is empty.
        var orbitalContainerGO = EnsureChild(content, "OrbitalContainer");
        var orbContVL = orbitalContainerGO.GetComponent<VerticalLayoutGroup>()
                        ?? orbitalContainerGO.AddComponent<VerticalLayoutGroup>();
        orbContVL.spacing = 8f;
        orbContVL.childAlignment      = TextAnchor.UpperCenter;
        orbContVL.childControlWidth   = true;
        orbContVL.childControlHeight  = true;
        orbContVL.childForceExpandWidth  = true;
        orbContVL.childForceExpandHeight = false;

        // Remove any stale ContentSizeFitter — it fights childControlHeight=true on the
        // parent Content VLG and causes the OrbitalContainer rect to be wrong.
        var staleCSF = orbitalContainerGO.GetComponent<ContentSizeFitter>();
        if (staleCSF != null) Object.DestroyImmediate(staleCSF);

        SetLayoutElement(orbitalContainerGO, minH: 350, prefH: 450);

        // Keep / re-create the OrbitalBoxPrefab template inside the panel (inactive, at root level
        // so it is never counted as a child of Body/Content and never destroyed during GenerateOrbitalDiagram)
        var template = EnsureOrbitalBoxTemplate(panel);

        // Button row
        var btnRow  = EnsureButtonRow(panel);
        var btnBack = AddButton(btnRow, "BtnBack", "Back", BTN_BACK_BG);

        var ui = panel.GetComponent<ElectronsUI>();
        if (ui == null) ui = panel.AddComponent<ElectronsUI>();
        ui.elementNameText  = elemName;
        ui.configText       = config;
        ui.orbitalContainer = orbitalContainerGO.transform;
        ui.orbitalBoxPrefab = template;
        ui.btnBack          = btnBack;
        EditorUtility.SetDirty(ui);

        AttachDraggable(panel);
        return panel;
    }

    static GameObject RebuildPropertiesPanel(GameObject parent)
    {
        var panel = EnsurePanel(parent, "PropertiesPanel");
        BuildCommonHeader(panel, "Element Properties");

        var content = EnsureScrollBody(panel);
        var elemName          = AddTextRow(content, "ElementNameText",        "Tap an element", 36, TextAlignmentOptions.Center, TEXT_PRIMARY);
        var symbol            = AddTextRow(content, "SymbolText",             "", 24, TextAlignmentOptions.Left, TEXT_MUTED);
        var category          = AddTextRow(content, "CategoryText",           "", 24, TextAlignmentOptions.Left, TEXT_MUTED);
        var phase             = AddTextRow(content, "PhaseText",              "", 24, TextAlignmentOptions.Left, TEXT_MUTED);
        var appearance        = AddTextRow(content, "AppearanceText",         "", 24, TextAlignmentOptions.Left, TEXT_MUTED);
        var electronegativity = AddTextRow(content, "ElectronegativityText",  "", 24, TextAlignmentOptions.Left, TEXT_MUTED);
        var electronAffinity  = AddTextRow(content, "ElectronAffinityText",   "", 24, TextAlignmentOptions.Left, TEXT_MUTED);
        var molarHeat         = AddTextRow(content, "MolarHeatText",          "", 24, TextAlignmentOptions.Left, TEXT_MUTED);
        var discoveredBy      = AddTextRow(content, "DiscoveredByText",       "", 22, TextAlignmentOptions.Left, TEXT_MUTED);
        var namedBy           = AddTextRow(content, "NamedByText",            "", 22, TextAlignmentOptions.Left, TEXT_MUTED);
        var summary           = AddTextRow(content, "SummaryText",            "", 20, TextAlignmentOptions.TopLeft, TEXT_MUTED);
        // Give summary a comfortable minimum so it never collapses when empty
        SetLayoutElement(summary.gameObject, minH: 80, prefH: 120);

        var btnRow  = EnsureButtonRow(panel);
        var btnBack = AddButton(btnRow, "BtnBack", "Back", BTN_BACK_BG);

        var ui = panel.GetComponent<PropertiesUI>();
        if (ui == null) ui = panel.AddComponent<PropertiesUI>();
        ui.elementNameText        = elemName;
        ui.symbolText             = symbol;
        ui.categoryText           = category;
        ui.phaseText              = phase;
        ui.appearanceText         = appearance;
        ui.electronegativityText  = electronegativity;
        ui.electronAffinityText   = electronAffinity;
        ui.molarHeatText          = molarHeat;
        ui.discoveredByText       = discoveredBy;
        ui.namedByText            = namedBy;
        ui.summaryText            = summary;
        ui.btnBack                = btnBack;
        EditorUtility.SetDirty(ui);

        AttachDraggable(panel);
        return panel;
    }

    static void RebuildElementInfoPanel(GameObject parent)
    {
        var panel = EnsurePanel(parent, "ElementInfoPanel");
        BuildCommonHeader(panel, "Element Info");

        // Scrollable content area with three consolidated text blocks:
        //   TitleText       — element name + symbol (large, centred)
        //   PropertiesText  — key properties, one per line
        //   DescriptionText — full summary paragraph
        var content  = EnsureScrollBody(panel);
        var title    = AddTextRow(content, "TitleText",       "Tap an element", 40, TextAlignmentOptions.Center,  TEXT_PRIMARY);
        var props    = AddTextRow(content, "PropertiesText",  "",               22, TextAlignmentOptions.Left,    TEXT_MUTED);
        var desc     = AddTextRow(content, "DescriptionText", "",               20, TextAlignmentOptions.TopLeft, TEXT_MUTED);
        SetLayoutElement(desc.gameObject, minH: 100, prefH: 160);

        var btnRow  = EnsureButtonRow(panel);
        var btnBack = AddButton(btnRow, "BtnBack", "Back", BTN_BACK_BG);

        // Wire up a lightweight UI driver so the panel can be populated at runtime.
        var ui = panel.GetComponent<ElementInfoPanelUI>();
        if (ui == null) ui = panel.AddComponent<ElementInfoPanelUI>();
        ui.titleText       = title;
        ui.propertiesText  = props;
        ui.descriptionText = desc;
        ui.btnBack         = btnBack;
        EditorUtility.SetDirty(ui);

        AttachDraggable(panel);
        Debug.Log("[RebuildModePanels] Rebuilt 'ElementInfoPanel'.");
    }

    // ---- ARModeManager re-wiring ---------------------------------------------------

    static void RewireARModeManager(GameObject compoundCalc, GameObject electrons, GameObject properties)
    {
        var arModeManager = Object.FindFirstObjectByType<ARModeManager>();
        if (arModeManager == null) { Debug.LogWarning("[RebuildModePanels] ARModeManager not found in scene."); return; }

        var so = new SerializedObject(arModeManager);
        so.FindProperty("compoundCalculatorPanel").objectReferenceValue = compoundCalc;
        so.FindProperty("electronsPanel").objectReferenceValue          = electrons;
        so.FindProperty("propertiesPanel").objectReferenceValue         = properties;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(arModeManager);
    }

    // ---- Building blocks -----------------------------------------------------------

    static GameObject EnsurePanel(GameObject parent, string name)
    {
        Transform existing = parent.transform.Find(name);
        GameObject panel;
        if (existing != null)
        {
            panel = existing.gameObject;
            // Strip old children so we can rebuild from scratch (idempotent)
            for (int i = panel.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(panel.transform.GetChild(i).gameObject);
            }
        }
        else
        {
            panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.layer = LayerMask.NameToLayer("UI");
            panel.transform.SetParent(parent.transform, false);
        }

        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        var img = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
        img.color = PANEL_BG;
        img.raycastTarget = true;
        img.type = Image.Type.Sliced;

        // Remove any stale VerticalLayoutGroup — children use anchor-based layout instead,
        // which means they automatically reposition/resize when sizeDelta changes at runtime.
        var vlg = panel.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) Object.DestroyImmediate(vlg);

        return panel;
    }

    static void BuildCommonHeader(GameObject panel, string title)
    {
        var header = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        header.layer = LayerMask.NameToLayer("UI");
        header.transform.SetParent(panel.transform, false);

        var img = header.GetComponent<Image>();
        img.color = HEADER_BG;

        // Anchor top-stretch: spans full panel width, fixed height pinned to the top.
        // Pivot at (0.5, 1) so anchoredPosition = (0, 0) means the top edge of the
        // header sits exactly on the top edge of the panel.
        var rt = (RectTransform)header.transform;
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(0f, HEADER_HEIGHT);

        // Title text (centered)
        var titleGO = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
        titleGO.layer = LayerMask.NameToLayer("UI");
        titleGO.transform.SetParent(header.transform, false);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = title;
        titleTMP.fontSize = 32;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = TEXT_PRIMARY;
        titleTMP.raycastTarget = false;
        var titleRT = (RectTransform)titleTMP.transform;
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = Vector2.one;
        titleRT.offsetMin = new Vector2(70f, 0f); // leave room for a future close button
        titleRT.offsetMax = new Vector2(-70f, 0f);
    }

    /// <summary>
    /// Creates the Body → ScrollView → Viewport → Content hierarchy.
    /// Returns the <b>Content</b> transform — this is where text rows are parented.
    ///
    /// Layout contract:
    ///   Body     — full-stretch between Header and ButtonRow (anchor-based, no layout group).
    ///   ScrollView — full-stretch inside Body, hosts the ScrollRect.
    ///   Viewport — full-stretch inside ScrollView, clips the content via Mask.
    ///   Content  — top-stretch in Viewport, grows downward via ContentSizeFitter.
    ///              Its VLG (childControlHeight=true) uses TMP's preferredHeight for each
    ///              row, which TMP calculates AFTER the VLG has already set the row width —
    ///              so word-wrap is correctly accounted for in a single layout pass.
    /// </summary>
    static GameObject EnsureScrollBody(GameObject panel)
    {
        // ── Body ──────────────────────────────────────────────────────────────────
        var body = new GameObject("Body", typeof(RectTransform));
        body.layer = LayerMask.NameToLayer("UI");
        body.transform.SetParent(panel.transform, false);

        var bodyRT = (RectTransform)body.transform;
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.pivot     = new Vector2(0.5f, 0.5f);
        bodyRT.offsetMin = new Vector2(0f,  BUTTON_ROW_TOTAL);
        bodyRT.offsetMax = new Vector2(0f, -HEADER_HEIGHT);

        // ── ScrollView ────────────────────────────────────────────────────────────
        var scrollGO = new GameObject("ScrollView",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        scrollGO.layer = LayerMask.NameToLayer("UI");
        scrollGO.transform.SetParent(body.transform, false);

        var scrollRT = (RectTransform)scrollGO.transform;
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.pivot     = new Vector2(0.5f, 0.5f);
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;

        // Transparent background so the panel's own Image shows through.
        var scrollImg = scrollGO.GetComponent<Image>();
        scrollImg.color         = Color.clear;
        scrollImg.raycastTarget = true; // must be true or ScrollRect won't receive drag events

        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal       = false;
        scrollRect.vertical         = true;
        scrollRect.movementType     = ScrollRect.MovementType.Clamped;
        scrollRect.inertia          = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 30f;

        // ── Viewport ──────────────────────────────────────────────────────────────
        var vpGO = new GameObject("Viewport",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        vpGO.layer = LayerMask.NameToLayer("UI");
        vpGO.transform.SetParent(scrollGO.transform, false);

        var vpRT = (RectTransform)vpGO.transform;
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.pivot     = new Vector2(0f, 1f); // top-left pivot required by ScrollRect
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;

        var vpImg = vpGO.GetComponent<Image>();
        vpImg.color         = Color.clear;
        vpImg.raycastTarget = false;

        vpGO.GetComponent<Mask>().showMaskGraphic = false; // hide the Mask image itself

        // ── Content ───────────────────────────────────────────────────────────────
        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.layer = LayerMask.NameToLayer("UI");
        contentGO.transform.SetParent(vpGO.transform, false);

        // Top-stretch: Content is as wide as the Viewport and grows downward.
        var contentRT = (RectTransform)contentGO.transform;
        contentRT.anchorMin        = new Vector2(0f, 1f);
        contentRT.anchorMax        = new Vector2(1f, 1f);
        contentRT.pivot            = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta        = Vector2.zero; // ContentSizeFitter sets the height

        // VLG stacks rows top-to-bottom and sets each row's WIDTH (childControlWidth=true)
        // and HEIGHT (childControlHeight=true).  Height comes from TMP's GetPreferredHeight(),
        // which TMP computes AFTER the VLG has committed the row width — so word-wrap
        // is fully baked in.  childForceExpandHeight=false keeps rows at their natural height.
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(
            (int)CONTENT_PADDING, (int)CONTENT_PADDING,
            (int)CONTENT_PADDING, (int)CONTENT_PADDING);
        vlg.spacing             = CONTENT_SPACING;
        vlg.childAlignment      = TextAnchor.UpperLeft;
        vlg.childControlWidth   = true;
        vlg.childControlHeight  = true;   // lets TMP's preferredHeight drive row height
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // ContentSizeFitter makes the Content rect as tall as the sum of row heights.
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // width comes from anchors
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // ── Wire ScrollRect ───────────────────────────────────────────────────────
        scrollRect.viewport = vpRT;
        scrollRect.content  = contentRT;

        return contentGO;
    }

    static GameObject EnsureButtonRow(GameObject panel)
    {
        var row = new GameObject("ButtonRow", typeof(RectTransform));
        row.layer = LayerMask.NameToLayer("UI");
        row.transform.SetParent(panel.transform, false);

        // Bottom-stretch: spans full panel width, fixed height pinned to the bottom.
        // Pivot at (0.5, 0) so anchoredPosition = (0, 0) pins the bottom of the row
        // to the bottom of the panel, and it stretches horizontally with the panel.
        var rt = (RectTransform)row.transform;
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(0f, BUTTON_ROW_TOTAL);

        // HorizontalLayoutGroup makes buttons fill and stretch across the row width.
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding             = new RectOffset(20, 20, 10, 16);
        hlg.spacing             = 16f;
        hlg.childAlignment      = TextAnchor.MiddleCenter;
        hlg.childControlWidth   = true;
        hlg.childControlHeight  = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;

        return row;
    }

    /// <summary>
    /// Creates a single TMP text row inside the scroll Content.
    /// No <see cref="LayoutElement"/> is added — the parent Content VLG uses TMP's
    /// <c>GetPreferredHeight()</c> directly (accurate after the VLG has set the width).
    /// Call <see cref="SetLayoutElement"/> afterwards only when a minimum height is
    /// needed (e.g. a summary field that should stay visible even when empty).
    /// </summary>
    static TMP_Text AddTextRow(GameObject parent, string name, string text,
                               float fontSize, TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent.transform, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.alignment        = align;
        tmp.color            = color;
        tmp.richText         = true;
        tmp.raycastTarget    = false;

        // Word-wrap is always on in a scrollable container — text reflows to fit width.
        tmp.textWrappingMode = TextWrappingModes.Normal;

        // Overflow=Overflow lets TMP report the true preferred height to the layout
        // system (Truncate/Ellipsis would clamp it to the rect height).
        tmp.overflowMode = TextOverflowModes.Overflow;

        // Auto-size picks the largest font that fits horizontally; the height then
        // adjusts to however many lines are needed.  fontSizeMax doubles as the
        // "normal" font size; fontSizeMin is a hard floor for very narrow panels.
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin      = TMP_FONT_SIZE_MIN;
        tmp.fontSizeMax      = fontSize;

        return tmp;
    }

    static Button AddButton(GameObject parent, string name, string label, Color bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent.transform, false);

        var img = go.GetComponent<Image>();
        img.color = bgColor;
        img.type = Image.Type.Sliced;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = BUTTON_ROW_HEIGHT;
        le.preferredHeight = BUTTON_ROW_HEIGHT;
        le.flexibleWidth = 1f;

        // Label
        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelGO.layer = LayerMask.NameToLayer("UI");
        labelGO.transform.SetParent(go.transform, false);
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.10f, 0.12f, 0.18f, 1f);
        tmp.raycastTarget = false;
        var lrt = (RectTransform)labelGO.transform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        return btn;
    }

    static GameObject EnsureOrbitalBoxTemplate(GameObject panel)
    {
        // We keep a single inactive child as the prefab template that ElectronsUI will
        // Instantiate at runtime. We DO NOT add it to the OrbitalContainer (it would be
        // counted as a permanent first child and break the runtime clearing logic).
        var t = panel.transform.Find("OrbitalBoxTemplate");
        GameObject template;
        if (t != null) { template = t.gameObject; }
        else
        {
            template = new GameObject("OrbitalBoxTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            template.layer = LayerMask.NameToLayer("UI");
            template.transform.SetParent(panel.transform, false);
        }
        template.SetActive(false);

        var img = template.GetComponent<Image>() ?? template.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.06f);
        img.type = Image.Type.Sliced;

        var le = template.GetComponent<LayoutElement>() ?? template.AddComponent<LayoutElement>();
        le.minHeight = 50f;
        le.preferredHeight = 60f;

        var hlg = template.GetComponent<HorizontalLayoutGroup>() ?? template.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(20, 20, 6, 6);
        hlg.spacing = 16f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Clear any old children
        for (int i = template.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(template.transform.GetChild(i).gameObject);
        }

        // Orbital label (e.g. "2p")
        var labelGO = new GameObject("OrbitalLabel", typeof(RectTransform));
        labelGO.layer = LayerMask.NameToLayer("UI");
        labelGO.transform.SetParent(template.transform, false);
        var lblTmp = labelGO.AddComponent<TextMeshProUGUI>();
        lblTmp.text = "1s";
        lblTmp.fontSize = 28;
        lblTmp.fontStyle = FontStyles.Bold;
        lblTmp.alignment = TextAlignmentOptions.Left;
        lblTmp.color = ACCENT;
        lblTmp.raycastTarget = false;
        var lblLE = labelGO.AddComponent<LayoutElement>();
        lblLE.minWidth = 60f;
        lblLE.preferredWidth = 60f;

        // Electrons text (the arrow boxes)
        var electronsGO = new GameObject("ElectronsText", typeof(RectTransform));
        electronsGO.layer = LayerMask.NameToLayer("UI");
        electronsGO.transform.SetParent(template.transform, false);
        var elecTmp = electronsGO.AddComponent<TextMeshProUGUI>();
        elecTmp.text = "[↑↓]";
        elecTmp.fontSize = 26;
        elecTmp.alignment = TextAlignmentOptions.Left;
        elecTmp.color = TEXT_PRIMARY;
        elecTmp.raycastTarget = false;
        var elecLE = electronsGO.AddComponent<LayoutElement>();
        elecLE.flexibleWidth = 1f;

        var orbitalBox = template.GetComponent<OrbitalBox>() ?? template.AddComponent<OrbitalBox>();
        orbitalBox.orbitalLabel = lblTmp;
        orbitalBox.electronsText = elecTmp;
        EditorUtility.SetDirty(orbitalBox);

        return template;
    }

    static GameObject EnsureChild(GameObject parent, string name)
    {
        var existing = parent.transform.Find(name);
        if (existing != null) return existing.gameObject;
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    /// <summary>
    /// Adds / updates a <see cref="LayoutElement"/> on <paramref name="go"/>.
    /// Use this after <see cref="AddTextRow"/> when a row needs a minimum or preferred
    /// height guarantee (e.g. the summary field, the orbital container).
    /// <c>flexibleH</c> is intentionally omitted — inside a ContentSizeFitter-driven
    /// scroll Content flexible heights have no effect.
    /// </summary>
    static void SetLayoutElement(GameObject go, float minH = -1, float prefH = -1)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        if (minH  >= 0) le.minHeight      = minH;
        if (prefH >= 0) le.preferredHeight = prefH;
        EditorUtility.SetDirty(le);
    }

    static void AttachDraggable(GameObject panel)
    {
        // Remove legacy DraggableUIPanel if still present from an older run
        var old = panel.GetComponent<DraggableUIPanel>();
        if (old != null) Object.DestroyImmediate(old);

        var drag = panel.GetComponent<ResizableDraggableUIPanel>()
                   ?? panel.AddComponent<ResizableDraggableUIPanel>();

        var header = panel.transform.Find("Header") as RectTransform;
        drag.dragHandle    = header;
        drag.clampToCanvas = true;
        drag.centerOnEnable = true;
        EditorUtility.SetDirty(drag);
    }

    // ---- Lookups -------------------------------------------------------------------

    static MolarMassCalculator FindOrLoadMolarMassCalculator()
    {
        var existing = Object.FindFirstObjectByType<MolarMassCalculator>();
        return existing;
    }

    static CompoundLookup FindOrLoadCompoundLookup()
    {
        var existing = Object.FindFirstObjectByType<CompoundLookup>();
        return existing;
    }
}
