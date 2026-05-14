using UnityEngine;
using UnityEngine.EventSystems;

namespace PeriodicAR.UI
{
    /// <summary>
    /// Attach to the "ResizeHandle" child RectTransform (bottom-right corner) of any panel
    /// that has <see cref="ResizableDraggableUIPanel"/>. Dragging this object resizes the
    /// parent panel.
    ///
    /// Pointer deltas are measured in the panel's parent (canvas) local space so the math
    /// is correct across all CanvasScaler modes and canvas scale factors.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class PanelResizeHandle : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        ResizableDraggableUIPanel _panel;
        RectTransform             _parentRT; // the panel's parent (e.g. MenuCanvas)
        Vector2                   _lastLocal;

        void Awake()
        {
            _panel    = GetComponentInParent<ResizableDraggableUIPanel>();
            _parentRT = _panel != null ? _panel.transform.parent as RectTransform : null;

            if (_panel == null)
                Debug.LogWarning("[PanelResizeHandle] No ResizableDraggableUIPanel found in parent chain.", this);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (_parentRT == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentRT, e.position, e.pressEventCamera, out _lastLocal);
        }

        public void OnDrag(PointerEventData e)
        {
            if (_panel == null || _parentRT == null) return;

            Vector2 current;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parentRT, e.position, e.pressEventCamera, out current))
                return;

            _panel.ApplyResizeDelta(current - _lastLocal);
            _lastLocal = current;
        }

        public void OnEndDrag(PointerEventData e) { }
    }
}
