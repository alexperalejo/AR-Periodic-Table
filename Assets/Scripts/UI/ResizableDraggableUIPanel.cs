using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PeriodicAR.UI
{
    /// <summary>
    /// Attach to any screen-space UI panel root to get:
    ///   • Dragging  – initiated only from <see cref="dragHandle"/> (e.g. the header bar).
    ///                 If <see cref="dragHandle"/> is null the whole panel is draggable.
    ///   • Resizing  – driven by a child <see cref="PanelResizeHandle"/> in the bottom-right
    ///                 corner (created by the SetupResizablePanels editor tool).
    ///   • Clamping  – panel cannot be dragged/resized completely outside the canvas.
    ///   • Centering – panel resets to centre (anchoredPosition 0,0) whenever it is shown.
    ///
    /// Works with Unity EventSystem and both the legacy Input Manager and the new Input
    /// System — the EventSystem selects the right input module automatically.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ResizableDraggableUIPanel : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ── Inspector ─────────────────────────────────────────────────────────────────

        [Header("Handles")]
        [Tooltip("Drag is only started when the pointer begins on this rect (e.g. header bar). " +
                 "Leave null to make the entire panel surface draggable.")]
        public RectTransform dragHandle;

        [Tooltip("Auto-populated by the setup tool. Reference to the bottom-right resize grip child.")]
        public RectTransform resizeHandle;

        [Header("Size Constraints")]
        public Vector2 minSize = new Vector2(280f, 200f);
        public Vector2 maxSize = new Vector2(1200f, 1600f);

        [Header("Canvas Clamping")]
        [Tooltip("Prevent the panel from being dragged or resized outside the parent canvas.")]
        public bool clampToCanvas = true;

        [Header("Behaviour")]
        [Tooltip("When enabled, reset anchoredPosition to (0,0) so the panel always opens centred.\n" +
                 "Requires the panel's anchors to be set to the canvas centre (0.5 / 0.5).")]
        public bool centerOnEnable = true;

        // ── Private ───────────────────────────────────────────────────────────────────

        RectTransform _rt;
        bool          _dragActive;
        Vector2       _pointerOffset; // anchoredPosition - pointer in parent-local space

        // ── Unity Messages ────────────────────────────────────────────────────────────

        void Awake()
        {
            _rt = (RectTransform)transform;
        }

        void OnEnable()
        {
            if (centerOnEnable)
                _rt.anchoredPosition = Vector2.zero;
        }

        // ── Drag (header area) ────────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData e)
        {
            if (!IsPointerOnHandle(e, dragHandle))
            {
                _dragActive = false;
                return;
            }

            _dragActive = true;
            _rt.SetAsLastSibling(); // bring to front

            var parentRT = _rt.parent as RectTransform;
            if (parentRT == null) return;

            Vector2 localPointer;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRT, e.position, e.pressEventCamera, out localPointer);

            _pointerOffset = _rt.anchoredPosition - localPointer;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_dragActive) return;

            var parentRT = _rt.parent as RectTransform;
            if (parentRT == null) return;

            Vector2 localPointer;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRT, e.position, e.pressEventCamera, out localPointer))
                return;

            Vector2 desired = localPointer + _pointerOffset;
            if (clampToCanvas) desired = ClampPosition(desired);
            _rt.anchoredPosition = desired;
        }

        public void OnEndDrag(PointerEventData e)
        {
            _dragActive = false;
        }

        // ── Resize (called by PanelResizeHandle child) ────────────────────────────────

        /// <param name="parentLocalDelta">Delta in the canvas/parent's local coordinate space.</param>
        internal void ApplyResizeDelta(Vector2 parentLocalDelta)
        {
            // +x drag → wider; dragging down in parent-local (−y) → taller
            Vector2 newSize = _rt.sizeDelta + new Vector2(parentLocalDelta.x, -parentLocalDelta.y);
            newSize.x = Mathf.Clamp(newSize.x, minSize.x, maxSize.x);
            newSize.y = Mathf.Clamp(newSize.y, minSize.y, maxSize.y);
            _rt.sizeDelta = newSize;

            // Tell Unity's layout system to immediately recompute all LayoutGroups and
            // ContentSizeFitters under this panel.  Without this, ScrollRect content and
            // nested VerticalLayoutGroups only reflow on the NEXT LateUpdate, causing a
            // one-frame (or persistent) visual lag where text stays at its old size.
            LayoutRebuilder.MarkLayoutForRebuild(_rt);

            if (clampToCanvas)
                _rt.anchoredPosition = ClampPosition(_rt.anchoredPosition);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        static bool IsPointerOnHandle(PointerEventData e, RectTransform handle)
        {
            if (handle == null) return true; // whole panel is draggable
            return RectTransformUtility.RectangleContainsScreenPoint(
                handle, e.position, e.pressEventCamera);
        }

        Vector2 ClampPosition(Vector2 desired)
        {
            var parentRT = _rt.parent as RectTransform;
            if (parentRT == null) return desired;

            Vector2 size  = _rt.rect.size;
            Vector2 pivot = _rt.pivot;
            Rect    p     = parentRT.rect;

            // Keep at least a sliver of the panel on-screen on every edge
            const float margin = 40f;
            desired.x = Mathf.Clamp(desired.x,
                p.xMin + size.x * pivot.x       - size.x + margin,
                p.xMax - size.x * (1f - pivot.x) + size.x - margin);
            desired.y = Mathf.Clamp(desired.y,
                p.yMin + size.y * pivot.y       - size.y + margin,
                p.yMax - size.y * (1f - pivot.y) + size.y - margin);
            return desired;
        }
    }

}
