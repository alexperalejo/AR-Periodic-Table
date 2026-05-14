// Assets/Scripts/AR/ImmersiveModeManager.cs
//
// Manages two display modes for BohrModel and ElementInfoCard.
//
// ═══════════════════════════════════════════════════════════════════════════════
//  SCREEN MODE  (default, immersiveMode = false)
// ═══════════════════════════════════════════════════════════════════════════════
//  • All UI panels stay as screen-space overlays rendered on top of the AR camera.
//    – MenuPanel, PropertiesPanel, ElectronsPanel, CompoundCalculatorPanel → always overlay.
//    – ElementInfoCard and BohrModel appear at their authored scene positions.
//  • No per-frame world-space repositioning is applied.
//
// ═══════════════════════════════════════════════════════════════════════════════
//  IMMERSIVE / AR WORLD MODE  (immersiveMode = true)
// ═══════════════════════════════════════════════════════════════════════════════
//  • BohrModelRoot floats to the LEFT of the AR periodic table at a configurable
//    offset in table-local space.
//  • ElementInfoCard floats to the RIGHT of the table and billboards toward the
//    camera so text stays readable at any table orientation.
//  • Screen-space overlay panels (Properties, Electrons, Compound Calculator, Menu)
//    are intentionally NOT touched — they remain on-screen for readability.
//
// ═══════════════════════════════════════════════════════════════════════════════
//  TABLE LINKING
// ═══════════════════════════════════════════════════════════════════════════════
//  TapToPlace fires TablePlaced / TableRemoved events.  This manager subscribes
//  to those events (once, when TapToPlace is first discovered) and caches the
//  placed table's Transform directly.  There is no per-frame polling of
//  CurrentInstance — the cached reference is always valid or null.
//
// ═══════════════════════════════════════════════════════════════════════════════
//  SCENE WIRING
// ═══════════════════════════════════════════════════════════════════════════════
//  1. Attach this component to any persistent GameObject (e.g. the ARModeManager GO).
//  2. Assign _bohrModelRoot in the Inspector (the same GO ARModeManager shows in
//     BohrModel mode — typically "BohrModelRoot").
//  3. _elementInfoCard and _tapToPlace are auto-discovered from the scene; you can
//     also pin them in the Inspector.
//  4. Optionally assign immersiveButtonLabel to auto-update the toggle button text.
//  5. Wire the toggle button's persistent onClick to ToggleImmersiveMode()
//     (done by SetupImmersiveMode editor script — do NOT add a second runtime listener).

using PeriodicAR.AR;
using TMPro;
using UnityEngine;

public class ImmersiveModeManager : MonoBehaviour
{
    public static ImmersiveModeManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────────

    [Header("Mode State")]
    [Tooltip("Start in Immersive (AR World) mode? Leave false for the normal Screen Mode default.")]
    [SerializeField] private bool _immersiveMode = false;

    [Header("AR World-Space Objects")]
    [Tooltip("The root GameObject that ARModeManager shows in BohrModel mode. " +
             "Positioned to the LEFT of the AR table in Immersive Mode.")]
    [SerializeField] private GameObject _bohrModelRoot;

    [Tooltip("(Optional) Pin the ElementInfoCard here to skip the runtime FindObjectOfType call.")]
    [SerializeField] private ElementInfoCard _elementInfoCardHint;

    [Tooltip("(Optional) Pin TapToPlace here to skip the runtime FindObjectOfType call.")]
    [SerializeField] private TapToPlace _tapToPlaceHint;

    [Header("World-Space Offsets (table-local coordinates)")]
    [Tooltip("Where the Bohr model floats — table-local space. " +
             "Negative X = left side of table, positive Y = above table surface.")]
    [SerializeField] private Vector3 _atomTableOffset = new Vector3(-0.30f, 0.20f, 0.0f);

