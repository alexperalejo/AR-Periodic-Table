using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeriodicAR.Tutor
{
    /// <summary>
    /// Inspector plumbing for TutorPanel — holds child-widget refs with no logic.
    /// </summary>
    public class TutorPanelBindings : MonoBehaviour
    {
        [HideInInspector] public GameObject    panelRoot;
        [HideInInspector] public Button        closeButton;
        [HideInInspector] public ScrollRect    chatScroll;
        [HideInInspector] public Transform     messageContainer;   // vertical layout parent
        [HideInInspector] public TMP_InputField inputField;
        [HideInInspector] public Button        sendButton;
    }
}
