// Assets/Scripts/AR/ARWorldObjectManipulator.cs
//
// Drag-to-move and pinch-to-scale for AR world-space objects (BohrModelRoot,
// ElementInfoCard).  Does NOT affect screen-space overlay panels.
//
// ── Drag ───────────────────────────────────────────────────────────────────────
// One-finger touch (or mouse in the Editor) moves the object along a plane
// perpendicular to the camera that passes through the object's centre.  This
// keeps the object at a constant depth and makes movement feel "flat" from the
// user's perspective regardless of world orientation.
//
// ── Pinch scale ────────────────────────────────────────────────────────────────
// Two-finger pinch scales the object uniformly between minScale and maxScale.
// For BohrModelRoot the scale is written to AtomGenerator.arWorldScale so that
// GenerateAtom() does not overwrite the user's chosen size when a new element is
// selected.  For other objects (e.g. ElementInfoCard) transform.localScale is
// used directly.
//
// ── Billboard ──────────────────────────────────────────────────────────────────
// When billboard = true the object is rotated each frame to face the camera.
// Enable for ElementInfoCard; leave off for BohrModelRoot so the atom can spin.
// Billboard is only applied when ImmersiveModeManager.IsImmersive is true (or
// when no ImmersiveModeManager exists, e.g. in isolated editor tests).
//
// ── Scale HUD ──────────────────────────────────────────────────────────────────
// When showScaleButtons = true a small screen-space overlay with +/- buttons is
// created at runtime.  Useful in the Editor where pinch is not available.  The
// HUD is automatically shown/hidden with the object.
//
// ── TapToPlace guard ───────────────────────────────────────────────────────────
// ARWorldObjectManipulator.AnyDragging is set while a drag is active.
// TapToPlace reads this flag and skips plane-tap processing so the table is
// never accidentally repositioned while the user is moving an AR object.
//
// ── ImmersiveModeManager cooperation ───────────────────────────────────────────
// Drag, pinch and billboard are gated on IsActivelyManipulable (no manager
// present, or manager exists and IsImmersive == true).  In Screen Mode the
// objects stay at their authored home positions; ImmersiveModeManager calls
// PlaceObjectsInitially() once when Immersive Mode turns ON, then this script
// owns position and scale until the mode turns OFF and RestoreHome is called.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[DisallowMultipleComponent]
public class ARWorldObjectManipulator : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────────

    [Header("Drag")]
    [Tooltip("Allow one-finger drag to move this object in world space.")]
    public bool draggable = true;

    [Header("Scale")]
    [Tooltip("Allow two-finger pinch to scale this object.")]
    public bool scalable  = true;

    [Tooltip("Minimum localScale value (or arWorldScale for AtomGenerator objects).")]
    public float minScale = 0.01f;

    [Tooltip("Maximum localScale value (or arWorldScale for AtomGenerator objects).")]
    public float maxScale = 0.20f;

    [Header("Billboard")]
    [Tooltip("Keep the object facing the camera each frame.  Enable for ElementInfoCard; " +
             "leave off for BohrModelRoot so the atom rotation is undisturbed.")]
    public bool billboard = false;

    [Header("Lock")]
    [Tooltip("When locked, drag and pinch are ignored. Toggle via Lock()/Unlock() or the HUD button.")]
    public bool isLocked = false;

    [Header("Always Manipulable")]
    [Tooltip("When true, drag and pinch work regardless of ImmersiveModeManager state. " +
             "Enable on BohrModelRoot and ElementInfoCard so they can be moved without " +
             "toggling Immersive Mode.")]
    public bool alwaysManipulable = true;

    [Header("Scale HUD")]
    [Tooltip("Show a screen-space +/- overlay while the object is active.  " +
             "Useful in the Editor where pinch is not available.")]
    public bool showScaleButtons = true;

    [Tooltip("Multiplicative factor per +/- tap (1.15 = 15% per press).")]
    [Range(1.05f, 1.50f)]
    public float scaleStep = 1.15f;

    [Tooltip("Label shown in the HUD panel.  Leave empty to use the GameObject name.")]
    public string hudLabel = "";

    [Tooltip("Screen corner the HUD snaps to.  (0,0)=bottom-left, (1,0)=bottom-right, " +
             "(0,1)=top-left, (1,1)=top-right.")]
    public Vector2 hudCorner = Vector2.zero;

    [Tooltip("Pixel inset from the chosen corner.")]
    public Vector2 hudMargin = new Vector2(20f, 20f);

    // ── Static drag guard ─────────────────────────────────────────────────────────

    private static int _draggingCount;

    /// <summary>
    /// True while any ARWorldObjectManipulator is actively being dragged.
    /// TapToPlace reads this to skip plane-tap processing during drags.
    /// </summary>
    public static bool AnyDragging => _draggingCount > 0;

    // ── Private state ─────────────────────────────────────────────────────────────

    private bool    _isDragging;
    private Plane   _dragPlane;
    private Vector3 _dragOffset;

    private bool  _isPinching;
    private float _pinchStartDist;
    private float _pinchStartScale;

    private AtomGenerator _atomGen;
    private SphereCollider _dragCollider;   // optional trigger sphere added by setup tool
    private GameObject     _hud;

    // ── Unity lifecycle ───────────────────────────────────────────────────────────

    private void Awake()
    {
        _atomGen      = GetComponent<AtomGenerator>()
                     ?? GetComponentInChildren<AtomGenerator>(includeInactive: true);
        _dragCollider = GetComponent<SphereCollider>();
    }

    private void Start()
    {
        if (showScaleButtons)
            BuildHUD();
    }

    private void OnEnable()
    {
        if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();
        if (_hud != null) _hud.SetActive(true);
    }

    private void OnDisable()
    {
        // Release drag reference count so TapToPlace is never permanently blocked.
        if (_isDragging)
        {
            _isDragging     = false;
            _draggingCount  = Mathf.Max(0, _draggingCount - 1);
        }
        _isPinching = false;
        if (_hud != null) _hud.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_isDragging) _draggingCount = Mathf.Max(0, _draggingCount - 1);
        if (_hud != null) Destroy(_hud);
    }

    private void Update()
    {
        // Enable drag collider only when actively manipulable to reduce false
        // positives in Screen Mode where the atom may be very large (scale ~1.0).
        if (_dragCollider != null)
            _dragCollider.enabled = IsActivelyManipulable;

        bool twoFingers = ETouch.activeTouches.Count >= 2;

        if (twoFingers && scalable && IsActivelyManipulable)
        {
            HandlePinch();
        }
        else
        {
            if (_isPinching) _isPinching = false;
            if (draggable && IsActivelyManipulable) HandleDrag();
            else if (_isDragging)                    EndDrag();   // mode turned off mid-drag
        }

        if (billboard && IsActivelyManipulable)
            ApplyBillboard();

        UpdateHUDVisibility();
    }

    // ── Billboard ─────────────────────────────────────────────────────────────────

    private void ApplyBillboard()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 away = transform.position - cam.transform.position;
        if (away.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(away, Vector3.up);
    }

    // ── Lock API ──────────────────────────────────────────────────────────────────

    /// <summary>Prevent this object from being dragged or pinch-scaled.</summary>
    public void Lock()
    {
        isLocked = true;
        if (_isDragging) EndDrag();
        Debug.Log($"[ARWorldObjectManipulator] '{gameObject.name}' LOCKED — drag/pinch disabled.");
    }

    /// <summary>Re-enable drag and pinch-scale for this object.</summary>
    public void Unlock()
    {
        isLocked = false;
        Debug.Log($"[ARWorldObjectManipulator] '{gameObject.name}' UNLOCKED — drag/pinch enabled.");
    }

    /// <summary>Toggle lock state.</summary>
    public void ToggleLock()
    {
        if (isLocked) Unlock(); else Lock();
    }

    // ── Drag ──────────────────────────────────────────────────────────────────────

    private void HandleDrag()
    {
        GetPointerState(out bool pressed, out bool held, out bool released, out Vector2 pos);

        if (isLocked)
        {
            if (_isDragging) EndDrag();
            if (pressed && IsPointerOverThisObject(pos))
                Debug.Log($"[ARWorldObjectManipulator] '{gameObject.name}' move ignored because locked.");
            return;
        }

        if (pressed && IsPointerOverThisObject(pos))
            BeginDrag(pos);
        else if (released && _isDragging)
            EndDrag();
        else if (held && _isDragging)
            ContinueDrag(pos);
    }

    private void BeginDrag(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Drag plane: perpendicular to the camera, passing through the object centre.
        _dragPlane = new Plane(-cam.transform.forward, transform.position);
        Ray ray    = cam.ScreenPointToRay(screenPos);
        _dragOffset = _dragPlane.Raycast(ray, out float dist)
            ? transform.position - ray.GetPoint(dist)
            : Vector3.zero;

        _isDragging = true;
        _draggingCount++;
        Debug.Log($"[ARWorldObjectManipulator] Drag started on '{gameObject.name}'.");
    }

    private void ContinueDrag(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (_dragPlane.Raycast(ray, out float dist))
            transform.position = ray.GetPoint(dist) + _dragOffset;
    }

    private void EndDrag()
    {
        _isDragging    = false;
        _draggingCount = Mathf.Max(0, _draggingCount - 1);
        Debug.Log($"[ARWorldObjectManipulator] '{gameObject.name}' moved to {transform.position}.");
    }

    // ── Pinch ─────────────────────────────────────────────────────────────────────

    private void HandlePinch()
    {
        if (isLocked) { _isPinching = false; return; }
        var touches = ETouch.activeTouches;
        if (touches.Count < 2) { _isPinching = false; return; }

        Vector2 p0 = touches[0].screenPosition;
        Vector2 p1 = touches[1].screenPosition;

        if (!_isPinching)
        {
            if (!IsPointerOverThisObject(p0) &&
                !IsPointerOverThisObject(p1))
                return;

            _pinchStartDist  = Vector2.Distance(p0, p1);
            _pinchStartScale = GetCurrentScale();
            _isPinching      = true;
            return;
        }

        float curDist = Vector2.Distance(p0, p1);
        if (_pinchStartDist > 0f)
            SetScale(_pinchStartScale * (curDist / _pinchStartDist));
    }

    // ── Scale API ─────────────────────────────────────────────────────────────────

    /// <summary>Increase scale by one step (called by the HUD + button).</summary>
    public void ScaleUp()   => SetScale(GetCurrentScale() * scaleStep);

    /// <summary>Decrease scale by one step (called by the HUD - button).</summary>
    public void ScaleDown() => SetScale(GetCurrentScale() / scaleStep);

    private float GetCurrentScale()
    {
        // AtomGenerator manages its own localScale via arWorldScale — read from there
        // so our multiplier compounds correctly instead of reading a stale localScale.
        if (_atomGen != null && IsActivelyManipulable)
            return _atomGen.arWorldScale;

        return transform.localScale.x;
    }

    private void SetScale(float value)
    {
        float clamped = Mathf.Clamp(value, minScale, maxScale);

        if (_atomGen != null && IsActivelyManipulable)
        {
            // Let AtomGenerator own localScale so GenerateAtom() does not clobber it.
            _atomGen.arWorldScale = clamped;
            _atomGen.ApplyScaleOnly();
        }
        else
        {
            transform.localScale = Vector3.one * clamped;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// True when this manipulator should process input.
    /// Returns true immediately if alwaysManipulable is set (default).
    /// Otherwise requires ImmersiveModeManager.IsImmersive, or passes when no
    /// manager exists (isolated editor tests).
    /// </summary>
    private bool IsActivelyManipulable =>
        alwaysManipulable ||
        ImmersiveModeManager.Instance == null ||
        ImmersiveModeManager.Instance.IsImmersive;

    private static void GetPointerState(out bool pressed, out bool held,
                                        out bool released, out Vector2 screenPos)
    {
        var touches = ETouch.activeTouches;
        if (touches.Count >= 1)
        {
            var t = touches[0];
            screenPos = t.screenPosition;
            pressed   = t.phase == UnityEngine.InputSystem.TouchPhase.Began;
            held      = t.phase == UnityEngine.InputSystem.TouchPhase.Moved
                     || t.phase == UnityEngine.InputSystem.TouchPhase.Stationary;
            released  = t.phase == UnityEngine.InputSystem.TouchPhase.Ended
                     || t.phase == UnityEngine.InputSystem.TouchPhase.Canceled;
            return;
        }

        if (Mouse.current != null)
        {
            pressed   = Mouse.current.leftButton.wasPressedThisFrame;
            held      = Mouse.current.leftButton.isPressed;
            released  = Mouse.current.leftButton.wasReleasedThisFrame;
            screenPos = Mouse.current.position.ReadValue();
            return;
        }

        pressed = held = released = false;
        screenPos = Vector2.zero;
    }

    /// <summary>
    /// Returns true when the screen point hits this GameObject or any of its
    /// children, using both Physics raycasting (3D Colliders) and EventSystem
    /// raycasting (world-space Canvas UI).
    /// </summary>
    private bool IsPointerOverThisObject(Vector2 screenPos)
    {
        // ── Physics (3D objects — requires a Collider, added by setup tool) ───────
        Camera cam = Camera.main;
        if (cam != null)
        {
            var ray  = cam.ScreenPointToRay(screenPos);
            var hits = Physics.RaycastAll(ray, 100f,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    return true;
            }
        }

        // ── EventSystem (world-space Canvas — via GraphicRaycaster) ──────────────
        if (EventSystem.current != null)
        {
            var pd  = new PointerEventData(EventSystem.current) { position = screenPos };
            var res = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pd, res);
            foreach (var r in res)
            {
                if (r.gameObject == gameObject ||
                    r.gameObject.transform.IsChildOf(transform))
                    return true;
            }
        }

        return false;
    }

    // ── Scale HUD ─────────────────────────────────────────────────────────────────

    private void BuildHUD()
    {
        string label = string.IsNullOrEmpty(hudLabel) ? gameObject.name : hudLabel;

        _hud = new GameObject($"[{label}]_ScaleHUD");

        var canvas       = _hud.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 210;   // above close buttons (110) and menu (101)
        _hud.AddComponent<GraphicRaycaster>();

        // ── Panel ─────────────────────────────────────────────────────────────────
        var panel   = new GameObject("Panel");
        panel.layer = LayerMask.NameToLayer("UI");
        panel.transform.SetParent(_hud.transform, false);
        panel.AddComponent<CanvasRenderer>();

        var panelRT         = panel.AddComponent<RectTransform>();
        panelRT.anchorMin   = hudCorner;
        panelRT.anchorMax   = hudCorner;
        panelRT.pivot       = hudCorner;
        panelRT.sizeDelta   = new Vector2(280f, 56f);
        // Push the panel inward from the chosen corner.
        panelRT.anchoredPosition = new Vector2(
            hudCorner.x > 0.5f ? -hudMargin.x :  hudMargin.x,
            hudCorner.y > 0.5f ? -hudMargin.y :  hudMargin.y);

        var bg    = panel.AddComponent<Image>();
        bg.color  = new Color(0.08f, 0.08f, 0.08f, 0.82f);

        // ── Minus button ──────────────────────────────────────────────────────────
        AddHudButton(panel.transform, "-", new Vector2(28f,  28f), ScaleDown);

        // ── Centre label ──────────────────────────────────────────────────────────
        var lGO   = new GameObject("Label");
        lGO.layer = LayerMask.NameToLayer("UI");
        lGO.transform.SetParent(panel.transform, false);
        lGO.AddComponent<CanvasRenderer>();
        var lRT              = lGO.AddComponent<RectTransform>();
        lRT.anchorMin        = Vector2.zero;
        lRT.anchorMax        = Vector2.zero;
        lRT.pivot            = new Vector2(0.5f, 0.5f);
        lRT.sizeDelta        = new Vector2(172f, 48f);
        lRT.anchoredPosition = new Vector2(140f, 28f);
        var lTMP       = lGO.AddComponent<TextMeshProUGUI>();
        lTMP.text      = label;
        lTMP.fontSize  = 18f;
        lTMP.alignment = TextAlignmentOptions.Center;
        lTMP.color     = Color.white;

        // ── Plus button ───────────────────────────────────────────────────────────
        AddHudButton(panel.transform, "+", new Vector2(252f, 28f), ScaleUp);
    }

    private void AddHudButton(Transform parent, string label,
                               Vector2 pos, UnityEngine.Events.UnityAction callback)
    {
        var go   = new GameObject(label == "+" ? "BtnPlus" : "BtnMinus");
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        go.AddComponent<CanvasRenderer>();

        var rt             = go.AddComponent<RectTransform>();
        rt.anchorMin       = Vector2.zero;
        rt.anchorMax       = Vector2.zero;
        rt.pivot           = new Vector2(0.5f, 0.5f);
        rt.sizeDelta       = new Vector2(48f, 48f);
        rt.anchoredPosition = pos;

        var img   = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.22f, 0.95f);

        var btn    = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = new Color(0.22f, 0.22f, 0.22f, 0.95f);
        colors.highlightedColor = new Color(0.30f, 0.60f, 0.95f, 1.00f);
        colors.pressedColor     = new Color(0.10f, 0.35f, 0.70f, 1.00f);
        btn.colors              = colors;
        btn.onClick.AddListener(callback);

        var lGO   = new GameObject("Lbl");
        lGO.layer = LayerMask.NameToLayer("UI");
        lGO.transform.SetParent(go.transform, false);
        lGO.AddComponent<CanvasRenderer>();
        var lRT   = lGO.AddComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = Vector2.zero;
        lRT.offsetMax = Vector2.zero;
        var tmp       = lGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 30f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;
    }

    private void UpdateHUDVisibility()
    {
        if (_hud == null) return;
        bool show = gameObject.activeInHierarchy && showScaleButtons;
        if (_hud.activeSelf != show) _hud.SetActive(show);
    }
}
