using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using PeriodicAR.AR;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ElementTileSelector : MonoBehaviour
{
    public ElementLoader elementLoader;
    public ElementInfoCard infoCard;
    public Camera arCamera;

    // Optional — scopes grab-in-progress detection to the spawned table. If unset,
    // falls back to a scene-wide XRGrabInteractable scan.
    public TapToPlace tapToPlace;

    // Tap/drag reconciliation. Each cube now has both tap-to-show-info (this
    // component, scene-wide raycast) and drag-to-reposition (per-cube XRGrabInteractable
    // from Phase 2). They must not double-fire on one gesture: a drag that happens to
    // start on a cube would otherwise also trigger the info card. Rule: fire LoadElement
    // only when the press was short (<=150ms), stationary (<=10px of movement), and no
    // grab became selected anywhere between press and release.
    const float TapMaxDurationSeconds = 0.15f;
    const float TapMaxMovementPixels  = 10f;

    bool    _pressActive;
    float   _pressStartTime;
    Vector2 _pressStartPos;
    Vector2 _pressCurPos;
    bool    _grabStartedDuringPress;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        bool began = false;
        bool ended = false;
        Vector2 pos = _pressCurPos;

        // Sample primary touch (phone).
        if (Touch.activeTouches.Count > 0)
        {
            var t = Touch.activeTouches[0];
            pos = t.screenPosition;
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began) began = true;
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                t.phase == UnityEngine.InputSystem.TouchPhase.Canceled) ended = true;
        }

        // Sample mouse (Editor testing). Input System gives clean one-frame press/release edges.
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                began = true;
                pos = Mouse.current.position.ReadValue();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                ended = true;
                pos = Mouse.current.position.ReadValue();
            }
            else if (Touch.activeTouches.Count == 0)
            {
                pos = Mouse.current.position.ReadValue();
            }
        }

        if (began)
        {
            _pressActive            = true;
            _pressStartTime         = Time.unscaledTime;
            _pressStartPos          = pos;
            _pressCurPos            = pos;
            _grabStartedDuringPress = false;
        }

        if (_pressActive)
        {
            _pressCurPos = pos;
            // Poll grab state: if any grab under the table became isSelected this frame
            // (or any earlier frame of this press), taint the press so TrySelectTile
            // does not fire on release. Equivalent to subscribing to selectEntered,
            // without the subscribe/unsubscribe bookkeeping.
            if (!_grabStartedDuringPress && AnyGrabSelectedUnderTable())
                _grabStartedDuringPress = true;
        }

        if (ended && _pressActive)
        {
            float duration = Time.unscaledTime - _pressStartTime;
            float movement = Vector2.Distance(_pressStartPos, pos);

            bool shortTap = duration <= TapMaxDurationSeconds
                         && movement <= TapMaxMovementPixels
                         && !_grabStartedDuringPress;

            if (shortTap)
                TrySelectTile(_pressStartPos);

            _pressActive = false;
        }
    }

    bool AnyGrabSelectedUnderTable()
    {
        if (tapToPlace != null && tapToPlace.CurrentInstance != null)
        {
            var grabs = tapToPlace.CurrentInstance.GetComponentsInChildren<XRGrabInteractable>(true);
            for (int i = 0; i < grabs.Length; i++)
                if (grabs[i].isSelected) return true;
            return false;
        }

        var all = Object.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
            if (all[i].isSelected) return true;
        return false;
    }

    void TrySelectTile(Vector2 screenPosition)
    {
        if (arCamera == null) { Debug.LogError("AR Camera is null!"); return; }

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            ElementTile tile = hit.collider.GetComponent<ElementTile>();
            if (tile != null)
                elementLoader.LoadElement(tile.atomicNumber);
        }
    }
}
