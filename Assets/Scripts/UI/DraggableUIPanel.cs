using UnityEngine;
using UnityEngine.EventSystems;

namespace PeriodicAR.UI
{
    /// <summary>
    /// Generic draggable panel script. Attach this to any UI panel (Image with a RectTransform)
    /// and the user can move it around the canvas with mouse OR touch.
    ///
    /// IBeginDragHandler / IDragHandler / IEndDragHandler all funnel through Unity's
    /// EventSystem, so this works automatically with both the legacy Input Manager AND the
    /// new Input System (provided the EventSystem in the scene uses the matching input
    /// module — Unity auto-creates the right one for the project's input setting).
    ///
    /// The drag is constrained to the parent canvas so the panel can't be dragged off
    /// screen. Set <see cref="dragHandle"/> to the header rect if you only want the user
    /// to be able to drag from a specific area (otherwise the whole panel is draggable).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class DraggableUIPanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("Optional drag handle. If set, the user can only initiate drag by pressing on this rect (e.g. the panel header). If null, the whole panel is the handle.")]
        public RectTransform dragHandle;

        [Tooltip("If true, the panel cannot be dragged outside its parent canvas rect.")]
        public bool clampToCanvas = true;

        [Tooltip("Optional. If set, panel position is clamped inside this rect instead of the canvas root rect.")]
        public RectTransform clampBounds;

        private RectTransform _rt;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Vector2 _pointerOffset; // pointer position - pivot, in canvas local space

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
            {
                _canvasRect = _canvas.rootCanvas != null
                    ? (RectTransform)_canvas.rootCanvas.transform
                    : (RectTransform)_canvas.transform;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsPointerOverHandle(eventData)) return;

            // Convert the pointer position into the panel's parent-local space, then store
            // the offset to the panel's anchored position so the panel doesn't "jump" to the
            // pointer when the user starts dragging.
            RectTransform parentRect = _rt.parent as RectTransform;
            if (parentRect == null) return;

            Vector2 localPointer;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, eventData.pressEventCamera, out localPointer);

            _pointerOffset = _rt.anchoredPosition - localPointer;

            // Bring the dragged panel to the front of its siblings.
            _rt.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransform parentRect = _rt.parent as RectTransform;
            if (parentRect == null) return;

            Vector2 localPointer;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, eventData.pressEventCamera, out localPointer))
                return;

            Vector2 desired = localPointer + _pointerOffset;

            if (clampToCanvas)
                desired = ClampToBounds(desired);

            _rt.anchoredPosition = desired;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Hook for subclasses; intentionally empty.
        }

        private bool IsPointerOverHandle(PointerEventData eventData)
        {
            if (dragHandle == null) return true; // whole panel is draggable

            return RectTransformUtility.RectangleContainsScreenPoint(
                dragHandle, eventData.position, eventData.pressEventCamera);
        }

        private Vector2 ClampToBounds(Vector2 desired)
        {
            RectTransform bounds = clampBounds != null ? clampBounds : _canvasRect;
            if (bounds == null) return desired;

            // Compute the panel's half-size in parent-local space.
            Vector2 size = _rt.rect.size;
            Vector2 pivot = _rt.pivot;

            // Bounds of the parent (anchored coordinates assume parent at origin if anchors collapsed)
            Rect parentRect = ((RectTransform)_rt.parent).rect;

            float minX = parentRect.xMin + size.x * pivot.x;
            float maxX = parentRect.xMax - size.x * (1f - pivot.x);
            float minY = parentRect.yMin + size.y * pivot.y;
            float maxY = parentRect.yMax - size.y * (1f - pivot.y);

            desired.x = Mathf.Clamp(desired.x, minX, maxX);
            desired.y = Mathf.Clamp(desired.y, minY, maxY);
            return desired;
        }
    }
}