    [Tooltip("Where the ElementInfoCard floats — table-local space. " +
             "Positive X = right side of table, positive Y = above table surface.")]
    [SerializeField] private Vector3 _cardTableOffset = new Vector3(0.35f, 0.20f, 0.0f);

    [Header("World-Space Scales")]
    [Tooltip("Uniform world-scale for the BohrModel root in AR. " +
             "Recommended range 0.05–0.30. Applied whenever PlaceArObjectsNow is called.")]
    [SerializeField, Range(0.01f, 0.50f)] private float _atomWorldScale = 0.15f;

    [Tooltip("Uniform world-scale for the ElementInfoCard root in AR. " +
             "Recommended range 0.05–0.20. Applied whenever PlaceArObjectsNow is called.")]
    [SerializeField, Range(0.01f, 0.50f)] private float _cardWorldScale = 0.12f;

    [Header("Optional UI")]
    [Tooltip("TMP_Text on the Immersive toggle button — auto-updated to reflect the current mode.")]
    public TMP_Text immersiveButtonLabel;

    // ── Private runtime state ─────────────────────────────────────────────────────

    // Resolved once; both can be set via Inspector hints or discovered lazily.
    private TapToPlace      _tapToPlace;
    private ElementInfoCard _elementInfoCard;

    // True once we've subscribed to _tapToPlace's TablePlaced / TableRemoved events.
    private bool _tapToPlaceSubscribed;

    // The Transform of the currently placed AR periodic table.
    // Set by OnTablePlaced, cleared by OnTableRemoved.
    // This is the authoritative reference — never polled from CurrentInstance.
    private Transform _tableTransform;

    // ── Logging throttles ─────────────────────────────────────────────────────────
    // Each flag resets when Immersive Mode is toggled ON so the user gets feedback
    // on every new immersive session without being spammed every frame.

    private bool _hasLoggedMissingTable; // suppress repeated "place the table" warnings
    private bool _hasLoggedPositioned;   // suppress repeated "objects positioned" logs

    // ── First-placement guards ────────────────────────────────────────────────────
    // Prevents PlaceArObjectsNow from snapping the object back to the table on every
    // element tap after the user has dragged it elsewhere.
    // Reset by NotifyBohrHidden / NotifyInfoHidden when the object is explicitly closed.

    private bool _bohrHasBeenPositioned;
    private bool _infoHasBeenPositioned;

    // ── Home transforms ───────────────────────────────────────────────────────────
    // Captured before the first Immersive override so we can restore objects to
    // their authored scene positions when toggling back to Screen Mode.

    private bool       _atomHomeCaptured;
    private Transform  _atomHomeParent;
    private Vector3    _atomHomeLocalPos;
    private Quaternion _atomHomeLocalRot;
    private Vector3    _atomHomeLocalScale;

    private bool       _cardHomeCaptured;
    private Transform  _cardHomeParent;
    private Vector3    _cardHomeLocalPos;
    private Quaternion _cardHomeLocalRot;
    private Vector3    _cardHomeLocalScale;

    // ── Properties ────────────────────────────────────────────────────────────────

    /// <summary>True while Immersive (AR World) mode is active.</summary>
    public bool IsImmersive => _immersiveMode;

    /// <summary>The Transform of the placed AR table, or null if no table is placed.</summary>
    public Transform TableTransform => _tableTransform;

