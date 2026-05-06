// Assets/Scripts/ObjectDetection/DetectionPopupBindings.cs
using TMPro;
using UnityEngine.UI;

namespace PeriodicAR.ObjectDetection
{
    /// <summary>
    /// Wires up the runtime-built DetectionPopup. Kept in a separate class so the
    /// popup script itself can stay declarative and Inspector-friendly when you
    /// hand-author a prefab.
    /// </summary>
    internal static class DetectionPopupBindings
    {
        public static void Bind(DetectionPopup popup,
                                TMP_Text title, TMP_Text body,
                                Button highlight, Button close)
        {
            popup.titleLabel     = title;
            popup.bodyLabel      = body;
            popup.highlightButton = highlight;
            popup.closeButton    = close;
        }
    }
}
