using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.ObjectDetection
{
    /// <summary>
    /// Inspector plumbing for DetectionPopup — holds child-widget references with no logic.
    /// Built programmatically by ObjectDetectionPrefabFactory.
    /// </summary>
    public class DetectionPopupBindings : MonoBehaviour
    {
        [HideInInspector] public TMP_Text      titleText;
        [HideInInspector] public TMP_Text      scoreText;
        [HideInInspector] public Transform     primaryContainer;   // horizontal layout for primary chips
        [HideInInspector] public Transform     traceContainer;     // horizontal layout for trace chips
        [HideInInspector] public TMP_Text      noteText;
        [HideInInspector] public Button        highlightButton;
        [HideInInspector] public Button        closeButton;
    }
}