    // ── Unity lifecycle ───────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        RefreshButtonLabel();
    }

    private void Update()
    {
        // ── Lazy-discover TapToPlace and subscribe to its table events ────────────
        if (_tapToPlace == null)
        {
            _tapToPlace = _tapToPlaceHint != null
                ? _tapToPlaceHint
                : FindAnyObjectByType<TapToPlace>();

            if (_tapToPlace != null)
                SubscribeToTableEvents(); // called exactly once after discovery
        }

        // ── Lazy-discover ElementInfoCard (read-only, no events needed) ───────────
        if (_elementInfoCard == null)
        {
            _elementInfoCard = _elementInfoCardHint != null
                ? _elementInfoCardHint
                : FindAnyObjectByType<ElementInfoCard>(FindObjectsInactive.Include);
        }

        // ── Capture home transforms once, before any Immersive override ───────────
        TryCaptureAtomHome();
        TryCaptureCardHome();

        // NOTE: No per-frame repositioning here.
        // ImmersiveModeManager calls PlaceObjectsInitially() ONCE when Immersive Mode
        // turns ON (or when the table is placed while mode is already ON).
        // After that, ARWorldObjectManipulator owns position, scale, and billboard so
        // the user can freely drag and resize the objects without a per-frame fight.
    }

    private void OnDestroy()
    {
        UnsubscribeFromTableEvents();
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Toggles between Screen Mode and Immersive (AR World) Mode.
    /// Wired as a persistent onClick listener on BtnImmersiveToggle.
    /// Do NOT add a second runtime listener in MenuUI.Start() — that causes a
    /// double-toggle (ON→OFF in the same frame) with no visible effect.
    /// </summary>
    public void ToggleImmersiveMode()
    {
        // Log fires first — if you never see this line the persistent listener
        // target is null or the component is missing from the scene.
        Debug.Log($"[ImmersiveModeManager] ToggleImmersiveMode called — was {(_immersiveMode ? "ON" : "OFF")}");

        _immersiveMode = !_immersiveMode;

        if (_immersiveMode)
        {
            // Reset per-session log throttles.
            _hasLoggedMissingTable = false;
            _hasLoggedPositioned   = false;

            if (_tableTransform != null)
            {
                Debug.Log($"[ImmersiveModeManager] Immersive ON — table linked: " +
                          $"'{_tableTransform.name}' at {_tableTransform.position}");
                PlaceObjectsInitially();
            }
            else
            {
                Debug.LogWarning("[ImmersiveModeManager] Immersive ON — table not placed yet. " +
                                 "Place the periodic table first.");
                _hasLoggedMissingTable = true;
            }
        }
        else
        {
            // Returning to Screen Mode — restore objects to their authored positions.
            // ARWorldObjectManipulator is gated on IsImmersive so it stops operating
            // automatically once this flag is false.
            RestoreAtomHome();
            RestoreCardHome();
        }

        RefreshButtonLabel();
        Debug.Log($"[ImmersiveModeManager] Immersive Mode → {(_immersiveMode ? "ON (AR World)" : "OFF (Screen)")}");
    }

    /// <summary>Returns true when Immersive (AR World) mode is active.</summary>
    public bool IsImmersiveMode() => _immersiveMode;

    // ── Table event subscription ──────────────────────────────────────────────────

    private void SubscribeToTableEvents()
    {
        if (_tapToPlaceSubscribed || _tapToPlace == null) return;
        _tapToPlace.TablePlaced  += OnTablePlaced;
        _tapToPlace.TableRemoved += OnTableRemoved;
        _tapToPlaceSubscribed = true;

        // If the table was already placed before we subscribed (e.g. scene loaded
        // with a table in it), grab the existing reference immediately.
        if (_tapToPlace.PlacedTransform != null)
        {
            _tableTransform = _tapToPlace.PlacedTransform;
            Debug.Log($"[ImmersiveModeManager] TapToPlace discovered — table already placed: " +
                      $"'{_tableTransform.name}' at {_tableTransform.position}. Table linked.");
        }
        else
        {
            Debug.Log("[ImmersiveModeManager] TapToPlace discovered — no table placed yet.");
        }
    }

    private void UnsubscribeFromTableEvents()
    {
        if (!_tapToPlaceSubscribed || _tapToPlace == null) return;
        _tapToPlace.TablePlaced  -= OnTablePlaced;
        _tapToPlace.TableRemoved -= OnTableRemoved;
        _tapToPlaceSubscribed = false;
    }

    // Called by TapToPlace.TablePlaced — fires after the new instance is created,
    // before SetState(Placed), so the Transform is valid and stable.
    private void OnTablePlaced(Transform tableTransform)
    {
        _tableTransform = tableTransform;
        _hasLoggedMissingTable = false;
        _hasLoggedPositioned   = false;
        Debug.Log($"[ImmersiveModeManager] Table linked: '{tableTransform.name}' " +
                  $"at {tableTransform.position}. Table linked.");

        // Re-position any active AR objects near the new table location.
        // Works regardless of _immersiveMode so BohrModel/Info always track the table.
        PlaceArObjectsNow();
    }

    // Called by TapToPlace.TableRemoved — fires before Destroy, while transform is valid.
    private void OnTableRemoved()
    {
        string prevName = _tableTransform != null ? _tableTransform.name : "(null)";
        _tableTransform = null;
        _hasLoggedPositioned   = false;
        // Allow re-positioning near the new table when one is placed again.
        _bohrHasBeenPositioned = false;
        _infoHasBeenPositioned = false;
        Debug.LogWarning($"[ImmersiveModeManager] Table removed (was '{prevName}'). " +
                         (_immersiveMode
                             ? "Immersive Mode is ON — place the periodic table first."
                             : "Immersive Mode is OFF; no action needed."));
    }

    // ── World-space placement ─────────────────────────────────────────────────────

    /// <summary>
    /// Places BohrModelRoot and ElementInfoCard near the AR table NOW,
    /// regardless of whether Immersive Mode is on. Safe to call any time:
    /// at mode switch, on table placement, or on element selection.
    /// Only repositions objects that are currently active (SetActive=true) AND
    /// have not already been positioned this session (prevents snapping back
    /// after the user has dragged them to a new location).
    /// </summary>
    public void PlaceArObjectsNow()
    {
        // Resolve position anchor: table if placed, otherwise 1m in front of camera.
        Vector3 anchor = ResolveAnchor(out bool hasTable);

        bool anyPlaced = false;

        // ── BohrModel ─────────────────────────────────────────────────────────────
        if (_bohrModelRoot != null && _bohrModelRoot.activeSelf)
        {
            // When no table is placed we must re-position EVERY call (the user is
            // probably aiming the camera somewhere new each time they tap a tile).
            // Once a table exists the position is anchored to it, so we keep the
            // _bohrHasBeenPositioned guard to avoid fighting ARWorldObjectManipulator.
            bool needsRepositioning = !hasTable || !_bohrHasBeenPositioned;

            if (needsRepositioning)
            {
                _bohrModelRoot.transform.position = hasTable
                    ? _tableTransform.TransformPoint(_atomTableOffset)
                    : anchor; // place directly at camera-forward anchor, not offset left

                if (!hasTable && Camera.main != null)
                {
                    // Face the camera so it doesn't appear edge-on if the camera was
                    // pointing along a weird axis when the user tapped.
                    _bohrModelRoot.transform.rotation = Quaternion.LookRotation(
                        Camera.main.transform.forward, Camera.main.transform.up);
                }

                // Apply the authoritative world scale to BohrModelRoot itself.
                // No-table fallback uses a much larger scale (~0.15m) so the atom is
                // big enough to see when it's just floating in space without a table
                // for size context.
                float scale = hasTable ? _atomWorldScale : Mathf.Max(_atomWorldScale, 0.15f);
                _bohrModelRoot.transform.localScale = Vector3.one * scale;

                // Sync AtomGenerator so its arWorldScale matches, and lock it so
                // subsequent element taps do NOT reset the scale.
                var atomGen = _bohrModelRoot.GetComponent<AtomGenerator>()
                           ?? _bohrModelRoot.GetComponentInChildren<AtomGenerator>(true);
                if (atomGen != null)
                {
                    atomGen.suppressAutoScale = false;   // allow one application
                    atomGen.arWorldScale      = scale;
                    atomGen.ApplyScaleOnly();            // apply now
                    atomGen.suppressAutoScale = true;    // lock so element taps don't reset
                }

                if (hasTable) _bohrHasBeenPositioned = true;

                // Sanity log: confirms the atom and its container are actually visible
                // and at the expected world position. If activeInHierarchy is false, an
                // ancestor (e.g. ARModeManager.HideBohrModel) is hiding it.
                Debug.Log($"[ImmersiveModeManager] Bohr positioned: worldScale={scale:F4} " +
                          $"pos={_bohrModelRoot.transform.position} (hasTable={hasTable})");
                Debug.Log($"[ImmersiveModeManager] Bohr visibility: activeSelf={_bohrModelRoot.activeSelf} " +
                          $"activeInHierarchy={_bohrModelRoot.activeInHierarchy} " +
                          $"localScale={_bohrModelRoot.transform.localScale.x:F3} " +
                          $"camPos={(Camera.main != null ? Camera.main.transform.position.ToString() : "<no cam>")}");
            }
            else
            {
                // Already positioned by user — only apply scale if it hasn't been set.
                // Do NOT move it; let ARWorldObjectManipulator own position.
                var atomGen = _bohrModelRoot.GetComponent<AtomGenerator>()
                           ?? _bohrModelRoot.GetComponentInChildren<AtomGenerator>(true);
                if (atomGen != null && !atomGen.suppressAutoScale)
                {
                    atomGen.suppressAutoScale = false;
                    atomGen.arWorldScale      = _atomWorldScale;
                    atomGen.ApplyScaleOnly();
                    atomGen.suppressAutoScale = true;
                }
                Debug.Log($"[ImmersiveModeManager] Bohr already positioned — scale maintained, position unchanged.");
            }
            anyPlaced = true;
        }

        // ── ElementInfoCard ───────────────────────────────────────────────────────
        if (_elementInfoCard != null && _elementInfoCard.gameObject.activeSelf)
        {
            var cardT = _elementInfoCard.transform;
            bool needsCardReposition = !hasTable || !_infoHasBeenPositioned;

            if (needsCardReposition)
            {
                cardT.position = hasTable
                    ? _tableTransform.TransformPoint(_cardTableOffset)
                    : anchor; // directly in front of camera, not offset right

                if (!hasTable && Camera.main != null)
                {
                    cardT.rotation = Quaternion.LookRotation(
                        Camera.main.transform.forward, Camera.main.transform.up);
                }

                // Larger scale when no table is anchoring it — the InfoCard is text
                // heavy and must be readable on its own.
                float cardScale = hasTable ? _cardWorldScale : Mathf.Max(_cardWorldScale, 0.10f);
                cardT.localScale = Vector3.one * cardScale;

                // Ensure the InfoCard's manipulator has billboard enabled so text
                // always faces the camera.
                var manip = _elementInfoCard.GetComponent<ARWorldObjectManipulator>()
                         ?? _elementInfoCard.GetComponentInChildren<ARWorldObjectManipulator>(true);
                if (manip != null && !manip.billboard)
                {
                    manip.billboard = true;
                    Debug.Log("[ImmersiveModeManager] InfoCard billboard enabled.");
                }

                if (hasTable) _infoHasBeenPositioned = true;
                Debug.Log($"[ImmersiveModeManager] Info positioned: worldScale={cardScale:F4} " +
                          $"pos={cardT.position} (hasTable={hasTable})");
            }
            else
            {
                Debug.Log($"[ImmersiveModeManager] Info already positioned — position unchanged.");
            }
            anyPlaced = true;
        }

        if (anyPlaced)
            _hasLoggedPositioned = true;
    }

    /// <summary>
    /// Called when BohrModelRoot is explicitly closed. Resets the positioning flag
    /// so the next time it is shown it is placed near the table again.
    /// </summary>
    public void NotifyBohrHidden()
    {
        _bohrHasBeenPositioned = false;
        Debug.Log("[ImmersiveModeManager] Bohr hidden — position flag reset.");

        // Allow AtomGenerator to manage its own scale again (Screen Mode behaviour).
        if (_bohrModelRoot != null)
        {
            var atomGen = _bohrModelRoot.GetComponent<AtomGenerator>()
                       ?? _bohrModelRoot.GetComponentInChildren<AtomGenerator>(true);
            if (atomGen != null) atomGen.suppressAutoScale = false;
        }
    }

    /// <summary>
    /// Called when ElementInfoCard is explicitly closed. Resets the positioning flag
    /// so the next time it is shown it is placed near the table again.
    /// </summary>
    public void NotifyInfoHidden()
    {
        _infoHasBeenPositioned = false;
        Debug.Log("[ImmersiveModeManager] Info hidden — position flag reset.");
    }

    // Called by ToggleImmersiveMode (unchanged behaviour path) and internally.
    private void PlaceObjectsInitially() => PlaceArObjectsNow();

    private Vector3 ResolveAnchor(out bool hasTable)
    {
        hasTable = _tableTransform != null;
        if (hasTable) return _tableTransform.position;

        Camera cam = Camera.main;
        if (cam != null)
        {
            // Place 1.5m in front of the camera along its current forward.
            // Distance chosen so the atom (default 15cm world-scale) fills enough of
            // the view to be readable without dominating the screen.
            return cam.transform.position + cam.transform.forward * 1.5f;
        }
        return Vector3.zero;
    }

    // ── Home transform capture ────────────────────────────────────────────────────

    private void TryCaptureAtomHome()
    {
        if (_atomHomeCaptured || _bohrModelRoot == null) return;
        var t            = _bohrModelRoot.transform;
        _atomHomeParent     = t.parent;
        _atomHomeLocalPos   = t.localPosition;
        _atomHomeLocalRot   = t.localRotation;
        _atomHomeLocalScale = t.localScale;
        _atomHomeCaptured   = true;
    }

    private void TryCaptureCardHome()
    {
        if (_cardHomeCaptured || _elementInfoCard == null) return;
        var t            = _elementInfoCard.transform;
        _cardHomeParent     = t.parent;
        _cardHomeLocalPos   = t.localPosition;
        _cardHomeLocalRot   = t.localRotation;
        _cardHomeLocalScale = t.localScale;
        _cardHomeCaptured   = true;
    }

    // ── Home transform restore ────────────────────────────────────────────────────

    private void RestoreAtomHome()
    {
        if (!_atomHomeCaptured || _bohrModelRoot == null) return;
        var t = _bohrModelRoot.transform;
        t.SetParent(_atomHomeParent, worldPositionStays: false);
        t.localPosition = _atomHomeLocalPos;
        t.localRotation = _atomHomeLocalRot;
        t.localScale    = _atomHomeLocalScale;

        // Re-allow AtomGenerator to manage its own scale in Screen Mode.
        var atomGen = _bohrModelRoot.GetComponent<AtomGenerator>()
                   ?? _bohrModelRoot.GetComponentInChildren<AtomGenerator>(true);
        if (atomGen != null) atomGen.suppressAutoScale = false;
    }

    private void RestoreCardHome()
    {
        if (!_cardHomeCaptured || _elementInfoCard == null) return;
        var t = _elementInfoCard.transform;
        t.SetParent(_cardHomeParent, worldPositionStays: false);
        t.localPosition = _cardHomeLocalPos;
        t.localRotation = _cardHomeLocalRot;
        t.localScale    = _cardHomeLocalScale;
    }

    // ── Button label ──────────────────────────────────────────────────────────────

    private void RefreshButtonLabel()
    {
        if (immersiveButtonLabel != null)
            immersiveButtonLabel.text = _immersiveMode ? "Immersive: ON" : "Immersive: OFF";
    }
}
