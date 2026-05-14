// Assets/Scripts/UI/ARCloseButton.cs
//
// Lightweight world-space close button for AR visualisation objects.
//
// Design decisions
// ────────────────
// • NOT a child of the AR object it closes.  Children of BohrModelRoot spin with
//   the atom; a child of ElementInfoCard's Canvas complicates layout.  Instead,
//   this GO sits as a sibling and tracks its anchor's position each frame.
// • Never calls SetActive(false) on itself.  The Canvas and GraphicRaycaster are
//   enabled/disabled so Update() keeps running for position tracking.
// • One script covers both BohrModel and ElementInfo targets; pick via Inspector.
// • Works identically in Screen Mode and Immersive AR Mode — in both cases the
//   button is a world-space canvas that billboards toward the camera.
//
// Scene wiring (done by SetupARCloseButtons editor tool)
// ────────────────────────────────────────────────────────
// • Attach to a root GO that has a Canvas (WorldSpace) child hierarchy.
// • Assign _anchor to the BohrModelRoot or ElementInfoCard transform.
// • Pick the appropriate _target enum value.
// • The editor tool wires the Button.onClick to OnClose via persistent listener.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(GraphicRaycaster))]
public class ARCloseButton : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────────

    public enum CloseTarget { BohrModel, ElementInfo }

    [Header("Target")]
    [Tooltip("Which AR object this button closes.")]
    [SerializeField] private CloseTarget _target = CloseTarget.BohrModel;

    [Tooltip("Transform of the AR object to track (BohrModelRoot or ElementInfoCard).")]
    [SerializeField] public Transform anchor;

    [Header("Positioning (camera-relative, in metres)")]
    [Tooltip("Offset right of the anchor, in camera space.  Positive = right side.")]
    [SerializeField] private float _rightOffset  =  0.14f;
    [Tooltip("Offset up from the anchor, in camera space.  Positive = above.")]
    [SerializeField] private float _upOffset     =  0.10f;
    [Tooltip("Small push toward the camera so the button is always in front of the AR object.")]
    [SerializeField] private float _forwardPush  =  0.02f;

    [Header("Canvas Scale")]
    [Tooltip("World-space localScale of the canvas root.  ~0.001 → 1 canvas-unit = 1 mm.")]
    [SerializeField] private float _canvasScale  = 0.001f;

    [Header("References (auto-discovered from children)")]
    [SerializeField] private Button   _button;
    [SerializeField] private TMP_Text _label;

    // ── Private ───────────────────────────────────────────────────────────────────

    private Canvas           _canvas;
    private GraphicRaycaster _raycaster;

    // ── Unity lifecycle ───────────────────────────────────────────────────────────

    private void Awake()
    {
        _canvas    = GetComponent<Canvas>();
        _raycaster = GetComponent<GraphicRaycaster>();

        _canvas.renderMode = RenderMode.WorldSpace;
        transform.localScale = Vector3.one * _canvasScale;
    }

    private void Start()
    {
        // Auto-discover Button and TMP_Text from children if not Inspector-assigned.
        if (_button == null) _button = GetComponentInChildren<Button>(includeInactive: true);
        if (_label  == null) _label  = GetComponentInChildren<TMP_Text>(includeInactive: true);

        if (_button != null)
            _button.onClick.AddListener(OnClose);

        // Set label text according to target.
        if (_label != null)
            _label.text = _target == CloseTarget.BohrModel ? "X  Close Atom" : "X  Close Info";

        // Start hidden — Update() will enable when the mode is active.
        SetVisible(false);
    }

    private void Update()
    {
        bool shouldShow = ShouldBeVisible();
        SetVisible(shouldShow);

        if (!shouldShow) return;

        PositionAndBillboard();
    }

    // ── Public API ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by the Button's onClick.  Exits the current AR mode and shows the
    /// menu, without affecting the periodic table or any overlay panels.
    /// </summary>
    public void OnClose()
    {
        Debug.Log($"[ARCloseButton] '{_target}' close button clicked.");
        if (ARModeManager.Instance != null)
        {
            if (_target == CloseTarget.BohrModel)
                ARModeManager.Instance.HideBohrModel();
            else
                ARModeManager.Instance.HideInfoCard();
            ARModeManager.Instance.ShowMenu();
        }
    }

    // ── Visibility ────────────────────────────────────────────────────────────────

    /// <summary>
    /// True when this button should be visible:
    ///   • ARModeManager is active
    ///   • the corresponding mode is selected
    ///   • the anchor object is active in the hierarchy
    /// </summary>
    private bool ShouldBeVisible()
    {
        if (ARModeManager.Instance == null) return false;
        if (anchor == null || !anchor.gameObject.activeInHierarchy) return false;

        var mode = ARModeManager.Instance.currentMode;
        return _target == CloseTarget.BohrModel
            ? mode == ARModeManager.ARMode.BohrModel
            : mode == ARModeManager.ARMode.ElementInfo;
    }

    private void SetVisible(bool visible)
    {
        if (_canvas    != null) _canvas.enabled    = visible;
        if (_raycaster != null) _raycaster.enabled = visible;
    }

    // ── Positioning ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Places the button at the anchor's world position offset along camera-right
    /// and camera-up, then billboards the canvas to face the camera.
    ///
    /// Using camera-relative axes means "top-right" is always visually top-right
    /// regardless of how the AR object or the AR table is oriented in world space.
    /// </summary>
    private void PositionAndBillboard()
    {
        Camera cam = Camera.main;
        if (cam == null || anchor == null) return;

        Vector3 anchorPos = anchor.position;
        Vector3 camRight   = cam.transform.right;
        Vector3 camUp      = cam.transform.up;
        Vector3 camForward = cam.transform.forward;

        // Place at top-right of the anchor as seen by the camera.
        transform.position =
            anchorPos
            + camRight   * _rightOffset
            + camUp      * _upOffset
            - camForward * _forwardPush; // push slightly toward camera

        // Billboard: +Z away from camera (text/glyph faces the viewer on -Z).
        Vector3 awayFromCam = transform.position - cam.transform.position;
        if (awayFromCam.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(awayFromCam, Vector3.up);
    }
}
