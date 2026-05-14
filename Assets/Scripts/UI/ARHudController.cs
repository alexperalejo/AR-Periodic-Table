using PeriodicAR.AR;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.UI
{
    /// <summary>
    /// Screen-space HUD overlay. Manages:
    ///   • Place / Cancel placement buttons for the AR periodic table.
    ///   • Close Atom / Close Info buttons (shown when respective AR object is active).
    ///   • Atom +/- and Info +/- scale buttons (simpler and more reliable than pinch).
    /// AR world objects (BohrModelRoot, ElementInfoCard) are NEVER hidden from here
    /// except by the explicit Close Atom / Close Info buttons.
    /// </summary>
    public class ARHudController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TapToPlace tapToPlace;
        [SerializeField] private ARPlaneDetectionWatcher planeWatcher;

        [Header("Place / Remove button")]
        [SerializeField] private Button placeButton;
        [SerializeField] private TMP_Text placeButtonLabel;
        [SerializeField] private string placeLabel  = "Place Table";
        [SerializeField] private string removeLabel = "Remove Table";

        [Header("Cancel placement (shown while Armed)")]
        [SerializeField] private GameObject cancelPlacementRoot;
        [SerializeField] private Button cancelPlacementButton;

        [Header("Instruction overlay")]
        [SerializeField] private GameObject instructionRoot;
        [SerializeField] private TMP_Text instructionLabel;
        [SerializeField] private string instructionText = "Tap a flat surface to place the table";

        [Header("AR Object scale limits")]
        [SerializeField] private float atomMinScale = 0.005f;
        [SerializeField] private float atomMaxScale = 0.15f;
        [SerializeField] private float infoMinScale = 0.002f;
        [SerializeField] private float infoMaxScale = 0.06f;
        [SerializeField] private float scaleStep    = 1.25f; // 25% per button press

        // ── Runtime HUD state ────────────────────────────────────────────────────
        private Canvas   _arObjCanvas;
        private Button   _btnCloseAtom;
        private Button   _btnAtomPlus;
        private Button   _btnAtomMinus;
        private Button   _btnCloseInfo;
        private Button   _btnInfoPlus;
        private Button   _btnInfoMinus;

        // Cached AR object references (set once from ARModeManager or scene search).
        private GameObject    _bohrModelRoot;
        private ElementInfoCard _elementInfoCard;

        // Placement state — used only to decide ShowMenu on Armed→Idle cancel.
        private TapToPlace.State _prevState = TapToPlace.State.Idle;

        // ── Unity lifecycle ──────────────────────────────────────────────────────

        private void OnEnable()
        {
            // Discover TapToPlace / PlaneWatcher
            if (tapToPlace  == null) tapToPlace  = FindAnyObjectByType<TapToPlace>();
            if (planeWatcher == null) planeWatcher = FindAnyObjectByType<ARPlaneDetectionWatcher>();
            if (tapToPlace  == null) Debug.LogWarning("[ARHudController] No TapToPlace found.");
            if (planeWatcher == null) Debug.LogWarning("[ARHudController] No ARPlaneDetectionWatcher found.");

            // Discover AR objects — prefer ARModeManager's exact references.
            DiscoverArObjects();

            // Wire inspector-assigned buttons.
            if (placeButton           != null) placeButton.onClick.AddListener(OnPlaceClicked);
            if (cancelPlacementButton != null) cancelPlacementButton.onClick.AddListener(OnCancelPlacementClicked);

            // Subscribe to TapToPlace and plane events.
            if (tapToPlace  != null)
            {
                tapToPlace.StateChanged += OnPlacementStateChanged;
                tapToPlace.LockChanged  += _ => RefreshPlacementHud();
            }
            if (planeWatcher != null) planeWatcher.PlaneAvailabilityChanged += _ => RefreshPlacementHud();

            BuildArObjectsHUD();
            RefreshPlacementHud();
        }

        private void OnDisable()
        {
            if (placeButton           != null) placeButton.onClick.RemoveListener(OnPlaceClicked);
            if (cancelPlacementButton != null) cancelPlacementButton.onClick.RemoveListener(OnCancelPlacementClicked);

            if (tapToPlace  != null)
            {
                tapToPlace.StateChanged -= OnPlacementStateChanged;
                tapToPlace.LockChanged  -= _ => RefreshPlacementHud();
            }
            if (planeWatcher != null) planeWatcher.PlaneAvailabilityChanged -= _ => RefreshPlacementHud();
        }

        private void Update()
        {
            // Re-discover if references have gone stale (scene reload, late init).
            if (_bohrModelRoot == null || _elementInfoCard == null)
                DiscoverArObjects();

            // Keep Close / Scale button visibility in sync with AR object state.
            RefreshArObjectsHud();
        }

        // ── AR object discovery ──────────────────────────────────────────────────

        private void DiscoverArObjects()
        {
            if (_bohrModelRoot == null && ARModeManager.Instance != null)
                _bohrModelRoot = ARModeManager.Instance.BohrModelRootGO;
            if (_bohrModelRoot == null)
            {
                var atmGen = FindAnyObjectByType<AtomGenerator>(FindObjectsInactive.Include);
                if (atmGen != null)
                    _bohrModelRoot = atmGen.transform.parent != null
                        ? atmGen.transform.parent.gameObject
                        : atmGen.gameObject;
            }

            if (_elementInfoCard == null && ARModeManager.Instance != null)
                _elementInfoCard = ARModeManager.Instance.ElementInfoCardRef;
            if (_elementInfoCard == null)
                _elementInfoCard = FindAnyObjectByType<ElementInfoCard>(FindObjectsInactive.Include);

            Debug.Log($"[ARHudController] AR objects — bohr={_bohrModelRoot?.name ?? "NULL"} " +
                      $"info={_elementInfoCard?.gameObject.name ?? "NULL"}");
        }

        // ── Placement buttons ────────────────────────────────────────────────────

        private void OnPlaceClicked()
        {
            if (tapToPlace == null) return;
            switch (tapToPlace.CurrentState)
            {
                case TapToPlace.State.Idle:
                    tapToPlace.ArmPlacement();
                    break;
                case TapToPlace.State.Armed:
                    tapToPlace.CancelPlacement();
                    Debug.Log("[ARHudController] Placement canceled via Place button.");
                    ARModeManager.Instance?.ShowMenu();
                    break;
                case TapToPlace.State.Placed:
                    tapToPlace.RemoveTable();
                    break;
            }
        }

        private void OnCancelPlacementClicked()
        {
            if (tapToPlace != null) tapToPlace.CancelPlacement();
            Debug.Log("[ARHudController] Placement canceled via Cancel button.");
            ARModeManager.Instance?.ShowMenu();
        }

        private void OnPlacementStateChanged(TapToPlace.State newState)
        {
            // Show menu ONLY when the user cancels out of Armed state.
            // Placed→Idle (table removed) must NOT show menu — AR objects stay visible.
            bool wasArmed = _prevState == TapToPlace.State.Armed;
            _prevState = newState;

            if (newState == TapToPlace.State.Idle && wasArmed)
            {
                Debug.Log("[ARHudController] Armed→Idle cancel — showing menu.");
                ARModeManager.Instance?.ShowMenu();
            }
            RefreshPlacementHud();
        }

        private void RefreshPlacementHud()
        {
            var state     = tapToPlace != null ? tapToPlace.CurrentState : TapToPlace.State.Idle;
            bool planeOk  = planeWatcher == null || planeWatcher.HasAnyHorizontalPlane;

            if (placeButton != null)
            {
                placeButton.interactable = state switch
                {
                    TapToPlace.State.Idle   => planeOk,
                    TapToPlace.State.Armed  => true,
                    TapToPlace.State.Placed => true,
                    _ => false,
                };
            }
            if (placeButtonLabel != null)
                placeButtonLabel.text = state == TapToPlace.State.Placed ? removeLabel : placeLabel;

            if (cancelPlacementRoot != null)
                cancelPlacementRoot.SetActive(state == TapToPlace.State.Armed);

            if (instructionRoot  != null) instructionRoot.SetActive(state == TapToPlace.State.Armed);
            if (instructionLabel != null) instructionLabel.text = instructionText;
        }

        // ── AR Objects HUD (runtime-built) ───────────────────────────────────────

        private void BuildArObjectsHUD()
        {
            if (_arObjCanvas != null) return; // already built

            var go = new GameObject("[ARObjectsHUD]");
            _arObjCanvas = go.AddComponent<Canvas>();
            _arObjCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _arObjCanvas.sortingOrder = 115;
            go.AddComponent<GraphicRaycaster>();

            // Canvas Scaler: portrait/landscape safe.
            var sc = go.AddComponent<CanvasScaler>();
            sc.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution  = new Vector2(1080f, 1920f);
            sc.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            sc.matchWidthOrHeight   = 0.5f;

            // Layout (top-left corner, anchored to top-left so it survives rotation):
            //   Row 1 (y=-50):  [Close Atom]  [Atom+]  [Atom-]
            //   Row 2 (y=-130): [Close Info]  [Info+]  [Info-]

            float row1 = -50f;
            float row2 = -130f;

            _btnCloseAtom = MakeBtn(go.transform, "Close Atom",
                new Vector2(10, row1), new Vector2(190, 70), new Color(0.65f, 0.10f, 0.10f, 0.90f));
            _btnCloseAtom.onClick.AddListener(OnCloseAtom);

            _btnAtomPlus = MakeBtn(go.transform, "Atom +",
                new Vector2(210, row1), new Vector2(90, 70), new Color(0.12f, 0.35f, 0.12f, 0.90f));
            _btnAtomPlus.onClick.AddListener(OnAtomScaleUp);

            _btnAtomMinus = MakeBtn(go.transform, "Atom -",
                new Vector2(310, row1), new Vector2(90, 70), new Color(0.12f, 0.20f, 0.35f, 0.90f));
            _btnAtomMinus.onClick.AddListener(OnAtomScaleDown);

            _btnCloseInfo = MakeBtn(go.transform, "Close Info",
                new Vector2(10, row2), new Vector2(190, 70), new Color(0.50f, 0.10f, 0.40f, 0.90f));
            _btnCloseInfo.onClick.AddListener(OnCloseInfo);

            _btnInfoPlus = MakeBtn(go.transform, "Info +",
                new Vector2(210, row2), new Vector2(90, 70), new Color(0.12f, 0.35f, 0.12f, 0.90f));
            _btnInfoPlus.onClick.AddListener(OnInfoScaleUp);

            _btnInfoMinus = MakeBtn(go.transform, "Info -",
                new Vector2(310, row2), new Vector2(90, 70), new Color(0.12f, 0.20f, 0.35f, 0.90f));
            _btnInfoMinus.onClick.AddListener(OnInfoScaleDown);

            RefreshArObjectsHud();
        }

        private bool _lastBohrOn = false;
        private bool _lastInfoOn = false;

        private void RefreshArObjectsHud()
        {
            bool bohrOn = _bohrModelRoot != null && _bohrModelRoot.activeSelf;
            bool infoOn = _elementInfoCard != null && _elementInfoCard.gameObject.activeSelf;

            SetActive(_btnCloseAtom, bohrOn);
            SetActive(_btnAtomPlus,  bohrOn);
            SetActive(_btnAtomMinus, bohrOn);
            SetActive(_btnCloseInfo, infoOn);
            SetActive(_btnInfoPlus,  infoOn);
            SetActive(_btnInfoMinus, infoOn);

            // Edge-trigger logging so we don't spam the console every frame.
            if (bohrOn != _lastBohrOn)
            {
                Debug.Log($"[ARHudController] Atom controls {(bohrOn ? "SHOWN" : "HIDDEN")} (BohrModelRoot.activeSelf={bohrOn}).");
                _lastBohrOn = bohrOn;
            }
            if (infoOn != _lastInfoOn)
            {
                Debug.Log($"[ARHudController] Info controls {(infoOn ? "SHOWN" : "HIDDEN")} (ElementInfoCard.activeSelf={infoOn}).");
                _lastInfoOn = infoOn;
            }
        }

        // ── Close handlers ───────────────────────────────────────────────────────

        private void OnCloseAtom()
        {
            Debug.Log("[ARHudController] Close Atom pressed — hiding BohrModelRoot.");
            if (ARModeManager.Instance != null)
                ARModeManager.Instance.HideBohrModel();
            else if (_bohrModelRoot != null)
            {
                Debug.Log($"[HIDE TRACE] ARHudController.OnCloseAtom hid BohrModelRoot\n{System.Environment.StackTrace}");
                Debug.Log("[ARHudController] BohrModelRoot hidden by ARHudController.OnCloseAtom (fallback).");
                _bohrModelRoot.SetActive(false);
                ImmersiveModeManager.Instance?.NotifyBohrHidden();
            }
            ARModeManager.Instance?.ShowMenu();
        }

        private void OnCloseInfo()
        {
            Debug.Log("[ARHudController] Close Info pressed — hiding ElementInfoCard.");
            if (ARModeManager.Instance != null)
                ARModeManager.Instance.HideInfoCard();
            else if (_elementInfoCard != null)
            {
                Debug.Log($"[HIDE TRACE] ARHudController.OnCloseInfo hid ElementInfoCard\n{System.Environment.StackTrace}");
                Debug.Log("[ARHudController] ElementInfoCard hidden by ARHudController.OnCloseInfo (fallback).");
                _elementInfoCard.gameObject.SetActive(false);
                ImmersiveModeManager.Instance?.NotifyInfoHidden();
            }
            ARModeManager.Instance?.ShowMenu();
        }

        // ── Scale handlers ───────────────────────────────────────────────────────

        private void OnAtomScaleUp()
        {
            if (_bohrModelRoot == null) return;
            float cur = _bohrModelRoot.transform.localScale.x;
            float next = Mathf.Clamp(cur * scaleStep, atomMinScale, atomMaxScale);
            _bohrModelRoot.transform.localScale = Vector3.one * next;

            // Sync AtomGenerator so GenerateAtom doesn't clobber the scale later.
            var ag = _bohrModelRoot.GetComponentInChildren<AtomGenerator>(true);
            if (ag != null) { ag.arWorldScale = next; ag.suppressAutoScale = true; }
            Debug.Log($"[ARHudController] Atom scale up → {next:F4}");
        }

        private void OnAtomScaleDown()
        {
            if (_bohrModelRoot == null) return;
            float cur = _bohrModelRoot.transform.localScale.x;
            float next = Mathf.Clamp(cur / scaleStep, atomMinScale, atomMaxScale);
            _bohrModelRoot.transform.localScale = Vector3.one * next;

            var ag = _bohrModelRoot.GetComponentInChildren<AtomGenerator>(true);
            if (ag != null) { ag.arWorldScale = next; ag.suppressAutoScale = true; }
            Debug.Log($"[ARHudController] Atom scale down → {next:F4}");
        }

        private void OnInfoScaleUp()
        {
            if (_elementInfoCard == null) return;
            float cur = _elementInfoCard.transform.localScale.x;
            float next = Mathf.Clamp(cur * scaleStep, infoMinScale, infoMaxScale);
            _elementInfoCard.transform.localScale = Vector3.one * next;
            Debug.Log($"[ARHudController] Info scale up → {next:F4}");
        }

        private void OnInfoScaleDown()
        {
            if (_elementInfoCard == null) return;
            float cur = _elementInfoCard.transform.localScale.x;
            float next = Mathf.Clamp(cur / scaleStep, infoMinScale, infoMaxScale);
            _elementInfoCard.transform.localScale = Vector3.one * next;
            Debug.Log($"[ARHudController] Info scale down → {next:F4}");
        }

        // ── Button factory ───────────────────────────────────────────────────────

        /// <summary>
        /// Creates a screen-space button anchored to the top-left corner.
        /// pos.x is the left edge offset from the screen left.
        /// pos.y is the downward offset from the screen top (negative = down).
        /// </summary>
        private static Button MakeBtn(Transform parent, string label,
                                      Vector2 pos, Vector2 size, Color bg)
        {
            var go = new GameObject(label, typeof(RectTransform),
                                    typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 1); // top-left
            rt.anchorMax        = new Vector2(0, 1);
            rt.pivot            = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;

            var img   = go.GetComponent<Image>();
            img.color = bg;

            var btn    = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.pressedColor     = new Color(0.60f, 0.60f, 0.60f, 1f);
            btn.colors = colors;

            var lgo = new GameObject("Lbl", typeof(RectTransform),
                                     typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            lgo.transform.SetParent(go.transform, false);
            lgo.layer = go.layer;

            var lrt = lgo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4, 4); lrt.offsetMax = new Vector2(-4, -4);

            var tmp = lgo.GetComponent<TextMeshProUGUI>();
            tmp.text             = label;
            tmp.color            = Color.white;
            tmp.alignment        = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin      = 10;
            tmp.fontSizeMax      = 24;
            tmp.fontStyle        = FontStyles.Bold;

            return btn;
        }

        private static void SetActive(Button btn, bool active)
        {
            if (btn != null && btn.gameObject.activeSelf != active)
                btn.gameObject.SetActive(active);
        }
    }
}
